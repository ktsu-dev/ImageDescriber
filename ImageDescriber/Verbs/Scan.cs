// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImageDescriber.Verbs;

using System.Collections.Generic;
using System.IO;

using CommandLine;

using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

[Verb("Scan", HelpText = "Scan a directory for images, describe them using Ollama, and store results.")]
internal sealed class Scan : BaseVerb<Scan>
{
	internal override void Run(Scan options)
	{
		Console.WriteLine($"Scanning: {options.Path}");
		Console.WriteLine($"Endpoint: {options.Endpoint}");
		Console.WriteLine($"Model: {options.Model}");
		Console.WriteLine();

		// Step 1: Check Ollama availability
		Console.WriteLine("Checking Ollama availability...");
		bool isAvailable = OllamaClient.IsAvailableAsync(options.Endpoint).GetAwaiter().GetResult();
		if (!isAvailable)
		{
			Console.WriteLine($"Error: Ollama is not available at {options.Endpoint}");
			Console.WriteLine("Make sure Ollama is running and the endpoint is correct.");
			return;
		}

		Console.WriteLine("Ollama is available.");
		Console.WriteLine();

		// Step 2: Discover image files
		Console.WriteLine("Discovering image files...");
		IReadOnlyList<AbsoluteFilePath> imageFiles = ImageScanner.ScanForImages(options.Path);
		Console.WriteLine($"Found {imageFiles.Count} image(s).");
		Console.WriteLine();

		if (imageFiles.Count == 0)
		{
			return;
		}

		// Step 3: Hash all files in parallel
		Console.WriteLine("Hashing image files...");
		Dictionary<AbsoluteFilePath, string> fileHashes = ImageHasher.HashFiles(imageFiles);
		Console.WriteLine();

		// Step 4: Filter out already-described hashes
		Dictionary<string, ImageDescription> descriptions = Program.Settings.Descriptions;
		List<KeyValuePair<AbsoluteFilePath, string>> newFiles = [];
		int skippedCount = 0;

		foreach (KeyValuePair<AbsoluteFilePath, string> kvp in fileHashes)
		{
			if (descriptions.ContainsKey(kvp.Value))
			{
				skippedCount++;
			}
			else
			{
				newFiles.Add(kvp);
			}
		}

		Console.WriteLine($"New images to describe: {newFiles.Count}");
		if (skippedCount > 0)
		{
			Console.WriteLine($"Skipping {skippedCount} already-described image(s).");
		}

		Console.WriteLine();

		// Step 5: Describe each new image sequentially (crash-safe with save after each)
		string descriptionPrompt = Program.Settings.DescriptionPrompt;
		string fileNamePrompt = Program.Settings.SuggestedFileNamePrompt;
		int current = 0;
		int total = newFiles.Count;

		foreach (KeyValuePair<AbsoluteFilePath, string> kvp in newFiles)
		{
			(AbsoluteFilePath filePath, string hash) = (kvp.Key, kvp.Value);
			current++;
			FileName fileName = filePath.FileName;
			Console.WriteLine($"[{current}/{total}] Describing {fileName}...");

			try
			{
				string description = OllamaClient.DescribeImageAsync(options.Endpoint, options.Model, descriptionPrompt, filePath).GetAwaiter().GetResult();

				string combinedFileNamePrompt = $"Image description: {description}\n\n{fileNamePrompt}";
				string rawSuggestion = OllamaClient.GenerateAsync(options.Endpoint, options.Model, combinedFileNamePrompt).GetAwaiter().GetResult();
				FileName suggestedFileName = SanitizeFileName(rawSuggestion, filePath.FileExtension);

				ImageDescription entry = new()
				{
					Hash = hash,
					FilePath = filePath,
					FileName = fileName,
					Description = description,
					SuggestedFileName = suggestedFileName,
					Model = options.Model,
					DescribedAt = DateTime.UtcNow,
					FileSizeBytes = new FileInfo(filePath.WeakString).Length,
				};

				Program.Settings.Descriptions[hash] = entry;
				Program.Settings.Save();

				Console.WriteLine($"  Suggested: {suggestedFileName}");
				Console.WriteLine($"  Done: {description[..Math.Min(80, description.Length)]}...");
			}
			catch (HttpRequestException ex)
			{
				Console.WriteLine($"  Error describing {fileName}: {ex.Message}");
			}

			Console.WriteLine();
		}

		Console.WriteLine("Scan complete.");
		Console.WriteLine($"Total descriptions in database: {Program.Settings.Descriptions.Count}");
	}

	private static FileName SanitizeFileName(string rawSuggestion, FileExtension extension)
	{
		string name = rawSuggestion.Trim().Trim('"', '\'', '`');

		// Take only the first line if the model returned multiple lines
		int newlineIndex = name.IndexOf('\n', StringComparison.Ordinal);
		if (newlineIndex >= 0)
		{
			name = name[..newlineIndex].Trim();
		}

		// Strip any extension the model may have included
		string existingExt = System.IO.Path.GetExtension(name);
		if (!string.IsNullOrEmpty(existingExt))
		{
			name = System.IO.Path.GetFileNameWithoutExtension(name);
		}

		// Remove invalid filename characters
		foreach (char c in System.IO.Path.GetInvalidFileNameChars())
		{
			name = name.Replace(c, '-');
		}

		// Collapse multiple hyphens and trim
		while (name.Contains("--", StringComparison.Ordinal))
		{
			name = name.Replace("--", "-", StringComparison.Ordinal);
		}

		name = name.Trim('-', ' ');

		if (string.IsNullOrEmpty(name))
		{
			name = "unnamed";
		}

		return $"{name}{extension}".As<FileName>();
	}
}
