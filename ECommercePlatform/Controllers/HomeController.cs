using Microsoft.AspNetCore.Mvc;
using ECommercePlatform.Data;
using System.Linq;

namespace ECommercePlatform.Controllers
{
    public class HomeController : Controller
    {
        private readonly ApplicationDbContext _context;
        public HomeController(ApplicationDbContext context)
        {
            _context = context;
        }
        public IActionResult Index()
        {
            var featuredProducts = _context.Products.Take(3).ToList();
            return View(featuredProducts);
        }
        public IActionResult Privacy()
        {
            return View();
        }
    }
}
