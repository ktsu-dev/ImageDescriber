// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber.Verbs;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using CommandLine;

using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

[Verb("Scan", HelpText = "Scan a directory for images, describe them using Ollama, and store results.")]
internal sealed class Scan : BaseVerb<Scan>
{
	internal override bool WritesStore => true;

	internal override bool ValidateArgs()
	{
		// Only a missing -p prompts. An explicit "-p ." is the current directory, as a script means it
		if (string.IsNullOrWhiteSpace(PathString))
		{
			Console.Write("Enter the path to scan: ");
			string? input = ReadPromptPath();
			if (string.IsNullOrEmpty(input))
			{
				Console.WriteLine("No path provided. Aborting.");
				return false;
			}

			PathString = input;
		}

		// Check the path before the Ollama check and the store lock, so a file or a missing folder
		// fails fast with a message and a non-zero exit instead of a crash or a "Found 0 image(s)"
		AbsoluteDirectoryPath? path = TryGetPath();
		if (path is null)
		{
			return false;
		}

		string? problem = ImageScanner.DescribeUnscannablePath(path);
		if (problem is not null)
		{
			Console.WriteLine(problem);
			return false;
		}

		return base.ValidateArgs();
	}

	/// <summary>
	/// Checks that Ollama answers and has the model, printing why not when it doesn't. The model is
	/// checked before hashing, so a model that was never pulled stops the scan with the fix instead
	/// of failing every image with the same error after the whole library is hashed.
	/// </summary>
	private static bool IsOllamaReady(Scan options)
	{
		Console.WriteLine("Checking Ollama availability...");
		if (!OllamaClient.IsAvailableAsync(options.Endpoint).GetAwaiter().GetResult())
		{
			Console.WriteLine($"Error: Ollama is not available at {options.Endpoint}");
			Console.WriteLine("Make sure Ollama is running and the endpoint is correct.");
			return false;
		}

		if (OllamaClient.IsModelInstalledAsync(options.Endpoint, options.Model).GetAwaiter().GetResult() == false)
		{
			Console.WriteLine($"Error: Model '{options.Model}' is not installed in Ollama at {options.Endpoint}. Run: ollama pull {options.Model}");
			return false;
		}

		return true;
	}

	internal override void Run(Scan options)
	{
		Console.WriteLine($"Scanning: {options.Path}");
		Console.WriteLine($"Endpoint: {options.Endpoint}");
		Console.WriteLine($"Model: {options.Model}");
		Console.WriteLine();

		if (OllamaClient.NormalizeEndpoint(options.Endpoint.WeakString) is null)
		{
			Console.WriteLine($"Error: Invalid Ollama endpoint \"{options.Endpoint}\". Expected an http or https address such as http://localhost:11434.");
			Failed = true;
			return;
		}

		// Step 1: Check Ollama availability
		if (!IsOllamaReady(options))
		{
			Failed = true;
			return;
		}

		Console.WriteLine("Ollama is available.");
		Console.WriteLine();

		// Step 2: Discover image files
		Console.WriteLine("Discovering image files...");
		IReadOnlyList<AbsoluteFilePath> imageFiles = ImageScanner.ScanForImages(options.Path);
		Console.WriteLine($"Found {imageFiles.Count} image(s).");
		Console.WriteLine();

		// Step 3: Hash all files in parallel
		Dictionary<AbsoluteFilePath, string> fileHashes = HashImageFiles(imageFiles, out IReadOnlyList<string> hashFailures);

		// Drop the paths under this root that no longer hold the image they were recorded for, so an
		// edited, moved or deleted file stops turning up under its old description in Search.
		PruneAndReportStaleKnownPaths(options.Path, fileHashes);

		if (imageFiles.Count == 0)
		{
			return;
		}

		// Step 4: Filter out already-described hashes and deduplicate within this scan
		Dictionary<string, ImageDescription> descriptions = Program.Settings.Descriptions;
		Dictionary<string, List<AbsoluteFilePath>> newHashPaths = [];
		int skippedCount = 0;
		int newPathCount = 0;
		int duplicateCount = 0;

		foreach (KeyValuePair<AbsoluteFilePath, string> kvp in fileHashes)
		{
			if (descriptions.TryGetValue(kvp.Value, out ImageDescription? existing))
			{
				skippedCount++;
				if (RecordKnownPath(existing, kvp.Key))
				{
					newPathCount++;
				}
			}
			else if (newHashPaths.TryGetValue(kvp.Value, out List<AbsoluteFilePath>? paths))
			{
				paths.Add(kvp.Key);
				duplicateCount++;
			}
			else
			{
				newHashPaths[kvp.Value] = [kvp.Key];
			}
		}

		if (newPathCount > 0)
		{
			Program.Settings.Save();
			Console.WriteLine($"Discovered {newPathCount} new path(s) for existing images.");
		}

		Console.WriteLine($"Unique new images to describe: {newHashPaths.Count}");
		if (duplicateCount > 0)
		{
			Console.WriteLine($"Found {duplicateCount} duplicate(s) that will share descriptions.");
		}

		if (skippedCount > 0)
		{
			Console.WriteLine($"Skipping {skippedCount} already-described image(s).");
		}

		Console.WriteLine();

		// Step 5: Describe new images with configurable concurrency
		int maxConcurrency = Math.Max(1, Program.Settings.MaxConcurrentRequests);
		Lock saveLock = new();

		Console.WriteLine($"Processing with {maxConcurrency} concurrent request(s)...");
		Console.WriteLine();

		IReadOnlyList<string> failures = DescribeImages(
			newHashPaths,
			options.Endpoint,
			options.Model,
			Program.Settings.DescriptionPrompt,
			Program.Settings.SuggestedFileNamePrompt,
			maxConcurrency,
			entry =>
			{
				lock (saveLock)
				{
					Program.Settings.Descriptions[entry.Hash] = entry;
					Program.Settings.Save();
				}
			});

		// An image that couldn't be hashed never reaches the describe stage, so report it here too.
		// Otherwise a run where files were locked or unreadable ends with no summary and exit code 0.
		PrintFailureSummary("hash", hashFailures);
		PrintFailureSummary("describe", failures);
		Failed = hashFailures.Count > 0 || failures.Count > 0;

		Console.WriteLine();
		Console.WriteLine("Scan complete.");
		Console.WriteLine($"Total descriptions in database: {Program.Settings.Descriptions.Count}");

		PathString = string.Empty;
	}

	/// <summary>
	/// Records a path an already-described image was found at, unless it already has a path naming
	/// the same file.
	/// </summary>
	/// <param name="existing">The stored description of the image.</param>
	/// <param name="path">Where this scan found it.</param>
	/// <returns>True when the path was new and has been added.</returns>
	internal static bool RecordKnownPath(ImageDescription existing, AbsoluteFilePath path)
	{
		if (KnownPathComparison.ContainsSame(existing.KnownPaths, path))
		{
			return false;
		}

		existing.KnownPaths.Add(path);
		return true;
	}

	/// <summary>
	/// Hashes the discovered images, printing progress when there are any to hash.
	/// </summary>
	internal static Dictionary<AbsoluteFilePath, string> HashImageFiles(IReadOnlyList<AbsoluteFilePath> imageFiles, out IReadOnlyList<string> hashFailures)
	{
		if (imageFiles.Count == 0)
		{
			hashFailures = [];
			return [];
		}

		Console.WriteLine("Hashing image files...");
		Dictionary<AbsoluteFilePath, string> fileHashes = ImageHasher.HashFiles(imageFiles, out hashFailures);
		Console.WriteLine();
		return fileHashes;
	}

	/// <summary>
	/// Prunes the stored paths under <paramref name="root"/> that this scan shows are stale, then saves
	/// the store and reports what was removed when anything was.
	/// </summary>
	internal static void PruneAndReportStaleKnownPaths(AbsoluteDirectoryPath root, IReadOnlyDictionary<AbsoluteFilePath, string> fileHashes)
	{
		(int prunedPaths, int orphanedRecords) = PruneStaleKnownPaths(Program.Settings.Descriptions, root, fileHashes);
		if (prunedPaths == 0)
		{
			return;
		}

		Program.Settings.Save();
		Console.WriteLine($"Removed {prunedPaths} path(s) whose file was deleted or changed since it was described.");
		if (orphanedRecords > 0)
		{
			Console.WriteLine($"{orphanedRecords} description(s) no longer match any known file.");
		}

		Console.WriteLine();
	}

	/// <summary>
	/// Removes the known paths under <paramref name="root"/> that no longer hold the image they were
	/// recorded for: the file is gone, or this scan hashed it to a different image.
	/// </summary>
	/// <remarks>
	/// Paths outside <paramref name="root"/> are left alone, so scanning one folder never prunes another.
	/// A path that still exists but that this scan did not hash, such as one behind an unreadable
	/// folder or a directory symlink, or one whose hash failed, is kept, since nothing shows it changed.
	/// A record left with no paths is kept, so its description isn't lost if the file comes back.
	/// </remarks>
	/// <param name="descriptions">The stored descriptions, keyed by hash, whose paths are pruned in place.</param>
	/// <param name="root">The folder that was scanned.</param>
	/// <param name="fileHashes">The hash of every file this scan hashed under <paramref name="root"/>.</param>
	/// <returns>The number of paths removed, and how many records that left with no paths.</returns>
	internal static (int PrunedPaths, int OrphanedRecords) PruneStaleKnownPaths(
		Dictionary<string, ImageDescription> descriptions,
		AbsoluteDirectoryPath root,
		IReadOnlyDictionary<AbsoluteFilePath, string> fileHashes)
	{
		StringComparison comparison = KnownPathComparison.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		Dictionary<string, string> hashByPath = new(KnownPathComparison.IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
		foreach (KeyValuePair<AbsoluteFilePath, string> kvp in fileHashes)
		{
			hashByPath[kvp.Key.WeakString] = kvp.Value;
		}

		string rootPrefix = System.IO.Path.TrimEndingDirectorySeparator(root.WeakString) + System.IO.Path.DirectorySeparatorChar;
		int prunedPaths = 0;
		int orphanedRecords = 0;
		foreach (ImageDescription description in descriptions.Values)
		{
			int removed = description.KnownPaths.RemoveAll(path =>
				IsUnder(path.WeakString) &&
				(hashByPath.TryGetValue(path.WeakString, out string? hash)
					? !string.Equals(hash, description.Hash, StringComparison.OrdinalIgnoreCase)
					: !File.Exists(path.WeakString)));
			prunedPaths += removed;
			if (removed > 0 && description.KnownPaths.Count == 0)
			{
				orphanedRecords++;
			}
		}

		return (prunedPaths, orphanedRecords);

		bool IsUnder(string path) =>
			path.StartsWith(rootPrefix, comparison) ||
			(System.IO.Path.AltDirectorySeparatorChar != System.IO.Path.DirectorySeparatorChar &&
				path.Replace(System.IO.Path.AltDirectorySeparatorChar, System.IO.Path.DirectorySeparatorChar).StartsWith(rootPrefix, comparison));
	}

	private static void PrintFailureSummary(string stage, IReadOnlyList<string> failures)
	{
		if (failures.Count == 0)
		{
			return;
		}

		Console.WriteLine();
		Console.WriteLine($"Failed to {stage} {failures.Count} image(s):");
		foreach (string failure in failures)
		{
			Console.WriteLine($"  {failure}");
		}
	}

	/// <summary>
	/// Describes each new image and hands every finished entry to <paramref name="store"/>.
	/// A failure on one image is logged and returned rather than thrown, so one bad file or one
	/// bad or timed-out model response does not abort a run that may have been going for hours.
	/// </summary>
	/// <returns>One line per image that could not be described.</returns>
	internal static IReadOnlyList<string> DescribeImages(
		Dictionary<string, List<AbsoluteFilePath>> newHashPaths,
		OllamaEndpoint endpoint,
		OllamaModelName model,
		string descriptionPrompt,
		string fileNamePrompt,
		int maxConcurrency,
		Action<ImageDescription> store)
	{
		int current = 0;
		int total = newHashPaths.Count;
		Lock consoleLock = new();
		ConcurrentQueue<string> failures = new();

		ParallelOptions parallelOptions = new() { MaxDegreeOfParallelism = maxConcurrency };

		Parallel.ForEach(newHashPaths, parallelOptions, kvp =>
		{
			(string hash, List<AbsoluteFilePath> paths) = (kvp.Key, kvp.Value);
			AbsoluteFilePath filePath = paths[0];
			int index = Interlocked.Increment(ref current);

			lock (consoleLock)
			{
				Console.WriteLine($"[{index}/{total}] Describing {filePath.FileName} ({paths.Count} copy/copies)...");
			}

			try
			{
				string pathContext = string.Join("\n", paths.Select(p => p.WeakString));
				string fullPrompt = $"Known file paths for this image:\n{pathContext}\n\n{descriptionPrompt}";
				string description = OllamaClient.DescribeImageAsync(endpoint, model, fullPrompt, filePath).GetAwaiter().GetResult();

				string combinedFileNamePrompt = $"Image description: {description}\n\n{fileNamePrompt}";
				string rawSuggestion = OllamaClient.GenerateAsync(endpoint, model, combinedFileNamePrompt).GetAwaiter().GetResult();
				FileName suggestedFileName = SanitizeFileName(rawSuggestion, filePath.FileExtension);

				ImageDescription entry = new()
				{
					Hash = hash,
					KnownPaths = [.. paths],
					Description = description,
					SuggestedFileName = suggestedFileName,
					Model = model,
					DescribedAt = DateTime.UtcNow,
					FileSizeBytes = new FileInfo(filePath.WeakString).Length,
				};

				store(entry);

				lock (consoleLock)
				{
					Console.WriteLine($"  [{index}/{total}] Suggested: {suggestedFileName}");
					Console.WriteLine($"  [{index}/{total}] Done: {description[..Math.Min(80, description.Length)]}...");
				}
			}
			catch (Exception ex) when (ex is HttpRequestException or JsonException or OperationCanceledException or IOException or UnauthorizedAccessException or InvalidDataException)
			{
				// OperationCanceledException covers the TaskCanceledException HttpClient throws on timeout.
				string failure = $"{filePath}: {ex.GetType().Name}: {ex.Message}";
				failures.Enqueue(failure);
				lock (consoleLock)
				{
					Console.WriteLine($"  [{index}/{total}] Error describing {filePath.FileName}: {ex.Message}");
				}
			}
		});

		return [.. failures];
	}

	internal static FileName SanitizeFileName(string rawSuggestion, FileExtension extension)
	{
		string name = SelectNameLine(rawSuggestion);
		// Remove characters Windows rejects. Path.GetInvalidFileNameChars() is only "\0" and "/" on
		// Linux and macOS, and a suggestion is stored once per hash and meant for renaming, so a name
		// suggested there has to be legal on Windows, NTFS, SMB and exFAT as well. This runs before the
		// extension check, so "before/after-comparison.jpg" keeps "before" on every OS instead of being
		// read as a directory, and "sunset-beach.jpg?" has its "?" turned into something trimmable
		name = string.Concat(name.Select(c => IsPortableFileNameChar(c) ? c : '-'));

		// Models often wrap the answer in quotes, backticks or markdown emphasis, or end it with a full
		// stop, in any nesting ("`sunset-beach.png`.", "**sunset-beach.jpg**"). Peel all of it off
		// before the extension check, so the extension is the last thing left and "sunset." does not
		// become "sunset..jpg"
		name = TrimDecoration(name);

		// Strip an image extension the model may have included. A suggestion normally has no
		// extension, so any other dot is part of the name ("st.-louis-arch", "sunset-at-5.30pm").
		// Sliced off by hand rather than with Path.GetFileNameWithoutExtension, which would also drop
		// anything before a "\" on Windows. TIFF counts too: the scan skips it, but a model that says
		// ".tiff" still means it as the extension, not as part of the name
		int extensionStart = name.LastIndexOf('.');
		if (extensionStart >= 0 && ImageScanner.IsKnownImageExtension(name[extensionStart..]))
		{
			name = name[..extensionStart];
		}

		// Collapse multiple hyphens and trim
		while (name.Contains("--", StringComparison.Ordinal))
		{
			name = name.Replace("--", "-", StringComparison.Ordinal);
		}

		// Trim the decoration again now the extension is gone: a stem wrapped on its own, with the
		// extension outside the wrapping ("`sunset-beach`.jpg", "'sunset-beach'.jpg"), still ends in its
		// closing mark here, which the trim before the extension check could not reach
		name = CapLength(TrimDecoration(name));

		if (string.IsNullOrEmpty(name))
		{
			name = "unnamed";
		}

		// Windows refuses a device name as a file name whatever follows it after a dot ("con.jpg",
		// "aux.cable.jpg", "nul.tar.gz"), and ignores trailing spaces before that dot, so the check is
		// on the part before the first dot rather than on the whole stem
		int firstDot = name.IndexOf('.', StringComparison.Ordinal);
		string device = (firstDot < 0 ? name : name[..firstDot]).TrimEnd(' ');
		if (WindowsReservedNames.Contains(device))
		{
			name = name.Insert(device.Length, "-image");
		}

		return $"{name}{extension}".As<FileName>();
	}

	// Wrapping a model puts around a name rather than part of it: quotes (ASCII and typographic),
	// backticks, markdown emphasis, a closing full stop, comma or semicolon, and the hyphens the
	// non-portable characters among them became
	private static readonly char[] NameDecoration =
	[
		'"', '\'', '`', '*', '_', '-', '.', ',', ';', ' ',
		'\u201C', '\u201D', '\u2018', '\u2019', '\u201E', '\u201A', '\u00AB', '\u00BB', '\u2039', '\u203A',
	];

	// Brackets a model may put around the whole name: "(sunset-beach.jpg)", "「sunset-beach」". Unlike
	// quotes they are only removed as a pair, so "photo-(1)" keeps its closing bracket
	private static readonly (char Open, char Close)[] NameBrackets =
	[
		('(', ')'), ('[', ']'), ('{', '}'),
		('\uFF08', '\uFF09'), ('\u300C', '\u300D'), ('\u300E', '\u300F'), ('\u3010', '\u3011'), ('\u3008', '\u3009'), ('\u300A', '\u300B'),
	];

	// Peels the decoration and enclosing brackets off a name, in any nesting: "“(sunset-beach.jpg)”."
	private static string TrimDecoration(string name)
	{
		string previous;
		do
		{
			previous = name;
			name = name.Trim(NameDecoration);
			foreach ((char open, char close) in NameBrackets)
			{
				if (name.Length >= 2 && name[0] == open && EnclosesWhole(name, open, close))
				{
					name = name[1..^1];
					break;
				}
			}
		}
		while (name != previous);

		return name;
	}

	// True when the bracket opening the name is the one that closes it, so "(a)-(b)" is not unwrapped
	private static bool EnclosesWhole(string name, char open, char close)
	{
		int depth = 0;
		for (int i = 0; i < name.Length; i++)
		{
			if (name[i] == open)
			{
				depth++;
			}
			else if (name[i] == close && --depth == 0)
			{
				return i == name.Length - 1;
			}
		}

		return false;
	}

	// NTFS, ext4 and APFS allow 255 bytes or UTF-16 units per name. Capping the stem well below that,
	// in UTF-8 bytes, keeps a multibyte name legal everywhere once the extension is added
	internal const int MaxStemBytes = 150;

	// A chatty model can answer with a whole sentence on one line. Keep whole characters up to the
	// cap, then cut back to the last hyphen or space so the name doesn't end mid-word
	private static string CapLength(string name)
	{
		if (Encoding.UTF8.GetByteCount(name) <= MaxStemBytes)
		{
			return name;
		}

		StringBuilder kept = new();
		int keptBytes = 0;
		TextElementEnumerator elements = StringInfo.GetTextElementEnumerator(name);
		while (elements.MoveNext())
		{
			string element = elements.GetTextElement();
			int elementBytes = Encoding.UTF8.GetByteCount(element);
			if (keptBytes + elementBytes > MaxStemBytes)
			{
				break;
			}

			kept.Append(element);
			keptBytes += elementBytes;
		}

		string capped = kept.ToString();
		bool cutAtWordBoundary = name[capped.Length] is '-' or ' ';
		int lastBoundary = capped.LastIndexOfAny(['-', ' ']);
		if (!cutAtWordBoundary && lastBoundary > 0)
		{
			capped = capped[..lastBoundary];
		}

		return capped.Trim('-', ' ', '.');
	}

	private static readonly HashSet<string> WindowsReservedNames = new(
		[
			"CON", "PRN", "AUX", "NUL",
			"COM0", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
			"LPT0", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
			// Current Windows also reserves the superscript-digit forms
			"COM\u00B9", "COM\u00B2", "COM\u00B3",
			"LPT\u00B9", "LPT\u00B2", "LPT\u00B3",
		],
		StringComparer.OrdinalIgnoreCase);

	private static bool IsPortableFileNameChar(char c) =>
		c is >= ' ' and not ('<' or '>' or ':' or '"' or '/' or '\\' or '|' or '?' or '*');

	// An opening or closing fence ("```" or "```text"), as opposed to a name written inline
	// between two fences on one line ("```sunset```")
	/// <summary>
	/// Picks the line of a model's answer that holds the name, and strips the markdown and the label
	/// around it. Chat-tuned models often write a lead-in line such as "Here is a suggested filename:"
	/// and put the name on the next line, or answer "**Filename:** `sunset-beach`" on one line.
	/// </summary>
	internal static string SelectNameLine(string rawSuggestion)
	{
		// Take the first line with content, so a name wrapped in a code fence or preceded by a
		// blank line is kept, and only then trim the quotes and backticks around it
		string[] lines = [.. rawSuggestion
			.Split('\n')
			.Select(line => line.Trim())
			.Where(line => line.Length > 0 && !IsCodeFence(line))];

		// A lead-in ends with a colon, or is a sentence ("Here is a suggested filename.", "Sure!"),
		// and a name is neither, so skip it when something follows it. A lone line is kept as is
		string name = lines.FirstOrDefault(line => !IsLeadIn(line) && !IsSentence(line))
			?? lines.FirstOrDefault(line => !IsLeadIn(line))
			?? lines.FirstOrDefault()
			?? string.Empty;

		name = MarkdownLineMarker.Replace(name, string.Empty);
		return NameLabel.Replace(name, string.Empty);
	}

	private static bool IsLeadIn(string line) => line.TrimEnd('*', '_', ' ').EndsWith(':');

	// An intro a model writes before the name: words ending in a full stop, or anything ending in an
	// exclamation mark ("Sure!"). A single token ending in a full stop is still taken as a name
	private static bool IsSentence(string line)
	{
		string trimmed = line.TrimEnd('*', '_', ' ');
		return trimmed.EndsWith('!')
			|| (trimmed.EndsWith('.') && trimmed.Contains(' ', StringComparison.Ordinal));
	}

	// A heading, bullet or numbered-list marker in front of the name: "# ", "- ", "* ", "1. "
	private static readonly Regex MarkdownLineMarker = new(@"^(?:#{1,6}|[-*+]|\d+[.)])\s+", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

	// A label in front of the name on the same line: "Filename: ", "**Suggested filename:** ",
	// "Filename (no extension): ", "Option 1: ", or a lead-in written as prose on the name's line
	// ("Sure! Here's a suggested filename: ", "Based on the description, a suitable filename would be: ").
	// Starts with a letter, holds only words, digits, spaces and simple punctuation, up to 80
	// characters, and must be followed by a space, so a time such as "sunset-at-5:30pm" is not a label.
	// A "." or "!" counts only where it ends a sentence, before a space, so "st.-louis-arch: day 2"
	// keeps its name
	private static readonly Regex NameLabel = new(@"^[*_]*\p{L}(?:[\p{L}\p{Nd} '\u2019(),\-]|[.!](?=\s)){0,79}[*_]*:[*_]*\s+(?=\S)", RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));

	private static bool IsCodeFence(string line) =>
		line.StartsWith("```", StringComparison.Ordinal)
		&& (line.Length < 6 || !line.EndsWith("```", StringComparison.Ordinal));
}
