// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImageDescriber.Tests;

using ktsu.ImageDescriber.Verbs;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

[TestClass]
public class ScanTests
{
	[TestMethod]
	public void SanitizeFileNameTrimsWhitespaceAndQuotes()
	{
		FileName result = Scan.SanitizeFileName("  \"sunset-photo\"  ", ".jpg".As<FileExtension>());

		Assert.AreEqual("sunset-photo.jpg", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameTakesFirstLine()
	{
		FileName result = Scan.SanitizeFileName("first-line\nsecond-line", ".png".As<FileExtension>());

		Assert.AreEqual("first-line.png", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameStripsExistingExtension()
	{
		FileName result = Scan.SanitizeFileName("photo.png", ".jpg".As<FileExtension>());

		Assert.AreEqual("photo.jpg", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameCollapsesMultipleHyphens()
	{
		FileName result = Scan.SanitizeFileName("a---b----c", ".jpg".As<FileExtension>());

		Assert.AreEqual("a-b-c.jpg", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameReturnsUnnamedForEmpty()
	{
		FileName result = Scan.SanitizeFileName("", ".jpg".As<FileExtension>());

		Assert.AreEqual("unnamed.jpg", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameReturnsUnnamedForWhitespace()
	{
		FileName result = Scan.SanitizeFileName("   ", ".jpg".As<FileExtension>());

		Assert.AreEqual("unnamed.jpg", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameTrimsBackticks()
	{
		FileName result = Scan.SanitizeFileName("`my-photo`", ".png".As<FileExtension>());

		Assert.AreEqual("my-photo.png", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameTrimsLeadingTrailingHyphens()
	{
		FileName result = Scan.SanitizeFileName("-leading-trailing-", ".jpg".As<FileExtension>());

		Assert.AreEqual("leading-trailing.jpg", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameHandlesSimpleName()
	{
		FileName result = Scan.SanitizeFileName("sunset-over-ocean", ".webp".As<FileExtension>());

		Assert.AreEqual("sunset-over-ocean.webp", result.WeakString);
	}
}
