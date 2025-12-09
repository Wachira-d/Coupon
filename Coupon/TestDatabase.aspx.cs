using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Text;
using System.Web.UI;
using Coupon.Helpers;

namespace Coupon
{
    public partial class TestDatabase : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                litResult.Text = "<p class='info'>Click 'Test Connection' button to run diagnostics...</p>";
            }
        }

        protected void btnTest_Click(object sender, EventArgs e)
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("<h2>Configuration Test</h2>");
            result.AppendLine("<pre>");

            try
            {
                // Test 1: Check Config Values
                result.AppendLine("=== Configuration Settings ===");
                string dbType = ConfigurationManager.AppSettings["DatabaseType"];
                string connString = ConfigurationManager.AppSettings["MSSQLConnectionString"];
                string dbName = ConfigurationManager.AppSettings["Coupon_DatabaseName"];

                result.AppendLine($"DatabaseType: {dbType ?? "NOT SET"}");
                result.AppendLine($"Database Name: {dbName ?? "NOT SET"}");
                result.AppendLine($"Connection String Length: {connString?.Length ?? 0} chars");
                result.AppendLine();

                // Test 2: Test DatabaseHelper
                result.AppendLine("=== DatabaseHelper Test ===");
                DatabaseHelper dbHelper = new DatabaseHelper();
                result.AppendLine($"Database Type: {dbHelper.DatabaseType}");
                result.AppendLine($"Table Name Format: {dbHelper.GetTableName("Coupon")}");
                result.AppendLine($"Column Name Format: {dbHelper.GetColumnName("ID")}");
                result.AppendLine();

                // Test 3: Test Connection
                result.AppendLine("=== Connection Test ===");
                using (SqlConnection conn = new SqlConnection(connString))
                {
                    conn.Open();
                    result.AppendLine($"<span class='success'>✓ Connection successful!</span>");
                    result.AppendLine($"Server Version: {conn.ServerVersion}");
                    result.AppendLine($"Database: {conn.Database}");
                    result.AppendLine();

                    // Test 4: Check if Coupon table exists
                    result.AppendLine("=== Table Check ===");
                    string checkTableQuery = @"
                        SELECT TABLE_NAME
                        FROM INFORMATION_SCHEMA.TABLES
                        WHERE TABLE_NAME = 'Coupon'";

                    using (SqlCommand cmd = new SqlCommand(checkTableQuery, conn))
                    {
                        object tableExists = cmd.ExecuteScalar();
                        if (tableExists != null)
                        {
                            result.AppendLine($"<span class='success'>✓ Coupon table exists</span>");

                            // Get column names
                            result.AppendLine();
                            result.AppendLine("=== Coupon Table Columns ===");
                            string columnQuery = @"
                                SELECT COLUMN_NAME, DATA_TYPE
                                FROM INFORMATION_SCHEMA.COLUMNS
                                WHERE TABLE_NAME = 'Coupon'
                                ORDER BY ORDINAL_POSITION";

                            using (SqlCommand colCmd = new SqlCommand(columnQuery, conn))
                            {
                                using (SqlDataReader reader = colCmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        result.AppendLine($"  - {reader["COLUMN_NAME"]} ({reader["DATA_TYPE"]})");
                                    }
                                }
                            }

                            result.AppendLine();
                            result.AppendLine("=== Sample Data ===");
                            string sampleQuery = "SELECT TOP 3 ID, Value, Expire_DateTime, Used_Status FROM Coupon";
                            using (SqlCommand sampleCmd = new SqlCommand(sampleQuery, conn))
                            {
                                using (SqlDataReader reader = sampleCmd.ExecuteReader())
                                {
                                    if (reader.HasRows)
                                    {
                                        while (reader.Read())
                                        {
                                            result.AppendLine($"  ID: {reader["ID"]}, Value: {reader["Value"]}, " +
                                                $"Expire: {reader["Expire_DateTime"]}, Used: {reader["Used_Status"]}");
                                        }
                                    }
                                    else
                                    {
                                        result.AppendLine("  <span class='info'>No data in Coupon table</span>");
                                    }
                                }
                            }
                        }
                        else
                        {
                            result.AppendLine($"<span class='error'>✗ Coupon table NOT FOUND!</span>");
                        }
                    }

                    result.AppendLine();
                    result.AppendLine("=== Other Tables ===");
                    string allTablesQuery = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE' ORDER BY TABLE_NAME";
                    using (SqlCommand cmd = new SqlCommand(allTablesQuery, conn))
                    {
                        using (SqlDataReader reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                result.AppendLine($"  - {reader["TABLE_NAME"]}");
                            }
                        }
                    }
                }

                result.AppendLine();
                result.AppendLine("<span class='success'>✓ All tests passed!</span>");
            }
            catch (Exception ex)
            {
                result.AppendLine();
                result.AppendLine($"<span class='error'>✗ ERROR: {ex.Message}</span>");
                result.AppendLine();
                result.AppendLine("Stack Trace:");
                result.AppendLine(ex.StackTrace);

                if (ex.InnerException != null)
                {
                    result.AppendLine();
                    result.AppendLine("Inner Exception:");
                    result.AppendLine(ex.InnerException.Message);
                }
            }

            result.AppendLine("</pre>");
            litResult.Text = result.ToString();
        }
    }
}
