using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class HuggingFaceLlmService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public HuggingFaceLlmService(IConfiguration config)
    {
        _httpClient = new HttpClient();
        _apiKey = config["HuggingFace:ApiKey"];
    }

    public async Task<string> GetRecommendationAsync(string contexte)
    {
        var requestBody = new
        {
            inputs = contexte
        };

        var request = new HttpRequestMessage(
    HttpMethod.Post,
    "https://router.huggingface.co/models/gpt2" // nouveau endpoint
);


        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(requestBody),
            Encoding.UTF8,
            "application/json"
        );

        var response = await _httpClient.SendAsync(request);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            // Retourne le texte brut si erreur
            return $"❌ Erreur LLM : {json}";
        }

        using var doc = JsonDocument.Parse(json);
        return doc.RootElement
                  .GetProperty("choices")[0]
                  .GetProperty("message")
                  .GetProperty("content")
                  .GetString();

    }
}
