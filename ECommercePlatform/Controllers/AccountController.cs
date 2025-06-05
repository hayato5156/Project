using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ECommercePlatform.Data;
using ECommercePlatform.Models;
using ECommercePlatform.Services;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

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

        /// 登入頁面
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        /// 處理登入
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password, string? returnUrl = null)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                {
                    ViewBag.Message = "請輸入帳號和密碼";
                    return View();
                }

                // 查找用戶
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

                if (user == null)
                {
                    ViewBag.Message = "帳號不存在或已被停用";
                    return View();
                }

                // 驗證密碼
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
                        _log.Log("Account", "PasswordUpgrade", user.Id.ToString(), "密碼格式升級為 BCrypt");
                    }
                }

                if (!passwordValid)
                {
                    ViewBag.Message = "密碼錯誤";
                    return View();
                }

                // 建立身份驗證
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim("UserId", user.Id.ToString()),
                    new Claim("UserRole", user.Role ?? "User"),
                    new Claim(ClaimTypes.Email, user.Email)
                };

                var identity = new ClaimsIdentity(claims, "UserCookie");
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync("UserCookie", principal, new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(24) // 延長到24小時
                });

                // 更新最後登入時間
                user.LastLoginAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                // 記錄操作日誌
                _log.Log("Account", "Login", user.Id.ToString(), $"用戶登入：{user.Username}");

                // 重導向
                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    return Redirect(returnUrl);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (Exception ex)
            {
                ViewBag.Message = "登入過程發生錯誤，請稍後再試";
                _log.Log("Account", "LoginError", "", ex.Message);
                return View();
            }
        }

        /// 註冊頁面
        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        /// 處理註冊
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // 檢查用戶名重複
                var existingUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username);

                if (existingUser != null)
                {
                    ViewBag.Message = "此用戶名已被使用";
                    return View(model);
                }

                // 檢查 Email 重複
                var existingEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email);

                if (existingEmail != null)
                {
                    ViewBag.Message = "此 Email 已被註冊";
                    return View(model);
                }

                // 創建新用戶
                var user = new User
                {
                    Username = model.Username,
                    Email = model.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Role = "User",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    FirstName = model.FirstName,
                    LastName = model.LastName
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // 記錄操作日誌
                _log.Log("Account", "Register", user.Id.ToString(), $"新用戶註冊：{user.Username}");

                // 發送歡迎郵件（可選）
                try
                {
                    await _emailService.SendWelcomeEmailAsync(user.Email, user.Username);
                }
                catch (Exception ex)
                {
                    // 郵件發送失敗不影響註冊
                    _log.Log("Account", "WelcomeEmailError", user.Id.ToString(), ex.Message);
                }

                ViewBag.Message = "註冊成功！請登入";
                ViewBag.Success = true;
                return View(model);
            }
            catch (Exception ex)
            {
                ViewBag.Message = "註冊失敗，請稍後重試";
                _log.Log("Account", "RegisterError", "", ex.Message);
                return View(model);
            }
        }

        /// 登出
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "UserId")?.Value;
            var username = User.Identity?.Name;

            await HttpContext.SignOutAsync("UserCookie");

            if (!string.IsNullOrEmpty(userId))
            {
                _log.Log("Account", "Logout", userId, $"用戶登出：{username}");
            }

            return RedirectToAction("Index", "Home");
        }

        /// 用戶資料頁面
        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Login");
            }

            var userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        /// 更新用戶資料
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(User model)
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Login");
            }

            var userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            var user = await _context.Users.FindAsync(userId);

            if (user == null)
            {
                return NotFound();
            }

            // 只更新允許用戶修改的欄位
            user.FirstName = model.FirstName;
            user.LastName = model.LastName;
            user.PhoneNumber = model.PhoneNumber;
            user.Address = model.Address;

            try
            {
                await _context.SaveChangesAsync();
                _log.Log("Account", "UpdateProfile", userId.ToString(), "更新個人資料");
                ViewBag.Message = "資料更新成功";
                ViewBag.Success = true;
            }
            catch (Exception ex)
            {
                ViewBag.Message = "更新失敗，請稍後再試";
                _log.Log("Account", "UpdateProfileError", userId.ToString(), ex.Message);
            }

            return View(user);
        }

        // 移除舊的 Project 路由相容性
        // 不再提供 /Project/SingIn 等舊路由
        // 如果需要重定向，可以在 Startup/Program.cs 中配置 URL 重寫規則
    }

    //註冊表單模型
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "用戶名為必填")]
        [StringLength(50, ErrorMessage = "用戶名長度不能超過50字元")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email 為必填")]
        [EmailAddress(ErrorMessage = "請輸入有效的 Email 格式")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "密碼為必填")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "密碼長度必須在6-100字元之間")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "確認密碼為必填")]
        [Compare("Password", ErrorMessage = "確認密碼與密碼不符")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [StringLength(50, ErrorMessage = "名字長度不能超過50字元")]
        public string? FirstName { get; set; }

        [StringLength(50, ErrorMessage = "姓氏長度不能超過50字元")]
        public string? LastName { get; set; }
    }
}