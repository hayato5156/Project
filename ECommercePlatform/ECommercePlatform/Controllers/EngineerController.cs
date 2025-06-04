using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ECommercePlatform.Data;
using ECommercePlatform.Models;

namespace ECommercePlatform.Controllers
{
    public class EngineerController : Controller
    {
        private readonly ApplicationDbContext _context;
        public EngineerController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            var engineer = _context.Engineers.FirstOrDefault(e => e.Username == username);
            if (engineer == null || !BCrypt.Net.BCrypt.Verify(password, engineer.PasswordHash))
            {
                ViewBag.Error = "帳號或密碼錯誤";
                return View();
            }
            var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, engineer.Username),
                    new Claim(ClaimTypes.Role, "Admin")
                };
            var claimsIdentity = new ClaimsIdentity(claims, "EngineerCookie");
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(1)
            };
            await HttpContext.SignInAsync("EngineerCookie", new ClaimsPrincipal(claimsIdentity), authProperties);
            return Redirect("/admin/dashboard");
        }
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("EngineerCookie");
            return RedirectToAction("Login");
        }
    }
}
