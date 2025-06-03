using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommercePlatform.Data;

namespace ECommercePlatform.Controllers.Api
{
    [Route("api/products")]
    [ApiController]
    public class ProductApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public ProductApiController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public IActionResult GetProducts()
        {
            var products = _context.Products
                .Where(p => p.IsActive)
                .OrderByDescending(p => p.Id)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Price,
                    p.ImageUrl
                })
                .ToList();
            return Ok(products);
        }
        [HttpGet("{id}")]
        public IActionResult GetProduct(int id)
        {
            var product = _context.Products
                .Where(p => p.Id == id)
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Price,
                    p.ImageUrl
                })
                .FirstOrDefault();
            if (product == null) return NotFound();
            return Ok(product);
        }
    }
}