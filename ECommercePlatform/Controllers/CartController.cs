using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommercePlatform.Data;
using ECommercePlatform.Models;
using System.Security.Claims;

namespace ECommercePlatform.Controllers
{
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }
        /// 購物車首頁
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            if (!User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Login", "Account");
            }

            var userId = GetCurrentUserId();
            if (userId == 0)
            {
                return RedirectToAction("Login", "Account");
            }

            var cartItems = await _context.CartItems
                .Include(c => c.Product)
                .Include(c => c.User)
                .Where(c => c.UserId == userId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(cartItems);
        }

        /// 加入商品到購物車
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                // 檢查用戶是否登入
                if (!User.Identity?.IsAuthenticated == true)
                {
                    return Json(new { success = false, message = "請先登入" });
                }

                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "請先登入" });
                }

                // 檢查產品是否存在且活躍
                var product = await _context.Products.FindAsync(request.ProductId);
                if (product == null)
                {
                    return Json(new { success = false, message = "商品不存在" });
                }

                if (!product.IsActive)
                {
                    return Json(new { success = false, message = "商品已下架" });
                }

                // 檢查庫存（如果Product有Stock屬性）
                try
                {
                    var stockProperty = product.GetType().GetProperty("Stock");
                    if (stockProperty != null)
                    {
                        var stockValue = (int?)stockProperty.GetValue(product);
                        if (stockValue.HasValue && stockValue <= 0)
                        {
                            return Json(new { success = false, message = "商品已售完" });
                        }

                        // 檢查購物車中的數量 + 新增數量是否超過庫存
                        var existingQuantity = await _context.CartItems
                            .Where(c => c.UserId == userId && c.ProductId == request.ProductId)
                            .SumAsync(c => c.Quantity);

                        if (stockValue.HasValue && (existingQuantity + request.Quantity) > stockValue)
                        {
                            return Json(new { success = false, message = $"庫存不足，目前庫存：{stockValue}，購物車已有：{existingQuantity}" });
                        }
                    }
                }
                catch
                {
                    // 如果沒有Stock屬性，忽略庫存檢查
                }

                // 檢查購物車中是否已有此商品
                var existingCartItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == request.ProductId);

                if (existingCartItem != null)
                {
                    // 更新數量
                    existingCartItem.Quantity += request.Quantity;
                    existingCartItem.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // 新增到購物車
                    var cartItem = new CartItem
                    {
                        UserId = userId,
                        ProductId = request.ProductId,
                        Quantity = request.Quantity,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.CartItems.Add(cartItem);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "成功加入購物車" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "系統錯誤：" + ex.Message });
            }
        }

        /// 獲取購物車商品數量
        [HttpGet]
        public async Task<IActionResult> GetCartCount()
        {
            try
            {
                if (!User.Identity?.IsAuthenticated == true)
                {
                    return Json(new { count = 0 });
                }

                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { count = 0 });
                }

                var count = await _context.CartItems
                    .Where(c => c.UserId == userId)
                    .SumAsync(c => c.Quantity);

                return Json(new { count = count });
            }
            catch
            {
                return Json(new { count = 0 });
            }
        }

        /// 更新購物車商品數量
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            try
            {
                if (!User.Identity?.IsAuthenticated == true)
                {
                    return Json(new { success = false, message = "請先登入" });
                }

                var userId = GetCurrentUserId();
                var cartItem = await _context.CartItems
                    .Include(c => c.Product)
                    .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

                if (cartItem == null)
                {
                    return Json(new { success = false, message = "購物車項目不存在" });
                }

                if (quantity <= 0)
                {
                    // 刪除項目
                    _context.CartItems.Remove(cartItem);
                }
                else
                {
                    // 檢查庫存（如果有Stock屬性）
                    try
                    {
                        var stockProperty = cartItem.Product.GetType().GetProperty("Stock");
                        if (stockProperty != null)
                        {
                            var stockValue = (int?)stockProperty.GetValue(cartItem.Product);
                            if (stockValue.HasValue && quantity > stockValue)
                            {
                                return Json(new { success = false, message = $"數量超過庫存，目前庫存：{stockValue}" });
                            }
                        }
                    }
                    catch
                    {
                        // 忽略庫存檢查
                    }

                    // 更新數量
                    cartItem.Quantity = quantity;
                    cartItem.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// 移除購物車商品
        [HttpPost]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            try
            {
                if (!User.Identity?.IsAuthenticated == true)
                {
                    return Json(new { success = false, message = "請先登入" });
                }

                var userId = GetCurrentUserId();
                var cartItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

                if (cartItem != null)
                {
                    _context.CartItems.Remove(cartItem);
                    await _context.SaveChangesAsync();
                }

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// 清空購物車
        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                if (!User.Identity?.IsAuthenticated == true)
                {
                    return Json(new { success = false, message = "請先登入" });
                }

                var userId = GetCurrentUserId();
                var cartItems = _context.CartItems.Where(c => c.UserId == userId);

                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "購物車已清空" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// 獲取當前用戶ID
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue("UserId");
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        /// 檢查用戶是否為管理員
        private bool IsAdmin()
        {
            var userRole = User.FindFirstValue("UserRole");
            return userRole == "Admin" || userRole == "Engineer";
        }

        /// 加入購物車請求模型
        public class AddToCartRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; } = 1;
        }
    }
}