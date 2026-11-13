// Copyright (c) 2023-2026 ktsu-dev contributors

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
	public void ScanForImagesReportsAFileAsNotADirectory()
	{
		string file = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.jpg");
		File.WriteAllBytes(file, [0xFF, 0xD8]);
		TextWriter originalOut = Console.Out;
		using StringWriter output = new();
		Console.SetOut(output);
		try
		{
			IReadOnlyList<AbsoluteFilePath> results = ImageScanner.ScanForImages(file.As<AbsoluteDirectoryPath>());

			Assert.AreEqual(0, results.Count);
			StringAssert.Contains(output.ToString(), "Not a directory");
		}
		finally
		{
			Console.SetOut(originalOut);
			File.Delete(file);
		}
	}

	[TestMethod]
	public void ScanForImagesSkipsUnreadableSubdirectories()
	{
		if (OperatingSystem.IsWindows())
		{
			Assert.Inconclusive("Unix file modes are used to make the folder unreadable.");
			return;
		}

		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string lockedDir = Path.Combine(tempDir, "locked");
		Directory.CreateDirectory(lockedDir);
		File.WriteAllBytes(Path.Combine(tempDir, "readable.jpg"), [0xFF, 0xD8]);
		File.WriteAllBytes(Path.Combine(lockedDir, "hidden.jpg"), [0xFF, 0xD8]);
		File.SetUnixFileMode(lockedDir, UnixFileMode.None);

		try
		{
			if (Directory.Exists(lockedDir) && CanList(lockedDir))
			{
				Assert.Inconclusive("Running as a user that can read any folder, such as root.");
			}

			IReadOnlyList<AbsoluteFilePath> results = ImageScanner.ScanForImages(tempDir.As<AbsoluteDirectoryPath>());

			Assert.AreEqual(1, results.Count);
			Assert.AreEqual("readable.jpg", Path.GetFileName(results[0].WeakString));
		}
		finally
		{
			File.SetUnixFileMode(lockedDir, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
			Directory.Delete(tempDir, true);
		}
	}

	[TestMethod]
	public void ScanForImagesListsEachImageOnceUnderADirectorySymlinkLoop()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string subDir = Path.Combine(tempDir, "a");
		Directory.CreateDirectory(subDir);
		File.WriteAllBytes(Path.Combine(subDir, "x.jpg"), [0xFF, 0xD8]);

		try
		{
			CreateDirectoryLinkOrSkip(Path.Combine(subDir, "loop"), tempDir);

			IReadOnlyList<AbsoluteFilePath> results = ImageScanner.ScanForImages(tempDir.As<AbsoluteDirectoryPath>());

			Assert.AreEqual(1, results.Count);
			Assert.AreEqual(Path.Combine(subDir, "x.jpg"), results[0].WeakString);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[TestMethod]
	public void ScanForImagesFinishesWithTwoSiblingSymlinkLoops()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string subDir = Path.Combine(tempDir, "a");
		Directory.CreateDirectory(subDir);
		File.WriteAllBytes(Path.Combine(subDir, "x.jpg"), [0xFF, 0xD8]);

		try
		{
			CreateDirectoryLinkOrSkip(Path.Combine(subDir, "loop"), tempDir);
			CreateDirectoryLinkOrSkip(Path.Combine(subDir, "loop2"), tempDir);

			// Following the links branches exponentially and never finishes, so bound the wait
			Task<IReadOnlyList<AbsoluteFilePath>> scan = Task.Run(() => ImageScanner.ScanForImages(tempDir.As<AbsoluteDirectoryPath>()));

			Assert.IsTrue(scan.Wait(TimeSpan.FromSeconds(30)), "Discovery did not finish");
			Assert.AreEqual(1, scan.Result.Count);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[TestMethod]
	public void ScanForImagesStillListsASymlinkedImageFile()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);
		string target = Path.Combine(tempDir, "real.jpg");
		File.WriteAllBytes(target, [0xFF, 0xD8]);

		try
		{
			try
			{
				File.CreateSymbolicLink(Path.Combine(tempDir, "alias.jpg"), target);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				Assert.Inconclusive($"Symbolic links can't be created here: {ex.Message}");
			}

			IReadOnlyList<AbsoluteFilePath> results = ImageScanner.ScanForImages(tempDir.As<AbsoluteDirectoryPath>());

			Assert.AreEqual(2, results.Count);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[TestMethod]
	public void IsLinkedDirectoryIsFalseWithoutTheReparsePointAttribute() =>
		Assert.IsFalse(ImageScanner.IsLinkedDirectory(FileAttributes.Directory, Path.GetTempPath()));

	[TestMethod]
	public void IsLinkedDirectoryIsFalseForAReparsePointThatIsNotALink() =>
		// A cloud-synced folder carries the attribute without being a link
		Assert.IsFalse(ImageScanner.IsLinkedDirectory(FileAttributes.Directory | FileAttributes.ReparsePoint, Path.GetTempPath()));

	[TestMethod]
	public void IsLinkedDirectoryIsTrueForADirectorySymlink()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);
		string link = Path.Combine(tempDir, "link");

		try
		{
			CreateDirectoryLinkOrSkip(link, tempDir);

			Assert.IsTrue(ImageScanner.IsLinkedDirectory(FileAttributes.Directory | FileAttributes.ReparsePoint, link));
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	private static void CreateDirectoryLinkOrSkip(string link, string target)
	{
		try
		{
			Directory.CreateSymbolicLink(link, target);
		}
		catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
		{
			Assert.Inconclusive($"Symbolic links can't be created here: {ex.Message}");
		}
	}

	private static bool CanList(string directory)
	{
		try
		{
			_ = Directory.GetFiles(directory);
			return true;
		}
		catch (UnauthorizedAccessException)
		{
			return false;
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
	public void ScanForImagesFindsHiddenFilesAndFilesInHiddenDirectories()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		string hiddenDir = Path.Combine(tempDir, ".hidden");
		string visibleDir = Path.Combine(tempDir, "vis");
		Directory.CreateDirectory(hiddenDir);
		Directory.CreateDirectory(visibleDir);

		try
		{
			string inHiddenDir = Path.Combine(hiddenDir, "a.jpg");
			string hiddenFile = Path.Combine(tempDir, ".c.png");
			string visibleFile = Path.Combine(visibleDir, "b.jpg");
			File.WriteAllBytes(inHiddenDir, [0xFF, 0xD8]);
			File.WriteAllBytes(hiddenFile, [0x89, 0x50]);
			File.WriteAllBytes(visibleFile, [0xFF, 0xD8]);

			// A leading dot is what makes an entry hidden on Linux and macOS; Windows uses the attribute.
			if (OperatingSystem.IsWindows())
			{
				File.SetAttributes(hiddenDir, File.GetAttributes(hiddenDir) | FileAttributes.Hidden);
				File.SetAttributes(hiddenFile, File.GetAttributes(hiddenFile) | FileAttributes.Hidden | FileAttributes.System);
			}

			IReadOnlyList<AbsoluteFilePath> results = ImageScanner.ScanForImages(tempDir.As<AbsoluteDirectoryPath>());

			CollectionAssert.AreEquivalent(
				new[] { inHiddenDir, hiddenFile, visibleFile }.Select(Path.GetFullPath).ToList(),
				results.Select(r => Path.GetFullPath(r.WeakString)).ToList());
		}
		finally
		{
			if (OperatingSystem.IsWindows())
			{
				File.SetAttributes(Path.Combine(tempDir, ".c.png"), FileAttributes.Normal);
			}

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
			string[] extensions = [".jpg", ".jpeg", ".png", ".gif", ".bmp", ".webp"];
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

	[TestMethod]
	public void ScanForImagesMatchesExtensionsCaseInsensitively()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		try
		{
			File.WriteAllBytes(Path.Combine(tempDir, "IMG_0001.JPG"), [0xFF, 0xD8]);
			File.WriteAllBytes(Path.Combine(tempDir, "screenshot.Png"), [0x89, 0x50]);
			File.WriteAllBytes(Path.Combine(tempDir, "photo.jpeg"), [0xFF, 0xD8]);
			File.WriteAllBytes(Path.Combine(tempDir, "scan.TIFF"), [0x49, 0x49]);
			File.WriteAllText(Path.Combine(tempDir, "NOTES.TXT"), "text");

			IReadOnlyList<AbsoluteFilePath> results = ImageScanner.ScanForImages(tempDir.As<AbsoluteDirectoryPath>());

			string[] names = [.. results.Select(r => Path.GetFileName(r.WeakString)).Order(StringComparer.Ordinal)];
			string[] expected = ["IMG_0001.JPG", "photo.jpeg", "screenshot.Png"];
			CollectionAssert.AreEqual(expected, names);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[TestMethod]
	public void ScanForImagesSkipsTiffAndReportsItOnce()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);
		TextWriter originalOut = Console.Out;
		using StringWriter output = new();
		Console.SetOut(output);

		try
		{
			File.WriteAllBytes(Path.Combine(tempDir, "photo.png"), [0x89, 0x50]);
			File.WriteAllBytes(Path.Combine(tempDir, "scan.tiff"), [0x49, 0x49]);
			File.WriteAllBytes(Path.Combine(tempDir, "page.TIF"), [0x4D, 0x4D]);

			IReadOnlyList<AbsoluteFilePath> results = ImageScanner.ScanForImages(tempDir.As<AbsoluteDirectoryPath>());

			Assert.HasCount(1, results);
			Assert.AreEqual("photo.png", Path.GetFileName(results[0].WeakString));
			string[] lines = output.ToString().Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
			Assert.HasCount(1, lines);
			StringAssert.Contains(lines[0], "Skipped 2 TIFF image(s)");
		}
		finally
		{
			Console.SetOut(originalOut);
			Directory.Delete(tempDir, true);
		}
	}
}
