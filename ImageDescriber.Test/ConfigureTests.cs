// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber.Tests;

using ktsu.ImageDescriber.Verbs;

[TestClass]
public class ConfigureTests
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
	public void CommandLineModelIsAppliedBeforePrompts()
	{
		Configure options = new() { ModelString = "  llava  " };

		Assert.IsTrue(Configure.TryApplyCommandLineOptions(options));
		Assert.AreEqual("llava", Program.Settings.OllamaModel.WeakString);
	}

	[TestMethod]
	public void CommandLineEndpointIsNormalizedAndAppliedBeforePrompts()
	{
		Configure options = new() { EndpointString = "localhost:1234" };

		Assert.IsTrue(Configure.TryApplyCommandLineOptions(options));
		Assert.AreEqual("http://localhost:1234", Program.Settings.OllamaEndpoint.WeakString);
	}

	[TestMethod]
	public void InvalidCommandLineEndpointIsRejectedWithoutChangingSettings()
	{
		OllamaEndpoint originalEndpoint = Program.Settings.OllamaEndpoint;
		Configure options = new() { EndpointString = "ftp://example.com" };

		Assert.IsFalse(Configure.TryApplyCommandLineOptions(options));
		Assert.AreEqual(originalEndpoint.WeakString, Program.Settings.OllamaEndpoint.WeakString);
	}
}
