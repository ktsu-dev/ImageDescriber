// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber.Tests;

using ktsu.ImageDescriber.Verbs;

[TestClass]
public class StoreLockTests
{
	private string lockPath = string.Empty;
	private string originalLockPath = string.Empty;

	[TestInitialize]
	public void Initialize()
	{
		lockPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "ImageDescriber.lock");
		originalLockPath = StoreLock.StorePath;
		StoreLock.StorePath = lockPath;
	}

	[TestCleanup]
	public void Cleanup()
	{
		StoreLock.StorePath = originalLockPath;
		string directory = Path.GetDirectoryName(lockPath)!;
		if (Directory.Exists(directory))
		{
			Directory.Delete(directory, true);
		}
	}

	[TestMethod]
	public void SecondAcquireFailsWhileTheFirstIsHeld()
	{
		using (StoreLock? first = StoreLock.TryAcquire(lockPath))
		{
			Assert.IsNotNull(first);
			Assert.IsNull(StoreLock.TryAcquire(lockPath), "A second run took the lock while the first still held it");
		}

		using StoreLock? again = StoreLock.TryAcquire(lockPath);
		Assert.IsNotNull(again, "The lock was not released when the first run ended");
	}

	[TestMethod]
	public void VerbsThatSaveTheStoreHoldTheLock()
	{
		Assert.IsTrue(new Scan().WritesStore);
		Assert.IsTrue(new Import().WritesStore);
		Assert.IsTrue(new Configure().WritesStore);
		Assert.IsFalse(new Search().WritesStore);
		Assert.IsFalse(new Stats().WritesStore);
		Assert.IsFalse(new Export().WritesStore);
	}

	[TestMethod]
	public void AWritingVerbRefusesToRunWhileAnotherRunHoldsTheLock()
	{
		TextWriter originalOut = Console.Out;
		using StringWriter output = new();
		using StoreLock? held = StoreLock.TryAcquire(lockPath);
		Assert.IsNotNull(held);

		try
		{
			Console.SetOut(output);

			// The input file does not exist, so if the verb ran at all it would say so, and it
			// would never reach a save.
			new Import { InputPath = Path.Combine(Path.GetDirectoryName(lockPath)!, "missing.json") }.Run();
		}
		finally
		{
			Console.SetOut(originalOut);
		}

		string text = output.ToString();
		Assert.Contains(StoreLock.BusyMessage, text);
		Assert.DoesNotContain("File not found", text);
	}
}
