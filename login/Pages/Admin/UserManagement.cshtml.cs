using login.Models;
using login.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace login.Pages.Admin
{
    [Authorize(Policy = "RequireAdminRole")]
    public class UserManagementModel : PageModel
    {
        private readonly UserService _userService;
        private readonly ILogger<UserManagementModel> _logger;

        [BindProperty]
        public User NewUser { get; set; }

        public List<User> Users { get; set; }

        public UserManagementModel(UserService userService, ILogger<UserManagementModel> logger)
        {
            _userService = userService;
            _logger = logger;
            Users = new List<User>();
        }

        public async Task<IActionResult> OnGetAsync()
        {
            Users = await _userService.GetAllUsersAsync();
            return Page();
        }

        public async Task<IActionResult> OnPostAddUserAsync()
        {
            if (!ModelState.IsValid)
            {
                Users = await _userService.GetAllUsersAsync();
                return Page();
            }

            if (string.IsNullOrEmpty(NewUser.Role))
            {
                NewUser.Role = "user";
            }

            var result = await _userService.AddUserAsync(NewUser);
            if (result)
            {
                _logger.LogInformation($"Admin menambahkan user baru: {NewUser.Username} dengan role: {NewUser.Role}");
                return RedirectToPage();
            }
            else
            {
                ModelState.AddModelError(string.Empty, "Gagal menambahkan user. Username mungkin sudah ada.");
                Users = await _userService.GetAllUsersAsync();
                return Page();
            }
        }
    }
}
