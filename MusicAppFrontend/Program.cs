using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using MusicApp.Data;
using MusicApp.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

// Add in-memory database
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseInMemoryDatabase("MusicAppDb"));

// Add HttpClient for API communication
builder.Services.AddHttpClient("MusicApi");

// Add HttpContextAccessor
builder.Services.AddHttpContextAccessor();

// Register application services
builder.Services.AddScoped<ApiService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<FileUploadService>();

// Add authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.Cookie.Name = "MusicApp.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always; // Use Always in production
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = true;
        options.Cookie.IsEssential = true;
        options.Events = new CookieAuthenticationEvents
        {
            OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = 401;
                    return Task.CompletedTask;
                }
                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            }
        };
    });

// Configure API settings
builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));

var app = builder.Build();

// Seed the database
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var dbContext = services.GetRequiredService<ApplicationDbContext>();
    
    // Make sure the database is created and seeded
    dbContext.Database.EnsureCreated();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Add proxy middleware for /uploads/* requests
app.Use(async (context, next) =>
{
    if (context.Request.Path.StartsWithSegments("/uploads"))
    {
        var httpClient = context.RequestServices.GetRequiredService<IHttpClientFactory>().CreateClient();
        var backendUrl = $"http://localhost:5117{context.Request.Path}{context.Request.QueryString}";
        
        try
        {
            var response = await httpClient.GetAsync(backendUrl);
            
            if (response.IsSuccessStatusCode)
            {
                context.Response.StatusCode = (int)response.StatusCode;
                context.Response.ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/octet-stream";
                
                // Copy response headers
                foreach (var header in response.Headers)
                {
                    context.Response.Headers.TryAdd(header.Key, header.Value.ToArray());
                }
                
                // Copy content headers
                foreach (var header in response.Content.Headers)
                {
                    if (header.Key != "Content-Type") // Already set above
                    {
                        context.Response.Headers.TryAdd(header.Key, header.Value.ToArray());
                    }
                }
                
                // Stream the content
                await response.Content.CopyToAsync(context.Response.Body);
                return;
            }
            else
            {
                context.Response.StatusCode = (int)response.StatusCode;
                await context.Response.WriteAsync($"Backend returned: {response.StatusCode}");
                return;
            }
        }
        catch (Exception ex)
        {
            context.Response.StatusCode = 502; // Bad Gateway
            await context.Response.WriteAsync($"Proxy error: {ex.Message}");
            return;
        }
    }
    
    await next();
});

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
