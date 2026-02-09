// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

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
			AbsoluteFilePath filePath = file.As<AbsoluteFilePath>();
			if (ImageExtensions.Contains(filePath.FileExtension))
			{
				imageFiles.Add(filePath);
			}
		}

		return imageFiles;
	}
}
