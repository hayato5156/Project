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
        /// �ʪ�������
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

        /// �[�J�ӫ~���ʪ���
        [HttpPost]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            try
            {
                // �ˬd�Τ�O�_�n�J
                if (!User.Identity?.IsAuthenticated == true)
                {
                    return Json(new { success = false, message = "�Х��n�J" });
                }

                var userId = GetCurrentUserId();
                if (userId == 0)
                {
                    return Json(new { success = false, message = "�Х��n�J" });
                }

                // �ˬd���~�O�_�s�b�B���D
                var product = await _context.Products.FindAsync(request.ProductId);
                if (product == null)
                {
                    return Json(new { success = false, message = "�ӫ~���s�b" });
                }

                if (!product.IsActive)
                {
                    return Json(new { success = false, message = "�ӫ~�w�U�[" });
                }

                // �ˬd�w�s�]�p�GProduct��Stock�ݩʡ^
                try
                {
                    var stockProperty = product.GetType().GetProperty("Stock");
                    if (stockProperty != null)
                    {
                        var stockValue = (int?)stockProperty.GetValue(product);
                        if (stockValue.HasValue && stockValue <= 0)
                        {
                            return Json(new { success = false, message = "�ӫ~�w�⧹" });
                        }

                        // �ˬd�ʪ��������ƶq + �s�W�ƶq�O�_�W�L�w�s
                        var existingQuantity = await _context.CartItems
                            .Where(c => c.UserId == userId && c.ProductId == request.ProductId)
                            .SumAsync(c => c.Quantity);

                        if (stockValue.HasValue && (existingQuantity + request.Quantity) > stockValue)
                        {
                            return Json(new { success = false, message = $"�w�s�����A�ثe�w�s�G{stockValue}�A�ʪ����w���G{existingQuantity}" });
                        }
                    }
                }
                catch
                {
                    // �p�G�S��Stock�ݩʡA�����w�s�ˬd
                }

                // �ˬd�ʪ������O�_�w�����ӫ~
                var existingCartItem = await _context.CartItems
                    .FirstOrDefaultAsync(c => c.UserId == userId && c.ProductId == request.ProductId);

                if (existingCartItem != null)
                {
                    // ��s�ƶq
                    existingCartItem.Quantity += request.Quantity;
                    existingCartItem.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // �s�W���ʪ���
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
                return Json(new { success = true, message = "���\�[�J�ʪ���" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "�t�ο��~�G" + ex.Message });
            }
        }

        /// ����ʪ����ӫ~�ƶq
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

        /// ��s�ʪ����ӫ~�ƶq
        [HttpPost]
        public async Task<IActionResult> UpdateQuantity(int cartItemId, int quantity)
        {
            try
            {
                if (!User.Identity?.IsAuthenticated == true)
                {
                    return Json(new { success = false, message = "�Х��n�J" });
                }

                var userId = GetCurrentUserId();
                var cartItem = await _context.CartItems
                    .Include(c => c.Product)
                    .FirstOrDefaultAsync(c => c.Id == cartItemId && c.UserId == userId);

                if (cartItem == null)
                {
                    return Json(new { success = false, message = "�ʪ������ؤ��s�b" });
                }

                if (quantity <= 0)
                {
                    // �R������
                    _context.CartItems.Remove(cartItem);
                }
                else
                {
                    // �ˬd�w�s�]�p�G��Stock�ݩʡ^
                    try
                    {
                        var stockProperty = cartItem.Product.GetType().GetProperty("Stock");
                        if (stockProperty != null)
                        {
                            var stockValue = (int?)stockProperty.GetValue(cartItem.Product);
                            if (stockValue.HasValue && quantity > stockValue)
                            {
                                return Json(new { success = false, message = $"�ƶq�W�L�w�s�A�ثe�w�s�G{stockValue}" });
                            }
                        }
                    }
                    catch
                    {
                        // �����w�s�ˬd
                    }

                    // ��s�ƶq
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

        /// �����ʪ����ӫ~
        [HttpPost]
        public async Task<IActionResult> RemoveItem(int cartItemId)
        {
            try
            {
                if (!User.Identity?.IsAuthenticated == true)
                {
                    return Json(new { success = false, message = "�Х��n�J" });
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

        /// �M���ʪ���
        [HttpPost]
        public async Task<IActionResult> ClearCart()
        {
            try
            {
                if (!User.Identity?.IsAuthenticated == true)
                {
                    return Json(new { success = false, message = "�Х��n�J" });
                }

                var userId = GetCurrentUserId();
                var cartItems = _context.CartItems.Where(c => c.UserId == userId);

                _context.CartItems.RemoveRange(cartItems);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "�ʪ����w�M��" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        /// �����e�Τ�ID
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue("UserId");
            return int.TryParse(userIdClaim, out int userId) ? userId : 0;
        }

        /// �ˬd�Τ�O�_���޲z��
        private bool IsAdmin()
        {
            var userRole = User.FindFirstValue("UserRole");
            return userRole == "Admin" || userRole == "Engineer";
        }

        /// �[�J�ʪ����ШD�ҫ�
        public class AddToCartRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; } = 1;
        }
    }
}