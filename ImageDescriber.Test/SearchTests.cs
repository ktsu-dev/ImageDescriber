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
	[DataRow("sunset")]
	[DataRow("beach sunset")]
	[DataRow("sunset beach")]
	[DataRow("dog beach")]
	[DataRow("dog  running")]
	[DataRow("SUNSET Beach")]
	[DataRow("waterline holiday")]
	public void SearchMatchesAnEntryContainingEveryKeywordInAnyOrder(string query)
	{
		Program.Settings = new PersistentState();
		AddBeachEntry();

		string output = CaptureConsole(() => new Search().Run(new Search { Query = query }));

		StringAssert.Contains(output, "1 match(es)");
	}

	[TestMethod]
	[DataRow("beach snow")]
	[DataRow("snow")]
	[DataRow("cat running")]
	public void SearchDoesNotMatchAnEntryMissingAKeyword(string query)
	{
		Program.Settings = new PersistentState();
		AddBeachEntry();

		string output = CaptureConsole(() => new Search().Run(new Search { Query = query }));

		StringAssert.Contains(output, "0 match(es)");
	}

	private static void AddBeachEntry()
	{
		string hash = new('d', ImageHasher.HashLength);
		Program.Settings.Descriptions[hash] = new ImageDescription
		{
			Hash = hash,
			Description = "A sandy beach at sunset, with a dog running along the waterline.",
			SuggestedFileName = "beach.jpg".As<FileName>(),
			KnownPaths = [Path.Combine(Path.GetTempPath(), "holiday-2024.jpg").As<AbsoluteFilePath>()],
		};
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
	public void MergeEntriesSkipsANullEntryAndKeepsTheRest()
	{
		// A JSON export ending in `null` deserializes to a list with a null item
		Program.Settings = new PersistentState();
		string validHash = new('c', ImageHasher.HashLength);
		List<ImageDescription> entries =
		[
			new() { Hash = validHash },
			null!,
		];

		(int NewCount, int UpdatedCount, int SkippedCount) result = default;
		CaptureConsole(() => result = Import.MergeEntries(entries));

		Assert.AreEqual(1, result.NewCount);
		Assert.AreEqual(1, result.SkippedCount);
		string[] expectedKeys = [validHash];
		CollectionAssert.AreEquivalent(expectedKeys, Program.Settings.Descriptions.Keys.ToArray());
	}

	[TestMethod]
	public void MergeEntriesStoresAnUpperCaseHashAsTheLowerCaseHashScanLooksUp()
	{
		Program.Settings = new PersistentState();
		string lowerHash = new('a', ImageHasher.HashLength);
		string upperHash = lowerHash.ToUpperInvariant();
		AbsoluteFilePath firstPath = Path.Combine(Path.GetTempPath(), "first.jpg").As<AbsoluteFilePath>();
		AbsoluteFilePath secondPath = Path.Combine(Path.GetTempPath(), "second.jpg").As<AbsoluteFilePath>();

		(int NewCount, int UpdatedCount, int SkippedCount) first = default;
		(int NewCount, int UpdatedCount, int SkippedCount) second = default;
		CaptureConsole(() => first = Import.MergeEntries([new() { Hash = upperHash, KnownPaths = [firstPath] }]));
		CaptureConsole(() => second = Import.MergeEntries([new() { Hash = lowerHash, KnownPaths = [secondPath] }]));

		Assert.AreEqual(1, first.NewCount);
		Assert.AreEqual(1, second.UpdatedCount);
		string[] expectedKeys = [lowerHash];
		CollectionAssert.AreEquivalent(expectedKeys, Program.Settings.Descriptions.Keys.ToArray());
		Assert.AreEqual(lowerHash, Program.Settings.Descriptions[lowerHash].Hash);
		Assert.AreEqual(2, Program.Settings.Descriptions[lowerHash].KnownPaths.Count);
	}

	[TestMethod]
	public void MergeEntriesRepairsNullFieldsSoSearchStatsAndExportStillRun()
	{
		Program.Settings = new PersistentState();
		string hash = new('b', ImageHasher.HashLength);
		List<ImageDescription> entries =
		[
			new()
			{
				Hash = hash,
				Description = null!,
				KnownPaths = null!,
				SuggestedFileName = null!,
				Model = null!,
			},
		];

		CaptureConsole(() => Import.MergeEntries(entries));

		ImageDescription stored = Program.Settings.Descriptions[hash];
		Assert.IsNotNull(stored.Description);
		Assert.IsNotNull(stored.KnownPaths);
		Assert.IsNotNull(stored.SuggestedFileName);
		Assert.IsNotNull(stored.Model);
		CaptureConsole(() => new Search().Run(new Search { Query = "dog" }));
		CaptureConsole(() => new Stats().Run(new Stats()));
		StringAssert.Contains(Export.BuildCsv(Program.Settings.Descriptions.Values), hash);
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
