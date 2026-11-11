// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber.Verbs;

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using CommandLine;

using ktsu.RoundTripStringJsonConverter;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

[Verb("Export", HelpText = "Export the description database to JSON or CSV.")]
internal sealed class Export : BaseVerb<Export>
{
	private static readonly JsonSerializerOptions JsonOptions = new()
	{
		WriteIndented = true,
		DefaultIgnoreCondition = JsonIgnoreCondition.Never,
		Converters = { new RoundTripStringJsonConverterFactory() },
	};

	[Option('o', "output", Required = true, HelpText = "Output file path (.json or .csv).")]
	public string OutputPath { get; set; } = string.Empty;

	internal override bool ValidateArgs()
	{
		// The menu runs verbs without CommandLineParser, which is what enforces Required = true
		if (string.IsNullOrWhiteSpace(OutputPath))
		{
			Console.Write("Enter the output file path (.json or .csv): ");
			string? input = ReadPromptPath();
			if (string.IsNullOrEmpty(input))
			{
				Console.WriteLine("No path provided. Aborting.");
				return false;
			}

			OutputPath = input;
		}

		return base.ValidateArgs();
	}

	internal override void Run(Export options)
	{
		Dictionary<string, ImageDescription> descriptions = Program.Settings.Descriptions;

		if (descriptions.Count == 0)
		{
			Console.WriteLine("No descriptions to export.");
			return;
		}

		AbsoluteFilePath? outputFile = TryConvertUserPath(options.OutputPath, static p => p.As<AbsoluteFilePath>());
		if (outputFile is null)
		{
			Failed = true;
			return;
		}

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
				Failed = true;
				return;
		}

		Console.WriteLine($"Exported {descriptions.Count} description(s) to {outputFile}");
	}

	internal static void ExportJson(AbsoluteFilePath outputPath, Dictionary<string, ImageDescription> descriptions)
	{
		ImageDescription[] entries = [.. descriptions.Values];
		string json = JsonSerializer.Serialize(entries, JsonOptions);

		// No byte order mark: RFC 8259 forbids one, and Python's json.load and Node's JSON.parse
		// reject a file that starts with it
		File.WriteAllText(outputPath.WeakString, json, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
	}

	// Encoding.UTF8 writes a byte order mark, which is what makes Excel read the CSV as UTF-8 rather
	// than the system code page, so non-ASCII descriptions and paths open intact
	private static void ExportCsv(AbsoluteFilePath outputPath, Dictionary<string, ImageDescription> descriptions) =>
		File.WriteAllText(outputPath.WeakString, BuildCsv(descriptions.Values), Encoding.UTF8);

	internal static string BuildCsv(IEnumerable<ImageDescription> descriptions)
	{
		StringBuilder sb = new();
		sb.AppendLine("Hash,SuggestedFileName,KnownPaths,Model,DescribedAt,FileSizeBytes,Description");

		foreach (ImageDescription desc in descriptions)
		{
			// End every path with a line break, which the quoted field keeps and no path contains.
			// "; " would do as a separator only until a folder name contains it.
			string joinedPaths = string.Concat(desc.KnownPaths.Select(p => p.WeakString + KnownPathTerminator));
			string[] fields =
			[
				desc.Hash,
				desc.SuggestedFileName.WeakString,
				joinedPaths,
				desc.Model.WeakString,
				desc.DescribedAt.ToString("O", CultureInfo.InvariantCulture),
				desc.FileSizeBytes.ToString(CultureInfo.InvariantCulture),
				desc.Description,
			];

			// Quote every field: file names may contain commas and quotes, and descriptions
			// usually contain paragraph breaks, so no field is safe to write bare.
			sb.AppendLine(string.Join(',', fields.Select(QuoteCsvField)));
		}

		return sb.ToString();
	}

	internal const char KnownPathTerminator = '\n';

	private static string QuoteCsvField(string value) => $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
