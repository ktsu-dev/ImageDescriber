// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

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

	private static List<ImageDescription> ImportCsv(AbsoluteFilePath inputPath)
	{
		List<ImageDescription> entries = [];
		string[] lines = File.ReadAllLines(inputPath.WeakString);

		if (lines.Length < 2)
		{
			Console.WriteLine("CSV file is empty or has no data rows.");
			return entries;
		}

		// Skip header row
		for (int i = 1; i < lines.Length; i++)
		{
			string line = lines[i];
			if (string.IsNullOrWhiteSpace(line))
			{
				continue;
			}

			try
			{
				List<string> fields = ParseCsvLine(line);
				if (fields.Count < 7)
				{
					Console.WriteLine($"  Skipping line {i + 1}: not enough fields.");
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
				Console.WriteLine($"  Skipping line {i + 1}: {ex.Message}");
			}
			catch (OverflowException ex)
			{
				Console.WriteLine($"  Skipping line {i + 1}: {ex.Message}");
			}
			catch (ArgumentException ex)
			{
				Console.WriteLine($"  Skipping line {i + 1}: {ex.Message}");
			}
		}

		return entries;
	}

	internal static List<string> ParseCsvLine(string line)
	{
		List<string> fields = [];
		int i = 0;

		while (i < line.Length)
		{
			if (line[i] == '"')
			{
				fields.Add(ParseQuotedField(line, ref i));
			}
			else
			{
				fields.Add(ParseUnquotedField(line, ref i));
			}
		}

		return fields;
	}

	private static string ParseQuotedField(string line, ref int i)
	{
		i++; // skip opening quote
		StringBuilder field = new();

		while (i < line.Length)
		{
			if (line[i] == '"')
			{
				if (i + 1 < line.Length && line[i + 1] == '"')
				{
					field.Append('"');
					i += 2;
				}
				else
				{
					i++; // closing quote
					break;
				}
			}
			else
			{
				field.Append(line[i]);
				i++;
			}
		}

		// Skip comma after quoted field
		if (i < line.Length && line[i] == ',')
		{
			i++;
		}

		return field.ToString();
	}

	private static string ParseUnquotedField(string line, ref int i)
	{
		int commaIndex = line.IndexOf(',', i);
		if (commaIndex < 0)
		{
			string result = line[i..];
			i = line.Length;
			return result;
		}

		string field = line[i..commaIndex];
		i = commaIndex + 1;
		return field;
	}
}
