using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ECommercePlatform.Data;

namespace ECommercePlatform.Controllers.Admin
{
    [Authorize(AuthenticationSchemes = "EngineerCookie")]
    [Route("admin")]
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("dashboard")]
        public IActionResult Dashboard()
        {
            // 可以在這裡準備儀表板需要的資料
            ViewBag.TotalUsers = _context.Users.Count();
            ViewBag.TotalOrders = _context.Orders.Count();
            ViewBag.TotalProducts = _context.Products.Count();

            return View();
        }

        [HttpGet("")]
        public IActionResult Index()
        {
            return RedirectToAction("Dashboard");
        }
    }
}