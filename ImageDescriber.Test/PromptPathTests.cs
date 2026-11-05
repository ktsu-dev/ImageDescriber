// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber.Tests;

using ktsu.ImageDescriber.Verbs;

[TestClass]
public class PromptPathTests
{
	[TestMethod]
	[DataRow("\"C:\\My Photos\"", "C:\\My Photos")]
	[DataRow("  \"/tmp/my photos\"  ", "/tmp/my photos")]
	[DataRow("'/tmp/my photos'", "/tmp/my photos")]
	[DataRow("\" /tmp \"", "/tmp")]
	[DataRow("/tmp/photos", "/tmp/photos")]
	[DataRow("  /tmp/photos  ", "/tmp/photos")]
	[DataRow("\"", "\"")]
	[DataRow("\"/tmp/unbalanced", "\"/tmp/unbalanced")]
	[DataRow("\"/tmp/mixed'", "\"/tmp/mixed'")]
	[DataRow("", "")]
	public void CleanPromptPathRemovesOneSurroundingPairOfQuotes(string input, string expected) =>
		Assert.AreEqual(expected, BaseVerb.CleanPromptPath(input));

	[TestMethod]
	public void CleanPromptPathKeepsNull() => Assert.IsNull(BaseVerb.CleanPromptPath(null));

	[TestMethod]
	public void ScanPromptAcceptsAQuotedFolder()
	{
		string folder = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
		Scan scan = new();

		(bool valid, string output) = ValidateWithInput(scan.ValidateArgs, $"\"{folder}\"\n");

		Assert.IsTrue(valid, output);
		Assert.AreEqual(folder, scan.PathString);
		Assert.AreEqual(Path.GetFullPath(folder), scan.Path.WeakString.TrimEnd(Path.DirectorySeparatorChar));
	}

	[TestMethod]
	public void ExportPromptAcceptsAQuotedFile()
	{
		string file = Path.Combine(Path.GetTempPath(), "my export.json");
		Export export = new();

		(bool valid, string output) = ValidateWithInput(export.ValidateArgs, $"\"{file}\"\n");

		Assert.IsTrue(valid, output);
		Assert.AreEqual(file, export.OutputPath);
	}

	[TestMethod]
	public void ImportPromptAcceptsAQuotedFile()
	{
		string file = Path.Combine(Path.GetTempPath(), "my export.csv");
		Import import = new();

		(bool valid, string output) = ValidateWithInput(import.ValidateArgs, $"\"{file}\"\n");

		Assert.IsTrue(valid, output);
		Assert.AreEqual(file, import.InputPath);
	}

	private static (bool Valid, string Output) ValidateWithInput(Func<bool> validate, string input)
	{
		TextReader originalIn = Console.In;
		TextWriter originalOut = Console.Out;
		using StringReader reader = new(input);
		using StringWriter output = new();
		Console.SetIn(reader);
		Console.SetOut(output);
		try
		{
			return (validate(), output.ToString());
		}
		finally
		{
			Console.SetIn(originalIn);
			Console.SetOut(originalOut);
		}
	}
}
