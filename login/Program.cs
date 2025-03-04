using Microsoft.AspNetCore.Authentication.Cookies;
using login.Services;
using login.Pages;
using login.Repositories;
using login.Models;
using login.Services; // Pastikan namespace ini sesuai dengan ComplaintService
var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddScoped<ProductTrackingService>();
builder.Services.AddTransient<ComplaintService>();
builder.Services.AddScoped<IProductCatalogRepository, ProductCatalogRepository>();

// Di dalam Program.cs, tambahkan konfigurasi policy untuk role
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("RequireAdminRole", policy => policy.RequireRole("admin"));
    options.AddPolicy("RequireUserRole", policy => policy.RequireRole("user", "admin")); // Admin dapat mengakses halaman user
});

// Tambahkan service autentikasi dengan perbaikan
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Index";
        options.LogoutPath = "/Account/Logout"; // Pastikan path sesuai dengan struktur folder
        options.AccessDeniedPath = "/AccessDenied";
        options.Cookie.Name = "LoginAuth";
        options.Cookie.HttpOnly = true;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(30);
        options.SlidingExpiration = true;

        // Tambahkan pengaturan keamanan cookie
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;

        // Gunakan event SigningOut sebagai pengganti (yang benar)
        options.Events = new CookieAuthenticationEvents
        {
            OnSigningOut = async context =>
            {
                // Hapus semua cookies klien saat logout
                foreach (var cookie in context.Request.Cookies.Keys)
                {
                    context.Response.Cookies.Delete(cookie);
                }

                await Task.CompletedTask;
            }
        };
    });

// Daftarkan service user
builder.Services.AddScoped<UserService>();

// Tambahkan session dengan perbaikan keamanan
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Tambahkan antiforgery protection
builder.Services.AddAntiforgery(options =>
{
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    // Dalam mode development, tambahkan developer exception page
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

// Tambahkan header keamanan
app.Use(async (context, next) =>
{
    // Mencegah browser menyimpan cache halaman yang memerlukan autentikasi
    context.Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
    context.Response.Headers.Append("Pragma", "no-cache");
    context.Response.Headers.Append("Expires", "0");

    await next();
});

app.UseRouting();

// Aktifkan autentikasi dan otorisasi
app.UseAuthentication();
app.UseAuthorization();

// Aktifkan session
app.UseSession();

app.MapRazorPages();

app.Run();