using System;
using ECommercePlatform.Data;
using ECommercePlatform.Models;

public static class DbInitializer
{
    public static void Seed(ApplicationDbContext context)
    {
        if (!context.Users.Any())
        {
            context.Users.AddRange(
                new User
                {
                    Username = "johndoe",
                    Email = "john@example.com",
                    PasswordHash = "hashed_password_123",
                    Address = "123 Main St",
                    PhoneNumber = "0987654321",
                    FirstName = "John",
                    LastName = "Doe",
                    CreatedAt = DateTime.Now
                },
                new User
                {
                    Username = "janedoe",
                    Email = "jane@example.com",
                    PasswordHash = "hashed_password_456",
                    Address = "456 Second St",
                    PhoneNumber = "0912345678",
                    FirstName = "Jane",
                    LastName = "Doe",
                    CreatedAt = DateTime.Now
                });
            context.SaveChanges();
        }

        // Add other seed data like Products, Engineers, etc.
    }
}
