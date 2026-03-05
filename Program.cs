using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.EntityFrameworkCore;
using Rota2.Data;
using Rota2.Services;
using Rota2.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IUserService, UserService>();
// provide access to HttpContext for auth provider to read cookies on full refresh
builder.Services.AddHttpContextAccessor();
// add in-memory caching used for rota admin checks
builder.Services.AddMemoryCache();
// Register HttpClient for server-side components via IHttpClientFactory
builder.Services.AddHttpClient();
builder.Services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient());
// Configure cookie authentication so server can issue signed cookies
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/login";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.Lax;
        // In production consider CookieSecurePolicy.Always
        options.Cookie.SecurePolicy = Microsoft.AspNetCore.Http.CookieSecurePolicy.SameAsRequest;
    });
// Register the concrete provider and ensure the AuthenticationStateProvider
// resolves to the same instance so NotifyAuthenticationStateChanged works
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddScoped<IRotaService, RotaService>();
builder.Services.AddScoped<ISwapService, SwapService>();
// simple email service (placeholder)
builder.Services.AddSingleton<Rota2.Services.IEmailService, Rota2.Services.EmailService>();
builder.Services.AddAuthorizationCore();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    // Apply EF Core migrations to bring the database schema up to date.
    // Create migrations and update the database with the CLI:
    //   dotnet tool install --global dotnet-ef
    //   dotnet ef migrations add AddRotas
    //   dotnet ef database update
    // Avoid automatically deleting the DB here to preserve data.
    db.Database.Migrate();
    var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
    // Seed default user if none exist
    if (!userService.GetAllUsers().Any())
    {
        userService.CreateUser(new User
        {
            Name = "Default Admin",
            Email = "admin@example.com",
            Role = UserRole.None,
            Wte = 1.0m,
            Active = true,
            IsGlobalAdmin = true
        }, "passw0rd");
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();

app.MapControllers();
app.MapBlazorHub();
app.MapFallbackToPage("/_Host");

app.Run();
