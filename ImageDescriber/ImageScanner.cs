// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber;

using System.Collections.Generic;
using System.IO;

using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

internal static class ImageScanner
{
	private static readonly HashSet<FileExtension> ImageExtensions =
	[
		".jpg".As<FileExtension>(),
		".jpeg".As<FileExtension>(),
		".png".As<FileExtension>(),
		".gif".As<FileExtension>(),
		".bmp".As<FileExtension>(),
		".webp".As<FileExtension>(),
	];

	// Ollama can't decode TIFF: its Go engine registers no TIFF decoder and stb_image has none, so a
	// TIFF sent to it fails, and failures aren't stored, so it would be sent and fail on every scan.
	private static readonly HashSet<FileExtension> UnsupportedImageExtensions =
	[
		".tiff".As<FileExtension>(),
		".tif".As<FileExtension>(),
	];

	internal static IReadOnlyList<AbsoluteFilePath> ScanForImages(AbsoluteDirectoryPath path)
	{
		string? problem = DescribeUnscannablePath(path);
		if (problem is not null)
		{
			Console.WriteLine(problem);
			return [];
		}

		// The SearchOption overload stops at the first folder it can't open, which on Windows is
		// any drive or profile root, so skip unreadable folders and scan the rest of the tree.
		EnumerationOptions options = new()
		{
			RecurseSubdirectories = true,
			IgnoreInaccessible = true,
		};

		List<AbsoluteFilePath> imageFiles = [];
		int unsupported = 0;
		foreach (string file in Directory.EnumerateFiles(path.WeakString, "*", options))
		{
			string extension = Path.GetExtension(file);
			if (IsImageExtension(extension))
			{
				imageFiles.Add(file.As<AbsoluteFilePath>());
			}
			else if (IsUnsupportedImageExtension(extension))
			{
				unsupported++;
			}
		}

		if (unsupported > 0)
		{
			Console.WriteLine($"Skipped {unsupported} TIFF image(s): Ollama can't decode TIFF. Convert them to PNG or JPEG to describe them.");
		}

		return imageFiles;
	}

	/// <summary>
	/// Returns why <paramref name="path"/> can't be scanned, or null when it is an existing directory.
	/// </summary>
	internal static string? DescribeUnscannablePath(AbsoluteDirectoryPath path)
	{
		// path.Exists is also true for a file, which Directory.EnumerateFiles then rejects with an
		// unhandled DirectoryNotFoundException, so tell a file apart from a directory first
		if (File.Exists(path.WeakString))
		{
			return $"Not a directory: {path}";
		}

		return Directory.Exists(path.WeakString) ? null : $"Directory not found: {path}";
	}

	internal static bool IsImageExtension(string extension)
	{
		if (string.IsNullOrEmpty(extension))
		{
			return false;
		}

		// FileExtension compares ordinally, and cameras and Windows tools write .JPG/.PNG,
		// so fold to the lower-case form the set holds before looking it up.
		return ImageExtensions.Contains(extension.ToLowerInvariant().As<FileExtension>());
	}

	internal static bool IsUnsupportedImageExtension(string extension) =>
		!string.IsNullOrEmpty(extension)
		&& UnsupportedImageExtensions.Contains(extension.ToLowerInvariant().As<FileExtension>());
}
