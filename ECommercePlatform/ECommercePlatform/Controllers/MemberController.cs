using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommercePlatform.Data;
using Microsoft.EntityFrameworkCore;
using ECommercePlatform.Models;

namespace ECommercePlatform.Controllers
{
    public class MemberController : Controller
    {
        [Authorize(AuthenticationSchemes = "UserCookie")]
        [HttpGet("/Member/Profile")]
        public IActionResult Profile()
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return NotFound();
            return View(user);
        }
        [HttpPost("/Member/Profile")]
        public IActionResult Profile(User updated)
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return NotFound();
            user.FirstName = updated.FirstName;
            user.LastName = updated.LastName;
            user.PhoneNumber = updated.PhoneNumber;
            user.Address = updated.Address;
            _context.SaveChanges();
            return Redirect("/Member");
        }
        [HttpGet("/Member/Orders")]
        public IActionResult Orders()
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            var orders = _context.Orders
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToList();
            return View(orders);
        }
        [HttpGet("/Member/Orders/{id}")]
        public IActionResult OrderDetail(int id)
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            var order = _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefault(o => o.Id == id && o.UserId == userId);
            if (order == null) return NotFound();
            return View("OrderDetail", order);
        }
        [HttpGet("/Member/Reviews")]
        public IActionResult Reviews()
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            var reviews = _context.Reviews
                .Include(r => r.Product)
                .Where(r => r.UserId == userId)
                .OrderByDescending(r => r.CreatedAt)
                .ToList();
            return View(reviews);
        }
        [HttpGet("/Member/Reviews/Delete/{id}")]
        public IActionResult DeleteReview(int id)
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            var review = _context.Reviews.FirstOrDefault(r => r.Id == id && r.UserId == userId);
            if (review == null) return NotFound();
            _context.Reviews.Remove(review);
            _context.SaveChanges();
            return RedirectToAction("Reviews");
        }
        private readonly ApplicationDbContext _context;
        public MemberController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet("/Member")]
        public IActionResult Index()
        {
            var userId = int.Parse(User.Claims.First(c => c.Type == "UserId").Value);
            var user = _context.Users.FirstOrDefault(u => u.Id == userId);
            if (user == null) return NotFound();
            return View(user);
        }
    }
}
