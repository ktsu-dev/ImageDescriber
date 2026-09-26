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

		List<AbsoluteFilePath> imageFiles = [];
		foreach (string file in Directory.EnumerateFiles(path.WeakString, "*", SearchOption.AllDirectories))
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
