using Microsoft.AspNetCore.Mvc;
using ECommercePlatform.Models;
using ECommercePlatform.Data;
using Microsoft.AspNetCore.Authorization;

namespace ECommercePlatform.Controllers
{
    // ��x���
    public class LogsControllerV2 : Controller // �w��W�H�קK���Ʃw�q
    {
        private readonly ApplicationDbContext _context;

        // �غc�l�A�`�J��Ʈw���e
        public LogsControllerV2(ApplicationDbContext context)
        {
            _context = context;
        }

        // ���o�̪� 20 ���ާ@��x
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