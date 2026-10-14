using System.Text;
using System.Text.Json;

namespace GameWithApiASPTemplate;

public sealed class API
{
    readonly HttpClient _httpClient;
    static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public API(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = BuildApiBaseAddress();
        _httpClient.Timeout = TimeSpan.FromSeconds(10);
    }

    static Uri BuildApiBaseAddress()
    {
        var baseUrl = (Environment.GetEnvironmentVariable("LINGVO_API_URL")
            ?? "http://game.рудзынг.рф/api").TrimEnd('/');

        if (!baseUrl.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
            baseUrl += "/api";

        return new Uri(baseUrl + "/", UriKind.Absolute);
    }

    public async Task<GameSessionResponse> ResolveGameSessionAsync(
        int gameSessionId,
        string? token)
    {
        if (gameSessionId <= 0 || string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Портал не передал игровую сессию и токен.");

        var configuredGameId = Environment.GetEnvironmentVariable("LINGVO_GAME_ID");
        if (!int.TryParse(configuredGameId, out var expectedGameId) || expectedGameId <= 0)
            throw new InvalidOperationException("LINGVO_GAME_ID не настроен для этой игры.");

        using var content = new StringContent(
            JsonSerializer.Serialize(new { gameSessionId, token }),
            Encoding.UTF8,
            "application/json");
        using var response = await _httpClient.PostAsync("game/session/resolve", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Игровая сессия отклонена: {responseContent}");

        var result = JsonSerializer.Deserialize<GameSessionResponse>(responseContent, JsonOptions)
            ?? throw new InvalidOperationException("Портал вернул пустой ответ игровой сессии.");
        if (!result.valid || string.IsNullOrWhiteSpace(result.userId))
            throw new InvalidOperationException("Портал вернул некорректную игровую сессию.");
        if (result.gameId != expectedGameId)
            throw new InvalidOperationException("Игровая сессия выдана для другой игры.");

        return result;
    }

    public async Task<ScoreResponse> AddScoreAsync(
        int gameSessionId,
        string? token,
        int amount)
    {
        if (gameSessionId <= 0 || string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("Игра запущена не с портала: сессия не выдана.");
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount), "Количество баллов должно быть положительным.");

        using var content = new StringContent(
            JsonSerializer.Serialize(new { gameSessionId, score = amount, token }),
            Encoding.UTF8,
            "application/json");
        using var response = await _httpClient.PostAsync("score", content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Ошибка начисления баллов: {responseContent}");

        return JsonSerializer.Deserialize<ScoreResponse>(responseContent, JsonOptions)
            ?? throw new InvalidOperationException("Портал вернул пустой ответ начисления баллов.");
    }
}

public sealed class GameSessionResponse
{
    public bool valid { get; set; }
    public int gameId { get; set; }
    public string userId { get; set; } = string.Empty;
    public string userName { get; set; } = string.Empty;
    public int totalUserPoints { get; set; }
}

public sealed class ScoreResponse
{
    public string userId { get; set; } = string.Empty;
    public int newTotalPoints { get; set; }
}
