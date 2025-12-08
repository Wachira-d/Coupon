using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.UI;
using System.Web.UI.WebControls;
using Coupon.Helpers;

namespace Coupon.Admin
{
    public partial class ImportCoupons : Page
    {
        private DatabaseHelper _dbHelper = new DatabaseHelper();
        private const string SESSION_DATATABLE = "ImportCoupons_DataTable";
        private const string SESSION_FILENAME = "ImportCoupons_FileName";
        private const int MAX_FILE_SIZE = 10 * 1024 * 1024; // 10MB

        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                // Initialize page
            }
        }

        /// <summary>
        /// Handle file upload and preview
        /// </summary>
        protected void btnUpload_Click(object sender, EventArgs e)
        {
            try
            {
                if (!fileUpload.HasFile)
                {
                    ShowError("Please select a file to upload");
                    return;
                }

                // Validate file size
                if (fileUpload.PostedFile.ContentLength > MAX_FILE_SIZE)
                {
                    ShowError($"File size exceeds maximum limit of {MAX_FILE_SIZE / 1024 / 1024}MB");
                    return;
                }

                string fileName = fileUpload.FileName.ToLower();
                string fileExtension = Path.GetExtension(fileName);

                // Validate file type
                if (!(fileExtension == ".csv" || fileExtension == ".xlsx" || fileExtension == ".xls"))
                {
                    ShowError("Only CSV (.csv) and Excel (.xlsx, .xls) files are supported");
                    return;
                }

                // Parse file
                DataTable dt = null;
                if (fileExtension == ".csv")
                {
                    dt = ParseCSV(fileUpload.PostedFile.InputStream);
                }
                else
                {
                    // For Excel, save temporarily and use OleDb or recommend CSV
                    ShowError("Excel files are not yet supported. Please save your file as CSV (.csv) first.\n\n" +
                             "In Excel: File → Save As → CSV (Comma delimited)");
                    return;
                }

                if (dt == null || dt.Rows.Count == 0)
                {
                    ShowError("No data found in file");
                    return;
                }

                // Store in session
                Session[SESSION_DATATABLE] = dt;
                Session[SESSION_FILENAME] = fileName;

                // Show preview
                ShowPreview(dt);

                // Populate column mapping dropdowns
                PopulateColumnDropdowns(dt);

                // Update stats
                lblTotalRows.Text = dt.Rows.Count.ToString();
                lblReadyCount.Text = dt.Rows.Count.ToString();

                // Show preview panel
                panelPreview.Visible = true;
                panelResults.Visible = false;

                ErrorLogger.LogInfo("File uploaded for import", $"File: {fileName}, Rows: {dt.Rows.Count}");
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error uploading file for import", ex);
                ShowError($"Error reading file: {ex.Message}");
            }
        }

        /// <summary>
        /// Parse CSV file into DataTable
        /// </summary>
        private DataTable ParseCSV(Stream stream)
        {
            DataTable dt = new DataTable();

            try
            {
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    // Read header row
                    string headerLine = reader.ReadLine();
                    if (string.IsNullOrEmpty(headerLine))
                    {
                        throw new Exception("CSV file is empty");
                    }

                    // Parse headers
                    string[] headers = ParseCSVLine(headerLine);
                    foreach (string header in headers)
                    {
                        dt.Columns.Add(header.Trim());
                    }

                    // Read data rows
                    string line;
                    int rowCount = 0;
                    while ((line = reader.ReadLine()) != null && rowCount < 10000) // Limit to 10,000 rows
                    {
                        if (string.IsNullOrWhiteSpace(line))
                            continue;

                        string[] values = ParseCSVLine(line);

                        // Ensure we have the right number of columns
                        DataRow row = dt.NewRow();
                        for (int i = 0; i < Math.Min(values.Length, dt.Columns.Count); i++)
                        {
                            row[i] = values[i].Trim();
                        }
                        dt.Rows.Add(row);
                        rowCount++;
                    }
                }

                return dt;
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error parsing CSV file", ex);
                throw;
            }
        }

        /// <summary>
        /// Parse a single CSV line handling quoted values
        /// </summary>
        private string[] ParseCSVLine(string line)
        {
            var values = new System.Collections.Generic.List<string>();
            bool inQuotes = false;
            StringBuilder currentValue = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    values.Add(currentValue.ToString());
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(c);
                }
            }

            // Add last value
            values.Add(currentValue.ToString());

            return values.ToArray();
        }

        /// <summary>
        /// Show preview of data in GridView
        /// </summary>
        private void ShowPreview(DataTable dt)
        {
            // Show first 10 rows only
            DataTable previewDt = dt.Clone();
            int rowsToShow = Math.Min(10, dt.Rows.Count);

            for (int i = 0; i < rowsToShow; i++)
            {
                previewDt.ImportRow(dt.Rows[i]);
            }

            gvPreview.DataSource = previewDt;
            gvPreview.DataBind();
        }

        /// <summary>
        /// Populate column mapping dropdowns
        /// </summary>
        private void PopulateColumnDropdowns(DataTable dt)
        {
            ddlColumnID.Items.Clear();
            ddlColumnCode.Items.Clear();

            // Add "Select" option
            ddlColumnID.Items.Add(new ListItem("-- Select Column --", ""));
            ddlColumnCode.Items.Add(new ListItem("-- Select Column --", ""));

            // Add column names
            foreach (DataColumn column in dt.Columns)
            {
                ddlColumnID.Items.Add(new ListItem(column.ColumnName, column.ColumnName));
                ddlColumnCode.Items.Add(new ListItem(column.ColumnName, column.ColumnName));
            }

            // Auto-detect common column names
            AutoDetectColumns(dt);
        }

        /// <summary>
        /// Auto-detect column mappings based on common names
        /// </summary>
        private void AutoDetectColumns(DataTable dt)
        {
            foreach (DataColumn column in dt.Columns)
            {
                string colName = column.ColumnName.ToLower();

                // Detect ID column
                if (colName.Contains("id") || colName.Contains("code") || colName.Contains("coupon"))
                {
                    if (ddlColumnID.SelectedIndex == 0) // Not yet selected
                    {
                        ddlColumnID.SelectedValue = column.ColumnName;
                    }
                }

                // Detect Barcode/Code column
                if (colName.Contains("barcode") || colName.Contains("bar_code") ||
                    colName.Contains("code") && !colName.Contains("coupon"))
                {
                    if (ddlColumnCode.SelectedIndex == 0) // Not yet selected
                    {
                        ddlColumnCode.SelectedValue = column.ColumnName;
                    }
                }
            }
        }

        /// <summary>
        /// Import data to database
        /// </summary>
        protected void btnImport_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrEmpty(ddlColumnID.SelectedValue))
                {
                    ShowError("Please select the Coupon ID column");
                    return;
                }

                if (string.IsNullOrEmpty(ddlColumnCode.SelectedValue))
                {
                    ShowError("Please select the Barcode column");
                    return;
                }

                if (string.IsNullOrEmpty(txtValue.Text))
                {
                    ShowError("Please enter the coupon value");
                    return;
                }

                if (string.IsNullOrEmpty(txtExpireDate.Text))
                {
                    ShowError("Please select the expiration date");
                    return;
                }

                // Get data from session
                DataTable dt = Session[SESSION_DATATABLE] as DataTable;
                if (dt == null)
                {
                    ShowError("Session expired. Please upload file again");
                    return;
                }

                string columnID = ddlColumnID.SelectedValue;
                string columnCode = ddlColumnCode.SelectedValue;
                string value = txtValue.Text.Trim();
                DateTime expireDate = DateTime.Parse(txtExpireDate.Text);
                string description = txtDescription.Text.Trim();

                // Import data
                int successCount = 0;
                int errorCount = 0;
                int duplicateCount = 0;
                StringBuilder errors = new StringBuilder();

                foreach (DataRow row in dt.Rows)
                {
                    try
                    {
                        string couponID = row[columnID]?.ToString().Trim();
                        string barcodeCode = row[columnCode]?.ToString().Trim();

                        // Validate
                        if (string.IsNullOrEmpty(couponID) || string.IsNullOrEmpty(barcodeCode))
                        {
                            errorCount++;
                            continue;
                        }

                        // Check if coupon already exists
                        string checkQuery = $"SELECT COUNT(*) FROM {_dbHelper.GetTableName("Coupon")} WHERE {_dbHelper.GetColumnName("ID")} = @id";
                        var checkParam = _dbHelper.CreateParameter("@id", couponID);
                        int exists = Convert.ToInt32(_dbHelper.ExecuteScalar(checkQuery, checkParam));

                        if (exists > 0)
                        {
                            duplicateCount++;
                            continue;
                        }

                        // Insert into database
                        string insertQuery = $@"INSERT INTO {_dbHelper.GetTableName("Coupon")}
                            ({_dbHelper.GetColumnName("ID")},
                             {_dbHelper.GetColumnName("Code")},
                             {_dbHelper.GetColumnName("Value")},
                             {_dbHelper.GetColumnName("Expire_DateTime")},
                             {_dbHelper.GetColumnName("Used_Status")},
                             {_dbHelper.GetColumnName("Date_Use")})
                            VALUES
                            (@id, @code, @value, @expireDate, @usedStatus, NULL)";

                        var parameters = new[]
                        {
                            _dbHelper.CreateParameter("@id", couponID),
                            _dbHelper.CreateParameter("@code", barcodeCode),
                            _dbHelper.CreateParameter("@value", value),
                            _dbHelper.CreateParameter("@expireDate", expireDate.ToString("yyyy-MM-dd HH:mm:ss")),
                            _dbHelper.CreateParameter("@usedStatus", _dbHelper.GetBooleanValue(false))
                        };

                        _dbHelper.ExecuteNonQuery(insertQuery, parameters);
                        successCount++;
                    }
                    catch (Exception rowEx)
                    {
                        errorCount++;
                        errors.AppendLine($"Row error: {rowEx.Message}");
                        ErrorLogger.LogError("Error importing coupon row", rowEx,
                            $"ID: {row[columnID]}, Code: {row[columnCode]}");
                    }
                }

                // Show results
                ShowResults(successCount, errorCount, duplicateCount, errors.ToString());

                // Log import
                ErrorLogger.LogInfo("Coupons imported successfully",
                    $"Success: {successCount}, Errors: {errorCount}, Duplicates: {duplicateCount}, Value: {value}, Expire: {expireDate:yyyy-MM-dd}");

                // Log to audit
                AuditLogger.LogDataModification(
                    "admin",
                    "Coupon",
                    "Bulk Import",
                    "0 coupons",
                    $"{successCount} coupons imported",
                    Request.UserHostAddress
                );
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error during coupon import", ex);
                ShowError($"Import failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Show import results
        /// </summary>
        private void ShowResults(int successCount, int errorCount, int duplicateCount, string errorDetails)
        {
            StringBuilder html = new StringBuilder();

            if (successCount > 0)
            {
                html.Append("<div class='alert alert-success'>");
                html.Append($"<h3>✅ Import Successful!</h3>");
                html.Append($"<p><strong>{successCount}</strong> coupons imported successfully</p>");
                html.Append("</div>");
            }

            if (duplicateCount > 0)
            {
                html.Append("<div class='alert alert-info'>");
                html.Append($"<p><strong>{duplicateCount}</strong> coupons skipped (already exist in database)</p>");
                html.Append("</div>");
            }

            if (errorCount > 0)
            {
                html.Append("<div class='alert alert-error'>");
                html.Append($"<p><strong>{errorCount}</strong> rows failed to import</p>");
                if (!string.IsNullOrEmpty(errorDetails))
                {
                    html.Append($"<pre style='margin-top:10px; max-height:200px; overflow-y:auto;'>{System.Web.HttpUtility.HtmlEncode(errorDetails)}</pre>");
                }
                html.Append("</div>");
            }

            // Stats
            html.Append("<div class='stats'>");
            html.Append($"<div class='stat-card'><div class='stat-number' style='color:#28a745;'>{successCount}</div><div class='stat-label'>Imported</div></div>");
            html.Append($"<div class='stat-card'><div class='stat-number' style='color:#17a2b8;'>{duplicateCount}</div><div class='stat-label'>Skipped</div></div>");
            html.Append($"<div class='stat-card'><div class='stat-number' style='color:#dc3545;'>{errorCount}</div><div class='stat-label'>Errors</div></div>");
            html.Append("</div>");

            litResults.Text = html.ToString();
            panelResults.Visible = true;
            panelPreview.Visible = false;
        }

        /// <summary>
        /// Reset form for new import
        /// </summary>
        protected void btnReset_Click(object sender, EventArgs e)
        {
            Session.Remove(SESSION_DATATABLE);
            Session.Remove(SESSION_FILENAME);

            panelPreview.Visible = false;
            panelResults.Visible = false;

            txtValue.Text = "";
            txtExpireDate.Text = "";
            txtDescription.Text = "";

            Response.Redirect(Request.RawUrl);
        }

        /// <summary>
        /// Show error message
        /// </summary>
        private void ShowError(string message)
        {
            StringBuilder html = new StringBuilder();
            html.Append("<div class='alert alert-error' style='margin-bottom:20px;'>");
            html.Append($"<strong>❌ Error:</strong> {System.Web.HttpUtility.HtmlEncode(message)}");
            html.Append("</div>");

            // Show at top of content
            ClientScript.RegisterStartupScript(this.GetType(), "error",
                $"alert('{message.Replace("'", "\\'")}');", true);
        }
    }
}
