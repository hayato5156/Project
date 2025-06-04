using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommercePlatform.Data;
using ECommercePlatform.Models;

namespace ECommercePlatform.Controllers.Api
{
    [ApiController]
    [Route("api/reviews")]
    public class ReviewApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public ReviewApiController(ApplicationDbContext context)
        {
            _context = context;
        }
        [HttpGet("product/{productId}")]
        public IActionResult GetByProduct(int productId)
        {
            var reviews = _context.Reviews
                .Include(r => r.User)
                .Where(r => r.ProductId == productId)
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new
                {
                    r.Id,
                    r.Rating,
                    r.Content,
                    r.CreatedAt,
                    UserName = r.User.Username
                }).ToList();
            return Ok(reviews);
        }
        [HttpPost]
        public IActionResult Create([FromBody] Review review)
        {
            review.CreatedAt = DateTime.UtcNow;
            _context.Reviews.Add(review);
            _context.SaveChanges();
            return Ok(new { review.Id, Status = "Created" });
        }
    }
}
