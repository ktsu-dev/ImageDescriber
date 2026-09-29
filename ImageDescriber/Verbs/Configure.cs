// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber.Verbs;

using System.IO;

using CommandLine;

using ktsu.Semantics.Strings;

[Verb("Configure", HelpText = "Configure the Ollama endpoint and model settings.")]
internal sealed class Configure : BaseVerb<Configure>
{
	internal override void Run(Configure options)
	{
		Console.WriteLine("Current Settings:");
		Console.WriteLine($"  Endpoint:        {Program.Settings.OllamaEndpoint}");
		Console.WriteLine($"  Model:           {Program.Settings.OllamaModel}");
		Console.WriteLine($"  Concurrency:     {Program.Settings.MaxConcurrentRequests}");
		Console.WriteLine($"  Prompt:          {Program.Settings.DescriptionPrompt[..Math.Min(60, Program.Settings.DescriptionPrompt.Length)]}...");
		Console.WriteLine($"  Filename Prompt: {Program.Settings.SuggestedFileNamePrompt[..Math.Min(60, Program.Settings.SuggestedFileNamePrompt.Length)]}...");
		Console.WriteLine();

		Console.Write($"Ollama Endpoint [{Program.Settings.OllamaEndpoint}]: ");
		string? endpointInput = Console.ReadLine();
		Program.Settings.OllamaEndpoint = ChooseEndpoint(endpointInput, Program.Settings.OllamaEndpoint, Console.Out);

		Console.Write($"Ollama Model [{Program.Settings.OllamaModel}]: ");
		string? modelInput = Console.ReadLine();
		if (!string.IsNullOrWhiteSpace(modelInput))
		{
			Program.Settings.OllamaModel = modelInput.Trim().As<OllamaModelName>();
		}

		Console.Write($"Max Concurrent Requests [{Program.Settings.MaxConcurrentRequests}]: ");
		string? concurrencyInput = Console.ReadLine();
		if (!string.IsNullOrWhiteSpace(concurrencyInput) && int.TryParse(concurrencyInput.Trim(), out int concurrency) && concurrency >= 1)
		{
			Program.Settings.MaxConcurrentRequests = concurrency;
		}

		Console.Write($"Description Prompt [{Program.Settings.DescriptionPrompt[..Math.Min(60, Program.Settings.DescriptionPrompt.Length)]}...]: ");
		string? promptInput = Console.ReadLine();
		if (!string.IsNullOrWhiteSpace(promptInput))
		{
			Program.Settings.DescriptionPrompt = promptInput.Trim();
		}

		Console.Write($"Filename Prompt [{Program.Settings.SuggestedFileNamePrompt[..Math.Min(60, Program.Settings.SuggestedFileNamePrompt.Length)]}...]: ");
		string? fileNamePromptInput = Console.ReadLine();
		if (!string.IsNullOrWhiteSpace(fileNamePromptInput))
		{
			Program.Settings.SuggestedFileNamePrompt = fileNamePromptInput.Trim();
		}

		Program.Settings.Save();

		Console.WriteLine();
		Console.WriteLine("Settings saved.");
		Console.WriteLine($"  Endpoint:        {Program.Settings.OllamaEndpoint}");
		Console.WriteLine($"  Model:           {Program.Settings.OllamaModel}");
		Console.WriteLine($"  Concurrency:     {Program.Settings.MaxConcurrentRequests}");
		Console.WriteLine($"  Prompt:          {Program.Settings.DescriptionPrompt[..Math.Min(60, Program.Settings.DescriptionPrompt.Length)]}...");
		Console.WriteLine($"  Filename Prompt: {Program.Settings.SuggestedFileNamePrompt[..Math.Min(60, Program.Settings.SuggestedFileNamePrompt.Length)]}...");
	}

	/// <summary>
	/// Returns the normalized endpoint the user typed, or <paramref name="current"/> when they
	/// typed nothing or something that isn't an http or https address.
	/// </summary>
	internal static OllamaEndpoint ChooseEndpoint(string? input, OllamaEndpoint current, TextWriter output)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			return current;
		}

		string? endpoint = OllamaClient.NormalizeEndpoint(input);
		if (endpoint is null)
		{
			output.WriteLine($"  \"{input.Trim()}\" is not an http or https address. Keeping {current}.");
			return current;
		}

		return endpoint.As<OllamaEndpoint>();
	}
}
