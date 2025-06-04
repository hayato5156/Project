using Microsoft.EntityFrameworkCore;
using ECommercePlatform.Data;
using ECommercePlatform.Services;

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
        options.ExpireTimeSpan = TimeSpan.FromHours(1);
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();

// 支援 Operation Log 與 HttpContext
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<OperationLogService>();

var app = builder.Build();

app.UseStaticFiles();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var featuredProducts = dbContext.Products.Take(3).ToList();
    Console.WriteLine($"Featured Products Count: {featuredProducts.Count}");
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// 預設路由
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
