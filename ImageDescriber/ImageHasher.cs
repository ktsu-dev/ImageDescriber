// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber;

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
			string hash;
			try
			{
				hash = ComputeHash(filePath);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				// One unreadable, locked or dangling file must not abort hashing the rest.
				lock (ConsoleLock)
				{
					Console.WriteLine($"  Warning: could not hash {filePath}: {ex.Message}");
				}

				return;
			}

			results[filePath] = hash;

			lock (ConsoleLock)
			{
				Console.WriteLine($"  Hashed: {filePath.FileName} -> {hash[..12]}...");
			}
		});

		return new Dictionary<AbsoluteFilePath, string>(results);
	}

	internal const int HashLength = 64;

	internal static bool IsValidHash(string? hash) =>
		hash is { Length: HashLength } && hash.All(char.IsAsciiHexDigit);

	internal static string ShortHash(string hash) => hash[..Math.Min(12, hash.Length)];

	internal static string ComputeHash(AbsoluteFilePath filePath)
	{
		using FileStream stream = File.OpenRead(filePath.WeakString);
		byte[] hashBytes = SHA256.HashData(stream);
		return Convert.ToHexStringLower(hashBytes);
	}
}
