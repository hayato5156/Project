using Microsoft.EntityFrameworkCore;
using ECommercePlatform.Data;
using ECommercePlatform.Services;
using ECommercePlatform.Models;

var builder = WebApplication.CreateBuilder(args);

// 資料庫連線設定
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// MVC 與 SignalR
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// 正確註冊 Cookie Schemes（只註冊一次每個名稱）
builder.Services.AddAuthentication()
    .AddCookie("EngineerCookie", options =>
    {
        options.LoginPath = "/Engineer/Login";
        options.LogoutPath = "/Engineer/Logout";
        options.AccessDeniedPath = "/Engineer/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
    })
    .AddCookie("UserCookie", options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(24); // 延長到24小時
    });

// 註冊核心服務
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<OperationLogService>();

// 註冊新的業務服務
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IOrderService, OrderService>();

// 註冊後台任務服務
builder.Services.AddHostedService<InventoryCleanupService>();

// Authorization
builder.Services.AddAuthorization();

// 支援 Operation Log 與 HttpContext
builder.Services.AddHttpContextAccessor();

// 添加 CORS 支援（如果需要 API 調用）
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// 添加這個重要的設定 - 顯示詳細錯誤訊息
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

// 啟用 CORS
app.UseCors();

app.UseAuthentication();
app.UseAuthorization();

// 資料庫初始化和遷移
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        // 確保資料庫存在並執行遷移
        await context.Database.EnsureCreatedAsync();

        // 添加新的遷移處理
        var pendingMigrations = await context.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            logger.LogInformation("正在執行資料庫遷移...");
            await context.Database.MigrateAsync();
            logger.LogInformation("資料庫遷移完成");
        }

        // 初始化種子資料
        DbInitializer.Seed(context);
        logger.LogInformation("種子資料初始化完成");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "資料庫初始化失敗");
        throw;
    }
}

// 添加 API 路由配置
app.MapControllerRoute(
    name: "api",
    pattern: "api/{controller}/{action=Index}/{id?}");

// 預設路由
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// 添加健康檢查端點
app.MapGet("/health", async (ApplicationDbContext context) =>
{
    try
    {
        await context.Database.CanConnectAsync();
        return Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
        return Results.Problem(
            title: "Database connection failed",
            detail: ex.Message,
            statusCode: 503);
    }
});

app.Run();

//後台服務：清理過期庫存預留
public class InventoryCleanupService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<InventoryCleanupService> _logger;
    private readonly TimeSpan _cleanupInterval = TimeSpan.FromMinutes(5); // 每5分鐘執行一次

    public InventoryCleanupService(IServiceProvider serviceProvider, ILogger<InventoryCleanupService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var inventoryService = scope.ServiceProvider.GetRequiredService<IInventoryService>();

                await inventoryService.CleanupExpiredReservationsAsync();

                _logger.LogDebug("庫存預留清理任務完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "庫存預留清理任務執行失敗");
            }

            await Task.Delay(_cleanupInterval, stoppingToken);
        }
    }
}

//擴展的資料庫初始化器
public static class EnhancedDbInitializer
{
    public static void SeedEnhancedData(ApplicationDbContext context)
    {
        // 確保基礎資料存在
        DbInitializer.Seed(context);

        // 添加庫存變動歷史的種子資料
        if (!context.StockMovements.Any() && context.Products.Any())
        {
            var products = context.Products.Take(3).ToList();
            var admin = context.Users.FirstOrDefault(u => u.Role == "Admin");

            var movements = new List<StockMovement>();

            foreach (var product in products)
            {
                // 初始進貨記錄
                movements.Add(new StockMovement
                {
                    ProductId = product.Id,
                    MovementType = StockMovementType.Adjustment_In,
                    Quantity = product.Stock,
                    PreviousStock = 0,
                    NewStock = product.Stock,
                    Reason = "初始進貨",
                    UserId = admin?.Id,
                    CreatedAt = DateTime.UtcNow.AddDays(-30)
                });
            }

            context.StockMovements.AddRange(movements);
            context.SaveChanges();
        }

        // 添加測試用的操作日誌
        if (context.OperationLogs != null && !context.OperationLogs.Any())
        {
            var engineer = context.Engineers?.FirstOrDefault();
            if (engineer != null)
            {
                var logs = new List<OperationLog>
                {
                    new OperationLog
                    {
                        EngineerId = engineer.Id,
                        Controller = "System",
                        Action = "Initialize",
                        Description = "系統初始化完成",
                        ActionTime = DateTime.UtcNow,
                        Timestamp = DateTime.UtcNow
                    }
                };

                context.OperationLogs.AddRange(logs);
                context.SaveChanges();
            }
        }
    }
}

//全域異常處理中介軟體
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "未處理的異常發生在 {Path}", context.Request.Path);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";

        var response = new
        {
            error = "系統發生錯誤，請稍後再試",
            timestamp = DateTime.UtcNow,
            path = context.Request.Path.Value
        };

        await context.Response.WriteAsync(System.Text.Json.JsonSerializer.Serialize(response));
    }
}

// 使用中介軟體的擴展方法
public static class GlobalExceptionMiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalExceptionHandler(this IApplicationBuilder builder)
    {
        return builder.UseMiddleware<GlobalExceptionMiddleware>();
    }
}