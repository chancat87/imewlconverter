namespace ImeWlConverter.Core.LlmIntegration;

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using ImeWlConverter.Abstractions.Contracts;

/// <summary>
/// HTTP-based LLM client using OpenAI-compatible API.
/// </summary>
public sealed class HttpLlmClient : ILlmClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiEndpoint;
    private readonly string _apiKey;
    private readonly string _model;

    public HttpLlmClient(HttpClient httpClient, string apiEndpoint, string apiKey, string model)
    {
        _httpClient = httpClient;
        _apiEndpoint = NormalizeEndpoint(apiEndpoint);
        _apiKey = apiKey;
        _model = model;
    }

    public async Task<string> ChatCompletionAsync(
        string systemPrompt, string userPrompt, CancellationToken ct = default)
    {
        var requestBody = JsonSerializer.Serialize(new
        {
            model = _model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userPrompt }
            },
            temperature = 0.3,
            response_format = new { type = "json_object" }
        });

        using var request = new HttpRequestMessage(HttpMethod.Post, _apiEndpoint);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = new StringContent(requestBody, Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, ct);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(ct);
    }

    private static string NormalizeEndpoint(string endpoint)
    {
        endpoint = endpoint?.Trim() ?? "";
        if (endpoint.EndsWith("/v1/chat/completions") || endpoint.EndsWith("/v1/chat/completions/"))
            return endpoint;
        if (endpoint.EndsWith("/v1") || endpoint.EndsWith("/v1/"))
            return endpoint.TrimEnd('/') + "/chat/completions";
        return endpoint.TrimEnd('/') + "/v1/chat/completions";
    }
}
