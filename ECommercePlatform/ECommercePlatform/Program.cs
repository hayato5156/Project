using Microsoft.EntityFrameworkCore;
using ECommercePlatform.Data;
using ECommercePlatform.Services;

var builder = WebApplication.CreateBuilder(args);

// ��Ʈw�s�u�]�w
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// MVC �P SignalR
builder.Services.AddControllersWithViews();
builder.Services.AddSignalR();

// ���T���U Cookie Schemes�]�u���U�@���C�ӦW�١^
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

// �䴩 Operation Log �P HttpContext
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<OperationLogService>();

var app = builder.Build();

app.UseStaticFiles();

// ���տ�X Featured Products
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var featuredProducts = dbContext.Products.Take(3).ToList();
    Console.WriteLine($"Featured Products Count: {featuredProducts.Count}");
}

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

// �w�]����
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();