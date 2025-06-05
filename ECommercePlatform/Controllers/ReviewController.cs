using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommercePlatform.Data;
using ECommercePlatform.Models;
using ECommercePlatform.Services;
using System.Security.Claims;
using System.ComponentModel.DataAnnotations;

namespace ECommercePlatform.Controllers
{
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly EmailService _emailService;
        private readonly OperationLogService _log; // 注意：這裡是 _log，不是 _logService

        public ReviewController(ApplicationDbContext context, EmailService emailService, OperationLogService log)
        {
            _context = context;
            _emailService = emailService;
            _log = log; // 修正：使用 _log
        }

        //評價列表頁面（完全使用 EF Core + Review 模型）
        [HttpGet]
        public async Task<IActionResult> Index(int? productId = null, string sort = "latest",
            string keyword = "", int? scoreFilter = null, int page = 1)
        {
            const int pageSize = 10;

            try
            {
                var query = _context.Reviews
                    .Include(r => r.User)
                    .Include(r => r.Product)
                    .Where(r => r.IsVisible)
                    .AsQueryable();

                // 產品篩選
                if (productId.HasValue)
                {
                    query = query.Where(r => r.ProductId == productId.Value);
                    var product = await _context.Products.FindAsync(productId.Value);
                    ViewBag.ProductName = product?.Name;
                }

                // 關鍵字搜尋
                if (!string.IsNullOrEmpty(keyword))
                {
                    query = query.Where(r => r.Content.Contains(keyword) ||
                                       r.Product.Name.Contains(keyword));
                }

                // 評分篩選
                if (scoreFilter.HasValue)
                {
                    query = query.Where(r => r.Rating == scoreFilter.Value);
                }

                // 排序
                query = sort switch
                {
                    "latest" => query.OrderByDescending(r => r.CreatedAt),
                    "oldest" => query.OrderBy(r => r.CreatedAt),
                    "highscore" => query.OrderByDescending(r => r.Rating).ThenByDescending(r => r.CreatedAt),
                    "lowscore" => query.OrderBy(r => r.Rating).ThenByDescending(r => r.CreatedAt),
                    _ => query.OrderByDescending(r => r.CreatedAt)
                };

                var totalItems = await query.CountAsync();
                var reviews = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                // 計算統計數據
                var allReviews = await _context.Reviews
                    .Where(r => r.IsVisible && (!productId.HasValue || r.ProductId == productId.Value))
                    .ToListAsync();

                var result = new ReviewListViewModel
                {
                    Reviews = reviews,
                    TotalItems = totalItems,
                    PageNumber = page,
                    PageSize = pageSize,
                    TotalReviews = allReviews.Count,
                    AverageScore = allReviews.Any() ? allReviews.Average(r => r.Rating) : 0,
                    RatingDistribution = allReviews
                        .GroupBy(r => r.Rating)
                        .ToDictionary(g => g.Key, g => g.Count())
                };

                // 保存篩選參數
                ViewBag.ProductId = productId;
                ViewBag.Sort = sort;
                ViewBag.Keyword = keyword;
                ViewBag.ScoreFilter = scoreFilter;

                // 支援 AJAX 請求
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return PartialView("_ReviewListPartial", result);
                }

                return View(result);
            }
            catch (Exception ex)
            {
                _log.Log("Review", "IndexError", "", ex.Message); // 修正：使用 _log
                return View(new ReviewListViewModel());
            }
        }

        //新增評價（只支援 Review 模型）
        [HttpPost]
        [Authorize(AuthenticationSchemes = "UserCookie")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([FromForm] CreateReviewRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                var userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);
                var userName = User.Identity?.Name ?? "";

                // 檢查是否已經評價過
                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.UserId == userId && r.ProductId == request.ProductId);

                if (existingReview != null)
                {
                    return Json(new { success = false, message = "您已經評價過此商品" });
                }

                // 檢查是否有購買記錄（可選）
                var hasPurchased = await _context.OrderItems
                    .Include(oi => oi.Order)
                    .AnyAsync(oi => oi.ProductId == request.ProductId &&
                                  oi.Order.UserId == userId &&
                                  oi.Order.OrderStatus == "已送達");

                if (!hasPurchased)
                {
                    return Json(new { success = false, message = "只有購買過的商品才能評價" });
                }

                // 處理圖片上傳
                byte[]? imageData = null;
                if (request.ImageFile != null && request.ImageFile.Length > 0)
                {
                    if (request.ImageFile.Length > 5 * 1024 * 1024) // 5MB 限制
                    {
                        return Json(new { success = false, message = "圖片檔案不能超過 5MB" });
                    }

                    var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                    if (!allowedTypes.Contains(request.ImageFile.ContentType.ToLower()))
                    {
                        return Json(new { success = false, message = "只允許上傳 JPG、PNG 或 GIF 格式的圖片" });
                    }

                    using var ms = new MemoryStream();
                    await request.ImageFile.CopyToAsync(ms);
                    imageData = ms.ToArray();
                }

                // 創建評價
                var review = new Review
                {
                    UserId = userId,
                    ProductId = request.ProductId,
                    UserName = userName,
                    Content = request.Content,
                    Rating = request.Rating,
                    ImageData = imageData,
                    CreatedAt = DateTime.UtcNow,
                    IsVisible = true
                };

                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();

                _log.Log("Review", "Create", review.Id.ToString(), // 修正：使用 _log
                    $"新增評價：{request.Rating}星，商品ID: {request.ProductId}");

                return Json(new
                {
                    success = true,
                    message = "評價新增成功",
                    reviewId = review.Id
                });
            }
            catch (Exception ex)
            {
                _log.Log("Review", "CreateError", "", ex.Message); // 修正：使用 _log
                return Json(new { success = false, message = "新增評價失敗，請稍後重試" });
            }
        }

        //更新評價
        [HttpPost]
        [Authorize(AuthenticationSchemes = "UserCookie")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int reviewId, string content, int rating)
        {
            try
            {
                var userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);
                var review = await _context.Reviews
                    .FirstOrDefaultAsync(r => r.Id == reviewId && r.UserId == userId);

                if (review == null)
                {
                    return Json(new { success = false, message = "評價不存在或無權限修改" });
                }

                review.Content = content;
                review.Rating = rating;
                review.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _log.Log("Review", "Update", reviewId.ToString(), "更新評價");

                return Json(new { success = true, message = "評價更新成功" });
            }
            catch (Exception ex)
            {
                _log.Log("Review", "UpdateError", reviewId.ToString(), ex.Message);
                return Json(new { success = false, message = "更新評價失敗" });
            }
        }

        //刪除評價
        [HttpPost]
        [Authorize(AuthenticationSchemes = "UserCookie")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int reviewId)
        {
            try
            {
                var userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);
                var userRole = User.Claims.FirstOrDefault(c => c.Type == "UserRole")?.Value ?? "";

                var review = await _context.Reviews.FindAsync(reviewId);
                if (review == null)
                {
                    return Json(new { success = false, message = "評價不存在" });
                }

                // 檢查權限：評價者本人或管理員
                if (review.UserId != userId && userRole != "Admin" && userRole != "Engineer")
                {
                    return Json(new { success = false, message = "無權限刪除此評價" });
                }

                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();

                _log.Log("Review", "Delete", reviewId.ToString(), "刪除評價");

                return Json(new { success = true, message = "評價已刪除" });
            }
            catch (Exception ex)
            {
                _log.Log("Review", "DeleteError", reviewId.ToString(), ex.Message);
                return Json(new { success = false, message = "刪除評價失敗" });
            }
        }

        //檢舉評價
        [HttpPost]
        [Authorize(AuthenticationSchemes = "UserCookie")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Report([FromForm] ReportReviewRequest request)
        {
            try
            {
                var userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);

                // 檢查是否已經檢舉過
                var existingReport = await _context.ReviewReports
                    .FirstOrDefaultAsync(rr => rr.ReviewId == request.ReviewId && rr.ReporterId == userId);

                if (existingReport != null)
                {
                    return Json(new { success = false, message = "您已經檢舉過此評價" });
                }

                // 創建檢舉記錄
                var report = new ReviewReport
                {
                    ReviewId = request.ReviewId,
                    ReporterId = userId,
                    Reason = request.Reason,
                    Description = request.Description,
                    Harassment = request.Harassment,
                    Pornography = request.Pornography,
                    Threaten = request.Threaten,
                    Hatred = request.Hatred,
                    CreatedAt = DateTime.UtcNow
                };

                _context.ReviewReports.Add(report);
                await _context.SaveChangesAsync();

                // 發送檢舉通知郵件
                try
                {
                    var review = await _context.Reviews
                        .Include(r => r.Product)
                        .FirstOrDefaultAsync(r => r.Id == request.ReviewId);

                    if (review != null)
                    {
                        var emailBody = $@"
                            收到新的評價檢舉：
                            
                            評價ID: {request.ReviewId}
                            商品: {review.Product.Name}
                            檢舉原因: {request.Reason}
                            詳細描述: {request.Description}
                            
                            檢舉類型:
                            - 騷擾: {request.Harassment}
                            - 色情: {request.Pornography}
                            - 威脅: {request.Threaten}
                            - 仇恨: {request.Hatred}
                            
                            請前往後台處理此檢舉。
                        ";

                        await _emailService.SendComplainMailAsync("評價檢舉通知", emailBody);
                    }
                }
                catch (Exception ex)
                {
                    _log.Log("Review", "ReportEmailError", request.ReviewId.ToString(), ex.Message);
                }

                _log.Log("Review", "Report", request.ReviewId.ToString(), $"檢舉評價：{request.Reason}");

                return Json(new { success = true, message = "檢舉已提交，我們會儘快處理" });
            }
            catch (Exception ex)
            {
                _log.Log("Review", "ReportError", request.ReviewId.ToString(), ex.Message);
                return Json(new { success = false, message = "檢舉提交失敗，請稍後重試" });
            }
        }

        //獲取商品評價（API）
        [HttpGet]
        [Route("api/reviews/product/{productId}")]
        public async Task<IActionResult> GetProductReviews(int productId, int page = 1, int pageSize = 5)
        {
            try
            {
                var query = _context.Reviews
                    .Include(r => r.User)
                    .Where(r => r.ProductId == productId && r.IsVisible)
                    .OrderByDescending(r => r.CreatedAt);

                var totalItems = await query.CountAsync();
                var reviews = await query
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(r => new
                    {
                        r.Id,
                        r.Content,
                        r.Rating,
                        r.CreatedAt,
                        UserName = r.UserName ?? r.User.Username,
                        HasImage = r.ImageData != null
                    })
                    .ToListAsync();

                return Json(new
                {
                    success = true,
                    data = reviews,
                    totalItems = totalItems,
                    page = page,
                    pageSize = pageSize,
                    totalPages = (int)Math.Ceiling((double)totalItems / pageSize)
                });
            }
            catch (Exception ex)
            {
                _log.Log("Review", "GetProductReviewsError", productId.ToString(), ex.Message);
                return Json(new { success = false, message = "獲取評價失敗" });
            }
        }

        //獲取評價圖片
        [HttpGet]
        [Route("reviews/image/{reviewId}")]
        public async Task<IActionResult> GetReviewImage(int reviewId)
        {
            try
            {
                var review = await _context.Reviews.FindAsync(reviewId);
                if (review?.ImageData == null)
                {
                    return NotFound();
                }

                return File(review.ImageData, "image/jpeg");
            }
            catch
            {
                return NotFound();
            }
        }
    }

    //ViewModel 和 DTO 類別
    public class ReviewListViewModel
    {
        public List<Review> Reviews { get; set; } = new();
        public int TotalItems { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalReviews { get; set; }
        public double AverageScore { get; set; }
        public Dictionary<int, int> RatingDistribution { get; set; } = new();

        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
        public bool HasPreviousPage => PageNumber > 1;
        public bool HasNextPage => PageNumber < TotalPages;
    }

    public class CreateReviewRequest
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [StringLength(1000, ErrorMessage = "評價內容不能超過1000字")]
        public string Content { get; set; } = string.Empty;

        [Required]
        [Range(1, 5, ErrorMessage = "評分必須在1-5星之間")]
        public int Rating { get; set; }

        public IFormFile? ImageFile { get; set; }
    }

    public class ReportReviewRequest
    {
        [Required]
        public int ReviewId { get; set; }

        [Required]
        [StringLength(100, ErrorMessage = "檢舉原因不能超過100字")]
        public string Reason { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "詳細描述不能超過500字")]
        public string? Description { get; set; }

        public bool Harassment { get; set; } = false;
        public bool Pornography { get; set; } = false;
        public bool Threaten { get; set; } = false;
        public bool Hatred { get; set; } = false;
    }
}