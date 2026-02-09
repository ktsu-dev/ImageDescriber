// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImageDescriber.Verbs;

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

using CommandLine;

using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

[Verb("Import", HelpText = "Import descriptions from a JSON or CSV file.")]
internal sealed class Import : BaseVerb<Import>
{
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

		List<ImageDescription> entries;

		switch (inputFile.FileExtension.WeakString.ToUpperInvariant())
		{
			case ".JSON":
				entries = ImportJson(inputFile);
				break;
			case ".CSV":
				entries = ImportCsv(inputFile);
				break;
			default:
				Console.WriteLine("Error: Input file must have .json or .csv extension.");
				return;
		}

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
				bool pathsAdded = false;
				foreach (AbsoluteFilePath path in entry.KnownPaths)
				{
					if (!existing.KnownPaths.Contains(path))
					{
						existing.KnownPaths.Add(path);
						pathsAdded = true;
					}
				}

				if (pathsAdded)
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

		Program.Settings.Save();

		Console.WriteLine($"Import complete.");
		Console.WriteLine($"  New entries:     {newCount}");
		Console.WriteLine($"  Updated paths:   {updatedCount}");
		Console.WriteLine($"  Skipped:         {skippedCount}");
		Console.WriteLine($"  Total in database: {Program.Settings.Descriptions.Count}");
	}

	private static List<ImageDescription> ImportJson(AbsoluteFilePath inputPath)
	{
		string json = File.ReadAllText(inputPath.WeakString);
		return JsonSerializer.Deserialize<List<ImageDescription>>(json) ?? [];
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
				DateTime describedAt = DateTime.Parse(fields[4], null, System.Globalization.DateTimeStyles.RoundtripKind);
				long fileSizeBytes = long.Parse(fields[5], System.Globalization.CultureInfo.InvariantCulture);
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

	private static List<string> ParseCsvLine(string line)
	{
		List<string> fields = [];
		int i = 0;

		while (i < line.Length)
		{
			if (line[i] == '"')
			{
				// Quoted field
				i++;
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

				fields.Add(field.ToString());

				// Skip comma after quoted field
				if (i < line.Length && line[i] == ',')
				{
					i++;
				}
			}
			else
			{
				// Unquoted field
				int commaIndex = line.IndexOf(',', i);
				if (commaIndex < 0)
				{
					fields.Add(line[i..]);
					break;
				}
				else
				{
					fields.Add(line[i..commaIndex]);
					i = commaIndex + 1;
				}
			}
		}

		return fields;
	}
}
