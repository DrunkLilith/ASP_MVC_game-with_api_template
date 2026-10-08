using System;
using System.Text;
using System.Text.Json;

namespace GameWithApiASPTemplate
{
    public class API
    {
        private readonly HttpClient _httpClient;

        public API()
        {
            // адрес API портала Рудзынг.рф (переопределяется переменной окружения)
            string baseUrl = Environment.GetEnvironmentVariable("LINGVO_API_URL")
                ?? "http://game.рудзынг.рф";

            this._httpClient = new HttpClient();
            this._httpClient.BaseAddress = new System.Uri(baseUrl);
        }

        public async Task<LoginResponse?> Login(string email, string password)
        {
            var loginData = new { email = email, password = password };
            var json = JsonSerializer.Serialize(loginData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/account/login", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Ошибка входа: {responseContent}");

            return JsonSerializer.Deserialize<LoginResponse>(responseContent);
        }

        public async Task<PointsResponse?> GetPlayerPoints(string playerId)
        {
            var response = await _httpClient.GetAsync($"/api/users/{playerId}/points");
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Ошибка получения баллов: {responseContent}");

            return JsonSerializer.Deserialize<PointsResponse>(responseContent);
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

            var scoreData = new { gameSessionId = gameSessionId, score = amount, token = token };
            var json = JsonSerializer.Serialize(scoreData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/api/score", content);
            var responseContent = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                throw new Exception($"Ошибка начисления баллов: {responseContent}");

            return JsonSerializer.Deserialize<ScoreResponse>(responseContent);
        }
    }

    public class PointsResponse
    {
        public int points { get; set; }
    }

    public class ScoreResponse
    {
        public string userId { get; set; }
        public int newTotalPoints { get; set; }
    }

    public class LoginResponse
    {
        public string userId { get; set; }
    }
}
