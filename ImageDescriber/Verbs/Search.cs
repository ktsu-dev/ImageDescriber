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
	/// An entry matches when every keyword starts a word, case-insensitively, in its description, its suggested
	/// file name, or one of its known paths, folder names included. The keywords may appear in any order, need
	/// not be adjacent, and may each be found in a different one of those. A keyword may be the start of a
	/// longer word ("sun" finds "sunset"), but not a part inside one ("cat" doesn't find "located").
	/// </summary>
	internal static bool MatchesAllTerms(ImageDescription description, IReadOnlyCollection<string> terms) =>
		terms.Count > 0 && terms.All(term =>
			StartsAWord(description.Description, term)
			|| StartsAWord(description.SuggestedFileName.ToString(), term)
			|| description.KnownPaths.Any(p => StartsAWord(p.ToString(), term)));

	/// <summary>
	/// Whether <paramref name="term"/> occurs in <paramref name="text"/>, case-insensitively, where a word
	/// begins: at the start, or after a character that is not a letter or digit (a space, "-", "_", ".").
	/// </summary>
	internal static bool StartsAWord(string text, string term)
	{
		if (term.Length == 0)
		{
			return false;
		}

		// A keyword that starts with punctuation ("#tag", ".jpg") has no word start of its own to line up with
		bool needsBoundary = IsWordChar(term, 0);
		for (int index = text.IndexOf(term, StringComparison.OrdinalIgnoreCase);
			index >= 0;
			index = index + 1 < text.Length ? text.IndexOf(term, index + 1, StringComparison.OrdinalIgnoreCase) : -1)
		{
			if (!needsBoundary || index == 0 || !IsWordChar(text, index - 1))
			{
				return true;
			}
		}

		return false;
	}

	// A surrogate half belongs to a letter or symbol outside the BMP, so treat it as part of a word
	private static bool IsWordChar(string text, int index) =>
		char.IsLetterOrDigit(text[index])
		|| char.IsSurrogate(text[index])
		|| char.GetUnicodeCategory(text[index]) is System.Globalization.UnicodeCategory.NonSpacingMark
			or System.Globalization.UnicodeCategory.SpacingCombiningMark;
}
