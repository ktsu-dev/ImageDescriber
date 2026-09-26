// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber.Tests;

using ktsu.ImageDescriber.Verbs;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

[TestClass]
public class SearchTests
{
	private PersistentState? originalSettings;

	[TestInitialize]
	public void Initialize() => originalSettings = Program.Settings;

	[TestCleanup]
	public void Cleanup() => Program.Settings = originalSettings!;

	[TestMethod]
	public void SearchPrintsEntryWhoseHashIsShorterThanTwelveCharacters()
	{
		Program.Settings = new PersistentState();
		Program.Settings.Descriptions["abc123"] = new ImageDescription
		{
			Hash = "abc123",
			Description = "a dog on a beach",
			SuggestedFileName = "dog.jpg".As<FileName>(),
		};

		string output = CaptureConsole(() => new Search().Run(new Search { Query = "dog" }));

		StringAssert.Contains(output, "Hash: abc123...");
		StringAssert.Contains(output, "Description: a dog on a beach");
	}

	[TestMethod]
	public void MergeEntriesSkipsEntriesWhoseHashIsNotSha256Hex()
	{
		Program.Settings = new PersistentState();
		string validHash = new('a', ImageHasher.HashLength);
		List<ImageDescription> entries =
		[
			new() { Hash = "abc123" },
			new() { Hash = new string('z', ImageHasher.HashLength) },
			new() { Hash = validHash },
		];

		(int NewCount, int UpdatedCount, int SkippedCount) result = default;
		CaptureConsole(() => result = Import.MergeEntries(entries));

		Assert.AreEqual(1, result.NewCount);
		Assert.AreEqual(2, result.SkippedCount);
		string[] expectedKeys = [validHash];
		CollectionAssert.AreEquivalent(expectedKeys, Program.Settings.Descriptions.Keys.ToArray());
	}

	[TestMethod]
	public void IsValidHashAcceptsOnlySha256HexStrings()
	{
		Assert.IsTrue(ImageHasher.IsValidHash(new string('0', 64)));
		Assert.IsTrue(ImageHasher.IsValidHash(string.Concat(Enumerable.Repeat("0123456789abcdefABCDEF", 3))[..64]));
		Assert.IsFalse(ImageHasher.IsValidHash(null));
		Assert.IsFalse(ImageHasher.IsValidHash(string.Empty));
		Assert.IsFalse(ImageHasher.IsValidHash("abc123"));
		Assert.IsFalse(ImageHasher.IsValidHash(new string('0', 65)));
		Assert.IsFalse(ImageHasher.IsValidHash(new string('g', 64)));
	}

	private static string CaptureConsole(Action action)
	{
		TextWriter original = Console.Out;
		using StringWriter writer = new();
		Console.SetOut(writer);
		try
		{
			action();
		}
		finally
		{
			Console.SetOut(original);
		}

		return writer.ToString();
	}
}
