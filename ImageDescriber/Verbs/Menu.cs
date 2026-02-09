// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImageDescriber.Verbs;

using System.Diagnostics;
using System.Linq;

using CommandLine;

using DustInTheWind.ConsoleTools.Controls;
using DustInTheWind.ConsoleTools.Controls.Menus;
using DustInTheWind.ConsoleTools.Controls.Menus.MenuItems;

[Verb("Menu", isDefault: true)]
internal sealed class Menu : BaseVerb<Menu>
{
	internal override void Run(Menu options)
	{
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

		LabelMenuItem[] menuItems = [.. Program.Verbs
			.Where(verb => verb != GetType())
			.Select(CreateMenuItem)];

		scrollMenu.AddItems(menuItems);

		while (true)
		{
			menuRepeater.Display();
		}
	}

	private static LabelMenuItem CreateMenuItem(Type verbType)
	{
		BaseVerb? verb = Activator.CreateInstance(verbType) as BaseVerb;
		Debug.Assert(verb != null);
		return new LabelMenuItem()
		{
			Text = verbType.Name,
			Command = verb,
			IsEnabled = true,
		};
	}
}
