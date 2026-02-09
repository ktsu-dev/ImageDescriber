// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImageDescriber;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

using ktsu.Semantics.Paths;

internal static class ImageHasher
{
	private static readonly Lock ConsoleLock = new();

	internal static Dictionary<AbsoluteFilePath, string> HashFiles(IReadOnlyList<AbsoluteFilePath> filePaths)
	{
		ConcurrentDictionary<AbsoluteFilePath, string> results = new();

		Parallel.ForEach(filePaths, filePath =>
		{
			string hash = ComputeHash(filePath);
			results[filePath] = hash;

			lock (ConsoleLock)
			{
				Console.WriteLine($"  Hashed: {filePath.FileName} -> {hash[..12]}...");
			}
		});

		return new Dictionary<AbsoluteFilePath, string>(results);
	}

	internal static string ComputeHash(AbsoluteFilePath filePath)
	{
		using FileStream stream = File.OpenRead(filePath.WeakString);
		byte[] hashBytes = SHA256.HashData(stream);
		return Convert.ToHexStringLower(hashBytes);
	}
}
