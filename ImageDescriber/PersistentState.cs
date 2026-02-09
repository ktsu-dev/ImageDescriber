// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImageDescriber;

using System.Collections.Generic;

using ktsu.AppDataStorage;
using ktsu.Semantics.Strings;

internal sealed class PersistentState : AppData<PersistentState>
{
	public Dictionary<string, ImageDescription> Descriptions { get; set; } = [];
	public OllamaEndpoint OllamaEndpoint { get; set; } = "http://localhost:11434".As<OllamaEndpoint>();
	public OllamaModelName OllamaModel { get; set; } = "llama3.2-vision".As<OllamaModelName>();
	public string DescriptionPrompt { get; set; } = "Describe this image in detail. Include information about the subject, colors, composition, and any text visible in the image.";
}
