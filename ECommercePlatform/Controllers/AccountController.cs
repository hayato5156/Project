using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ECommercePlatform.Data;
using ECommercePlatform.Models;

namespace ECommercePlatform.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Register(string username, string email, string password)
        {
            if (_context.Users.Any(u => u.Email == email))
            {
                ViewBag.Error = "此 Email 已被註冊。";
                return View();
            }

            var user = new User
            {
                Username = username,
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            _context.SaveChanges();
            return RedirectToAction("Login");
        }

        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            var user = _context.Users.FirstOrDefault(u => u.Email == email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                ViewBag.Error = "帳號或密碼錯誤";
                return View();
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("UserId", user.Id.ToString())
            };

            var identity = new ClaimsIdentity(claims, "UserCookie");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("UserCookie", principal, new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
            });

            return Redirect("/Member");
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("UserCookie");
            return RedirectToAction("Login");
        }
    }
}