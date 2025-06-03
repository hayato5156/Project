using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommercePlatform.Data;
using Microsoft.EntityFrameworkCore;

namespace ECommercePlatform.Controllers
{
    [Authorize(AuthenticationSchemes = "UserCookie")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        public CartController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpPost("/Cart/Add/{productId}")]
        public IActionResult AddToCart(int productId, int quantity = 1)
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            var cartItem = _context.CartItems.FirstOrDefault(ci => ci.UserId == userId && ci.ProductId == productId);
            if (cartItem != null)
            {
                cartItem.Quantity += quantity;
            }
            else
            {
                _context.CartItems.Add(new Models.CartItem
                {
                    UserId = userId,
                    ProductId = productId,
                    Quantity = quantity
                });
            }
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        [HttpGet("/Cart")]
        public IActionResult Index()
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            var items = _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToList();
            return View(items);
        }
        [HttpGet("/Cart/Remove/{id}")]
        public IActionResult Remove(int id)
        {
            var item = _context.CartItems.FirstOrDefault(c => c.Id == id);
            if (item != null) _context.CartItems.Remove(item);
            _context.SaveChanges();
            return RedirectToAction("Index");
        }
        [HttpPost("/Cart/Update")]
        public IActionResult Update(int id, int quantity)
        {
            var item = _context.CartItems.FirstOrDefault(c => c.Id == id);
            if (item != null)
            {
                item.Quantity = quantity;
                _context.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}