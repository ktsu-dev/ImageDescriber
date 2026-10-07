// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber.Tests;

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
	public void RunReturnsZeroWhenTheVerbSucceeds() =>
		Assert.AreEqual(0, RunQuietly("Stats"));

	[TestMethod]
	public void RunReturnsZeroForHelp() =>
		Assert.AreEqual(0, RunQuietly("--help"));

	private static int RunQuietly(params string[] args)
	{
		TextWriter originalOut = Console.Out;
		TextWriter originalError = Console.Error;
		try
		{
			Console.SetOut(TextWriter.Null);
			Console.SetError(TextWriter.Null);
			return Program.Run(args);
		}
		finally
		{
			Console.SetOut(originalOut);
			Console.SetError(originalError);
		}
	}
}
