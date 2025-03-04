using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using System.Linq;

namespace login.Pages
{
    public class ConnectionTestSimpleModel : PageModel
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<ConnectionTestSimpleModel> _logger;

        public string ConnectionString { get; set; }
        public string TestResult { get; set; }
        public string ErrorMessage { get; set; }
        public List<string> TableNames { get; set; } = new List<string>();
        public int TableCount { get; set; }

        public ConnectionTestSimpleModel(IConfiguration configuration, ILogger<ConnectionTestSimpleModel> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            try
            {
                // Ambil connection string (mask password untuk tampilan)
                ConnectionString = _configuration.GetConnectionString("OracleConnection");
                var maskedConnectionString = System.Text.RegularExpressions.Regex.Replace(
                    ConnectionString,
                    @"(Password=)[^;]+",
                    "$1******"
                );
                ConnectionString = maskedConnectionString;

                _logger.LogInformation($"Connection string: {maskedConnectionString}");

                using (var connection = new OracleConnection(_configuration.GetConnectionString("OracleConnection")))
                {
                    // Test open connection
                    await connection.OpenAsync();
                    TestResult = "Koneksi berhasil dibuka!";
                    _logger.LogInformation("Database connection successful");

                    // Coba dapatkan daftar tabel
                    try
                    {
                        var tables = await connection.QueryAsync<string>(
                            "SELECT TABLE_NAME FROM ALL_TABLES WHERE OWNER = USER ORDER BY TABLE_NAME"
                        );

                        TableNames = tables.ToList();
                        TableCount = TableNames.Count;

                        // Periksa apakah ada tabel CUST_TRACKING
                        bool custTrackingExists = TableNames.Contains("CUST_TRACKING");
                        if (custTrackingExists)
                        {
                            TestResult += " Tabel CUST_TRACKING ditemukan.";
                        }
                        else
                        {
                            TestResult += " PERINGATAN: Tabel CUST_TRACKING tidak ditemukan.";
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Gagal mendapatkan daftar tabel");
                        TestResult += " Tidak bisa mendapatkan daftar tabel: " + ex.Message;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error testing connection");
                ErrorMessage = $"Koneksi gagal: {ex.Message}";

                if (ex.InnerException != null)
                {
                    ErrorMessage += $"\n\nInner exception: {ex.InnerException.Message}";
                    _logger.LogError(ex.InnerException, "Inner exception details");
                }
            }

            return Page();
        }
    }
}