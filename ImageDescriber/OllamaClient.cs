// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber;

using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

using ktsu.Semantics.Paths;

internal static class OllamaClient
{
	private static readonly HttpClient HttpClient = new()
	{
		Timeout = TimeSpan.FromMinutes(10),
	};

	/// <summary>
	/// The largest image file sent to Ollama. The image travels base64-encoded inside a JSON
	/// string, and System.Text.Json refuses a string value over 166,666,666 characters, which a
	/// file of about 125 MB already exceeds. A vision model downscales its input long before
	/// this size anyway.
	/// </summary>
	internal const long MaxImageBytes = 100L * 1024 * 1024;

	/// <summary>
	/// Normalizes an endpoint as a user would type it: adds <c>http://</c> when the scheme is
	/// missing (Ollama prints its address as <c>localhost:11434</c>) and drops trailing slashes.
	/// Returns <see langword="null"/> unless the result is an absolute http or https URI.
	/// </summary>
	internal static string? NormalizeEndpoint(string? value)
	{
		string trimmed = value?.Trim() ?? string.Empty;
		if (trimmed.Length == 0)
		{
			return null;
		}

		string withScheme = trimmed.Contains(Uri.SchemeDelimiter, StringComparison.Ordinal) ? trimmed : $"{Uri.UriSchemeHttp}{Uri.SchemeDelimiter}{trimmed}";
		if (!Uri.TryCreate(withScheme, UriKind.Absolute, out Uri? uri)
			|| (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps)
			|| string.IsNullOrEmpty(uri.Host)
			|| !string.IsNullOrEmpty(uri.Query)
			|| !string.IsNullOrEmpty(uri.Fragment))
		{
			return null;
		}

		return uri.GetLeftPart(UriPartial.Path).TrimEnd('/');
	}

	/// <summary>
	/// Returns the endpoint as a base URI ending in exactly one slash, so relative request paths
	/// resolve under it rather than doubling or replacing its last segment.
	/// </summary>
	internal static Uri GetBaseUri(OllamaEndpoint endpoint) =>
		new($"{NormalizeEndpoint(endpoint.WeakString) ?? throw new ArgumentException($"Invalid Ollama endpoint: {endpoint}", nameof(endpoint))}/");

	internal static Uri GetGenerateUri(OllamaEndpoint endpoint) => new(GetBaseUri(endpoint), "api/generate");

	internal static Uri GetShowUri(OllamaEndpoint endpoint) => new(GetBaseUri(endpoint), "api/show");

	internal static async Task<bool> IsAvailableAsync(OllamaEndpoint endpoint)
	{
		try
		{
			using HttpResponseMessage response = await HttpClient.GetAsync(GetBaseUri(endpoint)).ConfigureAwait(false);
			return response.IsSuccessStatusCode;
		}
		catch (HttpRequestException)
		{
			return false;
		}
		catch (TaskCanceledException)
		{
			return false;
		}
		catch (ArgumentException)
		{
			// GetBaseUri rejects anything NormalizeEndpoint can't turn into an http or https address
			return false;
		}
	}

	/// <summary>
	/// Asks Ollama whether <paramref name="model"/> is installed. Returns <see langword="false"/> only
	/// when Ollama answers 404, as it does for a model that hasn't been pulled, and
	/// <see langword="null"/> when the answer says nothing either way (an older server, a proxy, a
	/// network error), so the scan isn't blocked on a check it can't make.
	/// </summary>
	internal static async Task<bool?> IsModelInstalledAsync(OllamaEndpoint endpoint, OllamaModelName model)
	{
		try
		{
			// Current Ollama reads "model"; releases before it read "name"
			string json = JsonSerializer.Serialize(new OllamaShowRequest { Model = model, Name = model }, OllamaJsonContext.Default.OllamaShowRequest);
			using StringContent content = new(json, Encoding.UTF8, "application/json");
			using HttpResponseMessage response = await HttpClient.PostAsync(GetShowUri(endpoint), content).ConfigureAwait(false);
			if (response.IsSuccessStatusCode)
			{
				return true;
			}

			return response.StatusCode == HttpStatusCode.NotFound ? false : null;
		}
		catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or ArgumentException)
		{
			return null;
		}
	}

	/// <exception cref="InvalidDataException">The image is larger than <see cref="MaxImageBytes"/>.</exception>
	internal static async Task<string> DescribeImageAsync(OllamaEndpoint endpoint, OllamaModelName model, string prompt, AbsoluteFilePath imagePath)
	{
		long length = new FileInfo(imagePath.WeakString).Length;
		if (length > MaxImageBytes)
		{
			throw new InvalidDataException($"The image is {length} bytes, over the {MaxImageBytes}-byte limit for sending an image to Ollama.");
		}

		byte[] imageBytes = await File.ReadAllBytesAsync(imagePath.WeakString).ConfigureAwait(false);
		string base64Image = Convert.ToBase64String(imageBytes);

		OllamaRequest request = new()
		{
			Model = model,
			Prompt = prompt,
			Images = [base64Image],
			Stream = false,
		};

		return await SendRequestAsync(endpoint, request).ConfigureAwait(false);
	}

	internal static async Task<string> GenerateAsync(OllamaEndpoint endpoint, OllamaModelName model, string prompt)
	{
		OllamaRequest request = new()
		{
			Model = model,
			Prompt = prompt,
			Stream = false,
		};

		return await SendRequestAsync(endpoint, request).ConfigureAwait(false);
	}

	/// <exception cref="InvalidDataException">
	/// Ollama answered with no text, or stopped because it reached its token limit
	/// (<c>done_reason</c> <c>length</c>), so the answer is missing or cut off.
	/// </exception>
	private static async Task<string> SendRequestAsync(OllamaEndpoint endpoint, OllamaRequest request)
	{
		string jsonContent = JsonSerializer.Serialize(request, OllamaJsonContext.Default.OllamaRequest);
		using StringContent content = new(jsonContent, Encoding.UTF8, "application/json");

		using HttpResponseMessage response = await HttpClient.PostAsync(GetGenerateUri(endpoint), content).ConfigureAwait(false);
		string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
		if (!response.IsSuccessStatusCode)
		{
			// Ollama explains a failure in the body, such as a model that hasn't been pulled
			string? error = ReadError(responseBody);
			string status = $"Ollama returned {(int)response.StatusCode} ({response.ReasonPhrase})";
			throw new HttpRequestException(error is null ? $"{status}." : $"{status}: {error}", null, response.StatusCode);
		}

		OllamaResponse? ollamaResponse = JsonSerializer.Deserialize(responseBody, OllamaJsonContext.Default.OllamaResponse);

		return ReadAnswer(ollamaResponse);
	}

	/// <summary>
	/// Returns the <c>error</c> text of an Ollama error body, or null when the body isn't one.
	/// </summary>
	internal static string? ReadError(string body)
	{
		try
		{
			string? error = JsonSerializer.Deserialize(body, OllamaJsonContext.Default.OllamaError)?.Error;
			return string.IsNullOrWhiteSpace(error) ? null : error.Trim();
		}
		catch (JsonException)
		{
			return null;
		}
	}

	/// <summary>
	/// Returns the model's answer, or throws when it is empty or truncated. Either would otherwise be
	/// stored as a finished description, and a stored image is never described again.
	/// </summary>
	/// <exception cref="InvalidDataException">The answer is empty, or <c>done_reason</c> is <c>length</c>.</exception>
	internal static string ReadAnswer(OllamaResponse? response)
	{
		string answer = response?.Response ?? string.Empty;
		if (string.IsNullOrWhiteSpace(answer))
		{
			throw new InvalidDataException("Ollama returned an empty response.");
		}

		if (string.Equals(response?.DoneReason, "length", StringComparison.Ordinal))
		{
			throw new InvalidDataException("Ollama's response was cut off at the model's token limit (done_reason: length).");
		}

		return answer;
	}
}

[JsonSerializable(typeof(OllamaRequest))]
[JsonSerializable(typeof(OllamaResponse))]
[JsonSerializable(typeof(OllamaShowRequest))]
[JsonSerializable(typeof(OllamaError))]
internal sealed partial class OllamaJsonContext : JsonSerializerContext
{
}

internal sealed class OllamaRequest
{
	[JsonPropertyName("model")]
	public string Model { get; set; } = string.Empty;

	[JsonPropertyName("prompt")]
	public string Prompt { get; set; } = string.Empty;

	[JsonPropertyName("images")]
	public string[] Images { get; set; } = [];

	[JsonPropertyName("stream")]
	public bool Stream { get; set; }
}

internal sealed class OllamaResponse
{
	[JsonPropertyName("response")]
	public string Response { get; set; } = string.Empty;

	/// <summary>
	/// Why generation stopped: <c>stop</c> for a finished answer, <c>length</c> when the token
	/// limit cut it off, <c>load</c> when only the model was loaded.
	/// </summary>
	[JsonPropertyName("done_reason")]
	public string? DoneReason { get; set; }
}

internal sealed class OllamaShowRequest
{
	[JsonPropertyName("model")]
	public string Model { get; set; } = string.Empty;

	[JsonPropertyName("name")]
	public string Name { get; set; } = string.Empty;
}

internal sealed class OllamaError
{
	[JsonPropertyName("error")]
	public string? Error { get; set; }
}
