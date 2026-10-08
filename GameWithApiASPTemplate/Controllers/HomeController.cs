using Microsoft.AspNetCore.Mvc;

using GameWithApiASPTemplate;

using System.Text.Json;

namespace GameWithApiASPTemplate.Controllers
{
    public class HomeController : Controller
    {
        private readonly API _api;

        public HomeController(API api)
        {
            _api = api;
        }

        private string? CurrentUserId => Request.Cookies["UserId"];

        // Игровая сессия портала: iframe передаёт ?session=<id>&token=<hmac>
        private int? GameSessionId
        {
            get
            {
                var raw = Request.Query["session"].ToString();
                return int.TryParse(raw, out var id) ? id : (int?)null;
            }
        }

        private string? GameSessionToken => Request.Query["token"].ToString();

        public IActionResult Index()
        {
            var userId = CurrentUserId;
            ViewData["IsLoggedIn"] = !string.IsNullOrEmpty(userId);
            ViewData["CurrentUserId"] = userId;
            ViewData["GameSessionId"] = GameSessionId;

            if (TempData["Message"] != null)
                ViewData["Message"] = TempData["Message"]?.ToString();

            if (TempData["Error"] != null)
                ViewData["Error"] = TempData["Error"]?.ToString();

            return View();
        }

        public async Task<IActionResult> GetPoints(string playerId)
        {
            if (string.IsNullOrEmpty(CurrentUserId))
                return RedirectToAction("Login", "Account");

            try
            {
                var result = await _api.GetPlayerPoints(playerId);
                TempData["Message"] = $"Баллы игрока {playerId}: {result?.points ?? 0}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка получения баллов: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> AddPoints(string playerId, int amount)
        {
            if (string.IsNullOrWhiteSpace(playerId) || amount <= 0)
            {
                TempData["Error"] = "Некорректные данные для начисления";
                return RedirectToAction("Index");
            }

            try
            {
                var result = await _api.AddScoreAsync(GameSessionId ?? 0, GameSessionToken, amount);
                TempData["Message"] = $"Начислено {amount} баллов игроку {playerId}! Итог: {result?.newTotalPoints}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка начисления баллов: {ex.Message}";
            }

            return RedirectToAction("Index");
        }
    }
}
