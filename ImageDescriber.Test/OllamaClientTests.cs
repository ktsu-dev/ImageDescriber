// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber.Tests;

using ktsu.ImageDescriber.Verbs;
using ktsu.Semantics.Strings;

[TestClass]
public class OllamaClientTests
{
	[TestMethod]
	[DataRow("localhost:11434")]
	[DataRow("http://localhost:11434")]
	[DataRow("http://localhost:11434/")]
	[DataRow("  http://localhost:11434//  ")]
	public void GenerateUriIsUnderTheEndpointWhateverFormItWasTypedIn(string endpoint)
	{
		Uri uri = OllamaClient.GetGenerateUri(endpoint.As<OllamaEndpoint>());

		Assert.AreEqual("http://localhost:11434/api/generate", uri.AbsoluteUri);
	}

	[TestMethod]
	public void GenerateUriKeepsAnEndpointPathPrefix()
	{
		Uri uri = OllamaClient.GetGenerateUri("https://example.com/ollama/".As<OllamaEndpoint>());

		Assert.AreEqual("https://example.com/ollama/api/generate", uri.AbsoluteUri);
	}

	[TestMethod]
	[DataRow("localhost:11434", "http://localhost:11434")]
	[DataRow("http://localhost:11434/", "http://localhost:11434")]
	[DataRow("https://ollama.example.com", "https://ollama.example.com")]
	[DataRow("192.168.1.5:11434", "http://192.168.1.5:11434")]
	public void NormalizeEndpointAddsAMissingSchemeAndDropsTrailingSlashes(string input, string expected) =>
		Assert.AreEqual(expected, OllamaClient.NormalizeEndpoint(input));

	[TestMethod]
	[DataRow("")]
	[DataRow("   ")]
	[DataRow("http://")]
	[DataRow("not a url")]
	[DataRow("ftp://localhost:11434")]
	[DataRow("http://localhost:11434/?x=1")]
	public void NormalizeEndpointRejectsAnythingButAnHttpAddress(string input) =>
		Assert.IsNull(OllamaClient.NormalizeEndpoint(input));

	[TestMethod]
	[DataRow("http://")]
	[DataRow("not a url")]
	public void IsAvailableReturnsFalseForAnInvalidEndpoint(string endpoint) =>
		Assert.IsFalse(OllamaClient.IsAvailableAsync(endpoint.As<OllamaEndpoint>()).GetAwaiter().GetResult());

	[TestMethod]
	public void ConfigureSavesTheNormalizedEndpoint()
	{
		using StringWriter output = new();

		OllamaEndpoint chosen = Configure.ChooseEndpoint(" localhost:11434/ ", "http://old:1".As<OllamaEndpoint>(), output);

		Assert.AreEqual("http://localhost:11434", chosen.WeakString);
		Assert.AreEqual(string.Empty, output.ToString());
	}

	[TestMethod]
	[DataRow(null)]
	[DataRow("   ")]
	public void ConfigureKeepsTheCurrentEndpointWhenNothingIsTyped(string? input)
	{
		using StringWriter output = new();

		OllamaEndpoint chosen = Configure.ChooseEndpoint(input, "http://old:1".As<OllamaEndpoint>(), output);

		Assert.AreEqual("http://old:1", chosen.WeakString);
	}

	[TestMethod]
	public void ConfigureKeepsTheCurrentEndpointAndSaysWhyWhenTheInputIsInvalid()
	{
		using StringWriter output = new();

		OllamaEndpoint chosen = Configure.ChooseEndpoint("not a url", "http://old:1".As<OllamaEndpoint>(), output);

		Assert.AreEqual("http://old:1", chosen.WeakString);
		Assert.Contains("\"not a url\" is not an http or https address. Keeping http://old:1.", output.ToString());
	}

	[TestMethod]
	public void ReadAnswerReturnsAFinishedAnswer() =>
		Assert.AreEqual("a dog", OllamaClient.ReadAnswer(new OllamaResponse { Response = "a dog", DoneReason = "stop" }));

	[TestMethod]
	public void ReadAnswerAcceptsAnAnswerWithNoDoneReason() =>
		Assert.AreEqual("a dog", OllamaClient.ReadAnswer(new OllamaResponse { Response = "a dog" }));

	[TestMethod]
	[DataRow("")]
	[DataRow("  \n\t")]
	public void ReadAnswerRejectsAnEmptyAnswer(string answer) =>
		Assert.ThrowsExactly<InvalidDataException>(() => OllamaClient.ReadAnswer(new OllamaResponse { Response = answer, DoneReason = "stop" }));

	[TestMethod]
	public void ReadAnswerRejectsAMissingResponse() =>
		Assert.ThrowsExactly<InvalidDataException>(() => OllamaClient.ReadAnswer(null));

	[TestMethod]
	public void ReadAnswerRejectsAnAnswerCutOffAtTheTokenLimit() =>
		Assert.ThrowsExactly<InvalidDataException>(() => OllamaClient.ReadAnswer(new OllamaResponse { Response = "A dog on a", DoneReason = "length" }));

	[TestMethod]
	[DataRow("http://127.0.0.1:1")]
	[DataRow("not a url")]
	public void IsModelInstalledIsUnknownWhenOllamaCantBeAsked(string endpoint) =>
		Assert.IsNull(OllamaClient.IsModelInstalledAsync(endpoint.As<OllamaEndpoint>(), "llava".As<OllamaModelName>()).GetAwaiter().GetResult());

	[TestMethod]
	public void ReadErrorReturnsOllamasErrorText() =>
		Assert.AreEqual("model \"llava\" not found, try pulling it first", OllamaClient.ReadError("{\"error\":\" model \\\"llava\\\" not found, try pulling it first \"}"));

	[TestMethod]
	[DataRow("<html>Bad Gateway</html>")]
	[DataRow("")]
	[DataRow("{}")]
	[DataRow("{\"error\":\"  \"}")]
	public void ReadErrorIsNullForABodyWithNoErrorText(string body) =>
		Assert.IsNull(OllamaClient.ReadError(body));
}
