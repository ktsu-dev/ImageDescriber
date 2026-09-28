// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber.Verbs;

using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

using CommandLine;

using DustInTheWind.ConsoleTools.Controls;
using DustInTheWind.ConsoleTools.Controls.Menus;
using DustInTheWind.ConsoleTools.Controls.Menus.MenuItems;

[Verb("Menu", isDefault: true)]
internal sealed class Menu : BaseVerb<Menu>
{
	internal override void Run(Menu options)
	{
		Type[] verbs = [.. Program.Verbs.Where(verb => verb != GetType())];

		// ScrollMenu reads Console.CursorVisible, which .NET only supports on Windows
		if (OperatingSystem.IsWindows())
		{
			RunScrollMenu(verbs);
		}
		else
		{
			RunPromptMenu(Console.In, Console.Out, [.. verbs.Select(verb => (GetMenuText(verb), (Action)(() => CreateVerb(verb).Execute())))]);
		}
	}

	internal static void RunPromptMenu(TextReader input, TextWriter output, IReadOnlyList<(string Text, Action Run)> items)
	{
		int exitChoice = items.Count + 1;

		while (true)
		{
			output.WriteLine();
			for (int i = 0; i < items.Count; i++)
			{
				output.WriteLine($"{i + 1}. {items[i].Text}");
			}

			output.WriteLine($"{exitChoice}. Exit");
			output.Write("Choose an option: ");

			string? line = input.ReadLine();
			if (line is null)
			{
				// End of input: nobody is left to choose, so stop rather than prompt forever
				output.WriteLine();
				return;
			}

			if (!int.TryParse(line.Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int choice) || choice < 1 || choice > exitChoice)
			{
				output.WriteLine($"Enter a number from 1 to {exitChoice}.");
				continue;
			}

			if (choice == exitChoice)
			{
				return;
			}

			items[choice - 1].Run();
		}
	}

	private static void RunScrollMenu(Type[] verbs)
	{
		bool exitRequested = false;

		ScrollMenu scrollMenu = new()
		{
			HorizontalAlignment = HorizontalAlignment.Left,
			ItemsHorizontalAlignment = HorizontalAlignment.Left,
			KeepHighlightingOnClose = true,
		};

		ControlRepeater menuRepeater = new()
		{
			Control = scrollMenu,
		};

		LabelMenuItem[] menuItems = [.. verbs.Select(CreateMenuItem)];

		scrollMenu.AddItems(menuItems);
		scrollMenu.AddItem(new LabelMenuItem()
		{
			Text = "Exit",
			Command = new ActionCommand(() => exitRequested = true),
			IsEnabled = true,
		});

		while (!exitRequested)
		{
			menuRepeater.Display();
		}
	}

	private sealed class ActionCommand(Action action) : ICommand
	{
		public bool IsActive => true;
		public void Execute() => action();
	}

	private static BaseVerb CreateVerb(Type verbType)
	{
		BaseVerb? verb = Activator.CreateInstance(verbType) as BaseVerb;
		Debug.Assert(verb != null);
		return verb;
	}

	private static string GetMenuText(Type verbType)
	{
		string name = verbType.Name;
		string? helpText = verbType.GetCustomAttribute<VerbAttribute>()?.HelpText;
		return string.IsNullOrEmpty(helpText) ? name : $"{name} - {helpText}";
	}

	private static LabelMenuItem CreateMenuItem(Type verbType) => new()
	{
		Text = GetMenuText(verbType),
		Command = CreateVerb(verbType),
		IsEnabled = true,
	};
}
