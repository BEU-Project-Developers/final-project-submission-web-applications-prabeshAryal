// Program.cs
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MusicAppBackend.Data;
using MusicAppBackend.Middleware;
using MusicAppBackend.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        options.JsonSerializerOptions.WriteIndented = true;
    });

// Configure SQL Server connection
builder.Services.AddDbContext<MusicDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
    sqlServerOptions => sqlServerOptions.CommandTimeout(60)));

// Configure authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "MusicAppIssuer",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "MusicAppAudience",
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "MusicAppSecretKey12345678901234567890"))
    };
});

// Configure authorization
builder.Services.AddAuthorization();

// Register services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IFileStorageService, FileStorageService>();

// Register content type provider for file handling
builder.Services.AddSingleton<IContentTypeProvider, FileExtensionContentTypeProvider>();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Configure Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Music App API", Version = "v1" });
    
    // Configure Swagger to use JWT Authentication
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme. Example: 'Bearer {token}'",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Ensure database is created and migrations are applied in a safe way
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MusicDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    
    try
    {
        logger.LogInformation("Starting database setup...");
        
        // Check if we can connect to the database server
        logger.LogInformation("Testing database connection...");
        
        // Apply any pending migrations
        var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
        if (pendingMigrations.Any())
        {
            logger.LogInformation($"Applying {pendingMigrations.Count()} pending migrations...");
            await dbContext.Database.MigrateAsync();
            logger.LogInformation("Database migrations applied successfully.");
        }
        else
        {
            logger.LogInformation("Database is up to date.");
        }
        
        // Verify database connection
        if (await dbContext.Database.CanConnectAsync())
        {
            logger.LogInformation("Successfully connected to the database.");
            
            // Initialize database and seed data
            /*try
            {
                logger.LogInformation("Initializing database with seed data...");
                await DbInitializer.Initialize(app.Services);
                logger.LogInformation("Database initialization completed successfully.");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Database initialization failed. The application will start without initial data.");
            }*/
        }
        else
        {
            logger.LogError("Cannot connect to the database. Please check connection string and server availability.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "An error occurred while setting up the database. The application will continue but database functionality may be limited.");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Add global exception handling
app.UseMiddleware<MusicAppBackend.Middleware.GlobalExceptionHandlingMiddleware>();

app.UseHttpsRedirection();

// Use CORS
app.UseCors();

// Serve static files
app.UseStaticFiles();

// Use authentication and authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// In Program.cs after app initialization
app.UseApiDocumentation();

// Redirect to Swagger UI
app.Use(async (context, next) =>
{
    if (context.Request.Path.Value?.ToLower() == "/api" || 
        context.Request.Path.Value == "/")
    {
        context.Response.Redirect("/swagger");
        return;
    }

    await next();
});

app.Run();