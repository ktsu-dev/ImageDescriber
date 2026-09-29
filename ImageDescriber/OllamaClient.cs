// Copyright (c) 2023-2026 ktsu-dev contributors

namespace ktsu.ImageDescriber;

using System.IO;
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

	internal static async Task<string> DescribeImageAsync(OllamaEndpoint endpoint, OllamaModelName model, string prompt, AbsoluteFilePath imagePath)
	{
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

	private static async Task<string> SendRequestAsync(OllamaEndpoint endpoint, OllamaRequest request)
	{
		string jsonContent = JsonSerializer.Serialize(request, OllamaJsonContext.Default.OllamaRequest);
		using StringContent content = new(jsonContent, Encoding.UTF8, "application/json");

		using HttpResponseMessage response = await HttpClient.PostAsync(GetGenerateUri(endpoint), content).ConfigureAwait(false);
		response.EnsureSuccessStatusCode();

		string responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
		OllamaResponse? ollamaResponse = JsonSerializer.Deserialize(responseBody, OllamaJsonContext.Default.OllamaResponse);

		return ollamaResponse?.Response ?? string.Empty;
	}
}

[JsonSerializable(typeof(OllamaRequest))]
[JsonSerializable(typeof(OllamaResponse))]
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
}
