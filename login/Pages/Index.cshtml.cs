using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using login.Models;
using login.Services;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace login.Pages
{
    public class IndexModel : PageModel
    {
        private readonly UserService _userService;
        private readonly ILogger<IndexModel> _logger;

        [BindProperty]
        public LoginModel LoginInput { get; set; }

        public IndexModel(UserService userService, ILogger<IndexModel> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        public IActionResult OnGet()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToPage("/Welcome");
            }
            return Page();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            var user = await _userService.AuthenticateAsync(LoginInput.Username, LoginInput.Password);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Username atau password salah.");
                return Page();
            }

            _logger.LogInformation("User {Username} ditemukan dengan role {Role}.", user.Username, user.Role ?? "(null)");

            var claims = new List<Claim>
{
    new Claim(ClaimTypes.Name, user.Username ?? "UnknownUser"),
    new Claim(ClaimTypes.Email, user.Email ?? "noemail@example.com"),
    new Claim("UserId", user.Id.ToString()),
    new Claim(ClaimTypes.Role, user.Role ?? "user") // Pastikan role tidak null
};

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = LoginInput.RememberMe,
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30)
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            _logger.LogInformation("User {Username} berhasil login sebagai {Role}.", user.Username, user.Role);

            // **Cek redirect berdasarkan role**
            if (user.Role?.ToLower() == "admin")
            {
                _logger.LogInformation("Redirecting {Username} ke Admin Dashboard.", user.Username);
                return RedirectToPage("/Admin/Dashboard");
            }
            else
            {
                _logger.LogInformation("Redirecting {Username} ke Welcome Page.", user.Username);
                return RedirectToPage("/Welcome");
            }
        }
    }
}