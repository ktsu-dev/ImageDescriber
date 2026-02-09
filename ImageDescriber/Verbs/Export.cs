// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImageDescriber.Verbs;

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using CommandLine;

[Verb("Export", HelpText = "Export the description database to JSON or CSV.")]
internal sealed class Export : BaseVerb<Export>
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.Never,
	};

	[Option('o', "output", Required = true, HelpText = "Output file path (.json or .csv).")]
	public string OutputPath { get; set; } = string.Empty;

	internal override void Run(Export options)
	{
		Dictionary<string, ImageDescription> descriptions = Program.Settings.Descriptions;

		if (descriptions.Count == 0)
		{
			Console.WriteLine("No descriptions to export.");
			return;
		}

		string extension = System.IO.Path.GetExtension(options.OutputPath).ToUpperInvariant();

		switch (extension)
		{
			case ".JSON":
				ExportJson(options.OutputPath, descriptions);
				break;
			case ".CSV":
				ExportCsv(options.OutputPath, descriptions);
				break;
			default:
				Console.WriteLine("Error: Output file must have .json or .csv extension.");
				return;
		}

		Console.WriteLine($"Exported {descriptions.Count} description(s) to {options.OutputPath}");
	}

	private static void ExportJson(string outputPath, Dictionary<string, ImageDescription> descriptions)
	{
		ImageDescription[] entries = [.. descriptions.Values];
		string json = JsonSerializer.Serialize(entries, JsonOptions);
		File.WriteAllText(outputPath, json, Encoding.UTF8);
	}

	private static void ExportCsv(string outputPath, Dictionary<string, ImageDescription> descriptions)
	{
		StringBuilder sb = new();
		sb.AppendLine("Hash,FileName,FilePath,Model,DescribedAt,FileSizeBytes,Description");

		foreach (ImageDescription desc in descriptions.Values)
		{
			string escapedDescription = $"\"{desc.Description.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
			string escapedPath = $"\"{desc.FilePath.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
			sb.AppendLine($"{desc.Hash},{desc.FileName},{escapedPath},{desc.Model},{desc.DescribedAt:O},{desc.FileSizeBytes},{escapedDescription}");
		}

		File.WriteAllText(outputPath, sb.ToString(), Encoding.UTF8);
	}
}
