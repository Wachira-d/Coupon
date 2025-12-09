<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ImportCoupons.aspx.cs" Inherits="Coupon.Admin.ImportCoupons" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title>Import Coupons - Bulk Upload</title>
    <meta charset="utf-8">
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <style>
        * { margin: 0; padding: 0; box-sizing: border-box; }
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            padding: 20px;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background: white;
            border-radius: 15px;
            box-shadow: 0 20px 60px rgba(0,0,0,0.3);
            overflow: hidden;
        }
        .header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 30px;
            text-align: center;
        }
        .header h1 {
            font-size: 32px;
            margin-bottom: 10px;
        }
        .header p {
            font-size: 16px;
            opacity: 0.9;
        }
        .content {
            padding: 40px;
        }
        .step {
            margin-bottom: 40px;
            padding: 25px;
            background: #f8f9fa;
            border-radius: 10px;
            border-left: 5px solid #667eea;
        }
        .step-header {
            display: flex;
            align-items: center;
            margin-bottom: 20px;
        }
        .step-number {
            width: 40px;
            height: 40px;
            background: #667eea;
            color: white;
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            font-weight: bold;
            font-size: 20px;
            margin-right: 15px;
        }
        .step-title {
            font-size: 22px;
            font-weight: 600;
            color: #333;
        }
        .form-group {
            margin-bottom: 20px;
        }
        .form-group label {
            display: block;
            font-weight: 600;
            color: #555;
            margin-bottom: 8px;
            font-size: 14px;
        }
        .form-control {
            width: 100%;
            padding: 12px;
            border: 2px solid #ddd;
            border-radius: 8px;
            font-size: 14px;
            transition: all 0.3s;
        }
        .form-control:focus {
            outline: none;
            border-color: #667eea;
            box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
        }
        .file-upload {
            position: relative;
            display: inline-block;
            width: 100%;
        }
        .file-upload-label {
            display: block;
            padding: 40px;
            background: white;
            border: 3px dashed #ddd;
            border-radius: 10px;
            text-align: center;
            cursor: pointer;
            transition: all 0.3s;
        }
        .file-upload-label:hover {
            border-color: #667eea;
            background: #f8f9ff;
        }
        .file-upload-label .icon {
            font-size: 48px;
            color: #667eea;
            margin-bottom: 15px;
        }
        .file-upload-label .text {
            font-size: 16px;
            color: #666;
        }
        .file-upload-label .hint {
            font-size: 13px;
            color: #999;
            margin-top: 8px;
        }
        .btn {
            padding: 12px 30px;
            border: none;
            border-radius: 8px;
            font-size: 16px;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.3s;
            display: inline-block;
            text-align: center;
        }
        .btn-primary {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
        }
        .btn-primary:hover {
            transform: translateY(-2px);
            box-shadow: 0 10px 20px rgba(102, 126, 234, 0.3);
        }
        .btn-success {
            background: linear-gradient(135deg, #11998e 0%, #38ef7d 100%);
            color: white;
        }
        .btn-success:hover {
            transform: translateY(-2px);
            box-shadow: 0 10px 20px rgba(17, 153, 142, 0.3);
        }
        .btn:disabled {
            opacity: 0.5;
            cursor: not-allowed;
            transform: none !important;
        }
        .preview-table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 20px;
            background: white;
            border-radius: 8px;
            overflow: hidden;
            box-shadow: 0 2px 10px rgba(0,0,0,0.05);
        }
        .preview-table th {
            background: #667eea;
            color: white;
            padding: 15px;
            text-align: left;
            font-weight: 600;
        }
        .preview-table td {
            padding: 12px 15px;
            border-bottom: 1px solid #eee;
        }
        .preview-table tr:hover {
            background: #f8f9fa;
        }
        .column-map {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            margin-top: 20px;
        }
        .map-item {
            display: flex;
            align-items: center;
            padding: 15px;
            background: white;
            border-radius: 8px;
            box-shadow: 0 2px 5px rgba(0,0,0,0.05);
        }
        .map-label {
            flex: 1;
            font-weight: 600;
            color: #333;
        }
        .map-arrow {
            margin: 0 15px;
            color: #667eea;
            font-size: 20px;
        }
        .map-select {
            flex: 2;
        }
        .lot-settings {
            display: grid;
            grid-template-columns: 1fr 1fr;
            gap: 20px;
            margin-top: 20px;
        }
        .alert {
            padding: 15px 20px;
            border-radius: 8px;
            margin-bottom: 20px;
        }
        .alert-success {
            background: #d4edda;
            border-left: 4px solid #28a745;
            color: #155724;
        }
        .alert-error {
            background: #f8d7da;
            border-left: 4px solid #dc3545;
            color: #721c24;
        }
        .alert-info {
            background: #d1ecf1;
            border-left: 4px solid #17a2b8;
            color: #0c5460;
        }
        .progress-bar {
            width: 100%;
            height: 30px;
            background: #e9ecef;
            border-radius: 15px;
            overflow: hidden;
            margin-top: 20px;
        }
        .progress-bar-fill {
            height: 100%;
            background: linear-gradient(90deg, #667eea 0%, #764ba2 100%);
            transition: width 0.3s;
            display: flex;
            align-items: center;
            justify-content: center;
            color: white;
            font-weight: 600;
        }
        .stats {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 20px;
            margin-top: 20px;
        }
        .stat-card {
            padding: 20px;
            background: white;
            border-radius: 10px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.05);
            text-align: center;
        }
        .stat-number {
            font-size: 32px;
            font-weight: bold;
            color: #667eea;
        }
        .stat-label {
            font-size: 14px;
            color: #666;
            margin-top: 5px;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="container">
            <div class="header">
                <h1>📦 Import Coupons - Bulk Upload</h1>
                <p>Upload CSV or Excel file to import multiple coupons at once</p>
            </div>

            <!-- User Info Bar -->
            <div style="background: #f8f9fa; padding: 12px 30px; border-bottom: 2px solid #e0e0e0; display: flex; justify-content: space-between; align-items: center;">
                <div style="color: #555;">
                    <strong><asp:Label ID="lblCurrentUser" runat="server" /></strong>
                    <span style="margin-left: 15px;"><asp:Label ID="lblCurrentEmail" runat="server" /></span>
                </div>
                <div>
                    <a href="/Admin/Report.aspx" style="color: #667eea; text-decoration: none; padding: 6px 12px; background: white; border: 1px solid #667eea; border-radius: 5px; margin-right: 8px; display: inline-block; font-size: 14px;">Report</a>
                    <a href="/Admin/Logout.aspx" style="color: white; text-decoration: none; padding: 6px 12px; background: #667eea; border-radius: 5px; display: inline-block; font-size: 14px;">Logout</a>
                </div>
            </div>

            <div class="content">
                <!-- Step 1: Upload File -->
                <div class="step">
                    <div class="step-header">
                        <div class="step-number">1</div>
                        <div class="step-title">Upload File (CSV or Excel)</div>
                    </div>
                    <div class="file-upload">
                        <label for="fileUpload" class="file-upload-label">
                            <div class="icon">[File]</div>
                            <div class="text">Click to select file or drag and drop</div>
                            <div class="hint">Supported formats: CSV (.csv), Excel (.xlsx, .xls)</div>
                        </label>
                        <asp:FileUpload ID="fileUpload" runat="server" CssClass="form-control" style="display:none;" onchange="document.querySelector('.file-upload-label .text').textContent = this.files[0]?.name || 'Click to select file or drag and drop'" />
                    </div>
                    <div style="margin-top: 20px;">
                        <asp:Button ID="btnUpload" runat="server" Text="Upload &amp; Preview" CssClass="btn btn-primary" OnClick="btnUpload_Click" />
                    </div>
                </div>

                <!-- Step 2: Preview & Column Mapping -->
                <asp:Panel ID="panelPreview" runat="server" Visible="false">
                    <div class="step">
                        <div class="step-header">
                            <div class="step-number">2</div>
                            <div class="step-title">Preview Data & Map Columns</div>
                        </div>

                        <div class="alert alert-info">
                            <strong>Preview:</strong> Showing first 10 rows from your file
                        </div>

                        <div style="overflow-x: auto;">
                            <asp:GridView ID="gvPreview" runat="server" CssClass="preview-table" AutoGenerateColumns="true" />
                        </div>

                        <h3 style="margin-top: 30px; margin-bottom: 15px;">Map Your Columns</h3>
                        <div class="column-map">
                            <div class="map-item">
                                <div class="map-label">Coupon ID Column:</div>
                                <div class="map-arrow">→</div>
                                <div style="display: flex; align-items: center; gap: 10px;">
                                    <asp:DropDownList ID="ddlColumnID" runat="server" CssClass="form-control map-select" Enabled="false" />
                                    <div style="white-space: nowrap;">
                                        <asp:CheckBox ID="chkAutoGenerateID" runat="server" Checked="true" AutoPostBack="true" OnCheckedChanged="chkAutoGenerateID_CheckedChanged" />
                                        <label for="<%= chkAutoGenerateID.ClientID %>" style="margin-left: 5px; cursor: pointer;">Auto-generate ID</label>
                                    </div>
                                </div>
                            </div>
                            <div class="map-item">
                                <div class="map-label">Barcode Column:</div>
                                <div class="map-arrow">→</div>
                                <asp:DropDownList ID="ddlColumnCode" runat="server" CssClass="form-control map-select" />
                            </div>
                        </div>
                    </div>

                    <!-- Step 3: Lot Settings -->
                    <div class="step">
                        <div class="step-header">
                            <div class="step-number">3</div>
                            <div class="step-title">Set Lot Properties</div>
                        </div>

                        <div class="lot-settings">
                            <div class="form-group">
                                <label>Coupon Value (Baht):</label>
                                <asp:TextBox ID="txtValue" runat="server" CssClass="form-control" placeholder="e.g., 200, 300, 500" TextMode="Number" />
                            </div>
                            <div class="form-group">
                                <label>Expiration Date:</label>
                                <asp:TextBox ID="txtExpireDate" runat="server" CssClass="form-control" TextMode="Date" />
                            </div>
                        </div>

                        <div class="form-group" style="margin-top: 20px;">
                            <label>Lot Description (Optional):</label>
                            <asp:TextBox ID="txtDescription" runat="server" CssClass="form-control" placeholder="e.g., Honda CX Campaign 2025" TextMode="MultiLine" Rows="3" />
                        </div>
                    </div>

                    <!-- Step 4: Import -->
                    <div class="step">
                        <div class="step-header">
                            <div class="step-number">4</div>
                            <div class="step-title">Import to Database</div>
                        </div>

                        <div class="stats">
                            <div class="stat-card">
                                <div class="stat-number"><asp:Label ID="lblTotalRows" runat="server" Text="0" /></div>
                                <div class="stat-label">Total Rows</div>
                            </div>
                            <div class="stat-card">
                                <div class="stat-number"><asp:Label ID="lblReadyCount" runat="server" Text="0" /></div>
                                <div class="stat-label">Ready to Import</div>
                            </div>
                        </div>

                        <div style="margin-top: 30px; text-align: center;">
                            <asp:Button ID="btnImport" runat="server" Text="🚀 Start Import" CssClass="btn btn-success" OnClick="btnImport_Click" />
                        </div>
                    </div>
                </asp:Panel>

                <!-- Results -->
                <asp:Panel ID="panelResults" runat="server" Visible="false">
                    <div class="step">
                        <div class="step-header">
                            <div class="step-number">✓</div>
                            <div class="step-title">Import Results</div>
                        </div>

                        <asp:Literal ID="litResults" runat="server" />

                        <div style="margin-top: 30px; text-align: center;">
                            <asp:Button ID="btnReset" runat="server" Text="🔄 Import Another File" CssClass="btn btn-primary" OnClick="btnReset_Click" />
                        </div>
                    </div>
                </asp:Panel>
            </div>
        </div>
    </form>

    <script>
        // Enhance file upload UX
        document.getElementById('<%= fileUpload.ClientID %>').addEventListener('change', function() {
            const label = document.querySelector('.file-upload-label .text');
            if (this.files.length > 0) {
                label.textContent = this.files[0].name;
                label.style.color = '#667eea';
                label.style.fontWeight = '600';
            }
        });

        // Click label to trigger file input
        document.querySelector('.file-upload-label').addEventListener('click', function() {
            document.getElementById('<%= fileUpload.ClientID %>').click();
        });
    </script>
</body>
</html>
