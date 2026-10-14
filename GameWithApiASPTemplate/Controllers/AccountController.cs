using Microsoft.AspNetCore.Mvc;
using GameWithApiASPTemplate;

namespace GameWithApiASPTemplate.Controllers
{
    public class AccountController : Controller
    {
        private string? CurrentUserId => HttpContext.Session.GetString("user_id");

        private readonly API _api;

        public AccountController(API api)
        {
            _api = api;
        }

        public IActionResult Login()
        {
            if (!string.IsNullOrEmpty(CurrentUserId))
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["Error"] = "Введите email и пароль";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                var response = await _api.Login(email, password);

                if (response?.userId != null)
                {
                    HttpContext.Session.SetString("user_id", response.userId);

                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Ошибка входа: {ex.Message}";
            }

            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            HttpContext.Session.Remove("user_id");
            return RedirectToAction("Index", "Home");
        }

    }
}
