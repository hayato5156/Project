using System;
using ECommercePlatform.Data;
using ECommercePlatform.Models;

public static class DbInitializer
{
    public static void Seed(ApplicationDbContext context)
    {
        // 確保資料庫已創建
        context.Database.EnsureCreated();

        // 添加用戶資料
        if (!context.Users.Any())
        {
            context.Users.AddRange(
                new User
                {
                    Username = "admin",
                    Email = "admin@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("admin123"), // 使用 BCrypt 加密
                    Role = "Admin",
                    Address = "123 Admin St",
                    PhoneNumber = "0987654321",
                    FirstName = "Admin",
                    LastName = "User",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new User
                {
                    Username = "johndoe",
                    Email = "john@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    Role = "User",
                    Address = "123 Main St",
                    PhoneNumber = "0987654321",
                    FirstName = "John",
                    LastName = "Doe",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                },
                new User
                {
                    Username = "janedoe",
                    Email = "jane@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password456"),
                    Role = "User",
                    Address = "456 Second St",
                    PhoneNumber = "0912345678",
                    FirstName = "Jane",
                    LastName = "Doe",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                });
            context.SaveChanges();
        }

        // 添加商品資料（包含庫存）
        if (!context.Products.Any())
        {
            context.Products.AddRange(
                new Product
                {
                    Name = "iPhone 15 Pro",
                    Description = "最新款 iPhone，配備 A17 Pro 晶片，鈦金屬設計。",
                    Price = 36900m,
                    DiscountPrice = 34900m,
                    DiscountStart = DateTime.UtcNow,
                    DiscountEnd = DateTime.UtcNow.AddDays(30),
                    ImageUrl = "/images/iphone15pro.jpg",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Stock = 50 // 加入庫存
                },
                new Product
                {
                    Name = "MacBook Air M3",
                    Description = "輕薄強效的 MacBook Air，搭載 M3 晶片。",
                    Price = 41900m,
                    ImageUrl = "/images/macbook-air-m3.jpg",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Stock = 30 // 加入庫存
                },
                new Product
                {
                    Name = "AirPods Pro 2",
                    Description = "主動降噪無線耳機，全新 H2 晶片。",
                    Price = 7490m,
                    DiscountPrice = 6990m,
                    DiscountStart = DateTime.UtcNow,
                    DiscountEnd = DateTime.UtcNow.AddDays(15),
                    ImageUrl = "/images/airpods-pro-2.jpg",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Stock = 100 // 加入庫存
                },
                new Product
                {
                    Name = "iPad Air",
                    Description = "功能強大的 iPad Air，配備 M1 晶片。",
                    Price = 18900m,
                    ImageUrl = "/images/ipad-air.jpg",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Stock = 25 // 加入庫存
                },
                new Product
                {
                    Name = "Apple Watch Series 9",
                    Description = "最先進的 Apple Watch，內建 S9 晶片。",
                    Price = 12900m,
                    DiscountPrice = 11900m,
                    DiscountStart = DateTime.UtcNow,
                    DiscountEnd = DateTime.UtcNow.AddDays(20),
                    ImageUrl = "/images/apple-watch-9.jpg",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Stock = 75 // 加入庫存
                },
                new Product
                {
                    Name = "Magic Keyboard",
                    Description = "適用於 iPad 的 Magic Keyboard，提供絕佳打字體驗。",
                    Price = 10900m,
                    ImageUrl = "/images/magic-keyboard.jpg",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Stock = 15 // 加入庫存
                },
                // 額外的測試商品
                new Product
                {
                    Name = "限量商品測試",
                    Description = "僅剩3件的限量商品，用於測試庫存不足功能。",
                    Price = 999m,
                    DiscountPrice = 799m,
                    DiscountStart = DateTime.UtcNow,
                    DiscountEnd = DateTime.UtcNow.AddDays(7),
                    ImageUrl = "/images/limited-product.jpg",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Stock = 3 // 🚨 低庫存測試
                },
                new Product
                {
                    Name = "已售完商品",
                    Description = "用於測試已售完商品的顯示。",
                    Price = 1999m,
                    ImageUrl = "/images/sold-out.jpg",
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    Stock = 0 // ❌ 已售完
                }
            );
            context.SaveChanges();
        }

        // 添加工程師資料 (如果需要)
        if (context.Engineers != null && !context.Engineers.Any())
        {
            context.Engineers.AddRange(
                new Engineer
                {
                    Username = "engineer1",
                    Email = "engineer1@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("engineer123")
                },
                new Engineer
                {
                    Username = "engineer2",
                    Email = "engineer2@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("engineer456")
                }
            );
            context.SaveChanges();
        }

        // 添加示例評價資料
        if (!context.Reviews.Any() && context.Products.Any() && context.Users.Any())
        {
            var iphone = context.Products.First(p => p.Name.Contains("iPhone"));
            var macbook = context.Products.First(p => p.Name.Contains("MacBook"));
            var airpods = context.Products.First(p => p.Name.Contains("AirPods"));

            var john = context.Users.First(u => u.Username == "johndoe");
            var jane = context.Users.First(u => u.Username == "janedoe");
            var admin = context.Users.First(u => u.Username == "admin");

            context.Reviews.AddRange(
                // iPhone 評價
                new Review
                {
                    UserId = john.Id,
                    ProductId = iphone.Id,
                    UserName = john.Username,
                    Content = "iPhone 15 Pro 真的很棒！相機品質提升很多，鈦金屬設計很有質感。",
                    Rating = 5,
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    IsVisible = true
                },
                new Review
                {
                    UserId = jane.Id,
                    ProductId = iphone.Id,
                    UserName = jane.Username,
                    Content = "價格有點高，但是性能確實很好，建議等特價再買。",
                    Rating = 4,
                    CreatedAt = DateTime.UtcNow.AddDays(-8),
                    IsVisible = true
                },

                // MacBook 評價
                new Review
                {
                    UserId = admin.Id,
                    ProductId = macbook.Id,
                    UserName = admin.Username,
                    Content = "M3 晶片的效能真的很驚人，編譯速度快很多！",
                    Rating = 5,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    IsVisible = true
                },

                // AirPods 評價
                new Review
                {
                    UserId = jane.Id,
                    ProductId = airpods.Id,
                    UserName = jane.Username,
                    Content = "降噪效果很棒，音質也很好，值得購買！",
                    Rating = 5,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    IsVisible = true
                },
                new Review
                {
                    UserId = john.Id,
                    ProductId = airpods.Id,
                    UserName = john.Username,
                    Content = "整體不錯，但是價格還是偏高。",
                    Rating = 4,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    IsVisible = true
                }
            );
            context.SaveChanges();
        }
    }
}