// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImageDescriber;

using System.Collections.Generic;
using System.IO;

using ktsu.Semantics.Paths;

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

	internal static IReadOnlyList<string> ScanForImages(AbsoluteDirectoryPath path)
	{
		string directoryPath = path.ToString();
		if (!Directory.Exists(directoryPath))
		{
			Console.WriteLine($"Directory not found: {directoryPath}");
			return [];
		}

		List<string> imageFiles = [];
		foreach (string file in Directory.EnumerateFiles(directoryPath, "*", SearchOption.AllDirectories))
		{
			string extension = Path.GetExtension(file);
			if (ImageExtensions.Contains(extension))
			{
				imageFiles.Add(file);
			}
		}

		return imageFiles;
	}
}
