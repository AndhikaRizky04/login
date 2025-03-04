using System;
using System.Threading.Tasks;
using login.Models;
using Oracle.ManagedDataAccess.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace login.Services
{
    public class UserService
    {
        private readonly string _connectionString;
        private readonly ILogger<UserService> _logger;

        public UserService(IConfiguration configuration, ILogger<UserService> logger = null)
        {
            _connectionString = configuration.GetConnectionString("OracleConnection");
            _logger = logger;
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            List<User> users = new List<User>();

            try
            {
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    string query = "SELECT id, username, email, role, created_date FROM users ORDER BY id";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                users.Add(new User
                                {
                                    Id = Convert.ToInt32(reader["id"]),
                                    Username = reader["username"].ToString(),
                                    Email = reader["email"].ToString(),
                                    Role = reader["role"]?.ToString() ?? "user",
                                    CreatedDate = Convert.ToDateTime(reader["created_date"])
                                });
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error saat mengambil semua users: {ex.Message}");
            }

            return users;
        }

        public async Task<User> AuthenticateAsync(string username, string password)
        {
            User user = null;

            try
            {
                // Trim input username and password
                username = username?.Trim();
                password = password?.Trim();

                _logger?.LogInformation($"Attempting to authenticate user: {username}");

                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    try
                    {
                        await connection.OpenAsync();
                        _logger?.LogInformation("Database connection opened successfully");

                        // Query untuk debug - tampilkan semua users
                        string debugQuery = "SELECT username, password FROM users";
                        using (OracleCommand debugCmd = new OracleCommand(debugQuery, connection))
                        {
                            using (var debugReader = await debugCmd.ExecuteReaderAsync())
                            {
                                _logger?.LogInformation("Available users in database:");
                                while (await debugReader.ReadAsync())
                                {
                                    string dbUser = debugReader["username"]?.ToString()?.Trim();
                                    string dbPass = debugReader["password"]?.ToString()?.Trim();
                                    _logger?.LogInformation($"DB User: '{dbUser}', DB Pass: '{dbPass}'");
                                }
                            }
                        }

                        // Query utama - case insensitive untuk username
                        string query = "SELECT id, username, email, password, created_date FROM users WHERE LOWER(username) = LOWER(:username)";

                        using (OracleCommand command = new OracleCommand(query, connection))
                        {
                            command.Parameters.Add(new OracleParameter("username", username));

                            using (var reader = await command.ExecuteReaderAsync())
                            {
                                if (await reader.ReadAsync())
                                {
                                    string storedPassword = reader["password"]?.ToString()?.Trim();
                                    string storedUsername = reader["username"]?.ToString()?.Trim();

                                    _logger?.LogInformation($"Found user in database. Stored username: '{storedUsername}', Stored password: '{storedPassword}', Input password: '{password}'");

                                    // Verifikasi password dengan exact matching
                                    if (string.Equals(storedPassword, password))
                                    {
                                        user = new User
                                        {
                                            Id = Convert.ToInt32(reader["id"]),
                                            Username = reader["username"]?.ToString() ?? "UnknownUser",
                                            Email = reader["email"]?.ToString() ?? "noemail@example.com",
                                            Password = storedPassword,
                                            Role = reader["role"]?.ToString() ?? "user",
                                            CreatedDate = reader["created_date"] != DBNull.Value
                                    ? Convert.ToDateTime(reader["created_date"])
                                    : DateTime.Now
                                        };

                                        _logger?.LogInformation($"Authentication successful for user: {username}");
                                    }
                                    else
                                    {
                                        _logger?.LogWarning($"Password mismatch for user: {username}");
                                    }
                                }
                                else
                                {
                                    _logger?.LogWarning($"User not found in database: {username}");
                                }
                            }
                        }
                    }
                    catch (OracleException oraEx)
                    {
                        _logger?.LogError($"Oracle Error {oraEx.Number}: {oraEx.Message}");

                        if (oraEx.Number == 1017)
                        {
                            _logger?.LogError("Invalid username/password untuk koneksi database");
                        }
                        else if (oraEx.Number == 12154)
                        {
                            _logger?.LogError("Database service name tidak ditemukan. Cek TNS dan Data Source");
                        }
                    }
                }

                // Jika tidak berhasil dengan query normal, coba dengan metode alternatif hardcoded credential
                if (user == null)
                {
                    _logger?.LogInformation("Trying hardcoded credentials authentication");

                    bool isValid = false;
                    string role = "user"; // Default role

                    if (string.Equals(username, "admin", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(password, "admin123", StringComparison.Ordinal))
                    {
                        isValid = true;
                        role = "admin"; // HARUS SET ROLE ADMIN
                    }
                    else if (string.Equals(username, "dhika", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(password, "123456", StringComparison.Ordinal))
                    {
                        isValid = true;
                        role = "user";
                    }

                    else if (string.Equals(username, "ameylia", StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(password, "123456", StringComparison.Ordinal))
                    {
                        isValid = true;
                        role = "user";
                    }

                    if (isValid)
                    {
                        user = new User
                        {
                            Id = 0,
                            Username = username,
                            Password = password,
                            Email = $"{username}@example.com",
                            Role = role, // **PASTIKAN ROLE DIISI**
                            CreatedDate = DateTime.Now
                        };

                        _logger?.LogInformation($"Authentication successful using hardcoded credentials for user: {username} with role: {role}");
                    }
                }

            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error umum saat autentikasi: {ex.Message}");
                // Tidak melempar exception agar aplikasi tetap berjalan
            }

            return user;
        }

        // Metode untuk menambahkan user baru
        public async Task<bool> AddUserAsync(User user)
        {
            try
            {
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    _logger?.LogInformation("Koneksi database berhasil dibuka untuk penambahan user");

                    // Verifikasi apakah username sudah ada
                    string checkQuery = "SELECT COUNT(*) FROM users WHERE username = :username";
                    using (OracleCommand checkCommand = new OracleCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.Add(new OracleParameter("username", user.Username));
                        int existingCount = Convert.ToInt32(await checkCommand.ExecuteScalarAsync());

                        if (existingCount > 0)
                        {
                            _logger?.LogWarning($"Username {user.Username} sudah ada di database");
                            return false;
                        }
                    }

                    // Gunakan format kolom lowercase yang sudah terbukti berfungsi
                    string query = "INSERT INTO users (username, password, email, created_date) VALUES (:username, :password, :email, :created_date)";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("username", user.Username));
                        command.Parameters.Add(new OracleParameter("password", user.Password)); // Gunakan langsung dari model
                        command.Parameters.Add(new OracleParameter("email", user.Email ?? $"{user.Username}@example.com")); // Default email jika null
                        command.Parameters.Add(new OracleParameter("created_date", DateTime.Now));

                        int result = await command.ExecuteNonQueryAsync();
                        if (result > 0)
                        {
                            _logger?.LogInformation($"User {user.Username} berhasil ditambahkan");
                            return true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error umum saat menambah user: {ex.Message}");
            }

            return false;
        }

        // Metode untuk test koneksi database
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    _logger?.LogInformation("Koneksi test berhasil");
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Koneksi test gagal: {ex.Message}");
                return false;
            }
        }

        // Metode untuk mendapatkan user berdasarkan username
        public async Task<User> GetUserByUsernameAsync(string username)
        {
            try
            {
                using (OracleConnection connection = new OracleConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    // Gunakan format lowercase dan case-insensitive search
                    string query = "SELECT id, username, email, password, created_date FROM users WHERE LOWER(username) = LOWER(:username)";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("username", username));

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new User
                                {
                                    Id = Convert.ToInt32(reader["id"]),
                                    Username = reader["username"].ToString(),
                                    Email = reader["email"].ToString(),
                                    Password = reader["password"].ToString(),
                                    CreatedDate = Convert.ToDateTime(reader["created_date"])
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError($"Error saat mengambil user by username: {ex.Message}");
            }

            return null;
        }
    }
}