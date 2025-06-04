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
        public DbSet<ReviewReport> ReviewReports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User 配置 (添加新欄位)
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");

                // 新增欄位配置
                entity.Property(e => e.Role).HasDefaultValue("User");
                entity.Property(e => e.IsActive).HasDefaultValue(true);
            });

            // Review 配置
            modelBuilder.Entity<Review>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("Reviews");

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.IsVisible).HasDefaultValue(true);

                // 外鍵關係
                entity.HasOne(e => e.User)
                      .WithMany(u => u.Reviews)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                      .WithMany() // 假設 Product 沒有 Reviews 導航屬性
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ReplyTo)
                      .WithMany(r => r.Replies)
                      .HasForeignKey(e => e.ReplyId)
                      .OnDelete(DeleteBehavior.Restrict);

                // 建立索引
                entity.HasIndex(e => new { e.UserId, e.ProductId });
                entity.HasIndex(e => e.CreatedAt);
                entity.HasIndex(e => e.Rating);
                entity.HasIndex(e => e.IsVisible);
            });

            // ReviewReport 配置
            modelBuilder.Entity<ReviewReport>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.ToTable("ReviewReports");

                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.IsProcessed).HasDefaultValue(false);

                entity.HasOne(e => e.Review)
                      .WithMany(r => r.Reports)
                      .HasForeignKey(e => e.ReviewId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Reporter)
                      .WithMany()
                      .HasForeignKey(e => e.ReporterId)
                      .OnDelete(DeleteBehavior.Restrict);

                // 索引
                entity.HasIndex(e => e.IsProcessed);
                entity.HasIndex(e => e.CreatedAt);
            });

            // 其他現有實體的配置 (保持不變)
            ConfigureOtherEntities(modelBuilder);
        }

        private void ConfigureOtherEntities(ModelBuilder modelBuilder)
        {
            // Product 配置
            modelBuilder.Entity<Product>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Price).HasColumnType("decimal(18,2)");
                if (entity.Metadata.FindProperty("DiscountPrice") != null)
                {
                    entity.Property(e => e.DiscountPrice).HasColumnType("decimal(18,2)");
                }
            });

            // Order 配置
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.TotalAmount).HasColumnType("decimal(18,2)");
                entity.Property(e => e.OrderDate).HasDefaultValueSql("GETUTCDATE()");

                entity.HasOne(e => e.User)
                      .WithMany(u => u.Orders)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // OrderItem 配置
            modelBuilder.Entity<OrderItem>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.UnitPrice).HasColumnType("decimal(18,2)");

                entity.HasOne(e => e.Order)
                      .WithMany(o => o.OrderItems)
                      .HasForeignKey(e => e.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                      .WithMany()
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // CartItem 配置
            modelBuilder.Entity<CartItem>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.CartItems)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Product)
                      .WithMany()
                      .HasForeignKey(e => e.ProductId)
                      .OnDelete(DeleteBehavior.Cascade);

                // 確保同一用戶對同一商品的購物車項目唯一
                entity.HasIndex(e => new { e.UserId, e.ProductId }).IsUnique();
            });

            // Engineer 配置
            modelBuilder.Entity<Engineer>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Username).IsUnique();
                if (entity.Metadata.FindProperty("Email") != null)
                {
                    entity.HasIndex(e => e.Email).IsUnique();
                }
            });

            // OperationLog 配置
            modelBuilder.Entity<OperationLog>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).ValueGeneratedOnAdd();

                // 配置可選的 Engineer 關係
                entity.HasOne(e => e.Engineer)
                      .WithMany()
                      .HasForeignKey(e => e.EngineerId)
                      .OnDelete(DeleteBehavior.SetNull)
                      .IsRequired(false);

                entity.Property(e => e.ActionTime).HasDefaultValueSql("GETUTCDATE()");
                entity.Property(e => e.Timestamp).HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => e.Timestamp);
                entity.HasIndex(e => e.ActionTime);
                entity.HasIndex(e => e.Controller);
            });

            // DeviceToken 配置
            modelBuilder.Entity<DeviceToken>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.HasIndex(e => e.Token).IsUnique();
                entity.Property(e => e.CreatedAt).HasDefaultValueSql("GETUTCDATE()");
            });
        }
        public static void Seed(ModelBuilder modelBuilder)
        {
            // 預設種子資料
            modelBuilder.Entity<User>().HasData(
                new User { Id = 1, Username = "admin", Email = "admin@example.com", PasswordHash = "admin123", FirstName = "Admin", LastName = "User", Address = "Default Address" },
                new User { Id = 2, Username = "john_doe", Email = "john@example.com", PasswordHash = "password", FirstName = "John", LastName = "Doe", Address = "Default Address" }
            );

            modelBuilder.Entity<Product>().HasData(
                new Product { Id = 1, Name = "Laptop", Description = "High performance laptop", Price = 1500.00m, ImageUrl = "laptop.jpg" },
                new Product { Id = 2, Name = "Smartphone", Description = "Latest model smartphone", Price = 799.99m, ImageUrl = "smartphone.jpg" }
            );

            // 可根據需要新增更多資料
        }
    }
}
