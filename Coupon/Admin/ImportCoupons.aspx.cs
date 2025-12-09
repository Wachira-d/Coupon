using System;
using System.Configuration;
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

        // Random ID generation (matching old ImportCode application)
        private static readonly Random _random = new Random();
        private static readonly string ID_CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
        private static readonly int SHORT_URL_DIGIT = int.Parse(ConfigurationManager.AppSettings["ShortUrlDigit"] ?? "8");

        protected void Page_Load(object sender, EventArgs e)
        {
            // SECURITY: Check authentication before allowing access
            if (!AuthenticationHelper.ValidateSession(Session))
            {
                // Redirect to login page with return URL
                string returnUrl = Request.Url.PathAndQuery;
                Response.Redirect($"~/Admin/Login.aspx?ReturnUrl={Server.UrlEncode(returnUrl)}", false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!IsPostBack)
            {
                // Show user info
                UserInfo currentUser = AuthenticationHelper.GetCurrentUser(Session);
                if (currentUser != null)
                {
                    lblCurrentUser.Text = currentUser.DisplayName ?? currentUser.Username;
                    lblCurrentEmail.Text = currentUser.Email ?? "";
                }

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
                else // Excel files
                {
                    dt = ParseExcel(fileUpload.PostedFile.InputStream, fileExtension);
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
        /// Parse Excel file using reflection to avoid hard dependency on EPPlus
        /// If EPPlus is not installed, will show friendly error message
        /// </summary>
        private DataTable ParseExcel(Stream stream, string fileExtension)
        {
            try
            {
                // Try to load EPPlus assembly using reflection
                var epplusAssembly = System.Reflection.Assembly.Load("EPPlus");
                if (epplusAssembly == null)
                {
                    throw new FileNotFoundException("EPPlus assembly not found");
                }

                // Get ExcelPackage type
                Type excelPackageType = epplusAssembly.GetType("OfficeOpenXml.ExcelPackage");
                if (excelPackageType == null)
                {
                    throw new TypeLoadException("Could not load ExcelPackage type");
                }

                // Set EPPlus license context for newer versions
                Type licenseContextType = epplusAssembly.GetType("OfficeOpenXml.ExcelPackage+LicenseContext");
                if (licenseContextType != null)
                {
                    // EPPlus 5+ requires license context, but we're using 4.x which doesn't need it
                    // This code is here for future compatibility
                }

                DataTable dt = new DataTable();

                // Create ExcelPackage instance
                var package = Activator.CreateInstance(excelPackageType, stream);
                try
                {
                    // Get Workbook property
                    var workbookProperty = excelPackageType.GetProperty("Workbook");
                    var workbook = workbookProperty.GetValue(package);

                    // Get Worksheets property
                    var worksheetsProperty = workbook.GetType().GetProperty("Worksheets");
                    var worksheets = worksheetsProperty.GetValue(workbook);

                    // Get first worksheet
                    var firstMethod = worksheets.GetType().GetMethod("get_Item", new Type[] { typeof(int) });
                    var worksheet = firstMethod.Invoke(worksheets, new object[] { 1 }); // 1-indexed

                    if (worksheet == null)
                    {
                        throw new Exception("No worksheets found in Excel file");
                    }

                    // Get Dimension property
                    var dimensionProperty = worksheet.GetType().GetProperty("Dimension");
                    var dimension = dimensionProperty.GetValue(worksheet);

                    if (dimension == null)
                    {
                        throw new Exception("Excel worksheet is empty");
                    }

                    // Get row and column counts
                    var endProperty = dimension.GetType().GetProperty("End");
                    var end = endProperty.GetValue(dimension);
                    var rowProperty = end.GetType().GetProperty("Row");
                    var columnProperty = end.GetType().GetProperty("Column");

                    int endRow = (int)rowProperty.GetValue(end);
                    int endColumn = (int)columnProperty.GetValue(end);

                    // Limit to 10,000 rows
                    endRow = Math.Min(endRow, 10000);

                    // Get Cells property method
                    var cellsProperty = worksheet.GetType().GetProperty("Cells");

                    // Read header row (row 1)
                    for (int col = 1; col <= endColumn; col++)
                    {
                        var cellAddress = cellsProperty.GetValue(worksheet);
                        var indexer = cellAddress.GetType().GetProperty("Item", new Type[] { typeof(int), typeof(int) });
                        var cell = indexer.GetValue(cellAddress, new object[] { 1, col });

                        var textProperty = cell.GetType().GetProperty("Text");
                        string headerText = textProperty.GetValue(cell)?.ToString() ?? $"Column{col}";

                        dt.Columns.Add(headerText.Trim());
                    }

                    // Read data rows
                    for (int row = 2; row <= endRow; row++)
                    {
                        DataRow dataRow = dt.NewRow();
                        bool hasData = false;

                        for (int col = 1; col <= endColumn && col <= dt.Columns.Count; col++)
                        {
                            var cellAddress = cellsProperty.GetValue(worksheet);
                            var indexer = cellAddress.GetType().GetProperty("Item", new Type[] { typeof(int), typeof(int) });
                            var cell = indexer.GetValue(cellAddress, new object[] { row, col });

                            var textProperty = cell.GetType().GetProperty("Text");
                            string cellValue = textProperty.GetValue(cell)?.ToString() ?? "";

                            dataRow[col - 1] = cellValue.Trim();

                            if (!string.IsNullOrWhiteSpace(cellValue))
                            {
                                hasData = true;
                            }
                        }

                        // Only add rows that have at least one non-empty cell
                        if (hasData)
                        {
                            dt.Rows.Add(dataRow);
                        }
                    }
                }
                finally
                {
                    // Dispose if package implements IDisposable
                    if (package is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }

                return dt;
            }
            catch (FileNotFoundException)
            {
                // EPPlus not installed
                throw new Exception(
                    "Excel support requires EPPlus library.\n\n" +
                    "INSTALLATION STEPS:\n" +
                    "1. Run InstallEPPlus.ps1 from the solution directory\n" +
                    "   OR\n" +
                    "2. In Visual Studio: Right-click project → Manage NuGet Packages → Install 'EPPlus' version 4.5.3.3\n\n" +
                    "ALTERNATIVE:\n" +
                    "Convert your Excel file to CSV:\n" +
                    "- Open file in Excel\n" +
                    "- File → Save As → CSV (Comma delimited) (*.csv)\n" +
                    "- Then upload the CSV file"
                );
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error parsing Excel file", ex);
                throw new Exception($"Failed to read Excel file: {ex.Message}\n\nPlease try converting to CSV format instead.");
            }
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
                // Check if auto-generate ID is enabled
                bool autoGenerateId = chkAutoGenerateID.Checked;

                // Validate inputs
                if (!autoGenerateId && string.IsNullOrEmpty(ddlColumnID.SelectedValue))
                {
                    ShowError("Please select the Coupon ID column or enable Auto-generate ID");
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

                string columnID = autoGenerateId ? null : ddlColumnID.SelectedValue;
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
                        string couponID;
                        string barcodeCode = row[columnCode]?.ToString().Trim();

                        // Generate or get ID from CSV
                        if (autoGenerateId)
                        {
                            // Auto-generate unique ID (matching old ImportCode logic)
                            couponID = GenerateUniqueId();
                        }
                        else
                        {
                            couponID = row[columnID]?.ToString().Trim();
                        }

                        // Validate barcode
                        if (string.IsNullOrEmpty(barcodeCode))
                        {
                            errorCount++;
                            continue;
                        }

                        // Validate ID (only for non-auto-generated)
                        if (!autoGenerateId && string.IsNullOrEmpty(couponID))
                        {
                            errorCount++;
                            continue;
                        }

                        // Check if coupon already exists (skip for auto-generated IDs since they are already unique)
                        if (!autoGenerateId)
                        {
                            string checkQuery = $"SELECT COUNT(*) FROM {_dbHelper.GetTableName("Coupon")} WHERE {_dbHelper.GetColumnName("ID")} = @id";
                            var checkParam = _dbHelper.CreateParameter("@id", couponID);
                            int exists = Convert.ToInt32(_dbHelper.ExecuteScalar(checkQuery, checkParam));

                            if (exists > 0)
                            {
                                duplicateCount++;
                                continue;
                            }
                        }

                        // Insert into database (matching old ImportCode format)
                        string insertQuery = $@"INSERT INTO {_dbHelper.GetTableName("Coupon")}
                            ({_dbHelper.GetColumnName("ID")},
                             {_dbHelper.GetColumnName("Code")},
                             {_dbHelper.GetColumnName("Date_DataIn")},
                             {_dbHelper.GetColumnName("Expire_DateTime")},
                             {_dbHelper.GetColumnName("Value")},
                             {_dbHelper.GetColumnName("Used_Status")})
                            VALUES
                            (@id, @code, @dateDataIn, @expireDate, @value, @usedStatus)";

                        var parameters = new[]
                        {
                            _dbHelper.CreateParameter("@id", couponID),
                            _dbHelper.CreateParameter("@code", barcodeCode),
                            _dbHelper.CreateParameter("@dateDataIn", DateTime.Now.ToString("yyyy-MM-dd HH:mm")),
                            _dbHelper.CreateParameter("@expireDate", expireDate.ToString("yyyy-MM-dd HH:mm:ss")),
                            _dbHelper.CreateParameter("@value", value),
                            _dbHelper.CreateParameter("@usedStatus", _dbHelper.GetBooleanValue(false))
                        };

                        _dbHelper.ExecuteNonQuery(insertQuery, parameters);
                        successCount++;
                    }
                    catch (Exception rowEx)
                    {
                        errorCount++;
                        errors.AppendLine($"Row error: {rowEx.Message}");
                        string idInfo = autoGenerateId ? "Auto-generated" : (columnID != null ? row[columnID]?.ToString() : "N/A");
                        ErrorLogger.LogError("Error importing coupon row", rowEx,
                            $"ID: {idInfo}, Code: {row[columnCode]}");
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
                html.Append($"<h3>Import Successful!</h3>");
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
            html.Append($"<strong>Error:</strong> {System.Web.HttpUtility.HtmlEncode(message)}");
            html.Append("</div>");

            // Show at top of content
            ClientScript.RegisterStartupScript(this.GetType(), "error",
                $"alert('{message.Replace("'", "\\'")}');", true);
        }

        #region ID Generation (from old ImportCode application)

        /// <summary>
        /// Handle auto-generate ID checkbox change
        /// </summary>
        protected void chkAutoGenerateID_CheckedChanged(object sender, EventArgs e)
        {
            ddlColumnID.Enabled = !chkAutoGenerateID.Checked;
            if (chkAutoGenerateID.Checked)
            {
                ddlColumnID.SelectedIndex = 0;
            }
        }

        /// <summary>
        /// Generate a random alphanumeric string (matching old ImportCode logic)
        /// </summary>
        private static string RandomString(int length)
        {
            return new string(Enumerable.Repeat(ID_CHARS, length)
                .Select(s => s[_random.Next(s.Length)]).ToArray());
        }

        /// <summary>
        /// Generate a unique coupon ID that doesn't exist in the database
        /// </summary>
        private string GenerateUniqueId()
        {
            const int maxAttempts = 100;
            int attempts = 0;

            while (attempts < maxAttempts)
            {
                string id = RandomString(SHORT_URL_DIGIT);

                // Check if ID already exists in database
                string checkQuery = $"SELECT COUNT(*) FROM {_dbHelper.GetTableName("Coupon")} WHERE {_dbHelper.GetColumnName("ID")} = @id";
                var checkParam = _dbHelper.CreateParameter("@id", id);
                int exists = Convert.ToInt32(_dbHelper.ExecuteScalar(checkQuery, checkParam));

                if (exists == 0)
                {
                    return id;
                }

                attempts++;
            }

            // Fallback: use GUID-based ID if random generation keeps colliding
            ErrorLogger.LogWarning("GenerateUniqueId: Max attempts reached, using GUID fallback",
                $"Attempts: {maxAttempts}");
            return Guid.NewGuid().ToString("N").Substring(0, SHORT_URL_DIGIT);
        }

        #endregion
    }
}
