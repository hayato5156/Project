using Microsoft.AspNetCore.Mvc;
using ECommercePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ECommercePlatform.Data;

namespace ECommercePlatform.Controllers
{
    public class LogsController : Controller
    {
        // �`�J��Ʈw���e����
        private readonly ApplicationDbContext _dbContext;

        // �غc�l�A�N ApplicationDbContext �`�J
        public LogsController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // ���o�̪� 200 ���ާ@����
        [HttpGet("")]
        public async Task<IActionResult> Index()
        {
            var logs = await _dbContext.OperationLogs
                .OrderByDescending(l => l.ActionTime)
                .Take(200)
                .ToListAsync();
            return View(logs);
        }
    }
}