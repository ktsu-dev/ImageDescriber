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
	private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
	{
		".jpg",
		".jpeg",
		".png",
		".gif",
		".bmp",
		".webp",
		".tiff",
		".tif",
	};

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
			string extension = Path.GetExtension(file);
			if (ImageExtensions.Contains(extension))
			{
				imageFiles.Add(file.As<AbsoluteFilePath>());
			}
		}

		return imageFiles;
	}
}
