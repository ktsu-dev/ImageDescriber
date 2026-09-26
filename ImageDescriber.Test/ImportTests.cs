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
	public void ParseCsvRecordsKeepsLineBreaksInsideQuotedFields()
	{
		List<(int LineNumber, List<string> Fields)> records = Import.ParseCsvRecords("h1,h2\r\n\"a\nb\",c\r\n\r\nd,\"e\"\"f\"\n");

		Assert.HasCount(3, records);
		Assert.AreSequenceEqual(ExpectedHeader, records[0].Fields);
		Assert.AreSequenceEqual(ExpectedMultiLineRecord, records[1].Fields);
		Assert.AreEqual(2, records[1].LineNumber);
		Assert.AreSequenceEqual(ExpectedEscapedQuoteRecord, records[2].Fields);
		Assert.AreEqual(5, records[2].LineNumber);
	}
}
