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
			string? input = Console.ReadLine()?.Trim();
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
		AbsoluteFilePath inputFile = System.IO.Path.GetFullPath(options.InputPath).As<AbsoluteFilePath>();

		if (!File.Exists(inputFile.WeakString))
		{
			Console.WriteLine($"File not found: {inputFile}");
			return;
		}

		List<ImageDescription>? entries = LoadEntries(inputFile);
		if (entries is null)
		{
			return;
		}

		(int newCount, int updatedCount, int skippedCount) = MergeEntries(entries);

		Program.Settings.Save();

		Console.WriteLine($"Import complete.");
		Console.WriteLine($"  New entries:     {newCount}");
		Console.WriteLine($"  Updated paths:   {updatedCount}");
		Console.WriteLine($"  Skipped:         {skippedCount}");
		Console.WriteLine($"  Total in database: {Program.Settings.Descriptions.Count}");
	}

	private static List<ImageDescription>? LoadEntries(AbsoluteFilePath inputFile)
	{
		switch (inputFile.FileExtension.WeakString.ToUpperInvariant())
		{
			case ".JSON":
				return ImportJson(inputFile);
			case ".CSV":
				return ImportCsv(inputFile);
			default:
				Console.WriteLine("Error: Input file must have .json or .csv extension.");
				return null;
		}
	}

	private static (int NewCount, int UpdatedCount, int SkippedCount) MergeEntries(List<ImageDescription> entries)
	{
		int newCount = 0;
		int updatedCount = 0;
		int skippedCount = 0;

		foreach (ImageDescription entry in entries)
		{
			if (string.IsNullOrEmpty(entry.Hash))
			{
				skippedCount++;
				continue;
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

	internal static bool MergeKnownPaths(ImageDescription source, ImageDescription target)
	{
		bool pathsAdded = false;
		foreach (AbsoluteFilePath path in source.KnownPaths.Where(p => !target.KnownPaths.Contains(p)))
		{
			target.KnownPaths.Add(path);
			pathsAdded = true;
		}

		return pathsAdded;
	}

	private static List<ImageDescription> ImportJson(AbsoluteFilePath inputPath)
	{
		string json = File.ReadAllText(inputPath.WeakString);
		return JsonSerializer.Deserialize<List<ImageDescription>>(json, JsonOptions) ?? [];
	}

	private static List<ImageDescription> ImportCsv(AbsoluteFilePath inputPath) =>
		ParseCsv(File.ReadAllText(inputPath.WeakString));

	internal static List<ImageDescription> ParseCsv(string text)
	{
		List<ImageDescription> entries = [];

		// Parse the whole text rather than line by line, because a quoted field may span lines.
		List<(int LineNumber, List<string> Fields)> records = [.. ParseCsvRecords(text)
			.Where(r => r.Fields.Count > 1 || !string.IsNullOrWhiteSpace(r.Fields[0]))];

		if (records.Count < 2)
		{
			Console.WriteLine("CSV file is empty or has no data rows.");
			return entries;
		}

		// Skip header row
		foreach ((int lineNumber, List<string> fields) in records.Skip(1))
		{
			try
			{
				if (fields.Count < 7)
				{
					Console.WriteLine($"  Skipping line {lineNumber}: not enough fields.");
					continue;
				}

				// Fields: Hash, SuggestedFileName, KnownPaths, Model, DescribedAt, FileSizeBytes, Description
				string hash = fields[0];
				FileName suggestedFileName = fields[1].As<FileName>();
				List<AbsoluteFilePath> knownPaths = [.. fields[2]
					.Split("; ", StringSplitOptions.RemoveEmptyEntries)
					.Select(p => p.As<AbsoluteFilePath>())];
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
			}
			catch (OverflowException ex)
			{
				Console.WriteLine($"  Skipping line {lineNumber}: {ex.Message}");
			}
			catch (ArgumentException ex)
			{
				Console.WriteLine($"  Skipping line {lineNumber}: {ex.Message}");
			}
		}

		return entries;
	}

	internal static List<string> ParseCsvLine(string line) =>
		ParseCsvRecords(line).Select(r => r.Fields).FirstOrDefault() ?? [];

	/// <summary>
	/// Splits CSV text into records per RFC 4180: a quoted field may contain commas, doubled
	/// quotes and line breaks. Each record carries the 1-based line it starts on.
	/// </summary>
	internal static List<(int LineNumber, List<string> Fields)> ParseCsvRecords(string text)
	{
		List<(int LineNumber, List<string> Fields)> records = [];
		List<string> fields = [];
		StringBuilder field = new();
		bool inQuotes = false;
		bool recordHasContent = false;
		int line = 1;
		int recordLine = 1;

		for (int i = 0; i < text.Length; i++)
		{
			char c = text[i];
			if (inQuotes)
			{
				if (c != '"')
				{
					line += c == '\n' ? 1 : 0;
					field.Append(c);
				}
				else if (i + 1 < text.Length && text[i + 1] == '"')
				{
					field.Append('"');
					i++;
				}
				else
				{
					inQuotes = false;
				}

				continue;
			}

			switch (c)
			{
				case '"':
					inQuotes = true;
					recordHasContent = true;
					break;
				case ',':
					fields.Add(field.ToString());
					field.Clear();
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
				records.Add((recordLine, fields));
				fields = [];
			}

			field.Clear();
			recordHasContent = false;
		}
	}
}
