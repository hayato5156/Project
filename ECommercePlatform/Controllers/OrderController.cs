using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommercePlatform.Data;
using ECommercePlatform.Models;

namespace ECommercePlatform.Controllers
{
    [Authorize(AuthenticationSchemes = "UserCookie")]
    public class OrderController : Controller
    {
        private readonly ApplicationDbContext _context;
        public OrderController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet("/Order/Checkout")]
        public IActionResult Checkout()
        {
            return View();
        }
        [HttpPost("/Order/Checkout")]
        public IActionResult Checkout(string address, string paymentMethod)
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            var cartItems = _context.CartItems
                .Include(c => c.Product)
                .Where(c => c.UserId == userId)
                .ToList();
            if (!cartItems.Any())
                return Redirect("/Cart");
            var total = cartItems.Sum(c => c.Product.Price * c.Quantity);
            var order = new Order
            {
                UserId = userId,
                OrderDate = DateTime.UtcNow,
                ShippingAddress = address,
                PaymentMethod = paymentMethod,
                TotalAmount = total,
                OrderStatus = "待處理",
                PaymentVerified = false
            };
            _context.Orders.Add(order);
            _context.SaveChanges();
            foreach (var item in cartItems)
            {
                _context.OrderItems.Add(new OrderItem
                {
                    OrderId = order.Id,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Product.Price
                });
            }
            _context.CartItems.RemoveRange(cartItems);
            _context.SaveChanges();
            return RedirectToAction("Confirm", new { id = order.Id });
        }
        [HttpGet("/Order/Confirm")]
        public IActionResult Confirm(int id)
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            var order = _context.Orders.FirstOrDefault(o => o.Id == id && o.UserId == userId);
            if (order == null) return NotFound();
            return View(order);
        }
    }
}
