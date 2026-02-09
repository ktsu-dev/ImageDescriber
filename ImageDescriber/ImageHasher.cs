// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImageDescriber;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

internal static class ImageHasher
{
	private static readonly Lock ConsoleLock = new();

	internal static Dictionary<string, string> HashFiles(IReadOnlyList<string> filePaths)
	{
		ConcurrentDictionary<string, string> results = new(StringComparer.Ordinal);

		Parallel.ForEach(filePaths, filePath =>
		{
			string hash = ComputeHash(filePath);
			results[filePath] = hash;

			lock (ConsoleLock)
			{
				Console.WriteLine($"  Hashed: {Path.GetFileName(filePath)} -> {hash[..12]}...");
			}
		});

		return new Dictionary<string, string>(results, StringComparer.Ordinal);
	}

	internal static string ComputeHash(string filePath)
	{
		using FileStream stream = File.OpenRead(filePath);
		byte[] hashBytes = SHA256.HashData(stream);
		return Convert.ToHexStringLower(hashBytes);
	}
}
