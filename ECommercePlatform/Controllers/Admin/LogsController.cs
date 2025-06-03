using Microsoft.AspNetCore.Mvc;
using ECommercePlatform.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using ECommercePlatform.Data;

namespace ECommercePlatform.Controllers
{
    public class LogsController : Controller
    {
        // 注入資料庫內容物件
        private readonly ApplicationDbContext _dbContext;

        // 建構子，將 ApplicationDbContext 注入
        public LogsController(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        // 取得最近 200 筆操作紀錄
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