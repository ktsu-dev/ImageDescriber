// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

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

	internal string Endpoint => string.IsNullOrEmpty(EndpointString) ? Program.Settings.OllamaEndpoint : EndpointString;

	internal string Model => string.IsNullOrEmpty(ModelString) ? Program.Settings.OllamaModel : ModelString;

	public abstract void Run();

	internal virtual bool ValidateArgs() => true;

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
		Run((T)this);
		isActive = true;
	}

	internal abstract void Run(T options);
}
