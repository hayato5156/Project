using ECommercePlatform.Data;
using ECommercePlatform.Models;
using System.Security.Claims;

namespace ECommercePlatform.Services
{
    public class OperationLogService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _http;
        public OperationLogService(ApplicationDbContext context, IHttpContextAccessor http)
        {
            _context = context;
            _http = http;
        }
        public void Log(string controller, string action, string? targetId = null, string? description = null)
        {
            var username = _http.HttpContext?.User?.Identity?.Name;

            // �̾ڵn�J�W�٧�X������ Engineer ����
            var engineer = _context.Engineers.FirstOrDefault(e => e.Username == username);

            var log = new OperationLog
            {
                Engineer = engineer, // �o�̬O����A�ӫDstring
                ActionTime = DateTime.UtcNow,
                Controller = controller,
                Action = action,
                TargetId = targetId,
                Description = description
            };
            _context.OperationLogs.Add(log);
            _context.SaveChanges();
        }
    }
}