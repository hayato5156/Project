using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommercePlatform.Data;
using ECommercePlatform.Models;
using ECommercePlatform.Services;
using System.Security.Claims;

namespace ECommercePlatform.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly OperationLogService _log;

        public ReviewController(ApplicationDbContext context, EmailService emailService, OperationLogService log)
        {
            _context = context;
            _emailService = emailService;
            _log = log;
        }

        /// <summary>
        /// 評價列表 (完全保持 ProjectController.Index 的介面)
        /// </summary>
        public IActionResult Index(int? ID = null, string userName = "", string buyOrSell = "",
                                  string sort = "latest", string keyword = "", int? scoreFilter = null)
        {
            try
            {
                // 使用 EF Core 查詢，但保持相同的邏輯
                var query = _context.Reviews
                    .Include(r => r.User)
                    .Where(r => r.IsVisible)
                    .AsQueryable();

                // 如果 Product 導航屬性存在，也 Include 它
                try
                {
                    query = query.Include(r => r.Product);
                }
                catch
                {
                    // 如果 Product 不存在，忽略錯誤
                }

                // 關鍵字搜尋 (保持原功能)
                if (!string.IsNullOrEmpty(keyword))
                    query = query.Where(r => r.Content.Contains(keyword));

                // 評分篩選 (保持原功能)
                if (scoreFilter.HasValue)
                    query = query.Where(r => r.Rating == scoreFilter.Value);

                // 排序 (保持原功能)
                query = sort switch
                {
                    "latest" => query.OrderByDescending(r => r.CreatedAt),
                    "oldest" => query.OrderBy(r => r.CreatedAt),
                    "highscore" => query.OrderByDescending(r => r.Rating),
                    "lowscore" => query.OrderBy(r => r.Rating),
                    _ => query.OrderByDescending(r => r.CreatedAt)
                };

                var reviews = query.ToList();

                // 轉換為與 Messages 相容的格式
                var messages = reviews.Select(r => new Messages
                {
                    messageID = r.Id,
                    userID = r.UserId,
                    productID = r.ProductId,
                    userName = r.UserName ?? r.User?.Username ?? "",
                    main = r.Content,
                    score = r.Rating,
                    imageData = r.ImageData,
                    date = r.CreatedAt,
                    replyID = r.ReplyId ?? 0
                }).ToList();

                // 保持原有的 ViewBag 設定
                ViewBag.ID = ID;
                ViewBag.name = userName;
                ViewBag.identity = buyOrSell;
                ViewBag.messages = messages;
                ViewBag.totalMessages = messages.Count;
                ViewBag.averageScore = messages.Any() ? messages.Average(m => m.score) : 0;

                // 支援 AJAX 請求 (保持原功能)
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                    return PartialView("_MessageListPartial", messages);

                return View(messages);
            }
            catch (Exception ex)
            {
                _log.Log("Review", "IndexError", "", ex.Message);
                ViewBag.messages = new List<Messages>();
                ViewBag.totalMessages = 0;
                ViewBag.averageScore = 0;
                return View(new List<Messages>());
            }
        }

        /// <summary>
        /// 新增評價 (保持 ProjectController.SubmitMessage 的介面)
        /// </summary>
        [HttpPost]
        [Authorize(AuthenticationSchemes = "UserCookie")]
        public async Task<IActionResult> SubmitMessage(int replyID, int userID, int productID,
                                                      string userName, string main, int score, IFormFile? image)
        {
            try
            {
                byte[]? imageData = null;

                // 保持原有的圖片處理邏輯
                if (image != null && image.Length > 0)
                {
                    // 檢查檔案大小 (5MB 限制)
                    if (image.Length > 5 * 1024 * 1024)
                    {
                        return BadRequest(new { Message = "圖片檔案不能超過 5MB" });
                    }

                    // 檢查檔案類型
                    var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                    if (!allowedTypes.Contains(image.ContentType.ToLower()))
                    {
                        return BadRequest(new { Message = "只允許上傳 JPG、PNG 或 GIF 格式的圖片" });
                    }

                    using var ms = new MemoryStream();
                    await image.CopyToAsync(ms);
                    imageData = ms.ToArray();
                }

                var review = new Review
                {
                    UserId = userID,
                    ProductId = productID,
                    UserName = userName, // 冗余欄位，為了相容性
                    Content = main,
                    Rating = score,
                    ImageData = imageData,
                    ReplyId = replyID > 0 ? replyID : null,
                    CreatedAt = DateTime.Now,
                    IsVisible = true
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                _log.Log("Review", "Create", review.Id.ToString(), $"新增評價：{score}星");

                return Ok(new { Message = "評價新增成功" });
            }
            catch (Exception ex)
            {
                _log.Log("Review", "CreateError", "", ex.Message);
                return BadRequest(new { Message = "新增評價失敗" });
            }
        }

        /// <summary>
        /// 刪除評價 (保持 ProjectController.DeleteMessage 的介面)
        /// </summary>
        [HttpPost]
        [Authorize(AuthenticationSchemes = "UserCookie")]
        public async Task<IActionResult> DeleteMessage(int messageID)
        {
            try
            {
                var userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);
                var userRole = User.Claims.FirstOrDefault(c => c.Type == "UserRole")?.Value ??
                              User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value ?? "";

                var review = _context.Reviews.FirstOrDefault(r => r.Id == messageID);
                if (review == null)
                    return NotFound();

                // 保持原權限檢查邏輯
                if (review.UserId != userId && userRole != "admin")
                    return Unauthorized();

                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();

                _log.Log("Review", "Delete", messageID.ToString(), "刪除評價");

                return Ok();
            }
            catch (Exception ex)
            {
                _log.Log("Review", "DeleteError", messageID.ToString(), ex.Message);
                return BadRequest();
            }
        }

        /// <summary>
        /// 更新評價 (保持 ProjectController.UpdateMessage 的介面)
        /// </summary>
        [HttpPost]
        [Authorize(AuthenticationSchemes = "UserCookie")]
        public async Task<IActionResult> UpdateMessage(Messages m)
        {
            try
            {
                var userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);
                var review = _context.Reviews.FirstOrDefault(r => r.Id == m.messageID && r.UserId == userId);

                if (review == null)
                    return NotFound();

                review.Content = m.main;
                review.Rating = m.score;
                review.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _log.Log("Review", "Update", m.messageID.ToString(), "更新評價");

                return Ok();
            }
            catch (Exception ex)
            {
                _log.Log("Review", "UpdateError", m.messageID.ToString(), ex.Message);
                return BadRequest();
            }
        }

        /// <summary>
        /// 發送檢舉 (保持 ProjectController.ComplainSend 的介面)
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> ComplainSend(string content, bool harassment = false,
                                                     bool pornography = false, bool threaten = false,
                                                     bool hatred = false, string detail = "")
        {
            try
            {
                // 保持原有的郵件格式
                var body = $"檢舉內容:{content}\n騷擾:{harassment}\n色情:{pornography}\n威脅:{threaten}\n仇恨:{hatred}\n詳細描述:{detail}";

                // 使用 EmailService 發送 (保持原功能)
                _emailService.SendComplainMail("留言檢舉通知", body);

                // 同時記錄到資料庫
                if (User.Identity?.IsAuthenticated == true)
                {
                    var userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);

                    // 嘗試找到對應的評價
                    var targetReview = _context.Reviews
                        .Where(r => r.Content.Contains(content) || content.Contains(r.Content))
                        .OrderByDescending(r => r.CreatedAt)
                        .FirstOrDefault();

                    if (targetReview != null)
                    {
                        var report = new ReviewReport
                        {
                            ReviewId = targetReview.Id,
                            ReporterId = userId,
                            Reason = "檢舉不當內容",
                            Description = detail,
                            Harassment = harassment,
                            Pornography = pornography,
                            Threaten = threaten,
                            Hatred = hatred,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.ReviewReports.Add(report);
                        await _context.SaveChangesAsync();
                    }
                }

                _log.Log("Review", "Complain", "", "檢舉信件已發送");

                return Ok(new { Message = "檢舉已提交" });
            }
            catch (Exception ex)
            {
                _log.Log("Review", "ComplainError", "", ex.Message);
                return BadRequest(new { Message = "檢舉提交失敗" });
            }
        }

        /// <summary>
        /// 錯誤頁面 (保持 ProjectController.ErrorAccount 的介面)
        /// </summary>
        public IActionResult ErrorAccount()
        {
            var emptyMessages = new List<Messages>();
            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                return PartialView("_MessageListPartial", emptyMessages);
            return View(emptyMessages);
        }

        // ========= 為了相容性而保留的 ProjectController 路由 =========

        /// <summary>
        /// 保持與 ProjectController 路由的完全相容性
        /// </summary>
        [HttpGet]
        [Route("Project/Index")]
        public IActionResult ProjectIndex(int? ID = null, string userName = "", string buyOrSell = "",
                                         string sort = "latest", string keyword = "", int? scoreFilter = null)
        {
            return Index(ID, userName, buyOrSell, sort, keyword, scoreFilter);
        }

        [HttpPost]
        [Route("Project/SubmitMessage")]
        public async Task<IActionResult> ProjectSubmitMessage(int replyID, int userID, int productID,
                                                             string userName, string main, int score, IFormFile? image)
        {
            return await SubmitMessage(replyID, userID, productID, userName, main, score, image);
        }

        [HttpPost]
        [Route("Project/DeleteMessage")]
        public async Task<IActionResult> ProjectDeleteMessage(int messageID)
        {
            return await DeleteMessage(messageID);
        }

        [HttpPost]
        [Route("Project/UpdateMessage")]
        public async Task<IActionResult> ProjectUpdateMessage(Messages m)
        {
            return await UpdateMessage(m);
        }

        [HttpGet]
        [Route("Project/ErrorAccount")]
        public IActionResult ProjectErrorAccount()
        {
            return ErrorAccount();
        }
    }
}