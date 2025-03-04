// Dashboard.cshtml.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace login.Pages.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    public class DashboardModel : PageModel
    {
        private readonly ILogger<DashboardModel> _logger;

        public DashboardModel(ILogger<DashboardModel> logger = null)
        {
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            // Pastikan cache browser tidak menyimpan halaman yang memerlukan autentikasi
            Response.Headers.Append("Cache-Control", "no-cache, no-store, must-revalidate");
            Response.Headers.Append("Pragma", "no-cache");
            Response.Headers.Append("Expires", "0");

            ViewData["ActivePage"] = "AdminDashboard";
            return Page();
        }
    }
}