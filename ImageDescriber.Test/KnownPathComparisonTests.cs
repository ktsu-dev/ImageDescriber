// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber.Tests;

using ktsu.ImageDescriber.Verbs;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

[TestClass]
[DoNotParallelize]
public class KnownPathComparisonTests
{
	private static readonly bool PlatformIgnoresCase = KnownPathComparison.IgnoreCase;

	[TestCleanup]
	public void RestoreComparison() => KnownPathComparison.IgnoreCase = PlatformIgnoresCase;

	private static AbsoluteFilePath PathIn(string folder, string file) =>
		Path.Combine(Path.GetTempPath(), folder, file).As<AbsoluteFilePath>();

	[TestMethod]
	public void IgnoresCaseOnlyOnWindows() =>
		Assert.AreEqual(OperatingSystem.IsWindows(), PlatformIgnoresCase);

	[TestMethod]
	public void PathsDifferingOnlyInCaseAreTheSameWhereCaseIsIgnored()
	{
		KnownPathComparison.IgnoreCase = true;

		Assert.IsTrue(KnownPathComparison.AreSame(PathIn("Photos", "Sunset.JPG"), PathIn("photos", "sunset.jpg")));
		Assert.IsFalse(KnownPathComparison.AreSame(PathIn("Photos", "a.jpg"), PathIn("Photos", "b.jpg")));
	}

	[TestMethod]
	public void PathsDifferingOnlyInCaseAreDifferentWhereCaseMatters()
	{
		KnownPathComparison.IgnoreCase = false;

		Assert.IsFalse(KnownPathComparison.AreSame(PathIn("Photos", "Sunset.JPG"), PathIn("photos", "sunset.jpg")));
		Assert.IsTrue(KnownPathComparison.AreSame(PathIn("Photos", "a.jpg"), PathIn("Photos", "a.jpg")));
	}

	[TestMethod]
	public void ContainsSameFindsAPathTypedWithDifferentCase()
	{
		KnownPathComparison.IgnoreCase = true;
		List<AbsoluteFilePath> known = [PathIn("Photos", "a.jpg")];

		Assert.IsTrue(KnownPathComparison.ContainsSame(known, PathIn("photos", "A.jpg")));
		Assert.IsFalse(KnownPathComparison.ContainsSame(known, PathIn("photos", "b.jpg")));
	}

	[TestMethod]
	public void MergeKnownPathsAddsNoPathThatDiffersOnlyInCaseWhereCaseIsIgnored()
	{
		KnownPathComparison.IgnoreCase = true;
		ImageDescription source = new() { KnownPaths = [PathIn("photos", "a.jpg"), PathIn("PHOTOS", "B.jpg")] };
		ImageDescription target = new() { KnownPaths = [PathIn("Photos", "a.jpg"), PathIn("Photos", "b.jpg")] };

		bool added = Import.MergeKnownPaths(source, target);

		Assert.IsFalse(added);
		Assert.AreEqual(2, target.KnownPaths.Count);
	}

	[TestMethod]
	public void MergeKnownPathsKeepsPathsThatDifferOnlyInCaseWhereCaseMatters()
	{
		KnownPathComparison.IgnoreCase = false;
		ImageDescription source = new() { KnownPaths = [PathIn("photos", "a.jpg")] };
		ImageDescription target = new() { KnownPaths = [PathIn("Photos", "a.jpg")] };

		bool added = Import.MergeKnownPaths(source, target);

		Assert.IsTrue(added);
		Assert.AreEqual(2, target.KnownPaths.Count);
	}

	[TestMethod]
	public void ScanRecordsNoNewPathForARootTypedWithDifferentCaseWhereCaseIsIgnored()
	{
		KnownPathComparison.IgnoreCase = true;
		ImageDescription existing = new() { KnownPaths = [PathIn("Photos", "a.jpg")] };

		Assert.IsFalse(Scan.RecordKnownPath(existing, PathIn("photos", "a.jpg")));
		Assert.AreEqual(1, existing.KnownPaths.Count);

		Assert.IsTrue(Scan.RecordKnownPath(existing, PathIn("photos", "b.jpg")));
		Assert.AreEqual(2, existing.KnownPaths.Count);
	}

	[TestMethod]
	public void ScanRecordsAPathThatDiffersOnlyInCaseWhereCaseMatters()
	{
		KnownPathComparison.IgnoreCase = false;
		ImageDescription existing = new() { KnownPaths = [PathIn("Photos", "a.jpg")] };

		Assert.IsTrue(Scan.RecordKnownPath(existing, PathIn("photos", "a.jpg")));
		Assert.AreEqual(2, existing.KnownPaths.Count);
	}
}
