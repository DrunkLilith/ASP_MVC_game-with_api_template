using System.Text;
using System.Text.Json;

namespace GameWithApiASPTemplate
{
    public class API
    {
        private readonly HttpClient _httpClient;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public API(HttpClient httpClient)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = BuildApiBaseAddress();
            _httpClient.Timeout = TimeSpan.FromSeconds(10);
        }

        private static Uri BuildApiBaseAddress()
        {
            var baseUrl = (Environment.GetEnvironmentVariable("LINGVO_API_URL")
                ?? "http://game.рудзынг.рф/api").TrimEnd('/');

            if (!baseUrl.EndsWith("/api", StringComparison.OrdinalIgnoreCase))
                baseUrl += "/api";

            return new Uri(baseUrl + "/", UriKind.Absolute);
        }

        public async Task<LoginResponse?> Login(string email, string password)
        {
            var loginData = new { email = email, password = password };
            var json = JsonSerializer.Serialize(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("account/login", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Ошибка входа: {responseContent}");

            return JsonSerializer.Deserialize<LoginResponse>(responseContent, JsonOptions);
        }

        public async Task<PointsResponse?> GetPlayerPoints(string playerId)
        {
            var response = await _httpClient.GetAsync(
                $"users/{Uri.EscapeDataString(playerId)}/points");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Ошибка получения баллов: {responseContent}");

            return JsonSerializer.Deserialize<PointsResponse>(responseContent, JsonOptions);
        }

        /// <summary>
        /// Начисление баллов через игровую сессию портала.
        /// gameSessionId и token приходят в query (?session=...&token=...),
        /// когда игра запущена в iframe портала Рудзынг.рф.
        /// </summary>
        public async Task<ScoreResponse?> AddScoreAsync(int gameSessionId, string? token, int amount)
        {
            if (gameSessionId <= 0)
                throw new Exception("Игра запущена не с портала: сессия не выдана.");
            if (string.IsNullOrWhiteSpace(token))
                throw new Exception("Портал не передал токен игровой сессии.");
            if (amount <= 0)
                throw new Exception("Количество баллов должно быть положительным.");

            var scoreData = new { gameSessionId = gameSessionId, score = amount, token = token };
            var json = JsonSerializer.Serialize(scoreData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("score", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Ошибка начисления баллов: {responseContent}");

            return JsonSerializer.Deserialize<ScoreResponse>(responseContent, JsonOptions);
        }
    }

    public class PointsResponse
    {
        public int points { get; set; }
    }

    public class ScoreResponse
    {
        public string userId { get; set; } = string.Empty;
        public int newTotalPoints { get; set; }
    }

    public class LoginResponse
    {
        public string userId { get; set; } = string.Empty;
    }
}
