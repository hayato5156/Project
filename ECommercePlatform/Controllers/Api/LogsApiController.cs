using Microsoft.AspNetCore.Mvc;
using ECommercePlatform.Data;

namespace ECommercePlatform.Controllers.Api
{
    [ApiController]
    [Route("api/logs")]
    public class LogsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        // 建構子注入資料庫內容
        public LogsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 取得最近 20 筆操作日誌
        [HttpGet("recent")]
        public IActionResult GetRecentLogs()
        {
            var logs = _context.OperationLogs
                .OrderByDescending(l => l.Timestamp)
                .Take(20)
                .ToList();
            return Ok(logs);
        }
    }
}
