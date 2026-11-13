// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber.Verbs;

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

using CommandLine;

using ktsu.RoundTripStringJsonConverter;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

[Verb("Import", HelpText = "Import descriptions from a JSON or CSV file.")]
internal sealed class Import : BaseVerb<Import>
{
	internal override bool WritesStore => true;

	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		Converters = { new RoundTripStringJsonConverterFactory() },
	};

	[Option('i', "input", Required = false, HelpText = "Input file path (.json or .csv).")]
	public string InputPath { get; set; } = string.Empty;

	internal override bool ValidateArgs()
	{
		if (string.IsNullOrWhiteSpace(InputPath))
		{
			Console.Write("Enter the file path to import: ");
			string? input = ReadPromptPath();
			if (string.IsNullOrEmpty(input))
			{
				Console.WriteLine("No path provided. Aborting.");
				return false;
			}

			InputPath = input;
		}

		return base.ValidateArgs();
	}

	internal override void Run(Import options)
	{
		AbsoluteFilePath? inputFile = TryConvertUserPath(options.InputPath, static p => p.As<AbsoluteFilePath>());
		if (inputFile is null)
		{
			Failed = true;
			return;
		}

		if (!File.Exists(inputFile.WeakString))
		{
			Console.WriteLine($"File not found: {inputFile}");
			Failed = true;
			return;
		}

		List<ImageDescription>? entries = LoadEntries(inputFile, out int unreadableCount);
		if (entries is null)
		{
			Failed = true;
			return;
		}

		// Every data row was unreadable, so the file is not an export this tool can read. Saying
		// "Skipped: 0" and exiting 0 would make it look like a clean no-op to a script.
		if (entries.Count == 0 && unreadableCount > 0)
		{
			Console.WriteLine($"Import failed: none of the {unreadableCount} data row(s) in {inputFile} could be read.");
			Console.WriteLine($"  Skipped:         {unreadableCount}");
			Failed = true;
			return;
		}

		(int newCount, int updatedCount, int skippedCount) = MergeEntries(entries);
		skippedCount += unreadableCount;

		Program.Settings.Save();

		Console.WriteLine($"Import complete.");
		Console.WriteLine($"  New entries:     {newCount}");
		Console.WriteLine($"  Updated paths:   {updatedCount}");
		Console.WriteLine($"  Skipped:         {skippedCount}");
		Console.WriteLine($"  Total in database: {Program.Settings.Descriptions.Count}");
	}

	private static List<ImageDescription>? LoadEntries(AbsoluteFilePath inputFile, out int unreadableCount)
	{
		unreadableCount = 0;
		switch (inputFile.FileExtension.WeakString.ToUpperInvariant())
		{
			case ".JSON":
				return ImportJson(inputFile);
			case ".CSV":
				return ImportCsv(inputFile, out unreadableCount);
			default:
				Console.WriteLine("Error: Input file must have .json or .csv extension.");
				return null;
		}
	}

	internal static (int NewCount, int UpdatedCount, int SkippedCount) MergeEntries(List<ImageDescription> entries)
	{
		int newCount = 0;
		int updatedCount = 0;
		int skippedCount = 0;

		// A JSON null in the exported array deserializes to a null entry rather than a JsonException,
		// so it is skipped here like an entry with no hash instead of ending the import mid-merge.
		foreach (ImageDescription? entry in entries)
		{
			if (entry is null || string.IsNullOrEmpty(entry.Hash))
			{
				skippedCount++;
				continue;
			}

			// Every stored hash is a SHA-256 hex string, and Search and Scan both slice it, so a
			// malformed one would crash them on every later run rather than just this import.
			if (!ImageHasher.IsValidHash(entry.Hash))
			{
				Console.WriteLine($"  Skipping entry with invalid hash \"{entry.Hash}\" ({entry.SuggestedFileName}): expected {ImageHasher.HashLength} hex characters.");
				skippedCount++;
				continue;
			}

			// Scan looks hashes up as ComputeHash spells them, lower-case, and the store is keyed
			// case-sensitively, so an upper-case hash from another tool would never match.
			entry.Hash = entry.Hash.ToLowerInvariant();
			RepairNullFields(entry);

			// No file has a negative size, and Stats totals the stored sizes, so a negative one would
			// show as a negative total on every later run. The description is still worth keeping.
			if (entry.FileSizeBytes < 0)
			{
				Console.WriteLine($"  Entry {entry.Hash} ({entry.SuggestedFileName}) has a negative file size ({entry.FileSizeBytes}); storing 0 instead.");
				entry.FileSizeBytes = 0;
			}

			if (Program.Settings.Descriptions.TryGetValue(entry.Hash, out ImageDescription? existing))
			{
				if (MergeKnownPaths(entry, existing))
				{
					updatedCount++;
				}
				else
				{
					skippedCount++;
				}
			}
			else
			{
				Program.Settings.Descriptions[entry.Hash] = entry;
				newCount++;
			}
		}

		return (newCount, updatedCount, skippedCount);
	}

	/// <summary>
	/// Defaults the fields a JSON import can set to null. Search, Stats, Export and Scan all
	/// dereference them, so a stored null would make each of them throw on every later run.
	/// </summary>
	internal static void RepairNullFields(ImageDescription entry)
	{
		entry.KnownPaths = entry.KnownPaths is null ? [] : [.. entry.KnownPaths.Where(p => p is not null)];
		entry.Description ??= string.Empty;
		entry.SuggestedFileName ??= string.Empty.As<FileName>();
		entry.Model ??= string.Empty.As<OllamaModelName>();
	}

	internal static bool MergeKnownPaths(ImageDescription source, ImageDescription target)
	{
		bool pathsAdded = false;
		foreach (AbsoluteFilePath path in source.KnownPaths)
		{
			if (KnownPathComparison.ContainsSame(target.KnownPaths, path))
			{
				continue;
			}

			target.KnownPaths.Add(path);
			pathsAdded = true;
		}

		return pathsAdded;
	}

	internal static List<ImageDescription>? ImportJson(AbsoluteFilePath inputPath)
	{
		string? json = ReadInput(inputPath);
		if (json is null)
		{
			return null;
		}

		// A truncated or hand-edited file, or one that isn't an export at all, has to end the
		// import before anything is merged or saved, not end the process with a stack trace.
		try
		{
			return JsonSerializer.Deserialize<List<ImageDescription>>(json, JsonOptions) ?? [];
		}
		catch (JsonException ex)
		{
			Console.WriteLine($"Error: {inputPath} is not a valid ImageDescriber JSON export: {ex.Message}");
			return null;
		}
		catch (ArgumentException ex)
		{
			Console.WriteLine($"Error: {inputPath} is not a valid ImageDescriber JSON export: {ex.Message}");
			return null;
		}
	}

	internal static List<ImageDescription>? ImportCsv(AbsoluteFilePath inputPath) => ImportCsv(inputPath, out _);

	private static List<ImageDescription>? ImportCsv(AbsoluteFilePath inputPath, out int unreadableCount)
	{
		string? text = ReadInput(inputPath);
		if (text is null)
		{
			unreadableCount = 0;
			return null;
		}

		return ParseCsv(text, out unreadableCount);
	}

	/// <summary>
	/// Reads the whole input file, or reports why it can't be read and returns <see langword="null"/>.
	/// </summary>
	/// <remarks>
	/// File.Exists says nothing about whether the file can be opened. An export still open in Excel
	/// is locked, and a file the user has no read permission on is refused, and either has to end
	/// the import before anything is merged or saved rather than end the process (or the menu
	/// session) with a stack trace.
	/// </remarks>
	private static string? ReadInput(AbsoluteFilePath inputPath)
	{
		try
		{
			return File.ReadAllText(inputPath.WeakString);
		}
		catch (IOException ex)
		{
			Console.WriteLine($"Error: cannot read {inputPath}: {ex.Message}");
			return null;
		}
		catch (UnauthorizedAccessException ex)
		{
			Console.WriteLine($"Error: cannot read {inputPath}: {ex.Message}");
			return null;
		}
	}

	internal static List<ImageDescription> ParseCsv(string text) => ParseCsv(text, out _);

	/// <summary>
	/// Parses an exported CSV. <paramref name="unreadableCount"/> is the number of data rows
	/// dropped because they were short or held an unparsable field, so the caller can report them.
	/// </summary>
	internal static List<ImageDescription> ParseCsv(string text, out int unreadableCount)
	{
		List<ImageDescription> entries = [];
		unreadableCount = 0;

		// Parse the whole text rather than line by line, because a quoted field may span lines.
		List<(int LineNumber, List<string> Fields, bool Malformed)> records = [.. ParseCsvRecords(text)
			.Where(r => r.Malformed || r.Fields.Count > 1 || !string.IsNullOrWhiteSpace(r.Fields[0]))];

		if (records.Count < 2)
		{
			Console.WriteLine("CSV file is empty or has no data rows.");
			return entries;
		}

		// Skip header row
		foreach ((int lineNumber, List<string> fields, bool malformed) in records.Skip(1))
		{
			try
			{
				// A quote that never closes, or text after a closing quote, means the row's
				// fields can't be told apart, so none of them is trusted.
				if (malformed)
				{
					Console.WriteLine($"  Skipping line {lineNumber}: unbalanced quote.");
					unreadableCount++;
					continue;
				}

				if (fields.Count < 7)
				{
					Console.WriteLine($"  Skipping line {lineNumber}: not enough fields.");
					unreadableCount++;
					continue;
				}

				// Fields: Hash, SuggestedFileName, KnownPaths, Model, DescribedAt, FileSizeBytes, Description
				string hash = fields[0];
				FileName suggestedFileName = fields[1].As<FileName>();
				List<AbsoluteFilePath> knownPaths = [.. SplitKnownPaths(fields[2]).Select(p => p.As<AbsoluteFilePath>())];
				OllamaModelName model = fields[3].As<OllamaModelName>();
				DateTime describedAt = DateTime.Parse(fields[4], CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
				long fileSizeBytes = long.Parse(fields[5], CultureInfo.InvariantCulture);
				string description = fields[6];

				entries.Add(new ImageDescription
				{
					Hash = hash,
					SuggestedFileName = suggestedFileName,
					KnownPaths = knownPaths,
					Model = model,
					DescribedAt = describedAt,
					FileSizeBytes = fileSizeBytes,
					Description = description,
				});
			}
			catch (FormatException ex)
			{
				Console.WriteLine($"  Skipping line {lineNumber}: {ex.Message}");
				unreadableCount++;
			}
			catch (OverflowException ex)
			{
				Console.WriteLine($"  Skipping line {lineNumber}: {ex.Message}");
				unreadableCount++;
			}
			catch (ArgumentException ex)
			{
				Console.WriteLine($"  Skipping line {lineNumber}: {ex.Message}");
				unreadableCount++;
			}
		}

		return entries;
	}

	/// <summary>
	/// Splits a KnownPaths field. Current exports end every path with a line break; exports
	/// written before that joined the paths with "; ", so a field without one is read that way.
	/// </summary>
	internal static string[] SplitKnownPaths(string field) =>
		field.Contains(Export.KnownPathTerminator, StringComparison.Ordinal)
			? [.. field.Split(Export.KnownPathTerminator, StringSplitOptions.RemoveEmptyEntries)
				.Select(p => p.TrimEnd('\r'))
				.Where(p => p.Length > 0)]
			: field.Split("; ", StringSplitOptions.RemoveEmptyEntries);

	internal static List<string> ParseCsvLine(string line) =>
		ParseCsvRecords(line).Select(r => r.Fields).FirstOrDefault() ?? [];

	/// <summary>
	/// Splits CSV text into records per RFC 4180: a quoted field may contain commas, doubled
	/// quotes and line breaks. Each record carries the 1-based line it starts on.
	/// </summary>
	/// <remarks>
	/// A record is <c>Malformed</c> when a quoted field never closes, or when a closing quote is
	/// followed by anything but a comma, a line break or the end of the text. A stray quote
	/// inside a quoted field shows up as the latter; the rest of its line is then skipped so
	/// the parser picks up again at the next line instead of reading every later row as part
	/// of one field. A quote inside an unquoted field is kept as an ordinary character.
	/// </remarks>
	internal static List<(int LineNumber, List<string> Fields, bool Malformed)> ParseCsvRecords(string text)
	{
		List<(int LineNumber, List<string> Fields, bool Malformed)> records = [];
		List<string> fields = [];
		StringBuilder field = new();
		bool fieldStarted = false;
		bool recordHasContent = false;
		bool malformed = false;
		int line = 1;
		int recordLine = 1;

		int i = 0;
		while (i < text.Length)
		{
			char c = text[i++];
			switch (c)
			{
				case '"' when !fieldStarted:
					i = ReadQuotedField(text, i, field, ref line, out bool closed);
					fieldStarted = true;
					recordHasContent = true;
					malformed |= !closed || SkipStrayTextAfterQuote(text, ref i);
					break;
				case ',':
					fields.Add(field.ToString());
					field.Clear();
					fieldStarted = false;
					recordHasContent = true;
					break;
				case '\r':
					break;
				case '\n':
					EndRecord();
					line++;
					recordLine = line;
					break;
				default:
					field.Append(c);
					fieldStarted = true;
					recordHasContent = true;
					break;
			}
		}

		EndRecord();
		return records;

		void EndRecord()
		{
			if (recordHasContent)
			{
				fields.Add(field.ToString());
				records.Add((recordLine, fields, malformed));
				fields = [];
			}

			field.Clear();
			fieldStarted = false;
			recordHasContent = false;
			malformed = false;
		}
	}

	/// <summary>
	/// Reports whether a closing quote at <paramref name="i"/> - 1 is followed by anything but a comma, a line
	/// break or the end of the text. If it is, moves <paramref name="i"/> to the end of the line so parsing picks
	/// up again at the next one.
	/// </summary>
	private static bool SkipStrayTextAfterQuote(string text, ref int i)
	{
		if (i >= text.Length || text[i] is ',' or '\r' or '\n')
		{
			return false;
		}

		while (i < text.Length && text[i] != '\n')
		{
			i++;
		}

		return true;
	}

	/// <summary>
	/// Reads a quoted field's contents into <paramref name="field"/>, starting just after its
	/// opening quote, and returns the index just after its closing quote. <paramref name="closed"/>
	/// is false when the text ends before the field's closing quote.
	/// </summary>
	private static int ReadQuotedField(string text, int start, StringBuilder field, ref int line, out bool closed)
	{
		int i = start;
		while (i < text.Length)
		{
			char c = text[i++];
			if (c != '"')
			{
				line += c == '\n' ? 1 : 0;
				field.Append(c);
			}
			else if (i < text.Length && text[i] == '"')
			{
				field.Append('"');
				i++;
			}
			else
			{
				closed = true;
				return i;
			}
		}

		closed = false;
		return i;
	}
}
