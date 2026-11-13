// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber.Tests;

using ktsu.ImageDescriber.Verbs;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

[TestClass]
public class ImportTests
{
	private static readonly string[] ExpectedHeader = ["h1", "h2"];
	private static readonly string[] ExpectedMultiLineRecord = ["a\nb", "c"];
	private static readonly string[] ExpectedEscapedQuoteRecord = ["d", "e\"f"];

	[TestMethod]
	public void ParseCsvLineSimpleFields()
	{
		List<string> fields = Import.ParseCsvLine("a,b,c");

		Assert.AreEqual(3, fields.Count);
		Assert.AreEqual("a", fields[0]);
		Assert.AreEqual("b", fields[1]);
		Assert.AreEqual("c", fields[2]);
	}

	[TestMethod]
	public void ParseCsvLineSingleField()
	{
		List<string> fields = Import.ParseCsvLine("hello");

		Assert.AreEqual(1, fields.Count);
		Assert.AreEqual("hello", fields[0]);
	}

	[TestMethod]
	public void ParseCsvLineQuotedField()
	{
		List<string> fields = Import.ParseCsvLine("\"hello world\",b");

		Assert.AreEqual(2, fields.Count);
		Assert.AreEqual("hello world", fields[0]);
		Assert.AreEqual("b", fields[1]);
	}

	[TestMethod]
	public void ParseCsvLineQuotedFieldWithComma()
	{
		List<string> fields = Import.ParseCsvLine("\"hello, world\",b");

		Assert.AreEqual(2, fields.Count);
		Assert.AreEqual("hello, world", fields[0]);
		Assert.AreEqual("b", fields[1]);
	}

	[TestMethod]
	public void ParseCsvLineEscapedQuotes()
	{
		List<string> fields = Import.ParseCsvLine("\"she said \"\"hi\"\"\",b");

		Assert.AreEqual(2, fields.Count);
		Assert.AreEqual("she said \"hi\"", fields[0]);
		Assert.AreEqual("b", fields[1]);
	}

	[TestMethod]
	public void ParseCsvLineEmptyFields()
	{
		List<string> fields = Import.ParseCsvLine("a,,b");

		Assert.AreEqual(3, fields.Count);
		Assert.AreEqual("a", fields[0]);
		Assert.AreEqual(string.Empty, fields[1]);
		Assert.AreEqual("b", fields[2]);
	}

	[TestMethod]
	public void ParseCsvLineEmptyString()
	{
		List<string> fields = Import.ParseCsvLine(string.Empty);

		Assert.AreEqual(0, fields.Count);
	}

	[TestMethod]
	public void ParseCsvLineMixedQuotedAndUnquoted()
	{
		List<string> fields = Import.ParseCsvLine("abc,\"def,ghi\",jkl");

		Assert.AreEqual(3, fields.Count);
		Assert.AreEqual("abc", fields[0]);
		Assert.AreEqual("def,ghi", fields[1]);
		Assert.AreEqual("jkl", fields[2]);
	}

	[TestMethod]
	public void ParseCsvLineQuotedFieldAtEnd()
	{
		List<string> fields = Import.ParseCsvLine("a,\"b\"");

		Assert.AreEqual(2, fields.Count);
		Assert.AreEqual("a", fields[0]);
		Assert.AreEqual("b", fields[1]);
	}

	[TestMethod]
	public void ParseCsvLineEmptyQuotedField()
	{
		List<string> fields = Import.ParseCsvLine("\"\",b");

		Assert.AreEqual(2, fields.Count);
		Assert.AreEqual(string.Empty, fields[0]);
		Assert.AreEqual("b", fields[1]);
	}

	[TestMethod]
	public void MergeKnownPathsAddsNewPaths()
	{
		string tempDir = Path.GetTempPath();
		ImageDescription source = new()
		{
			KnownPaths = [Path.Combine(tempDir, "a.jpg").As<AbsoluteFilePath>(), Path.Combine(tempDir, "b.jpg").As<AbsoluteFilePath>()],
		};
		ImageDescription target = new()
		{
			KnownPaths = [Path.Combine(tempDir, "a.jpg").As<AbsoluteFilePath>()],
		};

		bool result = Import.MergeKnownPaths(source, target);

		Assert.IsTrue(result);
		Assert.AreEqual(2, target.KnownPaths.Count);
	}

	[TestMethod]
	public void MergeKnownPathsReturnsFalseWhenNoNewPaths()
	{
		string tempDir = Path.GetTempPath();
		ImageDescription source = new()
		{
			KnownPaths = [Path.Combine(tempDir, "a.jpg").As<AbsoluteFilePath>()],
		};
		ImageDescription target = new()
		{
			KnownPaths = [Path.Combine(tempDir, "a.jpg").As<AbsoluteFilePath>()],
		};

		bool result = Import.MergeKnownPaths(source, target);

		Assert.IsFalse(result);
		Assert.AreEqual(1, target.KnownPaths.Count);
	}

	[TestMethod]
	public void MergeKnownPathsAddsAllFromEmpty()
	{
		string tempDir = Path.GetTempPath();
		ImageDescription source = new()
		{
			KnownPaths = [Path.Combine(tempDir, "a.jpg").As<AbsoluteFilePath>(), Path.Combine(tempDir, "b.jpg").As<AbsoluteFilePath>()],
		};
		ImageDescription target = new()
		{
			KnownPaths = [],
		};

		bool result = Import.MergeKnownPaths(source, target);

		Assert.IsTrue(result);
		Assert.AreEqual(2, target.KnownPaths.Count);
	}

	[TestMethod]
	public void MergeKnownPathsReturnsFalseWhenSourceEmpty()
	{
		string tempDir = Path.GetTempPath();
		ImageDescription source = new()
		{
			KnownPaths = [],
		};
		ImageDescription target = new()
		{
			KnownPaths = [Path.Combine(tempDir, "a.jpg").As<AbsoluteFilePath>()],
		};

		bool result = Import.MergeKnownPaths(source, target);

		Assert.IsFalse(result);
		Assert.AreEqual(1, target.KnownPaths.Count);
	}

	[TestMethod]
	public void CsvExportThenImportRoundTripsEveryField()
	{
		string tempDir = Path.GetTempPath();
		ImageDescription[] originals =
		[
			new()
			{
				Hash = new string('a', 64),
				SuggestedFileName = "dog-on-beach.jpg".As<FileName>(),
				KnownPaths = [Path.Combine(tempDir, "a.jpg").As<AbsoluteFilePath>(), Path.Combine(tempDir, "b.jpg").As<AbsoluteFilePath>()],
				Model = "llama3.2-vision".As<OllamaModelName>(),
				DescribedAt = new DateTime(2026, 9, 26, 7, 10, 12, DateTimeKind.Utc),
				FileSizeBytes = 12345,
				Description = "A dog on a beach.\n\nThe sky is blue, and the dog says \"woof\".",
			},
			new()
			{
				Hash = new string('b', 64),
				SuggestedFileName = "dog, cat 'n' friends.jpg".As<FileName>(),
				KnownPaths = [Path.Combine(tempDir, "c.jpg").As<AbsoluteFilePath>()],
				Model = "llava".As<OllamaModelName>(),
				DescribedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
				FileSizeBytes = 1,
				Description = "Two animals.\r\nOn a sofa.",
			},
			new()
			{
				Hash = new string('c', 64),
				SuggestedFileName = "plain.jpg".As<FileName>(),
				KnownPaths = [Path.Combine(tempDir, "d.jpg").As<AbsoluteFilePath>()],
				Model = "llava".As<OllamaModelName>(),
				DescribedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
				FileSizeBytes = 2,
				Description = "Simple.",
			},
		];

		List<ImageDescription> imported = Import.ParseCsv(Export.BuildCsv(originals));

		Assert.HasCount(originals.Length, imported);
		for (int i = 0; i < originals.Length; i++)
		{
			ImageDescription expected = originals[i];
			ImageDescription actual = imported[i];
			Assert.AreEqual(expected.Hash, actual.Hash);
			Assert.AreEqual(expected.SuggestedFileName, actual.SuggestedFileName);
			Assert.AreSequenceEqual(expected.KnownPaths, actual.KnownPaths);
			Assert.AreEqual(expected.Model, actual.Model);
			Assert.AreEqual(expected.DescribedAt, actual.DescribedAt);
			Assert.AreEqual(expected.FileSizeBytes, actual.FileSizeBytes);
			Assert.AreEqual(expected.Description, actual.Description);
		}
	}

	[TestMethod]
	public void JsonExportHasNoByteOrderMarkAndImportsBack()
	{
		ImageDescription original = new()
		{
			Hash = new string('a', 64),
			SuggestedFileName = "dog-on-beach.jpg".As<FileName>(),
			KnownPaths = [Path.Combine(Path.GetTempPath(), "a.jpg").As<AbsoluteFilePath>()],
			Model = "llava".As<OllamaModelName>(),
			DescribedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
			FileSizeBytes = 1,
			Description = "A dog on a beach.",
		};
		string file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
		try
		{
			Export.ExportJson(file.As<AbsoluteFilePath>(), new() { [original.Hash] = original });

			// A byte order mark makes Python's json.load and Node's JSON.parse reject the file
			byte[] bytes = File.ReadAllBytes(file);
			Assert.AreEqual((byte)'[', bytes[0]);

			List<ImageDescription>? entries = Import.ImportJson(file.As<AbsoluteFilePath>());
			Assert.IsNotNull(entries);
			Assert.HasCount(1, entries);
			Assert.AreEqual(original.Hash, entries[0].Hash);
		}
		finally
		{
			File.Delete(file);
		}
	}

	[TestMethod]
	public void CsvExportThenImportKeepsPathsThatContainTheOldSeparator()
	{
		string folder = Path.Combine(Path.GetTempPath(), "Photos", "Smith; Jones wedding");
		ImageDescription original = new()
		{
			Hash = new string('d', 64),
			SuggestedFileName = "wedding.jpg".As<FileName>(),
			KnownPaths = [Path.Combine(folder, "IMG_001.jpg").As<AbsoluteFilePath>(), Path.Combine(folder, "IMG_002; copy.jpg").As<AbsoluteFilePath>()],
			Model = "llava".As<OllamaModelName>(),
			DescribedAt = new DateTime(2026, 9, 27, 1, 8, 10, DateTimeKind.Utc),
			FileSizeBytes = 3,
			Description = "A wedding.",
		};

		List<ImageDescription> imported = Import.ParseCsv(Export.BuildCsv([original]));

		Assert.HasCount(1, imported);
		Assert.AreSequenceEqual(original.KnownPaths, imported[0].KnownPaths);
	}

	[TestMethod]
	public void SplitKnownPathsReadsBothTheCurrentAndTheLegacyFormat()
	{
		string[] expected = ["/a/b.jpg", "/c/d.jpg"];

		Assert.AreSequenceEqual(expected, Import.SplitKnownPaths("/a/b.jpg\n/c/d.jpg\n"));
		Assert.AreSequenceEqual(expected, Import.SplitKnownPaths("/a/b.jpg\r\n/c/d.jpg\r\n"));
		Assert.AreSequenceEqual(expected, Import.SplitKnownPaths("/a/b.jpg; /c/d.jpg"));
		Assert.IsEmpty(Import.SplitKnownPaths(string.Empty));
	}

	[TestMethod]
	public void ParseCsvRecordsKeepsLineBreaksInsideQuotedFields()
	{
		List<(int LineNumber, List<string> Fields, bool Malformed)> records = Import.ParseCsvRecords("h1,h2\r\n\"a\nb\",c\r\n\r\nd,\"e\"\"f\"\n");

		Assert.HasCount(3, records);
		Assert.AreSequenceEqual(ExpectedHeader, records[0].Fields);
		Assert.AreSequenceEqual(ExpectedMultiLineRecord, records[1].Fields);
		Assert.AreEqual(2, records[1].LineNumber);
		Assert.AreSequenceEqual(ExpectedEscapedQuoteRecord, records[2].Fields);
		Assert.AreEqual(5, records[2].LineNumber);
	}

	[TestMethod]
	[DataRow("{\"not\":\"array\"}")]
	[DataRow("[{\"Hash\":\"aaaa")]
	[DataRow("[{\"Hash\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\",\"FileSizeBytes\":\"abc\"}]")]
	[DataRow("[{\"Hash\":\"aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa\",\"KnownPaths\":[\"relative/photo.jpg\"]}]")]
	public void RunReportsAMalformedJsonFileWithoutThrowingOrChangingTheStore(string json)
	{
		PersistentState originalSettings = Program.Settings;
		TextWriter originalOut = Console.Out;
		string file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
		File.WriteAllText(file, json);
		try
		{
			Program.Settings = new PersistentState();
			using StringWriter output = new();
			Console.SetOut(output);

			Import import = new();
			import.Run(new Import { InputPath = file });

			Assert.IsTrue(import.Failed);
			Assert.IsEmpty(Program.Settings.Descriptions);
			Assert.Contains(file, output.ToString());
		}
		finally
		{
			Console.SetOut(originalOut);
			Program.Settings = originalSettings;
			File.Delete(file);
		}
	}
	[TestMethod]
	[DataRow(".json", "[]")]
	[DataRow(".csv", "Hash\n")]
	public void RunReportsALockedInputFileWithoutThrowingOrChangingTheStore(string extension, string content)
	{
		PersistentState originalSettings = Program.Settings;
		TextWriter originalOut = Console.Out;
		string file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + extension);
		File.WriteAllText(file, content);
		try
		{
			Program.Settings = new PersistentState();
			using StringWriter output = new();
			Console.SetOut(output);

			Import import = new();
			using (new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
			{
				import.Run(new Import { InputPath = file });
			}

			Assert.IsTrue(import.Failed);
			Assert.IsEmpty(Program.Settings.Descriptions);
			Assert.Contains($"Error: cannot read {file}: ", output.ToString());
			Assert.DoesNotContain("Import complete.", output.ToString());
		}
		finally
		{
			Console.SetOut(originalOut);
			Program.Settings = originalSettings;
			File.Delete(file);
		}
	}

	[TestMethod]
	public void ImportJsonAndImportCsvReturnNullForALockedFile()
	{
		TextWriter originalOut = Console.Out;
		string file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
		File.WriteAllText(file, "[]");
		try
		{
			using StringWriter output = new();
			Console.SetOut(output);

			using (new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
			{
				Assert.IsNull(Import.ImportJson(file.As<AbsoluteFilePath>()));
				Assert.IsNull(Import.ImportCsv(file.As<AbsoluteFilePath>()));
			}
		}
		finally
		{
			Console.SetOut(originalOut);
			File.Delete(file);
		}
	}

	[TestMethod]
	[DataRow(".json")]
	[DataRow(".csv")]
	public void ImportJsonAndImportCsvReturnNullForAPathTheyAreDeniedAccessTo(string extension)
	{
		// Opening a directory as a file raises UnauthorizedAccessException on every platform,
		// whoever runs the test, which a file mode cannot promise under root.
		TextWriter originalOut = Console.Out;
		string directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + extension);
		Directory.CreateDirectory(directory);
		try
		{
			using StringWriter output = new();
			Console.SetOut(output);

			AbsoluteFilePath path = directory.As<AbsoluteFilePath>();
			List<ImageDescription>? entries = extension == ".json" ? Import.ImportJson(path) : Import.ImportCsv(path);

			Assert.IsNull(entries);
			Assert.Contains($"Error: cannot read {directory}: ", output.ToString());
		}
		finally
		{
			Console.SetOut(originalOut);
			Directory.Delete(directory);
		}
	}

	[TestMethod]
	public void MergeEntriesStoresZeroForANegativeFileSizeFromCsv()
	{
		ImageDescription original = new()
		{
			Hash = new string('a', 64),
			SuggestedFileName = "photo.jpg".As<FileName>(),
			KnownPaths = [Path.Combine(Path.GetTempPath(), "photo.jpg").As<AbsoluteFilePath>()],
			Model = "llava".As<OllamaModelName>(),
			DescribedAt = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc),
			FileSizeBytes = -5,
			Description = "A photo.",
		};

		List<ImageDescription> entries = Import.ParseCsv(Export.BuildCsv([original]));

		AssertMergeStoresZeroSize(entries, original.Hash);
	}

	[TestMethod]
	public void MergeEntriesStoresZeroForANegativeFileSizeFromJson()
	{
		string hash = new('b', 64);
		string file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".json");
		File.WriteAllText(file, $"[{{\"Hash\":\"{hash}\",\"FileSizeBytes\":-5,\"Description\":\"A photo.\"}}]");
		try
		{
			List<ImageDescription>? entries = Import.ImportJson(file.As<AbsoluteFilePath>());

			Assert.IsNotNull(entries);
			AssertMergeStoresZeroSize(entries, hash);
		}
		finally
		{
			File.Delete(file);
		}
	}

	private static void AssertMergeStoresZeroSize(List<ImageDescription> entries, string hash)
	{
		PersistentState originalSettings = Program.Settings;
		TextWriter originalOut = Console.Out;
		try
		{
			Program.Settings = new PersistentState();
			using StringWriter output = new();
			Console.SetOut(output);

			(int newCount, _, _) = Import.MergeEntries(entries);

			Assert.AreEqual(1, newCount);
			Assert.AreEqual(0, Program.Settings.Descriptions[hash].FileSizeBytes);
			Assert.Contains("negative file size", output.ToString());
		}
		finally
		{
			Console.SetOut(originalOut);
			Program.Settings = originalSettings;
		}
	}

	private static string CsvWithRows(params string[] dataRows)
	{
		string header = Export.BuildCsv([]).Split('\n')[0].TrimEnd('\r');
		return string.Join("\n", [header, .. dataRows]) + "\n";
	}

	private static string GoodCsvRow(char hashChar) =>
		$"{new string(hashChar, 64)},photo.jpg,,llava,2026-01-02T03:04:05.0000000Z,10,A photo.";

	private static readonly string[] UnreadableCsvRows =
	[
		$"{new string('a', 64)},photo.jpg,,llava,not-a-date,10,A photo.",
		$"{new string('b', 64)},photo.jpg,,llava,2026-01-02T03:04:05.0000000Z,12x,A photo.",
		$"{new string('c', 64)},photo.jpg",
	];

	[TestMethod]
	public void ParseCsvCountsEveryUnreadableRow()
	{
		List<ImageDescription> entries = Import.ParseCsv(CsvWithRows(UnreadableCsvRows), out int unreadableCount);

		Assert.IsEmpty(entries);
		Assert.AreEqual(3, unreadableCount);
	}

	[TestMethod]
	public void ParseCsvKeepsTheGoodRowsAndCountsTheBadOnes()
	{
		List<ImageDescription> entries = Import.ParseCsv(
			CsvWithRows(GoodCsvRow('d'), UnreadableCsvRows[0], GoodCsvRow('e'), UnreadableCsvRows[2]),
			out int unreadableCount);

		Assert.HasCount(2, entries);
		Assert.AreEqual(new string('d', 64), entries[0].Hash);
		Assert.AreEqual(new string('e', 64), entries[1].Hash);
		Assert.AreEqual(2, unreadableCount);
	}

	private static string QuotedCsvRow(char hashChar, string description) =>
		$"\"{new string(hashChar, 64)}\",\"photo.jpg\",\"\",\"llava\",\"2026-01-02T03:04:05.0000000Z\",\"10\",{description}";

	[TestMethod]
	public void ParseCsvSkipsARowWithAStrayQuoteAndKeepsTheRowsAfterIt()
	{
		List<ImageDescription> entries = Import.ParseCsv(
			CsvWithRows(
				QuotedCsvRow('a', "\"A 12\" vinyl record\""),
				QuotedCsvRow('b', "\"A dog\""),
				QuotedCsvRow('c', "\"A cat\"")),
			out int unreadableCount);

		Assert.HasCount(2, entries);
		Assert.AreEqual(new string('b', 64), entries[0].Hash);
		Assert.AreEqual("A dog", entries[0].Description);
		Assert.AreEqual(new string('c', 64), entries[1].Hash);
		Assert.AreEqual("A cat", entries[1].Description);
		Assert.AreEqual(1, unreadableCount);
	}

	[TestMethod]
	public void ParseCsvSkipsATruncatedLastRowWhoseQuoteNeverCloses()
	{
		string csv = CsvWithRows(QuotedCsvRow('a', "\"A cat\"")) + QuotedCsvRow('b', "\"A photo of a dog on a be");

		List<ImageDescription> entries = Import.ParseCsv(csv, out int unreadableCount);

		Assert.HasCount(1, entries);
		Assert.AreEqual(new string('a', 64), entries[0].Hash);
		Assert.AreEqual(1, unreadableCount);
	}

	[TestMethod]
	public void ParseCsvKeepsAQuoteInsideAnUnquotedField()
	{
		List<ImageDescription> entries = Import.ParseCsv(
			CsvWithRows($"{new string('a', 64)},photo.jpg,,llava,2026-01-02T03:04:05.0000000Z,10,A 12\" vinyl record", GoodCsvRow('b')),
			out int unreadableCount);

		Assert.HasCount(2, entries);
		Assert.AreEqual("A 12\" vinyl record", entries[0].Description);
		Assert.AreEqual(0, unreadableCount);
	}

	[TestMethod]
	public void ParseCsvRecordsMarksOnlyTheUnbalancedRecordsMalformed()
	{
		List<(int LineNumber, List<string> Fields, bool Malformed)> records =
			Import.ParseCsvRecords("h1,h2\n\"a\"x,b\nc,\"d\"\n\"e\",\"f");

		Assert.HasCount(4, records);
		Assert.IsFalse(records[0].Malformed);
		Assert.IsTrue(records[1].Malformed);
		Assert.IsFalse(records[2].Malformed);
		Assert.AreEqual(3, records[2].LineNumber);
		Assert.IsTrue(records[3].Malformed);
	}

	[TestMethod]
	public void ParseCsvCountsNothingForAValidExport()
	{
		List<ImageDescription> entries = Import.ParseCsv(CsvWithRows(GoodCsvRow('d'), GoodCsvRow('e')), out int unreadableCount);

		Assert.HasCount(2, entries);
		Assert.AreEqual(0, unreadableCount);
	}

	[TestMethod]
	public void RunFailsAndReportsTheSkippedRowsWhenNoCsvRowCanBeRead()
	{
		PersistentState originalSettings = Program.Settings;
		TextWriter originalOut = Console.Out;
		string file = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".csv");
		File.WriteAllText(file, CsvWithRows(UnreadableCsvRows));
		try
		{
			Program.Settings = new PersistentState();
			using StringWriter output = new();
			Console.SetOut(output);

			Import import = new();
			import.Run(new Import { InputPath = file });

			Assert.IsTrue(import.Failed, output.ToString());
			Assert.IsEmpty(Program.Settings.Descriptions);
			Assert.Contains("Skipped:         3", output.ToString());
			Assert.DoesNotContain("Import complete.", output.ToString());
		}
		finally
		{
			Console.SetOut(originalOut);
			Program.Settings = originalSettings;
			File.Delete(file);
		}
	}
}
