using System;
using System.Threading.Tasks;
using login.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Oracle.ManagedDataAccess.Client;

namespace login.Services
{
    public class ComplaintService
    {
        private readonly string _connectionString;
        private readonly ILogger<ComplaintService> _logger;

        public ComplaintService(IConfiguration configuration, ILogger<ComplaintService> logger = null)
        {
            _connectionString = configuration.GetConnectionString("OracleConnection");
            _logger = logger;
        }

        public string GenerateComplaintNumber()
        {
            string today = DateTime.Now.ToString("yyyyMMdd");
            string prefix = "CPL";
            string newNumber = "0001";

            try
            {
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    connection.Open();

                    string pattern = $"{prefix}/{today}/%";
                    string query = @"
                        SELECT MAX(TO_NUMBER(SUBSTR(complaint_number, INSTR(complaint_number, '/', 1, 2) + 1))) AS last_number 
                        FROM complaints 
                        WHERE complaint_number LIKE :pattern";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("pattern", pattern));

                        object result = command.ExecuteScalar();
                        if (result != null && result != DBNull.Value)
                        {
                            int lastNumber = Convert.ToInt32(result);
                            newNumber = (lastNumber + 1).ToString("D4");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error generating complaint number");
                // Default to 0001 if there's an error
            }

            return $"{prefix}/{today}/{newNumber}";
        }

        public async Task<bool> SaveComplaintAsync(Complaint complaint)
        {
            try
            {
                _logger?.LogInformation($"Attempting to save complaint: {complaint.ComplaintNumber}");

                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    _logger?.LogInformation("Database connection opened successfully");

                    // Detailed table and column verification
                    await VerifyTableStructureAsync(connection);

                    string query = @"
                        INSERT INTO complaints (
                            complaint_number, username, subject, description, 
                            created_at, no_po, product_name, company_name, 
                            contact_person, email, file_name, status
                        ) VALUES (
                            :complaint_number, :username, :subject, :description, 
                            :created_at, :no_po, :product_name, :company_name, 
                            :contact_person, :email, :file_name, :status
                        ) RETURNING id INTO :return_id";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        // Add parameters with detailed logging
                        AddParametersWithLogging(command, complaint);

                        // Add return parameter for ID
                        var returnIdParam = new OracleParameter("return_id", OracleDbType.Decimal)
                        {
                            Direction = System.Data.ParameterDirection.Output
                        };
                        command.Parameters.Add(returnIdParam);

                        int result = await command.ExecuteNonQueryAsync();

                        // Log the returned ID
                        var returnedId = returnIdParam.Value;
                        _logger?.LogInformation($"Inserted complaint with ID: {returnedId}");

                        return result > 0;
                    }
                }
            }
            catch (OracleException ex)
            {
                _logger?.LogError(ex, $"Oracle Database Error: {ex.Message}");
                _logger?.LogError($"Error Code: {ex.ErrorCode}");
                _logger?.LogError($"Error Source: {ex.Source}");
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Unexpected error saving complaint");
                throw;
            }
        }

        private void AddParametersWithLogging(OracleCommand command, Complaint complaint)
        {
            try
            {
                // Log all parameter values before adding
                _logger?.LogInformation($"Complaint Number: {complaint.ComplaintNumber}");
                _logger?.LogInformation($"Username: {complaint.Username}");
                _logger?.LogInformation($"Subject: {complaint.Subject}");
                _logger?.LogInformation($"Description: {complaint.Description}");
                _logger?.LogInformation($"Created At: {complaint.CreatedAt}");
                _logger?.LogInformation($"PO Number: {complaint.NoPO}");
                _logger?.LogInformation($"Product Name: {complaint.ProductName}");
                _logger?.LogInformation($"Company Name: {complaint.CompanyName}");
                _logger?.LogInformation($"Contact Person: {complaint.ContactPerson}");
                _logger?.LogInformation($"Email: {complaint.Email}");
                _logger?.LogInformation($"File Name: {complaint.FileName}");

                // Add parameters with null checks
                command.Parameters.Add(new OracleParameter("complaint_number", complaint.ComplaintNumber ?? string.Empty));
                command.Parameters.Add(new OracleParameter("username", complaint.Username ?? string.Empty));
                command.Parameters.Add(new OracleParameter("subject", complaint.Subject ?? string.Empty));
                command.Parameters.Add(new OracleParameter("description", complaint.Description ?? string.Empty));
                command.Parameters.Add(new OracleParameter("created_at", complaint.CreatedAt));
                command.Parameters.Add(new OracleParameter("no_po", complaint.NoPO ?? string.Empty));
                command.Parameters.Add(new OracleParameter("product_name", complaint.ProductName ?? string.Empty));
                command.Parameters.Add(new OracleParameter("company_name", complaint.CompanyName ?? string.Empty));
                command.Parameters.Add(new OracleParameter("contact_person", complaint.ContactPerson ?? string.Empty));
                command.Parameters.Add(new OracleParameter("email", complaint.Email ?? string.Empty));
                command.Parameters.Add(new OracleParameter("file_name", complaint.FileName ?? (object)DBNull.Value));
                command.Parameters.Add(new OracleParameter("status", "New"));
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error adding parameters");
                throw;
            }
        }

        private async Task VerifyTableStructureAsync(OracleConnection connection)
        {
            try
            {
                // Detailed table structure check
                string checkStructureQuery = @"
                    SELECT 
                        column_name, 
                        data_type, 
                        nullable, 
                        data_default
                    FROM USER_TAB_COLUMNS 
                    WHERE table_name = 'COMPLAINTS'
                    ORDER BY column_id";

                using (OracleCommand checkCommand = new OracleCommand(checkStructureQuery, connection))
                {
                    using (var reader = await checkCommand.ExecuteReaderAsync())
                    {
                        _logger?.LogInformation("Complaints Table Structure:");
                        while (await reader.ReadAsync())
                        {
                            string columnName = reader["COLUMN_NAME"].ToString();
                            string dataType = reader["DATA_TYPE"].ToString();
                            string nullable = reader["NULLABLE"].ToString();
                            string defaultValue = reader["DATA_DEFAULT"].ToString();

                            _logger?.LogInformation(
                                $"Column: {columnName}, " +
                                $"Type: {dataType}, " +
                                $"Nullable: {nullable}, " +
                                $"Default Value: {defaultValue}");
                        }
                    }
                }

                // Additional verification for identity column
                string identityCheck = @"
                    SELECT 
                        column_name, 
                        identity_column
                    FROM USER_TAB_COLUMNS 
                    WHERE table_name = 'COMPLAINTS' AND identity_column = 'YES'";

                using (OracleCommand identityCommand = new OracleCommand(identityCheck, connection))
                {
                    var identityResult = await identityCommand.ExecuteScalarAsync();
                    if (identityResult == null)
                    {
                        _logger?.LogWarning("No identity column found!");
                        // Attempt to recreate table with proper identity column
                        await RecreateTableWithIdentityAsync(connection);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error verifying table structure");
                throw;
            }
        }

        private async Task RecreateTableWithIdentityAsync(OracleConnection connection)
        {
            try
            {
                _logger?.LogWarning("Attempting to recreate table with proper identity column");

                // Drop existing table
                string dropTableQuery = "DROP TABLE complaints CASCADE CONSTRAINTS";
                using (OracleCommand dropCommand = new OracleCommand(dropTableQuery, connection))
                {
                    await dropCommand.ExecuteNonQueryAsync();
                }

                // Recreate table with proper identity column
                string createTableQuery = @"
                    CREATE TABLE complaints (
                        id NUMBER GENERATED ALWAYS AS IDENTITY PRIMARY KEY,
                        complaint_number VARCHAR2(50) NOT NULL,
                        username VARCHAR2(100) NOT NULL,
                        subject VARCHAR2(200) NOT NULL,
                        description CLOB NOT NULL,
                        created_at TIMESTAMP NOT NULL,
                        no_po VARCHAR2(50) NOT NULL,
                        product_name VARCHAR2(200) NOT NULL,
                        company_name VARCHAR2(200) NOT NULL,
                        contact_person VARCHAR2(100) NOT NULL,
                        email VARCHAR2(100) NOT NULL,
                        file_name VARCHAR2(255),
                        status VARCHAR2(20) DEFAULT 'New',
                        updated_at TIMESTAMP DEFAULT SYSTIMESTAMP
                    )";

                using (OracleCommand createCommand = new OracleCommand(createTableQuery, connection))
                {
                    await createCommand.ExecuteNonQueryAsync();
                }

                _logger?.LogInformation("Recreated complaints table with proper identity column");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error recreating table");
                throw;
            }
        }
    }
}