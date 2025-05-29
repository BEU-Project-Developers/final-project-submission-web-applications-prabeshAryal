using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MusicAppBackend.Data;
using MusicAppBackend.Services;

namespace MusicAppBackend
{
    public class SeedData
    {
        public static async Task Main(string[] args)
        {
            // Create a host builder with the same configuration as your main application
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container (similar to what you have in Program.cs)
            builder.Services.AddDbContext<MusicDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
                sqlServerOptions => sqlServerOptions.CommandTimeout(60)));
            
            // Register required services
            builder.Services.AddScoped<IAuthService, AuthService>();
            
            // Build the service provider
            var app = builder.Build();
            
            Console.WriteLine("Starting database seeding...");
            
            // Initialize the database with seed data
            await DbInitializer.Initialize(app.Services);
            
            Console.WriteLine("Database seeding completed!");
        }
    }
}
