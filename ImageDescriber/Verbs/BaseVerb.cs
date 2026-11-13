// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber.Verbs;

using System.Collections.Generic;
using System.Linq;

using CommandLine;

using DustInTheWind.ConsoleTools.Controls.Menus;

using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

internal abstract class BaseVerb : ICommand
{
	[Option('p', "path", Required = false, HelpText = "The root path to scan for images.")]
	public string PathString { get; set; } = string.Empty;

	[Option('e', "endpoint", Required = false, HelpText = "The Ollama API endpoint URL.")]
	public string EndpointString { get; set; } = string.Empty;

	[Option('m', "model", Required = false, HelpText = "The Ollama vision model to use.")]
	public string ModelString { get; set; } = string.Empty;

	/// <summary>
	/// Words no verb or option claims, such as a misspelled verb that fell back to the default
	/// <see cref="Menu"/>, or the second half of an unquoted path. Any of these fails the run.
	/// </summary>
	[Value(0, Hidden = true)]
	public IEnumerable<string> UnclaimedArguments { get; set; } = [];

	public abstract bool IsActive { get; }

	/// <summary>Whether the last run failed, which makes the process exit non-zero.</summary>
	internal bool Failed { get; set; }

	// An empty PathString means no -p was given, so a verb that doesn't ask for one uses the current directory
	internal AbsoluteDirectoryPath Path => System.IO.Path.GetFullPath(PathOrCurrentDirectory).As<AbsoluteDirectoryPath>();

	private string PathOrCurrentDirectory => string.IsNullOrWhiteSpace(PathString) ? "." : PathString;

	/// <summary>
	/// Converts the -p path like <see cref="Path"/> does, but prints why and returns null instead of throwing
	/// when the path can't be represented, such as one containing | &lt; &gt; or longer than 256 characters.
	/// </summary>
	internal AbsoluteDirectoryPath? TryGetPath() => TryConvertUserPath(PathOrCurrentDirectory, static p => p.As<AbsoluteDirectoryPath>());

	/// <summary>
	/// Converts a path the user typed into a semantic path type. ktsu.Semantics.Paths rejects some legal paths
	/// (| &lt; &gt; on any OS, or over 256 characters), so print why and return null instead of letting the
	/// <see cref="ArgumentException"/> end the process or the interactive menu.
	/// </summary>
	internal static TPath? TryConvertUserPath<TPath>(string pathString, Func<string, TPath> convert) where TPath : class
	{
		try
		{
			return convert(System.IO.Path.GetFullPath(pathString));
		}
		catch (Exception ex) when (ex is ArgumentException or PathTooLongException or NotSupportedException)
		{
			Console.WriteLine($"Unsupported path: {pathString}: {ex.Message}");
			return null;
		}
	}

	/// <summary>
	/// Reads a path typed or pasted at an interactive prompt. Explorer's "Copy as path" and dragging a
	/// folder into the console both wrap the path in double quotes, which a shell would strip but
	/// Console.ReadLine keeps, so remove one surrounding pair of matching quotes along with the whitespace.
	/// </summary>
	internal static string? ReadPromptPath() => CleanPromptPath(Console.ReadLine());

	/// <inheritdoc cref="ReadPromptPath"/>
	internal static string? CleanPromptPath(string? input)
	{
		string? path = input?.Trim();
		if (path is { Length: >= 2 } && (path[0] is '"' or '\'') && path[^1] == path[0])
		{
			path = path[1..^1].Trim();
		}

		return path;
	}

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
		Failed = false;
		string? unclaimed = UnclaimedArguments.FirstOrDefault();
		if (unclaimed is not null)
		{
			Console.Error.WriteLine($"Unknown verb or argument '{unclaimed}'. Quote a path that contains spaces.");
			Failed = true;
			return;
		}

		if (!ValidateArgs())
		{
			Failed = true;
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
			// The verb did nothing, so a script or scheduler has to see a failure, not success
			Console.WriteLine(StoreLock.BusyMessage);
			Failed = true;
			return;
		}

		// Another run may have saved since this process loaded the store, and this run's saves
		// write back the whole copy, so start from what is on disk now.
		Program.Settings = PersistentState.LoadOrCreate();
		Run((T)this);
	}

	internal abstract void Run(T options);
}
