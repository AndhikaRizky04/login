using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;
using login.Models;

namespace login.Repositories
{
    public class ProductCatalogRepository : IProductCatalogRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<ProductCatalogRepository> _logger;

        public ProductCatalogRepository(IConfiguration configuration, ILogger<ProductCatalogRepository> logger)
        {
            _connectionString = configuration.GetConnectionString("OracleConnection");
            _logger = logger;
        }

        public async Task<IEnumerable<ProductCatalogViewModel>> GetProductsForAdminAsync(string searchTerm = null)
        {
            try
            {
                _logger.LogInformation("GetProductsForAdminAsync sedang dijalankan");
                var products = new List<ProductCatalogViewModel>();

                using (var connection = new OracleConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string sql = @"
                    SELECT 
                        NO_JOB,
                        NAMA_JOB, 
                        NAMA, 
                        PO_INT, 
                        NO_PO,
                        OPLAAG,
                        TERKIRIM,
                        WIP_POT,
                        WIP_BRT,
                        WIP_LAM
                    FROM CUST_TRACKING
                    WHERE NAMA IS NOT NULL";

                    // Kondisi pencarian
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        sql += @" AND (
                            UPPER(NAMA_JOB) LIKE UPPER(:SearchTerm) OR 
                            UPPER(PO_INT) LIKE UPPER(:SearchTerm) OR 
                            UPPER(NO_PO) LIKE UPPER(:SearchTerm) OR
                            UPPER(NAMA) LIKE UPPER(:SearchTerm) OR
                            UPPER(NO_JOB) LIKE UPPER(:SearchTerm)
                        )";
                    }

                    // Pengurutan berdasarkan PO
                    sql += " ORDER BY NO_PO";

                    _logger.LogInformation($"Executing SQL: {sql}");
                    _logger.LogInformation($"Search term: {searchTerm ?? "KOSONG"}");

                    using (var command = new OracleCommand(sql, connection))
                    {
                        if (!string.IsNullOrWhiteSpace(searchTerm))
                        {
                            command.Parameters.Add(new OracleParameter("SearchTerm", $"%{searchTerm}%"));
                        }

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var product = new ProductCatalogViewModel
                                {
                                    NoJob = reader["NO_JOB"]?.ToString(),
                                    NamaJob = reader["NAMA_JOB"]?.ToString(),
                                    Nama = reader["NAMA"]?.ToString(),
                                    PoInt = reader["PO_INT"]?.ToString(),
                                    NoPo = reader["NO_PO"]?.ToString(),
                                    Oplaag = ParseDecimal(reader["OPLAAG"]),
                                    Terkirim = ParseDecimal(reader["TERKIRIM"]),
                                    WipPot = ParseDecimal(reader["WIP_POT"]),
                                    WipBrt = ParseDecimal(reader["WIP_BRT"]),
                                    WipLam = ParseDecimal(reader["WIP_LAM"])
                                };
                                products.Add(product);
                            }
                        }
                    }
                }

                _logger.LogInformation($"GetProductsForAdminAsync: Ditemukan {products.Count} produk");
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ERROR in GetProductsForAdminAsync: {ex.Message}");
                throw;
            }
        }

        public async Task<IEnumerable<ProductCatalogViewModel>> GetProductsForUserAsync(string username, string searchTerm = null)
        {
            try
            {
                _logger.LogInformation($"GetProductsForUserAsync untuk user {username}");
                var products = new List<ProductCatalogViewModel>();

                using (var connection = new OracleConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string sql = @"
                    SELECT 
                        NO_JOB,
                        NAMA_JOB, 
                        NAMA, 
                        PO_INT, 
                        NO_PO,
                        OPLAAG,
                        TERKIRIM,
                        WIP_POT,
                        WIP_BRT,
                        WIP_LAM
                    FROM CUST_TRACKING
                    WHERE NAMA IS NOT NULL";

                    // Filter berdasarkan username (kecuali admin)
                    if (!string.IsNullOrEmpty(username) && username.ToLower() != "ameylia")
                    {
                        sql += " AND (NAMA = :Username OR NAMA = 'PT CERES')";
                    }

                    // Kondisi pencarian
                    if (!string.IsNullOrWhiteSpace(searchTerm))
                    {
                        sql += @" AND (
                            UPPER(NAMA_JOB) LIKE UPPER(:SearchTerm) OR 
                            UPPER(PO_INT) LIKE UPPER(:SearchTerm) OR 
                            UPPER(NO_PO) LIKE UPPER(:SearchTerm) OR
                            UPPER(NAMA) LIKE UPPER(:SearchTerm) OR
                            UPPER(NO_JOB) LIKE UPPER(:SearchTerm)
                        )";
                    }

                    // Pengurutan berdasarkan PO
                    sql += " ORDER BY NO_PO";

                    _logger.LogInformation($"Executing SQL for user {username}: {sql}");

                    using (var command = new OracleCommand(sql, connection))
                    {
                        // Tambahkan parameter username jika bukan admin
                        if (!string.IsNullOrEmpty(username) && username.ToLower() != "admin")
                        {
                            command.Parameters.Add(new OracleParameter("Username", username));
                        }

                        // Tambahkan parameter pencarian jika ada
                        if (!string.IsNullOrWhiteSpace(searchTerm))
                        {
                            command.Parameters.Add(new OracleParameter("SearchTerm", $"%{searchTerm}%"));
                        }

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                var product = new ProductCatalogViewModel
                                {
                                    NoJob = reader["NO_JOB"]?.ToString(),
                                    NamaJob = reader["NAMA_JOB"]?.ToString(),
                                    Nama = reader["NAMA"]?.ToString(),
                                    PoInt = reader["PO_INT"]?.ToString(),
                                    NoPo = reader["NO_PO"]?.ToString(),
                                    Oplaag = ParseDecimal(reader["OPLAAG"]),
                                    Terkirim = ParseDecimal(reader["TERKIRIM"]),
                                    WipPot = ParseDecimal(reader["WIP_POT"]),
                                    WipBrt = ParseDecimal(reader["WIP_BRT"]),
                                    WipLam = ParseDecimal(reader["WIP_LAM"])
                                };
                                products.Add(product);
                            }
                        }
                    }
                }

                _logger.LogInformation($"GetProductsForUserAsync: Ditemukan {products.Count} produk untuk user {username}");
                return products;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"ERROR in GetProductsForUserAsync for user {username}: {ex.Message}");
                throw;
            }
        }

        // Helper untuk safely parsing decimal values
        private decimal ParseDecimal(object value)
        {
            if (value == null || value == DBNull.Value)
                return 0;

            // Handle different potential formats
            string strValue = value.ToString().Replace(",", ".");

            if (decimal.TryParse(strValue, System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture, out decimal result))
            {
                return result;
            }

            return 0;
        }
    }
}