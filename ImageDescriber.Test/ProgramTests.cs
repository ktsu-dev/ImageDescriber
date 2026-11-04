// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber.Tests;

using ktsu.ImageDescriber.Verbs;

[TestClass]
public class ProgramTests
{
	private PersistentState? originalSettings;

	[TestInitialize]
	public void Initialize()
	{
		originalSettings = Program.Settings;
		Program.Settings = new PersistentState();
	}

	[TestCleanup]
	public void Cleanup() => Program.Settings = originalSettings!;

	[TestMethod]
	public void RunReturnsNonZeroWhenARequiredOptionIsMissing() =>
		Assert.AreEqual(1, RunQuietly("Search"));

	[TestMethod]
	[DataRow("Scna")]
	[DataRow("Scna", "-p", ".")]
	public void RunReturnsNonZeroForAnUnknownVerb(params string[] args) =>
		Assert.AreEqual(1, RunQuietly(args));

	[TestMethod]
	public void RunReturnsNonZeroForAStrayPositionalWord() =>
		Assert.AreEqual(1, RunQuietly("Search", "-q", "dog", "cat"));

	[TestMethod]
	public void RunMatchesAVerbIgnoringCase()
	{
		TextReader originalIn = Console.In;
		TextWriter originalOut = Console.Out;
		using StringWriter output = new();
		int exitCode;
		try
		{
			// Empty input, so a fallback to the interactive menu ends instead of blocking
			Console.SetIn(new StringReader(string.Empty));
			Console.SetOut(output);
			exitCode = Program.Run(["stats"]);
		}
		finally
		{
			Console.SetIn(originalIn);
			Console.SetOut(originalOut);
		}

		Assert.AreEqual(0, exitCode);
		StringAssert.Contains(output.ToString(), "Database Statistics");
	}

	[TestMethod]
	public void RunReportsAMissingOptionForALowerCaseVerb() =>
		Assert.AreEqual(1, RunQuietly("search"));

	[TestMethod]
	public void RunReturnsNonZeroWhenOllamaIsUnavailable()
	{
		string directory = Directory.CreateTempSubdirectory().FullName;
		try
		{
			Assert.AreEqual(1, RunQuietly("Scan", "-p", directory, "-e", "http://127.0.0.1:1"));
		}
		finally
		{
			Directory.Delete(directory, recursive: true);
		}
	}

	[TestMethod]
	public void RunReturnsNonZeroWhenTheImportFileIsMissing() =>
		Assert.AreEqual(1, RunQuietly("Import", "-i", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".csv")));

	[TestMethod]
	public void RunReturnsNonZeroWhenTheImportFileHasAnUnsupportedExtension()
	{
		string file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt");
		File.WriteAllText(file, string.Empty);
		try
		{
			Assert.AreEqual(1, RunQuietly("Import", "-i", file));
		}
		finally
		{
			File.Delete(file);
		}
	}

	[TestMethod]
	public void RunReturnsNonZeroWhenTheExportFileHasAnUnsupportedExtension()
	{
		string hash = new('a', ImageHasher.HashLength);
		Program.Settings.Descriptions[hash] = new ImageDescription { Hash = hash };

		Assert.AreEqual(1, RunQuietly("Export", "-o", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".txt")));
	}

	[TestMethod]
	public void SearchReportsFailureForABlankQuery()
	{
		Search search = new();
		RunQuietly(() => search.Run(new Search { Query = " " }));
		Assert.IsTrue(search.Failed);
	}

	[TestMethod]
	public void RunReturnsNonZeroWhenTheOllamaEndpointIsInvalid() =>
		Assert.AreEqual(1, RunQuietly("Scan", "-p", Path.GetTempPath(), "-e", "ftp://example.com"));

	[TestMethod]
	public void RunReturnsNonZeroWhenThePromptedPathIsLeftEmpty() =>
		Assert.AreEqual(1, RunQuietly("Import"));

	[TestMethod]
	public void RunReturnsZeroForVersion() =>
		Assert.AreEqual(0, RunQuietly("--version"));

	[TestMethod]
	public void RunReturnsZeroWhenTheVerbSucceeds() =>
		Assert.AreEqual(0, RunQuietly("Stats"));

	[TestMethod]
	public void RunReturnsZeroForHelp() =>
		Assert.AreEqual(0, RunQuietly("--help"));

	[TestMethod]
	[DataRow("Scan", "-p")]
	[DataRow("Import", "-i")]
	[DataRow("Export", "-o")]
	public void RunReturnsNonZeroForAPathContainingABarInsteadOfThrowing(string verb, string option)
	{
		// | is legal in a POSIX file name, but ktsu.Semantics.Paths rejects it
		AddEntrySoExportHasSomethingToWrite();
		string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "a|b.json");

		Assert.AreEqual(1, RunQuietly(verb, option, path));
	}

	[TestMethod]
	[DataRow("Scan", "-p")]
	[DataRow("Import", "-i")]
	[DataRow("Export", "-o")]
	public void RunReturnsNonZeroForAPathOver256CharactersInsteadOfThrowing(string verb, string option)
	{
		AddEntrySoExportHasSomethingToWrite();
		string path = Path.Combine(Path.GetTempPath(), new string('a', 120), new string('b', 120), new string('c', 40) + ".json");

		Assert.AreEqual(1, RunQuietly(verb, option, path));
	}

	[TestMethod]
	public void ImportExplainsAnUnsupportedPath()
	{
		string path = Path.Combine(Path.GetTempPath(), "x|y.json");
		using StringWriter output = new();
		Import import = new();

		TextWriter original = Console.Out;
		Console.SetOut(output);
		try
		{
			import.Run(new Import { InputPath = path });
		}
		finally
		{
			Console.SetOut(original);
		}

		Assert.IsTrue(import.Failed);
		StringAssert.Contains(output.ToString(), $"Unsupported path: {path}");
	}

	private static void AddEntrySoExportHasSomethingToWrite()
	{
		string hash = new('e', ImageHasher.HashLength);
		Program.Settings.Descriptions[hash] = new ImageDescription { Hash = hash };
	}

	private static int RunQuietly(params string[] args)
	{
		int exitCode = 0;
		RunQuietly(() => exitCode = Program.Run(args));
		return exitCode;
	}

	/// <summary>
	/// Runs <paramref name="action"/> with output discarded and an empty standard input, so a
	/// verb that prompts for a missing value reads end-of-input instead of blocking.
	/// </summary>
	private static void RunQuietly(Action action)
	{
		TextReader originalIn = Console.In;
		TextWriter originalOut = Console.Out;
		TextWriter originalError = Console.Error;
		try
		{
			Console.SetIn(new StringReader(string.Empty));
			Console.SetOut(TextWriter.Null);
			Console.SetError(TextWriter.Null);
			action();
		}
		finally
		{
			Console.SetIn(originalIn);
			Console.SetOut(originalOut);
			Console.SetError(originalError);
		}
	}
}
