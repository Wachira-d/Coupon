using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading;
using Npgsql;

namespace Coupon.Helpers
{
    /// <summary>
    /// Secure database helper with parameterized queries to prevent SQL injection
    /// Includes retry logic for transient errors and connection health checks
    /// </summary>
    public class DatabaseHelper
    {
        private readonly string _databaseType;
        private readonly string _connectionString;
        private const int MAX_RETRIES = 3;
        private const int RETRY_DELAY_MS = 500;
        private const int COMMAND_TIMEOUT = 30;

        public DatabaseHelper()
        {
            _databaseType = ConfigurationManager.AppSettings["DatabaseType"]?.ToLower() ?? "mssql";

            if (_databaseType == "postgresql")
            {
                _connectionString = ConfigurationManager.AppSettings["PostgreSQLConnectionString"];
            }
            else
            {
                _connectionString = ConfigurationManager.AppSettings["MSSQLConnectionString"];
            }
        }

        public string DatabaseType => _databaseType;

        /// <summary>
        /// Check if connection string is configured
        /// </summary>
        public bool IsConfigured => !string.IsNullOrEmpty(_connectionString);

        /// <summary>
        /// Get table name with proper formatting for the database type
        /// </summary>
        public string GetTableName(string tableName)
        {
            if (_databaseType == "postgresql")
            {
                return tableName.ToLower();
            }
            string dbName = ConfigurationManager.AppSettings["Coupon_DatabaseName"];
            return $"[{dbName}].[dbo].[{tableName}]";
        }

        /// <summary>
        /// Get column name with proper formatting for the database type
        /// </summary>
        public string GetColumnName(string columnName)
        {
            if (_databaseType == "postgresql")
            {
                return columnName.ToLower();
            }
            return $"[{columnName}]";
        }

        /// <summary>
        /// Get boolean value formatted for the database type
        /// </summary>
        public string GetBooleanValue(bool value)
        {
            if (_databaseType == "postgresql")
            {
                return value.ToString().ToLower();
            }
            return value ? "1" : "0";
        }

        /// <summary>
        /// Check if exception is a transient error that should be retried
        /// </summary>
        private bool IsTransientError(Exception ex)
        {
            // SQL Server transient error numbers
            if (ex is SqlException sqlEx)
            {
                int[] transientErrors = { -2, 20, 64, 233, 10053, 10054, 10060, 40197, 40501, 40613, 49918, 49919, 49920 };
                foreach (int errorNumber in transientErrors)
                {
                    if (sqlEx.Number == errorNumber)
                        return true;
                }
            }

            // PostgreSQL transient errors
            if (ex is NpgsqlException npgEx)
            {
                // Connection-related errors
                if (ex.Message.Contains("connection") ||
                    ex.Message.Contains("timeout") ||
                    ex.Message.Contains("broken"))
                {
                    return true;
                }
            }

            // Timeout exceptions
            if (ex is TimeoutException)
                return true;

            return false;
        }

        /// <summary>
        /// Execute with retry logic for transient errors
        /// </summary>
        private T ExecuteWithRetry<T>(Func<T> operation, string operationName, string query)
        {
            int retryCount = 0;
            Exception lastException = null;

            while (retryCount < MAX_RETRIES)
            {
                try
                {
                    return operation();
                }
                catch (Exception ex)
                {
                    lastException = ex;

                    if (IsTransientError(ex) && retryCount < MAX_RETRIES - 1)
                    {
                        retryCount++;
                        // Log retry attempt (file only to avoid recursive DB calls)
                        try
                        {
                            ErrorLogger.LogToFileOnly($"Database retry attempt {retryCount}/{MAX_RETRIES}", ex,
                                $"Operation: {operationName}");
                        }
                        catch { }

                        // Exponential backoff
                        Thread.Sleep(RETRY_DELAY_MS * retryCount);
                    }
                    else
                    {
                        // Non-transient error or max retries reached
                        try
                        {
                            ErrorLogger.LogError($"Database {operationName} Error", ex, query);
                        }
                        catch { }
                        throw;
                    }
                }
            }

            // Should not reach here, but throw last exception if it does
            throw lastException ?? new Exception($"Database operation {operationName} failed");
        }

        /// <summary>
        /// Execute a parameterized query and return results
        /// SECURITY: Uses parameterized queries to prevent SQL injection
        /// </summary>
        public DataTable ExecuteQuery(string query, params DbParameter[] parameters)
        {
            return ExecuteWithRetry(() =>
            {
                DataTable dt = new DataTable();

                if (_databaseType == "postgresql")
                {
                    using (NpgsqlConnection con = new NpgsqlConnection(_connectionString))
                    {
                        using (NpgsqlCommand cmd = new NpgsqlCommand(query, con))
                        {
                            cmd.CommandTimeout = COMMAND_TIMEOUT;
                            if (parameters != null)
                            {
                                foreach (var param in parameters)
                                {
                                    cmd.Parameters.Add(ConvertToNpgsqlParameter(param));
                                }
                            }

                            NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(cmd);
                            adapter.Fill(dt);
                        }
                    }
                }
                else
                {
                    using (SqlConnection con = new SqlConnection(_connectionString))
                    {
                        using (SqlCommand cmd = new SqlCommand(query, con))
                        {
                            cmd.CommandTimeout = COMMAND_TIMEOUT;
                            if (parameters != null)
                            {
                                foreach (var param in parameters)
                                {
                                    cmd.Parameters.Add(ConvertToSqlParameter(param));
                                }
                            }

                            SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                            adapter.Fill(dt);
                        }
                    }
                }

                return dt;
            }, "Query", query);
        }

        /// <summary>
        /// Execute a parameterized scalar query (returns single value)
        /// SECURITY: Uses parameterized queries to prevent SQL injection
        /// </summary>
        public object ExecuteScalar(string query, params DbParameter[] parameters)
        {
            return ExecuteWithRetry(() =>
            {
                if (_databaseType == "postgresql")
                {
                    using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
                    {
                        using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                        {
                            command.CommandTimeout = COMMAND_TIMEOUT;
                            if (parameters != null)
                            {
                                foreach (var param in parameters)
                                {
                                    command.Parameters.Add(ConvertToNpgsqlParameter(param));
                                }
                            }

                            connection.Open();
                            return command.ExecuteScalar();
                        }
                    }
                }
                else
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.CommandTimeout = COMMAND_TIMEOUT;
                            if (parameters != null)
                            {
                                foreach (var param in parameters)
                                {
                                    command.Parameters.Add(ConvertToSqlParameter(param));
                                }
                            }

                            connection.Open();
                            return command.ExecuteScalar();
                        }
                    }
                }
            }, "Scalar", query);
        }

        /// <summary>
        /// Execute a parameterized non-query command (INSERT, UPDATE, DELETE)
        /// SECURITY: Uses parameterized queries to prevent SQL injection
        /// </summary>
        public int ExecuteNonQuery(string query, params DbParameter[] parameters)
        {
            return ExecuteWithRetry(() =>
            {
                if (_databaseType == "postgresql")
                {
                    using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
                    {
                        using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                        {
                            command.CommandTimeout = COMMAND_TIMEOUT;
                            if (parameters != null)
                            {
                                foreach (var param in parameters)
                                {
                                    command.Parameters.Add(ConvertToNpgsqlParameter(param));
                                }
                            }

                            connection.Open();
                            return command.ExecuteNonQuery();
                        }
                    }
                }
                else
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            command.CommandTimeout = COMMAND_TIMEOUT;
                            if (parameters != null)
                            {
                                foreach (var param in parameters)
                                {
                                    command.Parameters.Add(ConvertToSqlParameter(param));
                                }
                            }

                            connection.Open();
                            return command.ExecuteNonQuery();
                        }
                    }
                }
            }, "NonQuery", query);
        }

        /// <summary>
        /// Test database connection
        /// </summary>
        public bool TestConnection(int timeoutSeconds = 5)
        {
            try
            {
                if (string.IsNullOrEmpty(_connectionString))
                    return false;

                if (_databaseType == "postgresql")
                {
                    using (var conn = new NpgsqlConnection(_connectionString))
                    {
                        conn.Open();
                        using (var cmd = new NpgsqlCommand("SELECT 1", conn))
                        {
                            cmd.CommandTimeout = timeoutSeconds;
                            cmd.ExecuteScalar();
                        }
                        return true;
                    }
                }
                else
                {
                    using (var conn = new SqlConnection(_connectionString))
                    {
                        conn.Open();
                        using (var cmd = new SqlCommand("SELECT 1", conn))
                        {
                            cmd.CommandTimeout = timeoutSeconds;
                            cmd.ExecuteScalar();
                        }
                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Create a database parameter (compatible with both SQL Server and PostgreSQL)
        /// </summary>
        public DbParameter CreateParameter(string name, object value)
        {
            if (_databaseType == "postgresql")
            {
                return new NpgsqlParameter(name, value ?? DBNull.Value);
            }
            return new SqlParameter(name, value ?? DBNull.Value);
        }

        private NpgsqlParameter ConvertToNpgsqlParameter(DbParameter param)
        {
            if (param is NpgsqlParameter npgsqlParam)
            {
                return npgsqlParam;
            }
            return new NpgsqlParameter(param.ParameterName, param.Value);
        }

        private SqlParameter ConvertToSqlParameter(DbParameter param)
        {
            if (param is SqlParameter sqlParam)
            {
                return sqlParam;
            }
            return new SqlParameter(param.ParameterName, param.Value);
        }
    }
}
