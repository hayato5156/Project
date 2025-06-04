using Microsoft.AspNetCore.Mvc;
using ECommercePlatform.Models;
using ECommercePlatform.Data;
using Microsoft.AspNetCore.Authorization;

namespace ECommercePlatform.Controllers
{
}
    public class LogsControllerV2 : Controller // 已更名以避免重複定義
    {
        private readonly ApplicationDbContext _context;

        // 建構子，注入資料庫內容
        public LogsControllerV2(ApplicationDbContext context)
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