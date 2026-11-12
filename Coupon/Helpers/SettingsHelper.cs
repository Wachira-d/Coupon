using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace Coupon.Helpers
{
    public enum SettingType
    {
        Text,
        Secret,
        Integer,
        Boolean
    }

    /// <summary>
    /// Describes a setting that can be edited from Admin/Settings.aspx
    /// </summary>
    public class SettingDefinition
    {
        public string Key { get; set; }
        public string Label { get; set; }
        public SettingType Type { get; set; }
        public string DefaultValue { get; set; }
        public int Min { get; set; }
        public int Max { get; set; }
    }

    /// <summary>
    /// An account that is allowed to sign in to the admin area
    /// </summary>
    public class AdminUser
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public string DisplayName { get; set; }
        public string Note { get; set; }
        public bool IsActive { get; set; }
        public string AddedBy { get; set; }
        public DateTime? CreatedDateTime { get; set; }

        /// <summary>True when the entry comes from Web.config (read-only fallback)</summary>
        public bool IsFromWebConfig { get; set; }
    }

    /// <summary>
    /// Settings that can be changed from the web (stored in table App_Settings)
    /// and the allowed admin account list (table Admin_Users).
    ///
    /// Lookup order for a setting: App_Settings (database) -> Web.config appSettings -> built-in default.
    /// Values are cached for a short time so pages do not hit the database on every request.
    /// If the tables do not exist yet, everything falls back to Web.config.
    /// </summary>
    public static class SettingsHelper
    {
        // Setting keys (same names as Web.config appSettings so existing config keeps working)
        public const string KeyApiBaseUrl = "AuthenApiBaseUrl";
        public const string KeyApiKey = "AuthenApiKey";
        public const string KeyApiTimeout = "AuthenApiTimeout";
        public const string KeyRestrictAdminAccess = "RestrictAdminAccess";
        public const string KeySessionTimeoutHours = "SessionTimeoutHours";
        public const string KeyLockoutDurationMinutes = "LockoutDurationMinutes";
        public const string KeyCacheDurationDays = "CacheDurationDays";
        public const string KeyCouponIdLength = "NumberofCode";

        public const string SourceDatabase = "Database";
        public const string SourceWebConfig = "Web.config";
        public const string SourceDefault = "Default";

        private const int CACHE_SECONDS = 60;
        private const int RETRY_AFTER_FAILURE_MINUTES = 10;

        private static readonly object _settingsLock = new object();
        private static readonly object _adminLock = new object();

        private static Dictionary<string, string> _dbValues = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        private static DateTime _settingsNextLoad = DateTime.MinValue;
        private static bool _settingsTableAvailable;

        private static List<AdminUser> _dbAdmins = new List<AdminUser>();
        private static DateTime _adminsNextLoad = DateTime.MinValue;
        private static bool _adminTableAvailable;
        private static bool _openAccessWarningLogged;

        private static readonly Regex AdminUsernamePattern = new Regex(@"^[A-Za-z0-9._@\-\\]{2,200}$", RegexOptions.Compiled);

        public static readonly IList<SettingDefinition> Definitions = new List<SettingDefinition>
        {
            new SettingDefinition { Key = KeyApiBaseUrl, Label = "Authen API Base URL", Type = SettingType.Text, DefaultValue = "" },
            new SettingDefinition { Key = KeyApiKey, Label = "Authen API Key", Type = SettingType.Secret, DefaultValue = "" },
            new SettingDefinition { Key = KeyApiTimeout, Label = "Authen API Timeout (seconds)", Type = SettingType.Integer, DefaultValue = "30", Min = 5, Max = 120 },
            new SettingDefinition { Key = KeyRestrictAdminAccess, Label = "Allow only listed admin accounts", Type = SettingType.Boolean, DefaultValue = "true" },
            new SettingDefinition { Key = KeySessionTimeoutHours, Label = "Admin session timeout (hours)", Type = SettingType.Integer, DefaultValue = "8", Min = 1, Max = 24 },
            new SettingDefinition { Key = KeyLockoutDurationMinutes, Label = "Account lockout duration (minutes)", Type = SettingType.Integer, DefaultValue = "15", Min = 1, Max = 1440 },
            new SettingDefinition { Key = KeyCacheDurationDays, Label = "Login cache duration (days)", Type = SettingType.Integer, DefaultValue = "90", Min = 1, Max = 365 },
            new SettingDefinition { Key = KeyCouponIdLength, Label = "Coupon ID length", Type = SettingType.Integer, DefaultValue = "4", Min = 4, Max = 32 }
        }.AsReadOnly();

        #region Settings

        public static SettingDefinition FindDefinition(string key)
        {
            return Definitions.FirstOrDefault(d => string.Equals(d.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>True when table App_Settings could be read (settings can be saved from the web)</summary>
        public static bool SettingsTableAvailable
        {
            get
            {
                EnsureSettingsLoaded(false);
                return _settingsTableAvailable;
            }
        }

        /// <summary>Effective value: database -> Web.config -> default</summary>
        public static string Get(string key)
        {
            EnsureSettingsLoaded(false);

            string value;
            lock (_settingsLock)
            {
                if (_dbValues.TryGetValue(key, out value) && !string.IsNullOrEmpty(value))
                {
                    return value;
                }
            }

            value = ConfigurationManager.AppSettings[key];
            if (!string.IsNullOrEmpty(value))
            {
                return value;
            }

            return FindDefinition(key)?.DefaultValue;
        }

        public static int GetInt(string key, int fallback)
        {
            int result;
            if (!int.TryParse(Get(key), out result))
            {
                return fallback;
            }

            SettingDefinition definition = FindDefinition(key);
            if (definition != null && definition.Max > 0)
            {
                if (result < definition.Min) return definition.Min;
                if (result > definition.Max) return definition.Max;
            }

            return result;
        }

        public static bool GetBool(string key, bool fallback)
        {
            string value = (Get(key) ?? "").Trim().ToLowerInvariant();
            if (value == "true" || value == "1" || value == "yes" || value == "on") return true;
            if (value == "false" || value == "0" || value == "no" || value == "off") return false;
            return fallback;
        }

        /// <summary>Where the effective value comes from: Database, Web.config or Default</summary>
        public static string GetSource(string key)
        {
            EnsureSettingsLoaded(false);

            string value;
            lock (_settingsLock)
            {
                if (_dbValues.TryGetValue(key, out value) && !string.IsNullOrEmpty(value))
                {
                    return SourceDatabase;
                }
            }

            return string.IsNullOrEmpty(ConfigurationManager.AppSettings[key]) ? SourceDefault : SourceWebConfig;
        }

        /// <summary>Coupon ID length used both by the import page and the redemption pages</summary>
        public static int CouponIdLength
        {
            get { return GetInt(KeyCouponIdLength, 4); }
        }

        /// <summary>Save a setting to the database (overrides Web.config)</summary>
        public static void Save(string key, string value, string updatedBy)
        {
            var db = new DatabaseHelper();
            string table = db.GetTableName("App_Settings");
            DateTime now = DateTime.Now;

            string updateQuery = $"UPDATE {table} SET {db.GetColumnName("SettingValue")} = @value, " +
                                 $"{db.GetColumnName("UpdatedBy")} = @updatedBy, {db.GetColumnName("UpdatedDateTime")} = @now " +
                                 $"WHERE {db.GetColumnName("SettingKey")} = @key";

            int rows = db.ExecuteNonQuery(updateQuery,
                db.CreateParameter("@value", value),
                db.CreateParameter("@updatedBy", updatedBy),
                db.CreateParameter("@now", now),
                db.CreateParameter("@key", key));

            if (rows == 0)
            {
                string insertQuery = $"INSERT INTO {table} ({db.GetColumnName("SettingKey")}, {db.GetColumnName("SettingValue")}, " +
                                     $"{db.GetColumnName("UpdatedBy")}, {db.GetColumnName("UpdatedDateTime")}) " +
                                     "VALUES (@key, @value, @updatedBy, @now)";

                db.ExecuteNonQuery(insertQuery,
                    db.CreateParameter("@key", key),
                    db.CreateParameter("@value", value),
                    db.CreateParameter("@updatedBy", updatedBy),
                    db.CreateParameter("@now", now));
            }

            EnsureSettingsLoaded(true);
        }

        /// <summary>Remove the database value so the setting falls back to Web.config / default</summary>
        public static void Remove(string key)
        {
            var db = new DatabaseHelper();
            string query = $"DELETE FROM {db.GetTableName("App_Settings")} WHERE {db.GetColumnName("SettingKey")} = @key";
            db.ExecuteNonQuery(query, db.CreateParameter("@key", key));
            EnsureSettingsLoaded(true);
        }

        /// <summary>True when the database holds a value for this key</summary>
        public static bool HasDatabaseValue(string key)
        {
            return GetSource(key) == SourceDatabase;
        }

        private static void EnsureSettingsLoaded(bool force)
        {
            if (!force && DateTime.Now < _settingsNextLoad)
            {
                return;
            }

            lock (_settingsLock)
            {
                if (!force && DateTime.Now < _settingsNextLoad)
                {
                    return;
                }

                try
                {
                    var db = new DatabaseHelper();
                    if (!db.IsConfigured)
                    {
                        _settingsTableAvailable = false;
                        _settingsNextLoad = DateTime.Now.AddMinutes(RETRY_AFTER_FAILURE_MINUTES);
                        return;
                    }

                    string query = $"SELECT {db.GetColumnName("SettingKey")}, {db.GetColumnName("SettingValue")} " +
                                   $"FROM {db.GetTableName("App_Settings")}";
                    DataTable dt = db.ExecuteQuery(query);

                    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (DataRow row in dt.Rows)
                    {
                        string key = Convert.ToString(row[0])?.Trim();
                        if (string.IsNullOrEmpty(key)) continue;
                        values[key] = row[1] == DBNull.Value ? null : Convert.ToString(row[1]);
                    }

                    _dbValues = values;
                    _settingsTableAvailable = true;
                    _settingsNextLoad = DateTime.Now.AddSeconds(CACHE_SECONDS);
                }
                catch (Exception ex)
                {
                    // Table not created yet or database down: keep last known values, fall back to Web.config
                    _settingsTableAvailable = false;
                    _settingsNextLoad = DateTime.Now.AddMinutes(RETRY_AFTER_FAILURE_MINUTES);
                    try
                    {
                        ErrorLogger.LogToFileOnly("SettingsHelper: cannot read App_Settings, using Web.config values " +
                                                  "(run Database/CreateSettingsTables.sql)", ex);
                    }
                    catch
                    {
                        // ignore logging failure
                    }
                }
            }
        }

        #endregion

        #region Admin accounts

        /// <summary>True when table Admin_Users could be read (admin list can be edited from the web)</summary>
        public static bool AdminTableAvailable
        {
            get
            {
                EnsureAdminsLoaded(false);
                return _adminTableAvailable;
            }
        }

        /// <summary>Admin accounts from the database, or the Web.config list (ReportPassword) when the table is unavailable or empty</summary>
        public static List<AdminUser> GetAdminUsers(bool forceReload = false)
        {
            EnsureAdminsLoaded(forceReload);

            lock (_adminLock)
            {
                if (_adminTableAvailable && _dbAdmins.Count > 0)
                {
                    return _dbAdmins.Select(Clone).ToList();
                }
            }

            return GetWebConfigAdmins();
        }

        /// <summary>
        /// Check whether any of the given identities (typed username, AD account name, email)
        /// is in the allowed admin list. Comparison is case-insensitive.
        /// </summary>
        public static bool IsAdminAllowed(params string[] identities)
        {
            if (!GetBool(KeyRestrictAdminAccess, true))
            {
                return true;
            }

            List<AdminUser> active = GetAdminUsers().Where(a => a.IsActive && !string.IsNullOrWhiteSpace(a.Username)).ToList();
            if (active.Count == 0)
            {
                // No admin configured anywhere: allow sign-in so the system cannot lock everybody out.
                if (!_openAccessWarningLogged)
                {
                    _openAccessWarningLogged = true;
                    try
                    {
                        ErrorLogger.LogWarning("No allowed admin accounts configured - any AD account can sign in",
                            "Add admin accounts on Admin/Settings.aspx");
                    }
                    catch
                    {
                        // ignore logging failure
                    }
                }
                return true;
            }

            var ids = (identities ?? new string[0])
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s.Trim().ToLowerInvariant())
                .ToList();

            return active.Any(a => ids.Contains(a.Username.Trim().ToLowerInvariant()));
        }

        public static int CountActiveAdmins()
        {
            return GetAdminUsers().Count(a => a.IsActive);
        }

        public static bool IsValidAdminUsername(string username)
        {
            return !string.IsNullOrWhiteSpace(username) && AdminUsernamePattern.IsMatch(username.Trim());
        }

        public static AdminUser FindAdmin(int id)
        {
            return GetAdminUsers().FirstOrDefault(a => a.Id == id && !a.IsFromWebConfig);
        }

        /// <summary>Add an admin account. Returns false if it already exists.</summary>
        public static bool AddAdmin(string username, string displayName, string note, string addedBy)
        {
            string normalized = username.Trim().ToLowerInvariant();
            var db = new DatabaseHelper();
            string table = db.GetTableName("Admin_Users");

            string checkQuery = $"SELECT COUNT(*) FROM {table} WHERE LOWER({db.GetColumnName("Username")}) = @username";
            int exists = Convert.ToInt32(db.ExecuteScalar(checkQuery, db.CreateParameter("@username", normalized)));
            if (exists > 0)
            {
                return false;
            }

            DateTime now = DateTime.Now;
            string insertQuery = $"INSERT INTO {table} ({db.GetColumnName("Username")}, {db.GetColumnName("DisplayName")}, " +
                                 $"{db.GetColumnName("Note")}, {db.GetColumnName("IsActive")}, {db.GetColumnName("AddedBy")}, " +
                                 $"{db.GetColumnName("CreatedDateTime")}, {db.GetColumnName("UpdatedDateTime")}) " +
                                 "VALUES (@username, @displayName, @note, @isActive, @addedBy, @now, @now2)";

            db.ExecuteNonQuery(insertQuery,
                db.CreateParameter("@username", normalized),
                db.CreateParameter("@displayName", string.IsNullOrWhiteSpace(displayName) ? null : displayName.Trim()),
                db.CreateParameter("@note", string.IsNullOrWhiteSpace(note) ? null : note.Trim()),
                db.CreateParameter("@isActive", true),
                db.CreateParameter("@addedBy", addedBy),
                db.CreateParameter("@now", now),
                db.CreateParameter("@now2", now));

            EnsureAdminsLoaded(true);
            return true;
        }

        public static void SetAdminActive(int id, bool isActive)
        {
            var db = new DatabaseHelper();
            string query = $"UPDATE {db.GetTableName("Admin_Users")} SET {db.GetColumnName("IsActive")} = @isActive, " +
                           $"{db.GetColumnName("UpdatedDateTime")} = @now WHERE {db.GetColumnName("Id")} = @id";

            db.ExecuteNonQuery(query,
                db.CreateParameter("@isActive", isActive),
                db.CreateParameter("@now", DateTime.Now),
                db.CreateParameter("@id", id));

            EnsureAdminsLoaded(true);
        }

        public static void DeleteAdmin(int id)
        {
            var db = new DatabaseHelper();
            string query = $"DELETE FROM {db.GetTableName("Admin_Users")} WHERE {db.GetColumnName("Id")} = @id";
            db.ExecuteNonQuery(query, db.CreateParameter("@id", id));
            EnsureAdminsLoaded(true);
        }

        private static List<AdminUser> GetWebConfigAdmins()
        {
            string list = ConfigurationManager.AppSettings["ReportPassword"] ?? "";
            return list.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(s => new AdminUser
                {
                    Id = 0,
                    Username = s.ToLowerInvariant(),
                    IsActive = true,
                    AddedBy = SourceWebConfig,
                    IsFromWebConfig = true
                })
                .ToList();
        }

        private static void EnsureAdminsLoaded(bool force)
        {
            if (!force && DateTime.Now < _adminsNextLoad)
            {
                return;
            }

            lock (_adminLock)
            {
                if (!force && DateTime.Now < _adminsNextLoad)
                {
                    return;
                }

                try
                {
                    var db = new DatabaseHelper();
                    if (!db.IsConfigured)
                    {
                        _adminTableAvailable = false;
                        _adminsNextLoad = DateTime.Now.AddMinutes(RETRY_AFTER_FAILURE_MINUTES);
                        return;
                    }

                    string query = $"SELECT {db.GetColumnName("Id")}, {db.GetColumnName("Username")}, {db.GetColumnName("DisplayName")}, " +
                                   $"{db.GetColumnName("Note")}, {db.GetColumnName("IsActive")}, {db.GetColumnName("AddedBy")}, " +
                                   $"{db.GetColumnName("CreatedDateTime")} FROM {db.GetTableName("Admin_Users")} " +
                                   $"ORDER BY {db.GetColumnName("Username")}";
                    DataTable dt = db.ExecuteQuery(query);

                    var admins = new List<AdminUser>();
                    foreach (DataRow row in dt.Rows)
                    {
                        admins.Add(new AdminUser
                        {
                            Id = Convert.ToInt32(row[0]),
                            Username = Convert.ToString(row[1]),
                            DisplayName = row[2] == DBNull.Value ? null : Convert.ToString(row[2]),
                            Note = row[3] == DBNull.Value ? null : Convert.ToString(row[3]),
                            IsActive = row[4] != DBNull.Value && Convert.ToBoolean(row[4]),
                            AddedBy = row[5] == DBNull.Value ? null : Convert.ToString(row[5]),
                            CreatedDateTime = row[6] == DBNull.Value ? (DateTime?)null : Convert.ToDateTime(row[6])
                        });
                    }

                    _dbAdmins = admins;
                    _adminTableAvailable = true;
                    _adminsNextLoad = DateTime.Now.AddSeconds(CACHE_SECONDS);
                }
                catch (Exception ex)
                {
                    _adminTableAvailable = false;
                    _adminsNextLoad = DateTime.Now.AddMinutes(RETRY_AFTER_FAILURE_MINUTES);
                    try
                    {
                        ErrorLogger.LogToFileOnly("SettingsHelper: cannot read Admin_Users, using ReportPassword from Web.config " +
                                                  "(run Database/CreateSettingsTables.sql)", ex);
                    }
                    catch
                    {
                        // ignore logging failure
                    }
                }
            }
        }

        private static AdminUser Clone(AdminUser a)
        {
            return new AdminUser
            {
                Id = a.Id,
                Username = a.Username,
                DisplayName = a.DisplayName,
                Note = a.Note,
                IsActive = a.IsActive,
                AddedBy = a.AddedBy,
                CreatedDateTime = a.CreatedDateTime,
                IsFromWebConfig = a.IsFromWebConfig
            };
        }

        #endregion
    }
}
