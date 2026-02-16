// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImageDescriber.Tests;

using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

[TestClass]
public class ImageHasherTests
{
	[TestMethod]
	public void ComputeHashReturnsSameHashForSameContent()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		try
		{
			string file1 = Path.Combine(tempDir, "file1.bin");
			string file2 = Path.Combine(tempDir, "file2.bin");
			byte[] content = [1, 2, 3, 4, 5];
			File.WriteAllBytes(file1, content);
			File.WriteAllBytes(file2, content);

			string hash1 = ImageHasher.ComputeHash(file1.As<AbsoluteFilePath>());
			string hash2 = ImageHasher.ComputeHash(file2.As<AbsoluteFilePath>());

			Assert.AreEqual(hash1, hash2);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[TestMethod]
	public void ComputeHashReturnsDifferentHashForDifferentContent()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		try
		{
			string file1 = Path.Combine(tempDir, "file1.bin");
			string file2 = Path.Combine(tempDir, "file2.bin");
			File.WriteAllBytes(file1, [1, 2, 3]);
			File.WriteAllBytes(file2, [4, 5, 6]);

			string hash1 = ImageHasher.ComputeHash(file1.As<AbsoluteFilePath>());
			string hash2 = ImageHasher.ComputeHash(file2.As<AbsoluteFilePath>());

			Assert.AreNotEqual(hash1, hash2);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[TestMethod]
	public void ComputeHashReturnsLowercaseHexString()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		try
		{
			string file = Path.Combine(tempDir, "file.bin");
			File.WriteAllBytes(file, [0xFF, 0xAB, 0xCD]);

			string hash = ImageHasher.ComputeHash(file.As<AbsoluteFilePath>());

			Assert.IsFalse(string.IsNullOrEmpty(hash));
			Assert.AreEqual(64, hash.Length); // SHA256 produces 64 hex chars
			Assert.IsTrue(hash.All(c => c is (>= '0' and <= '9') or (>= 'a' and <= 'f')));
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[TestMethod]
	public void HashFilesReturnsHashForEachFile()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		try
		{
			string file1 = Path.Combine(tempDir, "file1.bin");
			string file2 = Path.Combine(tempDir, "file2.bin");
			File.WriteAllBytes(file1, [1, 2, 3]);
			File.WriteAllBytes(file2, [4, 5, 6]);

			AbsoluteFilePath[] paths = [file1.As<AbsoluteFilePath>(), file2.As<AbsoluteFilePath>()];
			Dictionary<AbsoluteFilePath, string> results = ImageHasher.HashFiles(paths);

			Assert.AreEqual(2, results.Count);
			Assert.IsTrue(results.ContainsKey(paths[0]));
			Assert.IsTrue(results.ContainsKey(paths[1]));
			Assert.AreNotEqual(results[paths[0]], results[paths[1]]);
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}
}
