using Microsoft.AspNetCore.Mvc;

namespace GameWithApiASPTemplate.Controllers
{
    public class HomeController : Controller
    {
        private readonly API _api;

        public HomeController(API api)
        {
            _api = api;
        }

        private string? CurrentUserId => HttpContext.Session.GetString("user_id");

        // Игровая сессия портала: iframe передаёт ?session=<id>&token=<hmac>
        private int? GameSessionId
        {
            get
            {
                return HttpContext.Session.GetInt32("game_session_id");
            }
        }

        private string? GameSessionToken =>
            HttpContext.Session.GetString("game_session_token");

        public IActionResult Index(
            [FromQuery(Name = "session")] int? gameSessionId,
            [FromQuery] string? token)
        {
            CaptureGameSession(gameSessionId, token);
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

        [HttpGet("/start")]
        public IActionResult Start(
            [FromQuery(Name = "session")] int? gameSessionId,
            [FromQuery] string? token)
        {
            CaptureGameSession(gameSessionId, token);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPoints(int amount)
        {
            if (amount <= 0)
            {
                TempData["Error"] = "Некорректные данные для начисления";
                return RedirectToAction("Index");
            }

            try
            {
                var result = await _api.AddScoreAsync(GameSessionId ?? 0, GameSessionToken, amount);
                TempData["Message"] = $"Начислено {amount} баллов. Итог: {result?.newTotalPoints}";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка начисления баллов: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        private void CaptureGameSession(int? gameSessionId, string? token)
        {
            if (gameSessionId is not > 0)
                return;

            HttpContext.Session.SetInt32("game_session_id", gameSessionId.Value);

            if (string.IsNullOrWhiteSpace(token))
                HttpContext.Session.Remove("game_session_token");
            else
                HttpContext.Session.SetString("game_session_token", token);
        }
    }
}
