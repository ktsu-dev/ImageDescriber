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
