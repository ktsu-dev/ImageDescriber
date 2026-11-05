// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber.Verbs;

using System.IO;

using CommandLine;

using ktsu.Semantics.Strings;

[Verb("Configure", HelpText = "Configure the Ollama endpoint and model settings.")]
internal sealed class Configure : BaseVerb<Configure>
{
	// The prompts can wait on the user indefinitely, so Configure takes StoreLock only around the
	// reload and save in Apply, rather than through BaseVerb for the whole run
	internal override bool WritesStore => false;

	internal override void Run(Configure options)
	{
		PersistentState current = Program.Settings;
		Console.WriteLine("Current Settings:");
		PrintSettings(current);
		Console.WriteLine();

		Console.Write($"Ollama Endpoint [{current.OllamaEndpoint}]: ");
		string? endpointInput = Console.ReadLine();
		OllamaEndpoint endpoint = ChooseEndpoint(endpointInput, current.OllamaEndpoint, Console.Out);
		OllamaEndpoint? newEndpoint = endpoint == current.OllamaEndpoint ? null : endpoint;

		Console.Write($"Ollama Model [{current.OllamaModel}]: ");
		string? modelInput = Console.ReadLine();
		OllamaModelName? newModel = string.IsNullOrWhiteSpace(modelInput) ? null : modelInput.Trim().As<OllamaModelName>();

		Console.Write($"Max Concurrent Requests [{current.MaxConcurrentRequests}]: ");
		string? concurrencyInput = Console.ReadLine();
		int? newConcurrency = !string.IsNullOrWhiteSpace(concurrencyInput) && int.TryParse(concurrencyInput.Trim(), out int concurrency) && concurrency >= 1
			? concurrency
			: null;

		Console.Write($"Description Prompt [{Abbreviate(current.DescriptionPrompt)}...]: ");
		string? promptInput = Console.ReadLine();
		string? newPrompt = string.IsNullOrWhiteSpace(promptInput) ? null : promptInput.Trim();

		Console.Write($"Filename Prompt [{Abbreviate(current.SuggestedFileNamePrompt)}...]: ");
		string? fileNamePromptInput = Console.ReadLine();
		string? newFileNamePrompt = string.IsNullOrWhiteSpace(fileNamePromptInput) ? null : fileNamePromptInput.Trim();

		if (newEndpoint is null && newModel is null && newConcurrency is null && newPrompt is null && newFileNamePrompt is null)
		{
			Console.WriteLine();
			Console.WriteLine("No changes.");
			return;
		}

		using StoreLock? storeLock = StoreLock.TryAcquire(StoreLock.StorePath);
		if (storeLock is null)
		{
			Console.WriteLine(StoreLock.BusyMessage);
			return;
		}

		// Another run may have saved while the prompts were open, and Save writes back the whole store,
		// so apply only the answered fields to what is on disk now
		PersistentState settings = PersistentState.LoadOrCreate();
		settings.OllamaEndpoint = newEndpoint ?? settings.OllamaEndpoint;
		settings.OllamaModel = newModel ?? settings.OllamaModel;
		settings.MaxConcurrentRequests = newConcurrency ?? settings.MaxConcurrentRequests;
		settings.DescriptionPrompt = newPrompt ?? settings.DescriptionPrompt;
		settings.SuggestedFileNamePrompt = newFileNamePrompt ?? settings.SuggestedFileNamePrompt;
		settings.Save();
		Program.Settings = settings;

		Console.WriteLine();
		Console.WriteLine("Settings saved.");
		PrintSettings(settings);
	}

	private static void PrintSettings(PersistentState settings)
	{
		Console.WriteLine($"  Endpoint:        {settings.OllamaEndpoint}");
		Console.WriteLine($"  Model:           {settings.OllamaModel}");
		Console.WriteLine($"  Concurrency:     {settings.MaxConcurrentRequests}");
		Console.WriteLine($"  Prompt:          {Abbreviate(settings.DescriptionPrompt)}...");
		Console.WriteLine($"  Filename Prompt: {Abbreviate(settings.SuggestedFileNamePrompt)}...");
	}

	private static string Abbreviate(string prompt) => prompt[..Math.Min(60, prompt.Length)];

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
