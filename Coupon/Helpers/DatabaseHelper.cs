using System;
using System.Configuration;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using Npgsql;

namespace Coupon.Helpers
{
    /// <summary>
    /// Secure database helper with parameterized queries to prevent SQL injection
    /// </summary>
    public class DatabaseHelper
    {
        private readonly string _databaseType;
        private readonly string _connectionString;

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
        /// Execute a parameterized query and return results
        /// SECURITY: Uses parameterized queries to prevent SQL injection
        /// </summary>
        public DataTable ExecuteQuery(string query, params DbParameter[] parameters)
        {
            DataTable dt = new DataTable();

            try
            {
                if (_databaseType == "postgresql")
                {
                    using (NpgsqlConnection con = new NpgsqlConnection(_connectionString))
                    {
                        using (NpgsqlCommand cmd = new NpgsqlCommand(query, con))
                        {
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
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Database Query Error", ex, query);
                throw;
            }

            return dt;
        }

        /// <summary>
        /// Execute a parameterized non-query command (INSERT, UPDATE, DELETE)
        /// SECURITY: Uses parameterized queries to prevent SQL injection
        /// </summary>
        public int ExecuteNonQuery(string query, params DbParameter[] parameters)
        {
            int rowsAffected = 0;

            try
            {
                if (_databaseType == "postgresql")
                {
                    using (NpgsqlConnection connection = new NpgsqlConnection(_connectionString))
                    {
                        using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                        {
                            if (parameters != null)
                            {
                                foreach (var param in parameters)
                                {
                                    command.Parameters.Add(ConvertToNpgsqlParameter(param));
                                }
                            }

                            connection.Open();
                            rowsAffected = command.ExecuteNonQuery();
                        }
                    }
                }
                else
                {
                    using (SqlConnection connection = new SqlConnection(_connectionString))
                    {
                        using (SqlCommand command = new SqlCommand(query, connection))
                        {
                            if (parameters != null)
                            {
                                foreach (var param in parameters)
                                {
                                    command.Parameters.Add(ConvertToSqlParameter(param));
                                }
                            }

                            connection.Open();
                            rowsAffected = command.ExecuteNonQuery();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Database NonQuery Error", ex, query);
                throw;
            }

            return rowsAffected;
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
