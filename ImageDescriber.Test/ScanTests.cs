// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber.Tests;

using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

using ktsu.ImageDescriber.Verbs;
using ktsu.Semantics.Paths;
using ktsu.Semantics.Strings;

[TestClass]
public class ScanTests
{
	[TestMethod]
	public void SanitizeFileNameTrimsWhitespaceAndQuotes()
	{
		FileName result = Scan.SanitizeFileName("  \"sunset-photo\"  ", ".jpg".As<FileExtension>());

		Assert.AreEqual("sunset-photo.jpg", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameTakesFirstLine()
	{
		FileName result = Scan.SanitizeFileName("first-line\nsecond-line", ".png".As<FileExtension>());

		Assert.AreEqual("first-line.png", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameStripsExistingExtension()
	{
		FileName result = Scan.SanitizeFileName("photo.png", ".jpg".As<FileExtension>());

		Assert.AreEqual("photo.jpg", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameCollapsesMultipleHyphens()
	{
		FileName result = Scan.SanitizeFileName("a---b----c", ".jpg".As<FileExtension>());

		Assert.AreEqual("a-b-c.jpg", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameReturnsUnnamedForEmpty()
	{
		FileName result = Scan.SanitizeFileName("", ".jpg".As<FileExtension>());

		Assert.AreEqual("unnamed.jpg", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameReturnsUnnamedForWhitespace()
	{
		FileName result = Scan.SanitizeFileName("   ", ".jpg".As<FileExtension>());

		Assert.AreEqual("unnamed.jpg", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameTrimsBackticks()
	{
		FileName result = Scan.SanitizeFileName("`my-photo`", ".png".As<FileExtension>());

		Assert.AreEqual("my-photo.png", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameTrimsLeadingTrailingHyphens()
	{
		FileName result = Scan.SanitizeFileName("-leading-trailing-", ".jpg".As<FileExtension>());

		Assert.AreEqual("leading-trailing.jpg", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameHandlesSimpleName()
	{
		FileName result = Scan.SanitizeFileName("sunset-over-ocean", ".webp".As<FileExtension>());

		Assert.AreEqual("sunset-over-ocean.webp", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameReplacesCharactersWindowsRejectsOnEveryPlatform()
	{
		// Path.GetInvalidFileNameChars() is only "\0" and "/" on Linux and macOS, so this used to
		// come back unchanged there and produce a name Windows cannot hold
		FileName result = Scan.SanitizeFileName("sunset at 5:30pm? <draft>", ".jpg".As<FileExtension>());

		Assert.AreEqual("sunset at 5-30pm- -draft.jpg", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameReplacesEveryWindowsReservedCharacterAndControlCharacter()
	{
		FileName result = Scan.SanitizeFileName("a<b>c:d\"e|f?g*h\\i\tj\u0001k", ".jpg".As<FileExtension>());

		Assert.AreEqual("a-b-c-d-e-f-g-h-i-j-k.jpg", result.WeakString);
	}

	[TestMethod]
	[DataRow("con")]
	[DataRow("NUL")]
	[DataRow("Com1")]
	[DataRow("lpt9")]
	public void SanitizeFileNameAvoidsWindowsDeviceNames(string suggestion)
	{
		FileName result = Scan.SanitizeFileName(suggestion, ".jpg".As<FileExtension>());

		Assert.AreEqual($"{suggestion}-image.jpg", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameKeepsNamesThatOnlyStartWithADeviceName()
	{
		FileName result = Scan.SanitizeFileName("console-on-desk", ".jpg".As<FileExtension>());

		Assert.AreEqual("console-on-desk.jpg", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameTakesNameFromInsideCodeFence()
	{
		FileName result = Scan.SanitizeFileName("```\nsunset-over-lake\n```", ".jpg".As<FileExtension>());

		Assert.AreEqual("sunset-over-lake.jpg", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameTakesNameFromInsideCodeFenceWithLanguage()
	{
		FileName result = Scan.SanitizeFileName("```text\r\nsunset-over-lake\r\n```", ".jpg".As<FileExtension>());

		Assert.AreEqual("sunset-over-lake.jpg", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameTakesNameFromInlineCodeFence()
	{
		FileName result = Scan.SanitizeFileName("```sunset-over-lake```", ".jpg".As<FileExtension>());

		Assert.AreEqual("sunset-over-lake.jpg", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameSkipsLeadingBlankLines()
	{
		FileName result = Scan.SanitizeFileName("\nsunset-over-lake\n", ".jpg".As<FileExtension>());

		Assert.AreEqual("sunset-over-lake.jpg", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameKeepsDotFollowedByText()
	{
		FileName result = Scan.SanitizeFileName("st.-louis-arch-at-dusk", ".jpg".As<FileExtension>());

		Assert.AreEqual("st.-louis-arch-at-dusk.jpg", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameKeepsDotInTime()
	{
		FileName result = Scan.SanitizeFileName("sunset-at-5.30pm", ".jpg".As<FileExtension>());

		Assert.AreEqual("sunset-at-5.30pm.jpg", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameStripsUpperCaseImageExtension()
	{
		FileName result = Scan.SanitizeFileName("photo.JPEG", ".png".As<FileExtension>());

		Assert.AreEqual("photo.png", result.WeakString);
	}

	[TestMethod]
	[DataRow("sunset-over-the-lake.", "sunset-over-the-lake.jpg")]
	[DataRow("Sunset over the lake.", "Sunset over the lake.jpg")]
	[DataRow("sunset-over-the-lake. ", "sunset-over-the-lake.jpg")]
	[DataRow("\"sunset-over-the-lake.\"", "sunset-over-the-lake.jpg")]
	public void SanitizeFileNameDropsTrailingPeriod(string suggestion, string expected)
	{
		FileName result = Scan.SanitizeFileName(suggestion, ".jpg".As<FileExtension>());

		Assert.AreEqual(expected, result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameStripsImageExtensionFollowedByPeriod()
	{
		FileName result = Scan.SanitizeFileName("photo.jpg.", ".png".As<FileExtension>());

		Assert.AreEqual("photo.png", result.WeakString);
	}

	[TestMethod]
	[DataRow("...")]
	[DataRow(". . .")]
	[DataRow("-.-")]
	public void SanitizeFileNameReturnsUnnamedForOnlyPeriods(string suggestion)
	{
		FileName result = Scan.SanitizeFileName(suggestion, ".jpg".As<FileExtension>());

		Assert.AreEqual("unnamed.jpg", result.WeakString);
	}

	[TestMethod]
	public void SanitizeFileNameDropsPeriodLeftBeforeReplacedCharacter()
	{
		FileName result = Scan.SanitizeFileName("sunset.?", ".jpg".As<FileExtension>());

		Assert.AreEqual("sunset.jpg", result.WeakString);
	}

	[TestMethod]
	public void HashFilesSkipsFilesThatCannotBeRead()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		try
		{
			AbsoluteFilePath real = Path.Combine(tempDir, "real.jpg").As<AbsoluteFilePath>();
			AbsoluteFilePath missing = Path.Combine(tempDir, "broken.jpg").As<AbsoluteFilePath>();
			File.WriteAllBytes(real.WeakString, [0xFF, 0xD8]);

			Dictionary<AbsoluteFilePath, string> hashes = ImageHasher.HashFiles([real, missing]);

			Assert.HasCount(1, hashes);
			Assert.IsTrue(hashes.ContainsKey(real));
		}
		finally
		{
			Directory.Delete(tempDir, true);
		}
	}

	[TestMethod]
	public void DescribeImagesContinuesPastPerImageFailures()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);

		using HttpListener listener = StartFakeOllama(out string endpoint);
		try
		{
			AbsoluteFilePath good = Path.Combine(tempDir, "good.jpg").As<AbsoluteFilePath>();
			AbsoluteFilePath bad = Path.Combine(tempDir, "bad.jpg").As<AbsoluteFilePath>();
			AbsoluteFilePath gone = Path.Combine(tempDir, "gone.jpg").As<AbsoluteFilePath>();
			File.WriteAllBytes(good.WeakString, [0xFF, 0xD8]);
			File.WriteAllBytes(bad.WeakString, [0xFF, 0xD8]);

			Dictionary<string, List<AbsoluteFilePath>> newHashPaths = new()
			{
				["good"] = [good],
				["bad"] = [bad],
				["gone"] = [gone],
			};
			ConcurrentBag<ImageDescription> stored = [];

			IReadOnlyList<string> failures = Scan.DescribeImages(
				newHashPaths,
				endpoint.As<OllamaEndpoint>(),
				"test-model".As<OllamaModelName>(),
				"Describe.",
				"Name it.",
				maxConcurrency: 1,
				stored.Add);

			Assert.HasCount(1, stored);
			Assert.AreEqual("good", stored.Single().Hash);
			Assert.AreEqual("a dog on a beach", stored.Single().Description);
			Assert.HasCount(2, failures);
			Assert.Contains(f => f.Contains("bad.jpg", StringComparison.Ordinal) && f.Contains(nameof(JsonException), StringComparison.Ordinal), failures);
			Assert.Contains(f => f.Contains("gone.jpg", StringComparison.Ordinal), failures);
		}
		finally
		{
			listener.Stop();
			Directory.Delete(tempDir, true);
		}
	}

	[TestMethod]
	public void ScanReportsFailedImagesAndCompletes()
	{
		string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
		Directory.CreateDirectory(tempDir);
		PersistentState originalSettings = Program.Settings;
		TextWriter originalOut = Console.Out;

		using HttpListener listener = StartFakeOllama(out string endpoint);
		using StringWriter output = new();
		try
		{
			// Every image fails, so nothing is described and nothing is saved to the real app data.
			File.WriteAllBytes(Path.Combine(tempDir, "bad.jpg"), [0xFF, 0xD8]);
			Program.Settings = new PersistentState();
			Console.SetOut(output);

			Scan scan = new() { PathString = tempDir, EndpointString = endpoint, ModelString = "test-model" };
			scan.Run(scan);
		}
		finally
		{
			Console.SetOut(originalOut);
			Program.Settings = originalSettings;
			listener.Stop();
			Directory.Delete(tempDir, true);
		}

		string text = output.ToString();
		Assert.Contains("Failed to describe 1 image(s):", text);
		Assert.Contains("bad.jpg: JsonException", text);
		Assert.Contains("Scan complete.", text);
	}

	[TestMethod]
	public void ScanReportsAnInvalidEndpointInsteadOfCrashing()
	{
		PersistentState originalSettings = Program.Settings;
		TextWriter originalOut = Console.Out;
		using StringWriter output = new();
		try
		{
			Program.Settings = new PersistentState();
			Console.SetOut(output);

			Scan scan = new() { PathString = Path.GetTempPath(), EndpointString = "not a url", ModelString = "test-model" };
			scan.Run(scan);
		}
		finally
		{
			Console.SetOut(originalOut);
			Program.Settings = originalSettings;
		}

		Assert.Contains("Error: Invalid Ollama endpoint \"not a url\"", output.ToString());
	}

	/// <summary>
	/// Serves /api/generate like Ollama, except that a request mentioning bad.jpg gets an HTML
	/// page, as a proxy or the wrong service on the endpoint would return.
	/// </summary>
	private static HttpListener StartFakeOllama(out string endpoint)
	{
		using TcpListener portFinder = new(IPAddress.Loopback, 0);
		portFinder.Start();
		int port = ((IPEndPoint)portFinder.LocalEndpoint).Port;
		portFinder.Stop();

		endpoint = $"http://localhost:{port}";
		HttpListener listener = new();
		listener.Prefixes.Add($"{endpoint}/");
		listener.Start();

		_ = Task.Run(async () =>
		{
			while (listener.IsListening)
			{
				HttpListenerContext context;
				try
				{
					context = await listener.GetContextAsync().ConfigureAwait(false);
				}
				catch (HttpListenerException)
				{
					return;
				}
				catch (ObjectDisposedException)
				{
					return;
				}

				using StreamReader reader = new(context.Request.InputStream);
				string body = await reader.ReadToEndAsync().ConfigureAwait(false);
				bool isBad = body.Contains("bad.jpg", StringComparison.Ordinal);
				context.Response.ContentType = isBad ? "text/html" : "application/json";
				byte[] response = Encoding.UTF8.GetBytes(isBad ? "<html>Not Ollama</html>" : "{\"response\":\"a dog on a beach\"}");
				await context.Response.OutputStream.WriteAsync(response).ConfigureAwait(false);
				context.Response.Close();
			}
		});

		return listener;
	}
}
