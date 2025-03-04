using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;

namespace login.Services
{
    public partial class ProductTrackingService
    {
        // Method to get product names by PO number
        public List<string> GetProductNamesByPo(string poNumber)
        {
            var productNames = new List<string>();

            try
            {
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    string query = "SELECT DISTINCT NAMA_JOB FROM cust_tracking WHERE PO_INT = :poNumber AND NAMA_JOB IS NOT NULL";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("poNumber", poNumber));

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string productName = reader["NAMA_JOB"]?.ToString();
                                if (!string.IsNullOrEmpty(productName))
                                {
                                    productNames.Add(productName.Trim());
                                }
                            }
                        }
                    }
                }

                _logger?.LogInformation($"Retrieved {productNames.Count} product names for PO {poNumber}");
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error retrieving product names: {ex.Message}");
                throw;
            }

            return productNames;
        }
    }
}