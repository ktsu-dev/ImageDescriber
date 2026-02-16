// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImageDescriber.Tests;

using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

[TestClass]
public class ImageScannerTests
{
	[TestMethod]
	public void ScanForImagesFindsImageFiles()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		try
		{
			File.WriteAllBytes(Path.Combine(tempDir, "photo.jpg"), [0xFF, 0xD8]);
			File.WriteAllBytes(Path.Combine(tempDir, "image.png"), [0x89, 0x50]);
			File.WriteAllBytes(Path.Combine(tempDir, "picture.gif"), [0x47, 0x49]);
			File.WriteAllBytes(Path.Combine(tempDir, "bitmap.bmp"), [0x42, 0x4D]);

			IReadOnlyList<AbsoluteFilePath> results = ImageScanner.ScanForImages(tempDir.As<AbsoluteDirectoryPath>());

			Assert.AreEqual(4, results.Count);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[TestMethod]
	public void ScanForImagesIgnoresNonImageFiles()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		try
		{
			File.WriteAllText(Path.Combine(tempDir, "readme.txt"), "text");
			File.WriteAllText(Path.Combine(tempDir, "data.json"), "{}");
			File.WriteAllText(Path.Combine(tempDir, "script.cs"), "class A{}");

			IReadOnlyList<AbsoluteFilePath> results = ImageScanner.ScanForImages(tempDir.As<AbsoluteDirectoryPath>());

			Assert.AreEqual(0, results.Count);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[TestMethod]
	public void ScanForImagesFindsFilesInSubdirectories()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string subDir = Path.Combine(tempDir, "sub");
		Directory.CreateDirectory(subDir);

		try
		{
			File.WriteAllBytes(Path.Combine(tempDir, "top.jpg"), [0xFF, 0xD8]);
			File.WriteAllBytes(Path.Combine(subDir, "nested.png"), [0x89, 0x50]);

			IReadOnlyList<AbsoluteFilePath> results = ImageScanner.ScanForImages(tempDir.As<AbsoluteDirectoryPath>());

			Assert.AreEqual(2, results.Count);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[TestMethod]
	public void ScanForImagesReturnsEmptyForNonExistentDirectory()
	{
		string fakePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent");

		IReadOnlyList<AbsoluteFilePath> results = ImageScanner.ScanForImages(fakePath.As<AbsoluteDirectoryPath>());

		Assert.AreEqual(0, results.Count);
	}

	[TestMethod]
	public void ScanForImagesReturnsEmptyForEmptyDirectory()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		try
		{
			IReadOnlyList<AbsoluteFilePath> results = ImageScanner.ScanForImages(tempDir.As<AbsoluteDirectoryPath>());

			Assert.AreEqual(0, results.Count);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[TestMethod]
	public void ScanForImagesRecognizesAllSupportedExtensions()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		try
		{
			string[] extensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp", ".tiff", ".tif"];
			foreach (string ext in extensions)
			{
				File.WriteAllBytes(Path.Combine(tempDir, $"file{ext}"), [0x00]);
			}

			IReadOnlyList<AbsoluteFilePath> results = ImageScanner.ScanForImages(tempDir.As<AbsoluteDirectoryPath>());

			Assert.AreEqual(extensions.Length, results.Count);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}
}
