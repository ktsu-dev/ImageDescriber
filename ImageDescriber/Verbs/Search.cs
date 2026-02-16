// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

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

	internal override void Run(Search options)
	{
		if (string.IsNullOrWhiteSpace(options.Query))
		{
			Console.WriteLine("Error: Search query cannot be empty.");
			return;
		}

		Dictionary<string, ImageDescription> descriptions = Program.Settings.Descriptions;

		ImageDescription[] matches = [.. descriptions
			.Where(kvp => kvp.Value.Description.Contains(options.Query, StringComparison.OrdinalIgnoreCase)
				|| kvp.Value.KnownPaths.Any(p => p.FileName.Contains(options.Query, StringComparison.OrdinalIgnoreCase)))
			.OrderByDescending(kvp => kvp.Value.DescribedAt)
			.Select(kvp => kvp.Value)];

		Console.WriteLine($"Search results for \"{options.Query}\": {matches.Length} match(es)");
		Console.WriteLine();

		foreach (ImageDescription desc in matches)
		{
			Console.WriteLine($"  Suggested: {desc.SuggestedFileName}");
			Console.WriteLine($"  Hash: {desc.Hash[..12]}...");
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
}
