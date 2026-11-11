// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber;

using System.Collections.Generic;
using System.Linq;

using ktsu.Semantics.Paths;

/// <summary>
/// Decides whether two known paths name the same file, the way the file system that holds them does.
/// </summary>
/// <remarks>
/// <see cref="AbsoluteFilePath"/> equality is ordinal, but a Windows file system ignores letter case,
/// so <c>C:\Photos\a.jpg</c> and <c>c:\photos\a.jpg</c> are one file there. Comparing them ordinally
/// recorded every image again as a new path whenever a folder was typed with different casing (#191).
/// </remarks>
internal static class KnownPathComparison
{
	/// <summary>
	/// Gets or sets a value indicating whether paths that differ only in letter case name the same
	/// file. True on Windows; settable so the comparison can be tested on any platform.
	/// </summary>
	internal static bool IgnoreCase { get; set; } = OperatingSystem.IsWindows();

	/// <summary>
	/// Reports whether two paths name the same file.
	/// </summary>
	/// <param name="left">The first path.</param>
	/// <param name="right">The second path.</param>
	/// <returns>True when they are equal, ignoring letter case where <see cref="IgnoreCase"/> says to.</returns>
	internal static bool AreSame(AbsoluteFilePath left, AbsoluteFilePath right) =>
		string.Equals(
			left.WeakString,
			right.WeakString,
			IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);

	/// <summary>
	/// Reports whether a list of known paths already holds a path naming the same file.
	/// </summary>
	/// <param name="paths">The known paths.</param>
	/// <param name="path">The path to look for.</param>
	/// <returns>True when one of <paramref name="paths"/> names the same file as <paramref name="path"/>.</returns>
	internal static bool ContainsSame(IEnumerable<AbsoluteFilePath> paths, AbsoluteFilePath path) =>
		paths.Any(known => AreSame(known, path));
}
