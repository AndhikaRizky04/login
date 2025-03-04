using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using login.Models;
using login.Repositories;
using Microsoft.AspNetCore.Authorization;

namespace login.Pages
{
    [Authorize]
    public class ProductCatalogModel : PageModel
    {
        private readonly IProductCatalogRepository _productRepository;
        private readonly ILogger<ProductCatalogModel> _logger;

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; }

        public List<ProductCatalogViewModel> Products { get; set; } = new List<ProductCatalogViewModel>();

        public string ErrorMessage { get; set; }

        public string CurrentTime { get; set; } = DateTime.Now.ToString("dddd, dd MMMM yyyy - HH:mm:ss");

        public ProductCatalogModel(
            IProductCatalogRepository productRepository,
            ILogger<ProductCatalogModel> logger)
        {
            _productRepository = productRepository;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                _logger.LogInformation($"ProductCatalog OnGetAsync: pencarian dengan '{SearchTerm}'");

                // Dapatkan username dari User.Identity
                var username = User.Identity?.Name;
                _logger.LogInformation($"Current username: {username ?? "tidak ada"}");

                // Cek apakah user adalah admin
                bool isAdmin = username?.ToLower() == "admin";

                // Ambil data produk berdasarkan role
                if (isAdmin)
                {
                    _logger.LogInformation("Fetching products as admin");
                    var adminProducts = await _productRepository.GetProductsForAdminAsync(SearchTerm);
                    Products = adminProducts.ToList();
                }
                else
                {
                    _logger.LogInformation($"Fetching products for user {username}");
                    var userProducts = await _productRepository.GetProductsForUserAsync(username, SearchTerm);
                    Products = userProducts.ToList();
                }

                _logger.LogInformation($"Found {Products.Count} products");

                // Set error message jika tidak ada hasil
                if (Products.Count == 0 && !string.IsNullOrEmpty(SearchTerm))
                {
                    ErrorMessage = $"Tidak ada produk ditemukan dengan kata kunci: {SearchTerm}";
                    _logger.LogWarning(ErrorMessage);
                }

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error in ProductCatalog: {ex.Message}");
                ErrorMessage = "Kesalahan database: " + ex.Message;
                return Page();
            }
        }
    }
}