using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ECommercePlatform.Data;
using ECommercePlatform.Models;
using ECommercePlatform.Services;

namespace ECommercePlatform.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly OperationLogService _log;

        public AccountController(ApplicationDbContext context, EmailService emailService, OperationLogService log)
        {
            _context = context;
            _emailService = emailService;
            _log = log;
        }

        /// <summary>
        /// 登入頁面 (保持與 ProjectController.SingIn 相容)
        /// </summary>
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Review");
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        /// <summary>
        /// 處理登入 (完全相容 ProjectController 的登入邏輯)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            try
            {
                // 使用與 ProjectController 相同的登入邏輯
                var user = _context.Users.FirstOrDefault(u => u.Username == username && u.IsActive);

                if (user == null)
                {
                    ViewBag.Message = "登入失敗"; // 保持原有錯誤訊息格式
                    return View();
                }

                // 驗證密碼 (支援兩種格式：BCrypt 和原有格式)
                bool passwordValid = false;

                if (user.PasswordHash.StartsWith("$2"))
                {
                    // BCrypt 格式
                    passwordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
                }
                else
                {
                    // 原有格式 (明文或簡單 hash)
                    passwordValid = user.PasswordHash == password;

                    // 如果驗證成功，順便升級為 BCrypt
                    if (passwordValid)
                    {
                        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(password);
                        await _context.SaveChangesAsync();
                    }
                }

                if (!passwordValid)
                {
                    ViewBag.Message = "登入失敗";
                    return View();
                }

                // 建立身份驗證 (保持與原有格式相容)
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim("UserId", user.Id.ToString()),
                    new Claim("UserRole", user.Role ?? "User")
                };

                var identity = new ClaimsIdentity(claims, "UserCookie");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync("UserCookie", principal, new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2)
                });

                // 更新最後登入時間
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // 記錄操作日誌
                _log.Log("Account", "Login", user.Id.ToString(), $"用戶登入：{user.Username}");

                // 重導向到評價頁面 (保持與 ProjectController 相同的行為)
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Review", new
                {
                    ID = user.Id,
                    userName = user.Username,
                    buyOrSell = user.Role
                });
            }
            catch (Exception ex)
            {
                ViewBag.Message = "登入過程發生錯誤";
                _log.Log("Account", "LoginError", "", ex.Message);
                return View();
            }
        }

        /// <summary>
        /// 註冊頁面 (保持與 ProjectController.Register 相容)
        /// </summary>
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Review");
            }
            return View();
        }

        /// <summary>
        /// 處理註冊 (完全相容 ProjectController 的註冊邏輯)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string Username, string Email, string PasswordHash)
        {
            try
            {
                // 檢查用戶名重複 (保持與 ProjectController 相同的邏輯)
                var existingUsers = _context.Users.ToList();
                if (existingUsers.Any(u => u.Username == Username))
                {
                    ViewBag.Message = "帳號已存在";
                    return View();
                }

                var user = new User
                {
                    Username = Username,
                    Email = Email ?? $"{Username}@example.com", // 提供預設 Email
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(PasswordHash),
                    Role = "User",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // 記錄操作日誌
                _log.Log("Account", "Register", user.Id.ToString(), $"新用戶註冊：{user.Username}");

                ViewBag.Message = "註冊成功，請登入";
                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ViewBag.Message = "註冊失敗，請稍後重試";
                _log.Log("Account", "RegisterError", "", ex.Message);
                return View();
            }
        }

        /// <summary>
        /// 登出 (保持原有行為)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;

            await HttpContext.SignOutAsync("UserCookie");

            if (!string.IsNullOrEmpty(userId))
            {
                _log.Log("Account", "Logout", userId, "用戶登出");
            }

            return RedirectToAction("Index", "Home");
        }

        //為了相容性而保留的 ProjectController 路由
        //保持與 ProjectController.SingIn 的相容性
        [HttpGet]
        [Route("Project/SingIn")]
        public IActionResult SingIn() => Login();

        [HttpPost]
        [Route("Project/SingIn")]
        public async Task<IActionResult> SingIn(string username, string password)
        {
            return await Login(username, password);
        }

        [HttpGet]
        [Route("Project/Register")]
        public IActionResult ProjectRegister() => Register();

        [HttpPost]
        [Route("Project/Register")]
        public async Task<IActionResult> ProjectRegister(string Username, string Email, string PasswordHash)
        {
            return await Register(Username, Email, PasswordHash);
        }
    }
}