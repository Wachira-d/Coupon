using System;
using System.Data;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Coupon.Helpers
{
    /// <summary>
    /// Hybrid Authentication Service
    /// Uses local cache + AuthenAPI for AD verification
    /// </summary>
    public class AuthenticationHelper
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        private static readonly DatabaseHelper _dbHelper = new DatabaseHelper();

        // Configuration
        private static readonly string API_BASE_URL = ConfigurationManager.AppSettings["AuthenApiBaseUrl"] ?? "https://authen-api.com/api";
        private static readonly string API_KEY = ConfigurationManager.AppSettings["AuthenApiKey"] ?? "";
        private static readonly int API_TIMEOUT_SECONDS = int.Parse(ConfigurationManager.AppSettings["AuthenApiTimeout"] ?? "30");
        private static readonly int CACHE_DURATION_DAYS = int.Parse(ConfigurationManager.AppSettings["CacheDurationDays"] ?? "90");
        private static readonly string PASSWORD_SALT = ConfigurationManager.AppSettings["PasswordSalt"] ?? "HondaCX_2024_Salt";
        private static readonly int MAX_FAILED_ATTEMPTS = int.Parse(ConfigurationManager.AppSettings["MaxFailedAttempts"] ?? "5");
        private static readonly int LOCKOUT_DURATION_MINUTES = int.Parse(ConfigurationManager.AppSettings["LockoutDurationMinutes"] ?? "15");

        static AuthenticationHelper()
        {
            _httpClient.BaseAddress = new Uri(API_BASE_URL);
            _httpClient.Timeout = TimeSpan.FromSeconds(API_TIMEOUT_SECONDS);
        }

        /// <summary>
        /// Main authentication method
        /// </summary>
        public static AuthResult Authenticate(string username, string password, string ipAddress = null, string userAgent = null)
        {
            try
            {
                username = username?.ToLower().Trim();

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    return new AuthResult
                    {
                        Success = false,
                        Message = "Username and password are required",
                        AuthSource = "validation"
                    };
                }

                string passwordHash = HashPassword(password);

                // Step 1: Check if user exists in cache
                UserCache cachedUser = GetCachedUser(username);

                // Step 2: Check if account is locked
                if (cachedUser != null && cachedUser.IsLocked)
                {
                    // Check if lockout period has expired
                    if (cachedUser.LastFailedLogin.HasValue &&
                        DateTime.Now.Subtract(cachedUser.LastFailedLogin.Value).TotalMinutes < LOCKOUT_DURATION_MINUTES)
                    {
                        LogLoginAttempt(username, false, "lockout", "Account temporarily locked", ipAddress, userAgent);
                        return new AuthResult
                        {
                            Success = false,
                            Message = $"Account is temporarily locked. Please try again in {LOCKOUT_DURATION_MINUTES} minutes.",
                            AuthSource = "lockout"
                        };
                    }
                    else
                    {
                        // Lockout expired, reset failed attempts
                        ResetFailedAttempts(username);
                        cachedUser.IsLocked = false;
                        cachedUser.FailedAttempts = 0;
                    }
                }

                AuthResult result;

                if (cachedUser == null)
                {
                    // Case 1: No cache - First time login
                    result = AuthenticateViaApi(username, password, passwordHash, ipAddress, userAgent);
                }
                else if (IsCacheExpired(cachedUser))
                {
                    // Case 2: Cache expired - Re-verify with AD
                    result = AuthenticateViaApi(username, password, passwordHash, ipAddress, userAgent);
                }
                else if (cachedUser.PasswordHash != passwordHash)
                {
                    // Case 3: Password changed - Verify with AD
                    result = AuthenticateViaApi(username, password, passwordHash, ipAddress, userAgent);
                }
                else
                {
                    // Case 4: Valid cache & matching password - Authenticate locally
                    result = AuthenticateFromCache(cachedUser, ipAddress, userAgent);
                }

                // Handle failed login
                if (!result.Success && cachedUser != null)
                {
                    RecordFailedLogin(username);
                }

                return result;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Authentication error", ex, $"Username: {username}");
                return new AuthResult
                {
                    Success = false,
                    Message = "An error occurred during authentication. Please try again.",
                    AuthSource = "error"
                };
            }
        }

        /// <summary>
        /// Hash password with SHA256 + salt
        /// </summary>
        private static string HashPassword(string password)
        {
            using (SHA256 sha256 = SHA256.Create())
            {
                string salted = $"{password}{PASSWORD_SALT}";
                byte[] bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(salted));
                return BitConverter.ToString(bytes).Replace("-", "").ToLower();
            }
        }

        /// <summary>
        /// Get user from local cache
        /// </summary>
        private static UserCache GetCachedUser(string username)
        {
            try
            {
                string query = $@"SELECT * FROM {_dbHelper.GetTableName("User_Cache")}
                                 WHERE {_dbHelper.GetColumnName("Username")} = @username";

                var param = _dbHelper.CreateParameter("@username", username);
                DataTable dt = _dbHelper.ExecuteQuery(query, param);

                if (dt.Rows.Count == 0)
                    return null;

                DataRow row = dt.Rows[0];
                return new UserCache
                {
                    Id = Convert.ToInt32(row["Id"]),
                    Username = row["Username"].ToString(),
                    PasswordHash = row["PasswordHash"].ToString(),
                    Salt = row["Salt"].ToString(),
                    DisplayName = row["DisplayName"]?.ToString(),
                    Email = row["Email"]?.ToString(),
                    Department = row["Department"]?.ToString(),
                    Title = row["Title"]?.ToString(),
                    EmployeeId = row["EmployeeId"]?.ToString(),
                    Groups = row["Groups"]?.ToString(),
                    IsEnabled = Convert.ToBoolean(row["IsEnabled"]),
                    IsLocked = Convert.ToBoolean(row["IsLocked"]),
                    FailedAttempts = Convert.ToInt32(row["FailedAttempts"]),
                    LastFailedLogin = row["LastFailedLogin"] != DBNull.Value ? Convert.ToDateTime(row["LastFailedLogin"]) : (DateTime?)null,
                    LastLogin = row["LastLogin"] != DBNull.Value ? Convert.ToDateTime(row["LastLogin"]) : (DateTime?)null,
                    LastAdSync = Convert.ToDateTime(row["LastAdSync"]),
                    CacheExpires = Convert.ToDateTime(row["CacheExpires"])
                };
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error getting cached user", ex, $"Username: {username}");
                return null;
            }
        }

        /// <summary>
        /// Check if cache has expired
        /// </summary>
        private static bool IsCacheExpired(UserCache cachedUser)
        {
            return DateTime.Now > cachedUser.CacheExpires;
        }

        /// <summary>
        /// Authenticate via AuthenAPI
        /// </summary>
        private static AuthResult AuthenticateViaApi(string username, string password, string passwordHash,
                                                      string ipAddress, string userAgent)
        {
            try
            {
                var requestBody = new
                {
                    username = username,
                    password = password
                };

                string json = JsonConvert.SerializeObject(requestBody);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                // Create request with API Key header
                var request = new HttpRequestMessage(HttpMethod.Post, "/ldap/authenticate")
                {
                    Content = content
                };

                // Add X-API-Key header if configured
                if (!string.IsNullOrEmpty(API_KEY))
                {
                    request.Headers.Add("X-API-Key", API_KEY);
                }

                var response = _httpClient.SendAsync(request).Result;
                string responseBody = response.Content.ReadAsStringAsync().Result;

                var apiResult = JsonConvert.DeserializeObject<JObject>(responseBody);

                if (response.IsSuccessStatusCode && apiResult["success"]?.Value<bool>() == true)
                {
                    // Success - Update cache
                    var userData = apiResult["data"];
                    UpdateCache(username, passwordHash, userData);

                    var userInfo = new UserInfo
                    {
                        Username = userData["samAccountName"]?.ToString() ?? username,
                        DisplayName = userData["displayName"]?.ToString(),
                        Email = userData["email"]?.ToString(),
                        Department = userData["department"]?.ToString(),
                        Title = userData["title"]?.ToString(),
                        Groups = userData["groups"]?.ToObject<string[]>() ?? new string[0],
                        IsEnabled = userData["isEnabled"]?.Value<bool>() ?? true
                    };

                    LogLoginAttempt(username, true, "api", null, ipAddress, userAgent);

                    return new AuthResult
                    {
                        Success = true,
                        User = userInfo,
                        Message = "Authentication successful",
                        AuthSource = "api"
                    };
                }
                else
                {
                    // Failed
                    string message = apiResult["message"]?.ToString() ?? "Authentication failed";
                    LogLoginAttempt(username, false, "api", message, ipAddress, userAgent);

                    return new AuthResult
                    {
                        Success = false,
                        Message = message,
                        AuthSource = "api"
                    };
                }
            }
            catch (TaskCanceledException)
            {
                // Timeout - Try cache fallback
                ErrorLogger.LogWarning("API timeout during authentication", $"Username: {username}");
                return HandleApiFailure(username, passwordHash, "API timeout", ipAddress, userAgent);
            }
            catch (Exception ex)
            {
                // API Error - Try cache fallback
                ErrorLogger.LogError("API error during authentication", ex, $"Username: {username}");
                return HandleApiFailure(username, passwordHash, ex.Message, ipAddress, userAgent);
            }
        }

        /// <summary>
        /// Authenticate using cached credentials
        /// </summary>
        private static AuthResult AuthenticateFromCache(UserCache cachedUser, string ipAddress, string userAgent)
        {
            if (!cachedUser.IsEnabled)
            {
                LogLoginAttempt(cachedUser.Username, false, "cache", "Account disabled", ipAddress, userAgent);
                return new AuthResult
                {
                    Success = false,
                    Message = "Account is disabled",
                    AuthSource = "cache"
                };
            }

            // Update last login timestamp
            UpdateLastLogin(cachedUser.Username);

            var userInfo = new UserInfo
            {
                Username = cachedUser.Username,
                DisplayName = cachedUser.DisplayName,
                Email = cachedUser.Email,
                Department = cachedUser.Department,
                Title = cachedUser.Title,
                Groups = string.IsNullOrEmpty(cachedUser.Groups) ? new string[0] :
                         JsonConvert.DeserializeObject<string[]>(cachedUser.Groups),
                IsEnabled = cachedUser.IsEnabled
            };

            LogLoginAttempt(cachedUser.Username, true, "cache", null, ipAddress, userAgent);

            return new AuthResult
            {
                Success = true,
                User = userInfo,
                Message = "Authentication successful (cached)",
                AuthSource = "cache"
            };
        }

        /// <summary>
        /// Update or insert user cache
        /// </summary>
        private static void UpdateCache(string username, string passwordHash, JToken userData)
        {
            try
            {
                string groups = JsonConvert.SerializeObject(userData["groups"]?.ToObject<string[]>() ?? new string[0]);

                string query = $@"
                    EXEC sp_UpsertUserCache
                        @Username = @username,
                        @PasswordHash = @passwordHash,
                        @Salt = @salt,
                        @DisplayName = @displayName,
                        @Email = @email,
                        @Department = @department,
                        @Title = @title,
                        @EmployeeId = @employeeId,
                        @Groups = @groups,
                        @IsEnabled = @isEnabled,
                        @CacheDurationDays = @cacheDurationDays";

                var parameters = new[]
                {
                    _dbHelper.CreateParameter("@username", username),
                    _dbHelper.CreateParameter("@passwordHash", passwordHash),
                    _dbHelper.CreateParameter("@salt", PASSWORD_SALT),
                    _dbHelper.CreateParameter("@displayName", userData["displayName"]?.ToString() ?? (object)DBNull.Value),
                    _dbHelper.CreateParameter("@email", userData["email"]?.ToString() ?? (object)DBNull.Value),
                    _dbHelper.CreateParameter("@department", userData["department"]?.ToString() ?? (object)DBNull.Value),
                    _dbHelper.CreateParameter("@title", userData["title"]?.ToString() ?? (object)DBNull.Value),
                    _dbHelper.CreateParameter("@employeeId", userData["employeeId"]?.ToString() ?? (object)DBNull.Value),
                    _dbHelper.CreateParameter("@groups", groups),
                    _dbHelper.CreateParameter("@isEnabled", userData["isEnabled"]?.Value<bool>() ?? true),
                    _dbHelper.CreateParameter("@cacheDurationDays", CACHE_DURATION_DAYS)
                };

                _dbHelper.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error updating user cache", ex, $"Username: {username}");
            }
        }

        /// <summary>
        /// Update last login timestamp
        /// </summary>
        private static void UpdateLastLogin(string username)
        {
            try
            {
                string query = "EXEC sp_UpdateLastLogin @Username = @username";
                var param = _dbHelper.CreateParameter("@username", username);
                _dbHelper.ExecuteNonQuery(query, param);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error updating last login", ex, $"Username: {username}");
            }
        }

        /// <summary>
        /// Record failed login attempt
        /// </summary>
        private static void RecordFailedLogin(string username)
        {
            try
            {
                string query = "EXEC sp_RecordFailedLogin @Username = @username";
                var param = _dbHelper.CreateParameter("@username", username);
                _dbHelper.ExecuteNonQuery(query, param);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error recording failed login", ex, $"Username: {username}");
            }
        }

        /// <summary>
        /// Reset failed login attempts
        /// </summary>
        private static void ResetFailedAttempts(string username)
        {
            try
            {
                string query = "EXEC sp_ResetFailedAttempts @Username = @username";
                var param = _dbHelper.CreateParameter("@username", username);
                _dbHelper.ExecuteNonQuery(query, param);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error resetting failed attempts", ex, $"Username: {username}");
            }
        }

        /// <summary>
        /// Log login attempt to audit table
        /// </summary>
        private static void LogLoginAttempt(string username, bool success, string authSource,
                                           string failureReason, string ipAddress, string userAgent)
        {
            try
            {
                string query = $@"
                    EXEC sp_LogLoginAttempt
                        @Username = @username,
                        @Success = @success,
                        @AuthSource = @authSource,
                        @FailureReason = @failureReason,
                        @IpAddress = @ipAddress,
                        @UserAgent = @userAgent";

                var parameters = new[]
                {
                    _dbHelper.CreateParameter("@username", username),
                    _dbHelper.CreateParameter("@success", success),
                    _dbHelper.CreateParameter("@authSource", authSource ?? (object)DBNull.Value),
                    _dbHelper.CreateParameter("@failureReason", failureReason ?? (object)DBNull.Value),
                    _dbHelper.CreateParameter("@ipAddress", ipAddress ?? (object)DBNull.Value),
                    _dbHelper.CreateParameter("@userAgent", userAgent ?? (object)DBNull.Value)
                };

                _dbHelper.ExecuteNonQuery(query, parameters);
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error logging login attempt", ex, $"Username: {username}");
            }
        }

        /// <summary>
        /// Handle API failure - try cache fallback
        /// </summary>
        private static AuthResult HandleApiFailure(string username, string passwordHash,
                                                   string error, string ipAddress, string userAgent)
        {
            // Try cache fallback
            UserCache cachedUser = GetCachedUser(username);

            if (cachedUser != null && !IsCacheExpired(cachedUser) && cachedUser.PasswordHash == passwordHash)
            {
                // Use cache as fallback
                var result = AuthenticateFromCache(cachedUser, ipAddress, userAgent);
                result.Message += $" (API unavailable: {error})";
                ErrorLogger.LogWarning("Using cache fallback due to API failure",
                                      $"Username: {username}, Error: {error}");
                return result;
            }

            LogLoginAttempt(username, false, "error", $"API unavailable: {error}", ipAddress, userAgent);

            return new AuthResult
            {
                Success = false,
                Message = "Authentication service is temporarily unavailable. Please try again later.",
                AuthSource = "error"
            };
        }

        /// <summary>
        /// Validate user session
        /// </summary>
        public static bool ValidateSession(System.Web.SessionState.HttpSessionState session)
        {
            if (session == null || session["Username"] == null)
                return false;

            // Check if session has expired
            if (session["LoginTime"] != null)
            {
                DateTime loginTime = (DateTime)session["LoginTime"];
                int sessionTimeoutHours = int.Parse(ConfigurationManager.AppSettings["SessionTimeoutHours"] ?? "8");

                if (DateTime.Now.Subtract(loginTime).TotalHours > sessionTimeoutHours)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get current user from session
        /// </summary>
        public static UserInfo GetCurrentUser(System.Web.SessionState.HttpSessionState session)
        {
            if (!ValidateSession(session))
                return null;

            return new UserInfo
            {
                Username = session["Username"]?.ToString(),
                DisplayName = session["DisplayName"]?.ToString(),
                Email = session["Email"]?.ToString(),
                Department = session["Department"]?.ToString(),
                Title = session["Title"]?.ToString(),
                Groups = session["Groups"] as string[] ?? new string[0]
            };
        }

        /// <summary>
        /// Create authentication session for user
        /// </summary>
        public static void CreateAuthSession(System.Web.SessionState.HttpSessionState session, UserInfo user)
        {
            if (session == null || user == null)
                return;

            session["Username"] = user.Username;
            session["DisplayName"] = user.DisplayName;
            session["Email"] = user.Email;
            session["Department"] = user.Department;
            session["Title"] = user.Title;
            session["Groups"] = user.Groups;
            session["IsAuthenticated"] = user.IsAuthenticated;
            session["AuthSource"] = user.AuthSource;
            session["LoginTime"] = DateTime.Now;
        }
    }

    /// <summary>
    /// Authentication result
    /// </summary>
    public class AuthResult
    {
        public bool Success { get; set; }
        public UserInfo User { get; set; }
        public string Message { get; set; }
        public string AuthSource { get; set; }  // "api", "cache", "error", "lockout"
    }

    /// <summary>
    /// User information
    /// </summary>
    public class UserInfo
    {
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
        public string Title { get; set; }
        public string[] Groups { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsAuthenticated { get; set; }
        public string AuthSource { get; set; }
    }

    /// <summary>
    /// User cache model
    /// </summary>
    public class UserCache
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string PasswordHash { get; set; }
        public string Salt { get; set; }
        public string DisplayName { get; set; }
        public string Email { get; set; }
        public string Department { get; set; }
        public string Title { get; set; }
        public string EmployeeId { get; set; }
        public string Groups { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsLocked { get; set; }
        public int FailedAttempts { get; set; }
        public DateTime? LastFailedLogin { get; set; }
        public DateTime? LastLogin { get; set; }
        public DateTime LastAdSync { get; set; }
        public DateTime CacheExpires { get; set; }
    }
}
