using Microsoft.AspNetCore.Mvc;

namespace GameWithApiASPTemplate.Controllers;

public sealed class HomeController : Controller
{
    readonly API _api;

    public HomeController(API api)
    {
        _api = api;
    }

    int? GameSessionId => HttpContext.Session.GetInt32("game_session_id");
    string? GameSessionToken => HttpContext.Session.GetString("game_session_token");

    public IActionResult Index()
    {
        FillPortalProfile();
        ViewData["GameSessionId"] = GameSessionId;
        ViewData["Message"] = TempData["Message"]?.ToString();
        ViewData["Error"] = TempData["Error"]?.ToString();
        return View();
    }

    [HttpGet("/start")]
    public async Task<IActionResult> Start(
        [FromQuery(Name = "session")] int? gameSessionId,
        [FromQuery] string? token)
    {
        if (gameSessionId is not > 0 || string.IsNullOrWhiteSpace(token))
            return Unauthorized("Портал не передал игровую сессию и токен.");

        try
        {
            var portalSession = await _api.ResolveGameSessionAsync(gameSessionId.Value, token);
            HttpContext.Session.SetInt32("game_session_id", gameSessionId.Value);
            HttpContext.Session.SetString("game_session_token", token);
            HttpContext.Session.SetString("portal_user_id", portalSession.userId);
            HttpContext.Session.SetString("portal_user_name", portalSession.userName);
            HttpContext.Session.SetInt32("portal_total_points", portalSession.totalUserPoints);
        }
        catch (Exception ex)
        {
            return Unauthorized(ex.Message);
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddPoints(int amount)
    {
        try
        {
            var result = await _api.AddScoreAsync(
                GameSessionId ?? 0,
                GameSessionToken,
                amount);
            HttpContext.Session.SetInt32("portal_total_points", result.newTotalPoints);
            TempData["Message"] = $"Итог отправлен. Баллы портала: {result.newTotalPoints}";
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Результат не отправлен: {ex.Message}";
        }

        return RedirectToAction(nameof(Index));
    }

    void FillPortalProfile()
    {
        ViewData["PortalUserName"] = HttpContext.Session.GetString("portal_user_name");
        ViewData["PortalTotalPoints"] = HttpContext.Session.GetInt32("portal_total_points");
    }
}
