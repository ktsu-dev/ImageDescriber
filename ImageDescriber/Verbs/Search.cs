// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber.Verbs;

using System.Collections.Generic;
using System.Linq;

using CommandLine;

using ktsu.Semantics.Paths;

[Verb("Search", HelpText = "Search image descriptions by keyword.")]
internal sealed class Search : BaseVerb<Search>
{
	[Option('q', "query", Required = true, HelpText = "The search query to find in descriptions.")]
	public string Query { get; set; } = string.Empty;

	internal override bool ValidateArgs()
	{
		// The menu runs verbs without CommandLineParser, which is what enforces Required = true
		if (string.IsNullOrWhiteSpace(Query))
		{
			Console.Write("Enter the search query: ");
			string? input = Console.ReadLine()?.Trim();
			if (string.IsNullOrEmpty(input))
			{
				Console.WriteLine("No query provided. Aborting.");
				return false;
			}

			Query = input;
		}

		return base.ValidateArgs();
	}

	internal override void Run(Search options)
	{
		if (string.IsNullOrWhiteSpace(options.Query))
		{
			Console.WriteLine("Error: Search query cannot be empty.");
			Failed = true;
			return;
		}

		Dictionary<string, ImageDescription> descriptions = Program.Settings.Descriptions;

		string[] terms = SplitQuery(options.Query);

		ImageDescription[] matches = [.. descriptions
			.Where(kvp => MatchesAllTerms(kvp.Value, terms))
			.OrderByDescending(kvp => kvp.Value.DescribedAt)
			.Select(kvp => kvp.Value)];

		Console.WriteLine($"Search results for \"{options.Query}\": {matches.Length} match(es)");
		Console.WriteLine();

		foreach (ImageDescription desc in matches)
		{
			Console.WriteLine($"  Suggested: {desc.SuggestedFileName}");
			Console.WriteLine($"  Hash: {ImageHasher.ShortHash(desc.Hash)}...");
			Console.WriteLine($"  Date: {desc.DescribedAt:yyyy-MM-dd HH:mm:ss} UTC");
			Console.WriteLine($"  Paths ({desc.KnownPaths.Count}):");
			foreach (AbsoluteFilePath path in desc.KnownPaths)
			{
				Console.WriteLine($"    {path}");
			}

			Console.WriteLine($"  Description: {desc.Description}");
			Console.WriteLine();
		}
	}

	/// <summary>
	/// Splits a query into its whitespace-separated keywords.
	/// </summary>
	internal static string[] SplitQuery(string query) =>
		query.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

	/// <summary>
	/// An entry matches when every keyword appears, case-insensitively, in its description or in one of its
	/// known file names. The keywords may appear in any order and need not be adjacent.
	/// </summary>
	internal static bool MatchesAllTerms(ImageDescription description, IReadOnlyCollection<string> terms) =>
		terms.Count > 0 && terms.All(term =>
			description.Description.Contains(term, StringComparison.OrdinalIgnoreCase)
			|| description.KnownPaths.Any(p => p.FileName.Contains(term, StringComparison.OrdinalIgnoreCase)));
}
