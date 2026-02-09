// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImageDescriber;

using System.Collections.Generic;

using ktsu.AppDataStorage;

internal sealed class PersistentState : AppData<PersistentState>
{
	public Dictionary<string, ImageDescription> Descriptions { get; set; } = [];
	public string OllamaEndpoint { get; set; } = "http://localhost:11434";
	public string OllamaModel { get; set; } = "llama3.2-vision";
	public string DescriptionPrompt { get; set; } = "Describe this image in detail. Include information about the subject, colors, composition, and any text visible in the image.";
}
