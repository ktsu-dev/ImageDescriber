// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImageDescriber.Tests;

using ktsu.ImageDescriber.Verbs;

[TestClass]
public class StatsTests
{
	[TestMethod]
	public void FormatBytesReturnsBytes()
	{
		Assert.AreEqual("0 B", Stats.FormatBytes(0));
		Assert.AreEqual("1 B", Stats.FormatBytes(1));
		Assert.AreEqual("512 B", Stats.FormatBytes(512));
		Assert.AreEqual("1023 B", Stats.FormatBytes(1023));
	}

	[TestMethod]
	public void FormatBytesReturnsKilobytes()
	{
		Assert.AreEqual("1.0 KB", Stats.FormatBytes(1024));
		Assert.AreEqual("1.5 KB", Stats.FormatBytes(1536));
		Assert.AreEqual("1023.9 KB", Stats.FormatBytes((1024 * 1024) - 100));
	}

	[TestMethod]
	public void FormatBytesReturnsMegabytes()
	{
		Assert.AreEqual("1.0 MB", Stats.FormatBytes(1024L * 1024));
		Assert.AreEqual("10.0 MB", Stats.FormatBytes(1024L * 1024 * 10));
	}

	[TestMethod]
	public void FormatBytesReturnsGigabytes()
	{
		Assert.AreEqual("1.0 GB", Stats.FormatBytes(1024L * 1024 * 1024));
		Assert.AreEqual("2.5 GB", Stats.FormatBytes((long)(1024L * 1024 * 1024 * 2.5)));
	}
}
