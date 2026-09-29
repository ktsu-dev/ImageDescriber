// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber.Tests;

using ktsu.ImageDescriber.Verbs;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

[TestClass]
public class MenuTests
{
	[TestMethod]
	public void RunWithoutArgumentsOffersMenuOffWindows()
	{
		if (OperatingSystem.IsWindows())
		{
			Assert.Inconclusive("Windows uses the interactive ScrollMenu, which needs a real console.");
			return;
		}

		TextReader originalIn = Console.In;
		TextWriter originalOut = Console.Out;
		using StringReader input = new(string.Empty);
		using StringWriter output = new();
		Console.SetIn(input);
		Console.SetOut(output);

		try
		{
			Menu menu = new();
			menu.Run();
		}
		finally
		{
			Console.SetIn(originalIn);
			Console.SetOut(originalOut);
		}

		string text = output.ToString();
		StringAssert.Contains(text, "Scan");
		StringAssert.Contains(text, "Exit");
		Assert.IsFalse(text.Contains("1. Menu", StringComparison.Ordinal), "The menu should not list itself.");
	}

	[TestMethod]
	public void PromptMenuRunsTheChosenItemUntilExit()
	{
		List<string> ran = [];
		(string, Action)[] items =
		[
			("First", () => ran.Add("First")),
			("Second", () => ran.Add("Second")),
		];
		using StringReader input = new("2\n1\n3\n");
		using StringWriter output = new();

		Menu.RunPromptMenu(input, output, items);

		Assert.AreEqual("Second,First", string.Join(",", ran));
		StringAssert.Contains(output.ToString(), "1. First");
		StringAssert.Contains(output.ToString(), "2. Second");
		StringAssert.Contains(output.ToString(), "3. Exit");
	}

	[TestMethod]
	public void PromptMenuRejectsInvalidChoices()
	{
		List<string> ran = [];
		(string, Action)[] items = [("Only", () => ran.Add("Only"))];
		using StringReader input = new("bogus\n0\n3\n-1\n\n2\n");
		using StringWriter output = new();

		Menu.RunPromptMenu(input, output, items);

		Assert.AreEqual(0, ran.Count);
		string[] errors = output.ToString().Split("Enter a number from 1 to 2.");
		Assert.AreEqual(6, errors.Length, "Each of the five invalid lines should be rejected.");
	}

	[TestMethod]
	public void PromptMenuStopsAtEndOfInput()
	{
		List<string> ran = [];
		(string, Action)[] items = [("Only", () => ran.Add("Only"))];
		using StringReader input = new("1\n");
		using StringWriter output = new();

		Menu.RunPromptMenu(input, output, items);

		Assert.AreEqual("Only", string.Join(",", ran));
	}

	[TestMethod]
	public void ExportRunWithoutAnOutputPathAsksForOne()
	{
		string outputFile = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.json");
		try
		{
			string text = RunWithConsole(new Export(), $"{outputFile}\n");

			Assert.Contains("Enter the output file path", text);
			Assert.IsTrue(File.Exists(outputFile));
			Assert.Contains("A dog on a beach.", File.ReadAllText(outputFile));
		}
		finally
		{
			File.Delete(outputFile);
		}
	}

	[TestMethod]
	public void ExportRunWithAnEmptyAnswerAbortsCleanly()
	{
		string text = RunWithConsole(new Export(), "\n");

		Assert.Contains("No path provided. Aborting.", text);
	}

	[TestMethod]
	public void SearchRunWithoutAQueryAsksForOne()
	{
		string text = RunWithConsole(new Search(), "dog\n");

		Assert.Contains("Enter the search query", text);
		Assert.Contains("Search results for \"dog\": 1 match(es)", text);
	}

	[TestMethod]
	public void MenuItemsAskAgainOnEveryRun()
	{
		Action search = Menu.CreateVerbAction(typeof(Search));

		string text = RunWithConsole(search, "dog\ncat\n");

		Assert.Contains("Search results for \"dog\": 1 match(es)", text);
		Assert.Contains("Search results for \"cat\": 0 match(es)", text);
	}

	private static string RunWithConsole(BaseVerb verb, string input) => RunWithConsole(verb.Run, input);

	/// <summary>
	/// Runs <paramref name="run"/> as many times as <paramref name="input"/> has lines, against a
	/// database holding one description, with the console redirected.
	/// </summary>
	private static string RunWithConsole(Action run, string input)
	{
		PersistentState originalSettings = Program.Settings;
		TextReader originalIn = Console.In;
		TextWriter originalOut = Console.Out;
		using StringReader reader = new(input);
		using StringWriter output = new();
		try
		{
			Program.Settings = new PersistentState();
			Program.Settings.Descriptions[new string('a', 64)] = new ImageDescription
			{
				Hash = new string('a', 64),
				SuggestedFileName = "dog-on-beach.jpg".As<FileName>(),
				KnownPaths = [Path.Combine(Path.GetTempPath(), "a.jpg").As<AbsoluteFilePath>()],
				Model = "llava".As<OllamaModelName>(),
				DescribedAt = new DateTime(2026, 9, 27, 1, 7, 46, DateTimeKind.Utc),
				FileSizeBytes = 1,
				Description = "A dog on a beach.",
			};
			Console.SetIn(reader);
			Console.SetOut(output);

			int runs = input.Count(c => c == '\n');
			for (int i = 0; i < runs; i++)
			{
				run();
			}
		}
		finally
		{
			Console.SetIn(originalIn);
			Console.SetOut(originalOut);
			Program.Settings = originalSettings;
		}

		return output.ToString();
	}
}
