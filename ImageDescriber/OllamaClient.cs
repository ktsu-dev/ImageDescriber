// Copyright (c) ktsu.dev
// All rights reserved.
// Licensed under the MIT license.

namespace ktsu.ImageDescriber;

using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

internal static class OllamaClient
{
	private static readonly HttpClient HttpClient = new()
	{
		Timeout = TimeSpan.FromMinutes(5),
	};

	internal static async Task<bool> IsAvailableAsync(string endpoint)
	{
		try
		{
			using HttpResponseMessage response = await HttpClient.GetAsync(new Uri(endpoint)).ConfigureAwait(false);
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
	}

	internal static async Task<string> DescribeImageAsync(string endpoint, string model, string prompt, string imagePath)
	{
		byte[] imageBytes = await File.ReadAllBytesAsync(imagePath).ConfigureAwait(false);
		string base64Image = Convert.ToBase64String(imageBytes);

		OllamaRequest request = new()
		{
			Model = model,
			Prompt = prompt,
			Images = [base64Image],
			Stream = false,
		};

		string jsonContent = JsonSerializer.Serialize(request, OllamaJsonContext.Default.OllamaRequest);
		using StringContent content = new(jsonContent, Encoding.UTF8, "application/json");

		Uri requestUri = new($"{endpoint}/api/generate");
		using HttpResponseMessage response = await HttpClient.PostAsync(requestUri, content).ConfigureAwait(false);
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
