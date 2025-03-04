using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using login.Models;
using login.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Logging;
namespace login.Pages
{
    public class ComplaintsModel : PageModel
    {
        private readonly ProductTrackingService _trackingService;
        private readonly ComplaintService _complaintService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ComplaintsModel> _logger;
        [BindProperty]
        public Complaint Complaint { get; set; }
        [BindProperty]
        public IFormFile UploadedFile { get; set; }
        public string ComplaintNumber { get; set; }
        public string Message { get; set; }
        public string AlertClass { get; set; }
        public SelectList ProductList { get; set; }
        public ComplaintsModel(
            ProductTrackingService trackingService,
            ComplaintService complaintService,
            IWebHostEnvironment environment,
            ILogger<ComplaintsModel> logger)
        {
            _trackingService = trackingService;
            _complaintService = complaintService;
            _environment = environment;
            _logger = logger;
            Complaint = new Complaint();
        }
        public void OnGet()
        {
            ComplaintNumber = _complaintService.GenerateComplaintNumber();
            ProductList = new SelectList(new List<string>());
        }

        public JsonResult OnGetGetProductName(string noPo)
        {
            if (string.IsNullOrEmpty(noPo))
            {
                return new JsonResult(new { success = false, message = "Nomor PO tidak valid" });
            }
            try
            {
                var products = _trackingService.GetProductNamesByPo(noPo);
                if (products != null && products.Count > 0)
                {
                    var productItems = products.Select(p => new { productName = p }).ToList();
                    return new JsonResult(new { success = true, products = productItems });
                }
                return new JsonResult(new { success = false, message = "Produk tidak ditemukan" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product names for PO: {PO}", noPo);
                return new JsonResult(new { success = false, message = "Terjadi kesalahan saat memuat data" });
            }
        }

        public async Task<IActionResult> OnPostSubmitAsync()
        {
            try
            {
                // Generate Complaint Number
                Complaint.ComplaintNumber = _complaintService.GenerateComplaintNumber();

                // Set CreatedAt to current time
                Complaint.CreatedAt = DateTime.Now;

                // Handle File Upload
                if (UploadedFile != null && UploadedFile.Length > 0)
                {
                    // Validate file size (5MB max)
                    if (UploadedFile.Length > 5 * 1024 * 1024)
                    {
                        Message = "File size exceeds 5MB limit.";
                        AlertClass = "alert alert-danger";
                        return Page();
                    }
                    // Validate file extension
                    var allowedExtensions = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
                    var fileExtension = Path.GetExtension(UploadedFile.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(fileExtension))
                    {
                        Message = "Invalid file type. Only PDF, JPG, JPEG, and PNG are allowed.";
                        AlertClass = "alert alert-danger";
                        return Page();
                    }
                    // Create unique filename
                    var uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
                    var uploadFolder = Path.Combine(_environment.WebRootPath, "uploads", "complaints");

                    // Create directory if it doesn't exist
                    Directory.CreateDirectory(uploadFolder);

                    var filePath = Path.Combine(uploadFolder, uniqueFileName);

                    // Save file
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await UploadedFile.CopyToAsync(fileStream);
                    }
                    // Store relative path in the database
                    Complaint.FileName = Path.Combine("uploads", "complaints", uniqueFileName);
                }
                // Save Complaint
                bool saveResult = await _complaintService.SaveComplaintAsync(Complaint);
                if (saveResult)
                {
                    Message = "Pengaduan berhasil disimpan.";
                    AlertClass = "alert alert-success";
                    return Page();
                }
                else
                {
                    Message = "Gagal menyimpan pengaduan.";
                    AlertClass = "alert alert-danger";
                    return Page();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting complaint");
                Message = "Terjadi kesalahan: " + ex.Message;
                AlertClass = "alert alert-danger";
                return Page();
            }
        }
    }
}