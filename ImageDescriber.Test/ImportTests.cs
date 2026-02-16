// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImageDescriber.Tests;

using ktsu.ImageDescriber.Verbs;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

[TestClass]
public class ImportTests
{
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
}
