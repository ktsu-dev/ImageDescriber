// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImageDescriber;

using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

internal sealed class ImageDescription
{
	public string Hash { get; set; } = string.Empty;
	public AbsoluteFilePath FilePath { get; set; } = string.Empty.As<AbsoluteFilePath>();
	public FileName FileName { get; set; } = string.Empty.As<FileName>();
	public string Description { get; set; } = string.Empty;
	public FileName SuggestedFileName { get; set; } = string.Empty.As<FileName>();
	public OllamaModelName Model { get; set; } = string.Empty.As<OllamaModelName>();
	public DateTime DescribedAt { get; set; } = DateTime.UtcNow;
	public long FileSizeBytes { get; set; }
}
