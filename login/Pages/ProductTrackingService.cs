using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using login.Models;
using Oracle.ManagedDataAccess.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace login.Services
{
    public partial class ProductTrackingService
    {
        private readonly string _connectionString;
        private readonly ILogger<ProductTrackingService> _logger;

        public ProductTrackingService(IConfiguration configuration, ILogger<ProductTrackingService> logger = null)
        {
            _connectionString = configuration.GetConnectionString("OracleConnection");
            _logger = logger;
        }

        // Check if user has permission to view tracking data
        private bool HasTrackingPermission(string username)
        {
            // Only admin and ameylia have permission to view tracking data
            return username.ToLower() == "admin" || username.ToLower() == "ameylia";
        }

        // Get all products for a user
        public async Task<List<ProductTracking>> GetProductsAsync(string username)
        {
            var products = new List<ProductTracking>();

            try
            {
                // Check if user has permission to view tracking data
                if (!HasTrackingPermission(username))
                {
                    _logger?.LogWarning($"Unauthorized access attempt by user {username}");
                    return products; // Return empty list for unauthorized users
                }

                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT * FROM cust_tracking WHERE NAMA IS NOT NULL";

                    // If not admin, filter by username
                    OracleCommand command;
                    if (username.ToLower() == "ameylia")
                    {
                        // ameylia can see all products
                        command = new OracleCommand(query, connection);
                    }
                    else
                    {
                        // admin should also see all products
                        command = new OracleCommand(query, connection);
                    }

                    command.CommandText += " ORDER BY NO_JOB";

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            products.Add(MapProductFromReader(reader));
                        }
                    }
                }

                _logger?.LogInformation($"Retrieved {products.Count} products for user {username}");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error retrieving products: {ex.Message}");
            }

            return products;
        }

        // Get products by PO number
        public async Task<List<ProductTracking>> GetProductsByPoAsync(string username, string poNumber)
        {
            var products = new List<ProductTracking>();

            try
            {
                // Check if user has permission to view tracking data
                if (!HasTrackingPermission(username))
                {
                    _logger?.LogWarning($"Unauthorized access attempt for PO search by user {username}");
                    return products; // Return empty list for unauthorized users
                }

                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT * FROM cust_tracking WHERE NAMA IS NOT NULL AND PO_INT = :poNumber";
                    query += " ORDER BY NO_JOB";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("poNumber", poNumber));

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                products.Add(MapProductFromReader(reader));
                            }
                        }
                    }
                }

                _logger?.LogInformation($"Retrieved {products.Count} products with PO {poNumber} for user {username}");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error retrieving products by PO: {ex.Message}");
            }

            return products;
        }

        // Get product by PO number and ID
        public async Task<ProductTracking> GetProductByIdAsync(string username, string poNumber, string id)
        {
            try
            {
                // Check if user has permission to view tracking data
                if (!HasTrackingPermission(username))
                {
                    _logger?.LogWarning($"Unauthorized access attempt for product detail by user {username}");
                    return null; // Return null for unauthorized users
                }

                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT * FROM cust_tracking WHERE PO_INT = :poNumber AND NO_JOB = :id";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("poNumber", poNumber));
                        command.Parameters.Add(new OracleParameter("id", id));

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return MapProductFromReader(reader);
                            }
                        }
                    }
                }

                _logger?.LogInformation($"Retrieved product with PO {poNumber} and ID {id}");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error retrieving product details: {ex.Message}");
            }

            return null;
        }

        // Helper to map reader data to product object
        private ProductTracking MapProductFromReader(OracleDataReader reader)
        {
            return new ProductTracking
            {
                NO_JOB = reader["NO_JOB"]?.ToString(),
                NAMA_JOB = reader["NAMA_JOB"]?.ToString(),
                NAMA = reader["NAMA"]?.ToString(),
                NO_PO = reader["NO_PO"]?.ToString(),
                PO_INT = reader["PO_INT"]?.ToString(),
                OPLAAG = ParseDecimal(reader["OPLAAG"]),
                TERKIRIM = ParseDecimal(reader["TERKIRIM"]),
                WIP_POT = ParseDecimal(reader["WIP_POT"]),
                WIP_BRT = ParseDecimal(reader["WIP_BRT"]),
                WIP_TMR = ParseDecimal(reader["WIP_TMR"]),
                WIP_CEL = ParseDecimal(reader["WIP_CEL"]),
                WIP_VAR = ParseDecimal(reader["WIP_VAR"]),
                WIP_LAM = ParseDecimal(reader["WIP_LAM"]),
                WIP_FOI = ParseDecimal(reader["WIP_FOI"]),
                WIP_PON = ParseDecimal(reader["WIP_PON"]),
                WIP_LIP = ParseDecimal(reader["WIP_LIP"]),
                WIP_CBT = ParseDecimal(reader["WIP_CBT"]),
                WIP_PET = ParseDecimal(reader["WIP_PET"]),
                WIP_FNA = ParseDecimal(reader["WIP_FNA"]),
                WIP_FND = ParseDecimal(reader["WIP_FND"]),
                WIP_FNB = ParseDecimal(reader["WIP_FNB"])
            };
        }

        // Helper to safely parse decimal values
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

        // Get tracking step definitions
        public static List<TrackingStep> GetTrackingSteps()
        {
            return new List<TrackingStep>
            {
                new TrackingStep { Key = "WIP_POT", Label = "Potong Putihan" },
                new TrackingStep { Key = "WIP_BRT", Label = "Cetak Barat" },
                new TrackingStep { Key = "WIP_TMR", Label = "Cetak Timur" },
                new TrackingStep { Key = "WIP_CEL", Label = "Cello" },
                new TrackingStep { Key = "WIP_VAR", Label = "Varnish" },
                new TrackingStep { Key = "WIP_LAM", Label = "Laminasi" },
                new TrackingStep { Key = "WIP_FOI", Label = "Foil" },
                new TrackingStep { Key = "WIP_PON", Label = "Ponz" },
                new TrackingStep { Key = "WIP_LIP", Label = "Lipat" },
                new TrackingStep { Key = "WIP_CBT", Label = "Cabut" },
                new TrackingStep { Key = "WIP_PET", Label = "Potong e-ticket" },
                new TrackingStep { Key = "WIP_FNA", Label = "Finishing A" },
                new TrackingStep { Key = "WIP_FND", Label = "Finishing B" },
                new TrackingStep { Key = "WIP_FNB", Label = "Finishing C" }
            };
        }
    }
}