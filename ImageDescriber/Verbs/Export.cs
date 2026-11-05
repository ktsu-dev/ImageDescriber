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
			string? input = Console.ReadLine()?.Trim();
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

		string? content = outputFile.FileExtension.WeakString.ToUpperInvariant() switch
		{
			".JSON" => BuildJson(descriptions.Values),
			".CSV" => BuildCsv(descriptions.Values),
			_ => null,
		};
		if (content is null)
		{
			Console.WriteLine("Error: Output file must have .json or .csv extension.");
			Failed = true;
			return;
		}

		if (!TryWriteOutput(outputFile, content))
		{
			Failed = true;
			return;
		}

		Console.WriteLine($"Exported {descriptions.Count} description(s) to {outputFile}");
	}

	private static string BuildJson(IEnumerable<ImageDescription> descriptions) =>
		JsonSerializer.Serialize<ImageDescription[]>([.. descriptions], JsonOptions);

	/// <summary>
	/// Writes the export, or prints why it couldn't and returns false: a missing folder, a read-only
	/// location, or a file another program holds open (such as the previous CSV still open in Excel)
	/// must not end the process or the interactive menu with an unhandled exception.
	/// </summary>
	private static bool TryWriteOutput(AbsoluteFilePath outputPath, string content)
	{
		try
		{
			File.WriteAllText(outputPath.WeakString, content, Encoding.UTF8);
			return true;
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			Console.WriteLine($"Error: cannot write {outputPath}: {ex.Message}");
			return false;
		}
	}

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
