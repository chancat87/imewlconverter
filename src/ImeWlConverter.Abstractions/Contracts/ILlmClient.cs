namespace ImeWlConverter.Abstractions.Contracts;

/// <summary>
/// Abstraction for LLM API calls, enabling mocking in tests.
/// </summary>
public interface ILlmClient
{
    /// <summary>Send a chat completion request and get the response content.</summary>
    Task<string> ChatCompletionAsync(
        string systemPrompt,
        string userPrompt,
        CancellationToken ct = default);
}
