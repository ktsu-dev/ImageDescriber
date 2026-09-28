// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber.Tests;

using ktsu.ImageDescriber.Verbs;

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
}
