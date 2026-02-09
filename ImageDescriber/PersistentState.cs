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
	public string DescriptionPrompt { get; set; } = "Describe this image in plain prose. Write only the description itself with no labels, field names, headings, bullet points, or commentary. Do not start with phrases like 'This image shows' or 'The image depicts'.";
	public string SuggestedFileNamePrompt { get; set; } = "Based on the image description above, suggest a short descriptive filename. Respond with only the filename without extension, path, or explanation. Use lowercase words separated by hyphens.";
}
