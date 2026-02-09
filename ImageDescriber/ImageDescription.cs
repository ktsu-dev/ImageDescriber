// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImageDescriber;

internal sealed class ImageDescription
{
	public string Hash { get; set; } = string.Empty;
	public string FilePath { get; set; } = string.Empty;
	public string FileName { get; set; } = string.Empty;
	public string Description { get; set; } = string.Empty;
	public string Model { get; set; } = string.Empty;
	public DateTime DescribedAt { get; set; } = DateTime.UtcNow;
	public long FileSizeBytes { get; set; }
}
