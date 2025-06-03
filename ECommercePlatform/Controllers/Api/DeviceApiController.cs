using Microsoft.AspNetCore.Mvc;
using ECommercePlatform.Data;
using ECommercePlatform.Models;

namespace ECommercePlatform.Controllers.Api
{
    [ApiController]
    [Route("api/device")]
    public class DeviceApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        public DeviceApiController(ApplicationDbContext context)
        {
            _context = context;
        }
        public class TokenRequest
        {
            public string Token { get; set; } = string.Empty;
        }
        [HttpPost("register")]
        public IActionResult RegisterDevice([FromBody] TokenRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Token))
                return BadRequest("Token is required");
            // 若已存在則不重複寫入
            if (!_context.DeviceTokens.Any(dt => dt.Token == request.Token))
            {
                var record = new DeviceToken
                {
                    Token = request.Token,
                    CreatedAt = DateTime.UtcNow
                };
                _context.DeviceTokens.Add(record);
                _context.SaveChanges();
            }
            return Ok(new { Status = "Registered" });
        }
    }
}