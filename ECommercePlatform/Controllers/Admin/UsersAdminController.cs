using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommercePlatform.Data;
using ECommercePlatform.Models;
using ECommercePlatform.Services;

namespace ECommercePlatform.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/users")]
    [Authorize(AuthenticationSchemes = "EngineerCookie")]
    public class UsersAdminController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly OperationLogService _log;

        public UsersAdminController(ApplicationDbContext context, OperationLogService log)
        {
            _context = context;
            _log = log;
        }

        /// <summary>
        /// 取得所有用戶資料（支援分頁和搜尋）
        /// </summary>
        [HttpGet]
        public IActionResult GetAll([FromQuery] string? keyword, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            try
            {
                var query = _context.Users.AsQueryable();

                // 關鍵字搜尋
                if (!string.IsNullOrEmpty(keyword))
                {
                    query = query.Where(u => u.Username.Contains(keyword) ||
                                           u.Email.Contains(keyword) ||
                                           (u.FirstName != null && u.FirstName.Contains(keyword)) ||
                                           (u.LastName != null && u.LastName.Contains(keyword)));
                }

                var totalCount = query.Count();

                var users = query
                    .OrderByDescending(u => u.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(u => new
                    {
                        u.Id,
                        u.Username,
                        u.Email,
                        u.PhoneNumber,
                        u.FirstName,
                        u.LastName,
                        u.Address,
                        u.IsActive,
                        u.CreatedAt
                    })
                    .ToList();

                return Ok(new
                {
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling((double)totalCount / pageSize),
                    Data = users
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "取得用戶列表失敗", Error = ex.Message });
            }
        }

        /// <summary>
        /// 根據 ID 取得單一用戶資料
        /// </summary>
        [HttpGet("{id}")]
        public IActionResult GetById(int id)
        {
            try
            {
                var user = _context.Users.Find(id);
                if (user == null)
                    return NotFound(new { Message = "找不到指定的用戶" });

                return Ok(new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.PhoneNumber,
                    user.FirstName,
                    user.LastName,
                    user.Address,
                    user.IsActive,
                    user.CreatedAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "取得用戶資料失敗", Error = ex.Message });
            }
        }

        /// <summary>
        /// 創建新用戶
        /// </summary>
        [HttpPost]
        public IActionResult Create([FromBody] CreateUserRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                // 檢查用戶名是否已存在
                if (_context.Users.Any(u => u.Username == request.Username))
                {
                    return BadRequest(new { Message = "用戶名已存在" });
                }

                // 檢查 Email 是否已存在
                if (_context.Users.Any(u => u.Email == request.Email))
                {
                    return BadRequest(new { Message = "Email 已被註冊" });
                }

                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    FirstName = request.FirstName,
                    LastName = request.LastName,
                    PhoneNumber = request.PhoneNumber,
                    Address = request.Address,
                    IsActive = request.IsActive ?? true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);
                _context.SaveChanges();

                // 記錄操作日誌
                _log.Log("UsersAdmin", "Create", user.Id.ToString(), $"建立用戶: {user.Username}");

                return CreatedAtAction(nameof(GetById), new { id = user.Id }, new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.PhoneNumber,
                    user.FirstName,
                    user.LastName,
                    user.Address,
                    user.IsActive,
                    user.CreatedAt,
                    Message = "用戶建立成功"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "建立用戶失敗", Error = ex.Message });
            }
        }

        /// <summary>
        /// 更新用戶資料
        /// </summary>
        [HttpPut("{id}")]
        public IActionResult Update(int id, [FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var user = _context.Users.Find(id);
                if (user == null)
                    return NotFound(new { Message = "找不到指定的用戶" });

                // 檢查用戶名是否被其他人使用
                if (_context.Users.Any(u => u.Username == request.Username && u.Id != id))
                {
                    return BadRequest(new { Message = "用戶名已被其他人使用" });
                }

                // 檢查 Email 是否被其他人使用
                if (_context.Users.Any(u => u.Email == request.Email && u.Id != id))
                {
                    return BadRequest(new { Message = "Email 已被其他人使用" });
                }

                // 更新用戶資料
                user.Username = request.Username;
                user.Email = request.Email;
                user.PhoneNumber = request.PhoneNumber;
                user.FirstName = request.FirstName;
                user.LastName = request.LastName;
                user.Address = request.Address;
                user.IsActive = request.IsActive ?? user.IsActive;

                // 如果提供新密碼，則更新密碼
                if (!string.IsNullOrEmpty(request.NewPassword))
                {
                    user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                }

                _context.SaveChanges();

                // 記錄操作日誌
                _log.Log("UsersAdmin", "Update", id.ToString(), $"更新用戶: {user.Username}");

                return Ok(new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.PhoneNumber,
                    user.FirstName,
                    user.LastName,
                    user.Address,
                    user.IsActive,
                    user.CreatedAt,
                    Message = "用戶更新成功"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "更新用戶失敗", Error = ex.Message });
            }
        }

        /// <summary>
        /// 刪除用戶
        /// </summary>
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                var user = _context.Users.Find(id);
                if (user == null)
                    return NotFound(new { Message = "找不到指定的用戶" });

                // 檢查用戶是否有相關的訂單，如果有則不允許刪除
                var hasOrders = _context.Orders.Any(o => o.UserId == id);
                if (hasOrders)
                {
                    return BadRequest(new { Message = "該用戶有相關訂單記錄，無法刪除。建議將用戶設為停用狀態。" });
                }

                var username = user.Username; // 保存用戶名用於日誌記錄

                _context.Users.Remove(user);
                _context.SaveChanges();

                // 記錄操作日誌
                _log.Log("UsersAdmin", "Delete", id.ToString(), $"刪除用戶: {username}");

                return Ok(new { Message = $"用戶 {username} 已成功刪除" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "刪除用戶失敗", Error = ex.Message });
            }
        }

        /// <summary>
        /// 批量啟用/停用用戶
        /// </summary>
        [HttpPatch("batch-status")]
        public IActionResult BatchUpdateStatus([FromBody] BatchStatusRequest request)
        {
            try
            {
                if (request.UserIds == null || !request.UserIds.Any())
                {
                    return BadRequest(new { Message = "請提供要更新的用戶 ID" });
                }

                var users = _context.Users.Where(u => request.UserIds.Contains(u.Id)).ToList();

                if (!users.Any())
                {
                    return NotFound(new { Message = "找不到指定的用戶" });
                }

                foreach (var user in users)
                {
                    user.IsActive = request.IsActive;
                }

                _context.SaveChanges();

                // 記錄操作日誌
                var action = request.IsActive ? "啟用" : "停用";
                _log.Log("UsersAdmin", "BatchUpdate", string.Join(",", request.UserIds),
                        $"批量{action}用戶，共 {users.Count} 位");

                return Ok(new
                {
                    Message = $"成功{action} {users.Count} 位用戶",
                    UpdatedCount = users.Count
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "批量更新失敗", Error = ex.Message });
            }
        }
    }

    // DTO 類別
    public class CreateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public bool? IsActive { get; set; }
    }

    public class UpdateUserRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? NewPassword { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public bool? IsActive { get; set; }
    }

    public class BatchStatusRequest
    {
        public List<int> UserIds { get; set; } = new();
        public bool IsActive { get; set; }
    }
}