using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using login.Models;
using login.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace login.Pages
{
    [Authorize]
    public class TrackModel : PageModel
    {
        private readonly ProductTrackingService _trackingService;
        private readonly ILogger<TrackModel> _logger;

        public List<ProductTracking> Products { get; set; } = new List<ProductTracking>();
        public ProductTracking CurrentProduct { get; set; }
        public List<TrackingStep> TrackingSteps { get; set; }

        [BindProperty(SupportsGet = true)]
        public string PoNumber { get; set; }

        public string ErrorMessage { get; set; }
        public bool ShowProductDetail { get; set; }

        public TrackModel(ProductTrackingService trackingService, ILogger<TrackModel> logger)
        {
            _trackingService = trackingService;
            _logger = logger;
            TrackingSteps = ProductTrackingService.GetTrackingSteps();
        }

        public async Task<IActionResult> OnGetAsync(string no_po, string id)
        {
            // Get current username from claims
            string username = User.Identity.Name;
            try
            {
                // Check if we should show detailed view
                if (!string.IsNullOrEmpty(id) && !string.IsNullOrEmpty(no_po))
                {
                    // Get specific product
                    CurrentProduct = await _trackingService.GetProductByIdAsync(username, no_po, id);
                    ShowProductDetail = true;
                    if (CurrentProduct == null)
                    {
                        ErrorMessage = "Produk tidak ditemukan dengan filter yang diberikan";
                        ShowProductDetail = false;
                    }
                }
                else
                {
                    // Get product list
                    ShowProductDetail = false;
                    if (!string.IsNullOrEmpty(no_po))
                    {
                        // Get products filtered by PO
                        Products = await _trackingService.GetProductsByPoAsync(username, no_po);
                    }
                    else
                    {
                        // Get all products
                        Products = await _trackingService.GetProductsAsync(username);
                    }
                }

                return Page();
            }
            catch (Exception ex)
            {
                ErrorMessage = $"Terjadi kesalahan: {ex.Message}";
                _logger.LogError(ex, "Error in Track page");
                return Page();
            }
        }
    }
}