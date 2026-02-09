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

using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

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

		AbsoluteFilePath outputFile = System.IO.Path.GetFullPath(options.OutputPath).As<AbsoluteFilePath>();

		switch (outputFile.FileExtension.WeakString.ToUpperInvariant())
		{
			case ".JSON":
				ExportJson(outputFile, descriptions);
				break;
			case ".CSV":
				ExportCsv(outputFile, descriptions);
				break;
			default:
				Console.WriteLine("Error: Output file must have .json or .csv extension.");
				return;
		}

		Console.WriteLine($"Exported {descriptions.Count} description(s) to {outputFile}");
	}

	private static void ExportJson(AbsoluteFilePath outputPath, Dictionary<string, ImageDescription> descriptions)
	{
		ImageDescription[] entries = [.. descriptions.Values];
		string json = JsonSerializer.Serialize(entries, JsonOptions);
		File.WriteAllText(outputPath.WeakString, json, Encoding.UTF8);
	}

	private static void ExportCsv(AbsoluteFilePath outputPath, Dictionary<string, ImageDescription> descriptions)
	{
		StringBuilder sb = new();
		sb.AppendLine("Hash,FileName,FilePath,Model,DescribedAt,FileSizeBytes,Description");

		foreach (ImageDescription desc in descriptions.Values)
		{
			string escapedDescription = $"\"{desc.Description.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
			string escapedPath = $"\"{desc.FilePath.WeakString.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
			sb.AppendLine($"{desc.Hash},{desc.FileName},{escapedPath},{desc.Model},{desc.DescribedAt:O},{desc.FileSizeBytes},{escapedDescription}");
		}

		File.WriteAllText(outputPath.WeakString, sb.ToString(), Encoding.UTF8);
	}
}
