// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

[assembly: CLSCompliant(false)]
[assembly: System.Runtime.InteropServices.ComVisible(false)]
[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("ImageDescriber.Test")]

namespace ktsu.ImageDescriber;

using System.Reflection;
using System.Text;

using CommandLine;

using ktsu.ImageDescriber.Verbs;

internal static class Program
{
	internal static Type[] Verbs { get; } = LoadVerbs();
	internal static PersistentState Settings { get; set; } = new();

	private static void Main(string[] args)
	{
		Console.OutputEncoding = Encoding.UTF8;
		Settings = PersistentState.LoadOrCreate();

		_ = Parser.Default.ParseArguments(args, Verbs)
			.WithParsed<BaseVerb>(task => task.Run());
	}

	private static Type[] LoadVerbs() => [.. Assembly.GetExecutingAssembly().GetTypes().Where(t => t.GetCustomAttribute<VerbAttribute>() != null)];
}
