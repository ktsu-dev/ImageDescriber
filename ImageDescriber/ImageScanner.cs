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
		".tiff".As<FileExtension>(),
		".tif".As<FileExtension>(),
	];

	internal static IReadOnlyList<AbsoluteFilePath> ScanForImages(AbsoluteDirectoryPath path)
	{
		if (!path.Exists)
		{
			Console.WriteLine($"Directory not found: {path}");
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
		foreach (string file in Directory.EnumerateFiles(path.WeakString, "*", options))
		{
			string ext = Path.GetExtension(file);
			if (string.IsNullOrEmpty(ext))
			{
				continue;
			}

			// FileExtension compares ordinally, and cameras and Windows tools write .JPG/.PNG,
			// so fold to the lower-case form the set holds before looking it up.
			if (ImageExtensions.Contains(ext.ToLowerInvariant().As<FileExtension>()))
			{
				imageFiles.Add(file.As<AbsoluteFilePath>());
			}
		}

		return imageFiles;
	}
}
