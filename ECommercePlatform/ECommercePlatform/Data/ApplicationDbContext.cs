using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ECommercePlatform.Models;

namespace ECommercePlatform.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        public DbSet<User> Users { get; set; }
        public DbSet<Product> Products { get; set; }
        public DbSet<CartItem> CartItems { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<OrderItem> OrderItems { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<DeviceToken> DeviceTokens { get; set; }
        public DbSet<Engineer>? Engineers { get; set; }
        public DbSet<OperationLog>? OperationLogs { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {// 配置模型之間的關聯性 (Fluent API)

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => u.Username).IsUnique();
            });

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.User)
                .WithMany(u => u.CartItems)
                .HasForeignKey(ci => ci.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartItem>()
                .HasOne(ci => ci.Product)
                .WithMany(p => p.CartItems)
                .HasForeignKey(ci => ci.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Product)
                .WithMany(p => p.OrderItems)
                .HasForeignKey(oi => oi.ProductId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<OrderItem>()
                .HasOne(oi => oi.Order)
                .WithMany(o => o.OrderItems)
                .HasForeignKey(oi => oi.OrderId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.Product)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<OperationLog>()
                .HasOne(o => o.Engineer)
                .WithMany(e => e.OperationLogs)
                .HasForeignKey(o => o.EngineerId)
                .OnDelete(DeleteBehavior.SetNull);
            // 執行 Seeding
            Seed(modelBuilder);
        }
            public static void Seed(ModelBuilder modelBuilder)
        {
            // 預設種子資料

            // 建立 Users 資料
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "admin", Email = "admin@example.com", PasswordHash = "admin123", FirstName = "Admin", LastName = "User", Address = "Default Address" },
                new User { Id = 2, Username = "john_doe", Email = "john@example.com", PasswordHash = "password", FirstName = "John", LastName = "Doe", Address = "Default Address" }
            );

            // 建立 Products 資料
            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Laptop", Description = "High performance laptop", Price = 1500.00m, ImageUrl = "laptop.jpg" },
                new Product { Id = 2, Name = "Smartphone", Description = "Latest model smartphone", Price = 799.99m, ImageUrl = "smartphone.jpg" }
            );

            // 可根據需要新增更多資料
        }

    }

        //protected override void OnModelCreating(ModelBuilder builder)
        //{
        //    base.OnModelCreating(builder);

        //    // 配置模型之間的關聯性 (Fluent API)
        //    builder.Entity<CartItem>()
        //        .HasOne(ci => ci.User)
        //        .WithMany(u => u.CartItems)
        //        .HasForeignKey(ci => ci.UserId)
        //        .OnDelete(DeleteBehavior.Cascade);

        //    builder.Entity<CartItem>()
        //        .HasOne(ci => ci.Product)
        //        .WithMany()
        //        .HasForeignKey(ci => ci.ProductId)
        //        .OnDelete(DeleteBehavior.Cascade);

        //    builder.Entity<Order>()
        //        .HasOne(o => o.User)
        //        .WithMany(u => u.Orders)
        //        .HasForeignKey(o => o.UserId)
        //        .OnDelete(DeleteBehavior.Cascade);

        //    builder.Entity<OrderItem>()
        //        .HasOne(oi => oi.Order)
        //        .WithMany(o => o.OrderItems)
        //        .HasForeignKey(oi => oi.OrderId)
        //        .OnDelete(DeleteBehavior.Cascade);

        //    builder.Entity<OrderItem>()
        //        .HasOne(oi => oi.Product)
        //        .WithMany()
        //        .HasForeignKey(oi => oi.ProductId)
        //        .OnDelete(DeleteBehavior.Restrict);

        //    builder.Entity<Review>()
        //        .HasOne(r => r.Product)
        //        .WithMany(p => p.Reviews)
        //        .HasForeignKey(r => r.ProductId)
        //        .OnDelete(DeleteBehavior.Cascade);

        //    builder.Entity<Review>()
        //        .HasOne(r => r.User)
        //        .WithMany(u => u.Reviews)
        //        .HasForeignKey(r => r.UserId)
        //        .OnDelete(DeleteBehavior.Cascade);
    }
