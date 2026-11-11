// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber.Tests;

using System.IO.Abstractions.TestingHelpers;

using ktsu.AppDataStorage;
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
		// Configure takes the lock itself, only around its save, because its prompts wait on the user
		Assert.IsFalse(new Configure().WritesStore);
		Assert.IsFalse(new Search().WritesStore);
		Assert.IsFalse(new Stats().WritesStore);
		Assert.IsFalse(new Export().WritesStore);
	}

	[TestMethod]
	public void ConfigureDoesNotHoldTheLockWhileItsPromptsWait()
	{
		PersistentState originalSettings = Program.Settings;
		TextReader originalIn = Console.In;
		TextWriter originalOut = Console.Out;
		using StringWriter output = new();
		List<bool> lockFreeAtPrompt = [];
		StoreLock? heldByAnotherRun = null;

		// Every prompt checks that a concurrent Scan or Import could take the lock. The last answer then
		// takes it, as a run starting just before Configure saves would, so the test never writes the
		// real store
		string[] answers = ["", "llava", "", "", "a filename prompt"];
		int prompt = 0;
		using PromptReader input = new(() =>
		{
			using (StoreLock? concurrent = StoreLock.TryAcquire(lockPath))
			{
				lockFreeAtPrompt.Add(concurrent is not null);
			}

			string answer = answers[prompt++];
			if (prompt == answers.Length)
			{
				heldByAnotherRun = StoreLock.TryAcquire(lockPath);
			}

			return answer;
		});

		try
		{
			Program.Settings = new PersistentState();
			Console.SetIn(input);
			Console.SetOut(output);
			new Configure().Run();
		}
		finally
		{
			heldByAnotherRun?.Dispose();
			Console.SetIn(originalIn);
			Console.SetOut(originalOut);
			Program.Settings = originalSettings;
		}

		Assert.AreEqual(answers.Length, lockFreeAtPrompt.Count, output.ToString());
		Assert.IsTrue(lockFreeAtPrompt.TrueForAll(free => free), "Configure held the store lock while a prompt waited");
		Assert.IsNotNull(heldByAnotherRun);
		string text = output.ToString();
		Assert.Contains(StoreLock.BusyMessage, text);
		Assert.DoesNotContain("Settings saved.", text);
	}

	[TestMethod]
	public void ConfigureWithNoAnswersChangesNothing()
	{
		PersistentState originalSettings = Program.Settings;
		TextReader originalIn = Console.In;
		TextWriter originalOut = Console.Out;
		using StringWriter output = new();
		using StoreLock? held = StoreLock.TryAcquire(lockPath);
		Assert.IsNotNull(held);

		try
		{
			Program.Settings = new PersistentState();
			Console.SetIn(new StringReader("\n\n\n\n\n"));
			Console.SetOut(output);
			new Configure().Run();
		}
		finally
		{
			Console.SetIn(originalIn);
			Console.SetOut(originalOut);
			Program.Settings = originalSettings;
		}

		string text = output.ToString();
		Assert.Contains("No changes.", text);
		Assert.DoesNotContain(StoreLock.BusyMessage, text);
	}

	[TestMethod]
	public void ConfigureSavesOnlyTheAnsweredFieldsOverWhatAnotherRunSavedDuringItsPrompts()
	{
		PersistentState originalSettings = Program.Settings;
		TextReader originalIn = Console.In;
		TextWriter originalOut = Console.Out;
		using StringWriter output = new();
		string hash = new('f', ImageHasher.HashLength);

		// The second answer is given after another run saved a new description and its own concurrency,
		// which Configure must keep: it was not asked to change either
		string[] answers = ["", "llava", "", "", ""];
		int prompt = 0;
		using PromptReader input = new(() =>
		{
			if (prompt == 1)
			{
				using StoreLock? concurrent = StoreLock.TryAcquire(lockPath);
				Assert.IsNotNull(concurrent);
				PersistentState other = PersistentState.LoadOrCreate();
				other.Descriptions[hash] = new ImageDescription { Hash = hash, Description = "a dog" };
				other.MaxConcurrentRequests = 7;
				other.Save();
			}

			return answers[prompt++];
		});

		AppData.ConfigureForTesting(() => new MockFileSystem());
		try
		{
			Program.Settings = PersistentState.LoadOrCreate();
			Console.SetIn(input);
			Console.SetOut(output);
			new Configure().Run();

			PersistentState saved = PersistentState.LoadOrCreate();
			Assert.AreEqual("llava", saved.OllamaModel.WeakString, output.ToString());
			Assert.AreEqual(7, saved.MaxConcurrentRequests);
			Assert.IsTrue(saved.Descriptions.ContainsKey(hash), "Configure overwrote a description saved during its prompts");
			Assert.AreEqual("llava", Program.Settings.OllamaModel.WeakString);
			StringAssert.Contains(output.ToString(), "Settings saved.");
		}
		finally
		{
			AppData.ResetFileSystem();
			Console.SetIn(originalIn);
			Console.SetOut(originalOut);
			Program.Settings = originalSettings;
		}
	}

	private sealed class PromptReader(Func<string> answer) : TextReader
	{
		public override string? ReadLine() => answer();
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
