// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber.Verbs;

using System.Collections.Generic;
using System.Linq;

using CommandLine;

[Verb("Stats", HelpText = "Show database statistics.")]
internal sealed class Stats : BaseVerb<Stats>
{
	internal override void Run(Stats options)
	{
		Dictionary<string, ImageDescription> descriptions = Program.Settings.Descriptions;

		Console.WriteLine("=== ImageDescriber Database Statistics ===");
		Console.WriteLine();
		Console.WriteLine($"Total descriptions: {descriptions.Count}");

		if (descriptions.Count == 0)
		{
			return;
		}

		Console.WriteLine($"Total file size: {FormatBytes(TotalFileSize(descriptions.Values))}");
		Console.WriteLine();

		// Models used
		IGrouping<OllamaModelName, ImageDescription>[] modelGroups = [.. descriptions.Values
			.GroupBy(d => d.Model)
			.OrderByDescending(g => g.Count())];

		Console.WriteLine("Models used:");
		foreach (IGrouping<OllamaModelName, ImageDescription> group in modelGroups)
		{
			Console.WriteLine($"  {group.Key}: {group.Count()} description(s)");
		}

		Console.WriteLine();

		// Date range
		DateTime oldest = descriptions.Values.Min(d => d.DescribedAt);
		DateTime newest = descriptions.Values.Max(d => d.DescribedAt);
		Console.WriteLine($"Oldest description: {oldest:yyyy-MM-dd HH:mm:ss} UTC");
		Console.WriteLine($"Newest description: {newest:yyyy-MM-dd HH:mm:ss} UTC");

		Console.WriteLine();

		// Path statistics
		int totalPaths = descriptions.Values.Sum(d => d.KnownPaths.Count);
		int duplicateImages = descriptions.Values.Count(d => d.KnownPaths.Count > 1);
		Console.WriteLine($"Total known paths: {totalPaths}");
		if (duplicateImages > 0)
		{
			Console.WriteLine($"Images found at multiple paths: {duplicateImages}");
		}

		Console.WriteLine();

		// Average description length
		double avgLength = descriptions.Values.Average(d => d.Description.Length);
		Console.WriteLine($"Average description length: {avgLength:F0} characters");
	}

	/// <summary>
	/// Totals the stored sizes in a <see cref="decimal"/>, since checked <see cref="long"/> addition
	/// would throw once they add up to more than <see cref="long.MaxValue"/>.
	/// </summary>
	internal static decimal TotalFileSize(IEnumerable<ImageDescription> descriptions) =>
		descriptions.Sum(d => (decimal)d.FileSizeBytes);

	internal static string FormatBytes(decimal bytes) => bytes switch
	{
		< 1024m => $"{bytes} B",
		< 1024m * 1024 => $"{bytes / 1024m:F1} KB",
		< 1024m * 1024 * 1024 => $"{bytes / (1024m * 1024m):F1} MB",
		_ => $"{bytes / (1024m * 1024m * 1024m):F1} GB",
	};
}
