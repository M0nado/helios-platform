using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Helios.Connect.Api;

public sealed class OpenAiResponsesProvider(HttpClient httpClient)
{
    public async Task<JsonDocument> CreateResponseAsync(string input, CancellationToken cancellationToken = default)
    {
        var key = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        var model = Environment.GetEnvironmentVariable("OPENAI_MODEL");
        if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("OPENAI_API_KEY is not configured.");
        if (string.IsNullOrWhiteSpace(model)) throw new InvalidOperationException("OPENAI_MODEL must be selected explicitly.");

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/responses");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
        request.Content = new StringContent(JsonSerializer.Serialize(new { model, input }), Encoding.UTF8, "application/json");
        using var response = await httpClient.SendAsync(request, cancellationToken);
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();
        return JsonDocument.Parse(json);
    }
}
