using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommercePlatform.Data;
using ECommercePlatform.Models;

namespace ECommercePlatform.Controllers.Api
{
    [ApiController]
    [Route("api/orders")]
    public class OrderApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public OrderApiController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet("{userId}")]
        public IActionResult GetOrders(int userId)
        {
            var orders = _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .OrderByDescending(o => o.OrderDate)
                .ToList();
            return Ok(orders.Select(o => new
            {
                o.Id,
                o.TotalAmount,
                o.OrderDate,
                o.OrderStatus,
                o.PaymentMethod,
                Items = o.OrderItems.Select(i => new
                {
                    i.Product.Name,
                    i.Quantity,
                    i.UnitPrice
                })
            }));
        }
        [HttpPost]
        public IActionResult CreateOrder([FromBody] Order model)
        {
            model.OrderDate = DateTime.UtcNow;
            model.OrderStatus = "待處理";
            model.PaymentVerified = false;
            _context.Orders.Add(model);
            _context.SaveChanges();
            return Ok(new { model.Id, Status = "Created" });
        }
    }
}
