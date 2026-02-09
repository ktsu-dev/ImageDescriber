// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImageDescriber.Verbs;

using CommandLine;

[Verb("Configure", HelpText = "Configure the Ollama endpoint and model settings.")]
internal sealed class Configure : BaseVerb<Configure>
{
	internal override void Run(Configure options)
	{
		Console.WriteLine("Current Settings:");
		Console.WriteLine($"  Endpoint: {Program.Settings.OllamaEndpoint}");
		Console.WriteLine($"  Model:    {Program.Settings.OllamaModel}");
		Console.WriteLine($"  Prompt:   {Program.Settings.DescriptionPrompt[..Math.Min(60, Program.Settings.DescriptionPrompt.Length)]}...");
		Console.WriteLine();

		Console.Write($"Ollama Endpoint [{Program.Settings.OllamaEndpoint}]: ");
		string? endpointInput = Console.ReadLine();
		if (!string.IsNullOrWhiteSpace(endpointInput))
		{
			Program.Settings.OllamaEndpoint = endpointInput.Trim();
		}

		Console.Write($"Ollama Model [{Program.Settings.OllamaModel}]: ");
		string? modelInput = Console.ReadLine();
		if (!string.IsNullOrWhiteSpace(modelInput))
		{
			Program.Settings.OllamaModel = modelInput.Trim();
		}

		Console.Write($"Description Prompt [{Program.Settings.DescriptionPrompt[..Math.Min(60, Program.Settings.DescriptionPrompt.Length)]}...]: ");
		string? promptInput = Console.ReadLine();
		if (!string.IsNullOrWhiteSpace(promptInput))
		{
			Program.Settings.DescriptionPrompt = promptInput.Trim();
		}

		Program.Settings.Save();

		Console.WriteLine();
		Console.WriteLine("Settings saved.");
		Console.WriteLine($"  Endpoint: {Program.Settings.OllamaEndpoint}");
		Console.WriteLine($"  Model:    {Program.Settings.OllamaModel}");
		Console.WriteLine($"  Prompt:   {Program.Settings.DescriptionPrompt[..Math.Min(60, Program.Settings.DescriptionPrompt.Length)]}...");
	}
}
