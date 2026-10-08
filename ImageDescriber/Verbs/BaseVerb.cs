// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber.Verbs;

using CommandLine;

using DustInTheWind.ConsoleTools.Controls.Menus;

using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

internal abstract class BaseVerb : ICommand
{
	[Option('p', "path", Required = false, HelpText = "The root path to scan for images.")]
	public string PathString { get; set; } = ".";

	[Option('e', "endpoint", Required = false, HelpText = "The Ollama API endpoint URL.")]
	public string EndpointString { get; set; } = string.Empty;

	[Option('m', "model", Required = false, HelpText = "The Ollama vision model to use.")]
	public string ModelString { get; set; } = string.Empty;

	public abstract bool IsActive { get; }

	internal AbsoluteDirectoryPath Path => System.IO.Path.GetFullPath(PathString).As<AbsoluteDirectoryPath>();

	internal OllamaEndpoint Endpoint
	{
		get
		{
			string value = string.IsNullOrEmpty(EndpointString) ? Program.Settings.OllamaEndpoint.WeakString : EndpointString;
			return (OllamaClient.NormalizeEndpoint(value) ?? value).As<OllamaEndpoint>();
		}
	}

	internal OllamaModelName Model => string.IsNullOrEmpty(ModelString) ? Program.Settings.OllamaModel : ModelString.As<OllamaModelName>();

	public abstract void Run();

	internal virtual bool ValidateArgs() => true;

	/// <summary>
	/// Whether the verb saves the store, and so has to hold <see cref="StoreLock"/> while it runs.
	/// </summary>
	internal virtual bool WritesStore => false;

	public void Execute() => Run();
}

internal abstract class BaseVerb<T> : BaseVerb where T : BaseVerb<T>
{
	private bool isActive = true;
	public override bool IsActive => isActive;

	public override void Run()
	{
		if (!ValidateArgs())
		{
			return;
		}

		isActive = false;

		if (WritesStore)
		{
			RunHoldingStoreLock();
		}
		else
		{
			Run((T)this);
		}

		isActive = true;
	}

	private void RunHoldingStoreLock()
	{
		using StoreLock? storeLock = StoreLock.TryAcquire(StoreLock.StorePath);
		if (storeLock is null)
		{
			Console.WriteLine(StoreLock.BusyMessage);
			return;
		}

		// Another run may have saved since this process loaded the store, and this run's saves
		// write back the whole copy, so start from what is on disk now.
		Program.Settings = PersistentState.LoadOrCreate();
		Run((T)this);
	}

	internal abstract void Run(T options);
}
