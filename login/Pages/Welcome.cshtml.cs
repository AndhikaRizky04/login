// Welcome.cshtml.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace login.Pages
{
    [Authorize(Policy = "RequireUserRole")] // Pengguna dengan role user atau admin dapat mengakses
    public class WelcomeModel : PageModel
    {
        private readonly ILogger<WelcomeModel> _logger;

        public WelcomeModel(ILogger<WelcomeModel> logger = null)
        {
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            if (!User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Index");
            }

            // Pastikan cache browser tidak menyimpan halaman yang memerlukan autentikasi
            Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("Expires", "0");

            ViewData["ActivePage"] = "Dashboard";
            return Page();
        }

        public async Task<IActionResult> OnPostLogoutAsync()
        {
            _logger?.LogInformation("User {Name} initiated logout from Welcome page.",
                User.Identity?.Name);

            // Hapus autentikasi cookie
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Hapus semua cookies
            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }

            // Hapus sesi
            HttpContext.Session.Clear();

            return RedirectToPage("/Index");
        }
    }
}