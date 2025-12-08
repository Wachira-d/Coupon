<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TestLogs.aspx.cs" Inherits="Coupon.TestLogs" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title>Test Log System</title>
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 20px;
            background-color: #f5f5f5;
        }
        .container {
            max-width: 1200px;
            margin: 0 auto;
            background: white;
            padding: 20px;
            border-radius: 8px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }
        h1 {
            color: #333;
            border-bottom: 3px solid #007bff;
            padding-bottom: 10px;
        }
        .section {
            margin: 20px 0;
            padding: 15px;
            border: 1px solid #ddd;
            border-radius: 4px;
        }
        .section h2 {
            color: #007bff;
            margin-top: 0;
        }
        .button-group {
            margin: 10px 0;
        }
        .btn {
            padding: 10px 20px;
            margin: 5px;
            border: none;
            border-radius: 4px;
            cursor: pointer;
            font-size: 14px;
        }
        .btn-primary { background-color: #007bff; color: white; }
        .btn-success { background-color: #28a745; color: white; }
        .btn-warning { background-color: #ffc107; color: black; }
        .btn-danger { background-color: #dc3545; color: white; }
        .btn-info { background-color: #17a2b8; color: white; }
        .btn:hover { opacity: 0.8; }
        .result {
            margin-top: 15px;
            padding: 15px;
            border-radius: 4px;
            white-space: pre-wrap;
            font-family: 'Courier New', monospace;
            font-size: 12px;
        }
        .success { background-color: #d4edda; border: 1px solid #c3e6cb; color: #155724; }
        .error { background-color: #f8d7da; border: 1px solid #f5c6cb; color: #721c24; }
        .info { background-color: #d1ecf1; border: 1px solid #bee5eb; color: #0c5460; }
        .log-preview {
            background-color: #f8f9fa;
            border: 1px solid #dee2e6;
            padding: 10px;
            max-height: 300px;
            overflow-y: auto;
            font-family: 'Courier New', monospace;
            font-size: 11px;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="container">
            <h1>🔍 Log System Testing Tool</h1>

            <div class="section">
                <h2>1. Test Error Logging</h2>
                <div class="button-group">
                    <asp:Button ID="btnTestError" runat="server" Text="Test Error Log" CssClass="btn btn-danger" OnClick="btnTestError_Click" />
                    <asp:Button ID="btnTestWarning" runat="server" Text="Test Warning Log" CssClass="btn btn-warning" OnClick="btnTestWarning_Click" />
                    <asp:Button ID="btnTestInfo" runat="server" Text="Test Info Log" CssClass="btn btn-info" OnClick="btnTestInfo_Click" />
                </div>
                <asp:Label ID="lblErrorResult" runat="server" CssClass="result" Visible="false"></asp:Label>
            </div>

            <div class="section">
                <h2>2. Test Audit Logging</h2>
                <div class="button-group">
                    <asp:Button ID="btnTestAuth" runat="server" Text="Test Authentication Log" CssClass="btn btn-primary" OnClick="btnTestAuth_Click" />
                    <asp:Button ID="btnTestSecurity" runat="server" Text="Test Security Event" CssClass="btn btn-danger" OnClick="btnTestSecurity_Click" />
                    <asp:Button ID="btnTestCoupon" runat="server" Text="Test Coupon Redemption" CssClass="btn btn-success" OnClick="btnTestCoupon_Click" />
                </div>
                <asp:Label ID="lblAuditResult" runat="server" CssClass="result" Visible="false"></asp:Label>
            </div>

            <div class="section">
                <h2>3. Check Log Folders</h2>
                <asp:Button ID="btnCheckFolders" runat="server" Text="Check Folders & Permissions" CssClass="btn btn-info" OnClick="btnCheckFolders_Click" />
                <asp:Label ID="lblFolderResult" runat="server" CssClass="result" Visible="false"></asp:Label>
            </div>

            <div class="section">
                <h2>4. View Recent Logs</h2>
                <div class="button-group">
                    <asp:Button ID="btnViewError" runat="server" Text="View Error Log" CssClass="btn btn-danger" OnClick="btnViewError_Click" />
                    <asp:Button ID="btnViewAudit" runat="server" Text="View Audit Log" CssClass="btn btn-primary" OnClick="btnViewAudit_Click" />
                </div>
                <asp:Label ID="lblLogViewer" runat="server" CssClass="log-preview" Visible="false"></asp:Label>
            </div>

            <div class="section">
                <h2>5. System Information</h2>
                <asp:Button ID="btnSystemInfo" runat="server" Text="Show System Info" CssClass="btn btn-success" OnClick="btnSystemInfo_Click" />
                <asp:Label ID="lblSystemInfo" runat="server" CssClass="result" Visible="false"></asp:Label>
            </div>
        </div>
    </form>
</body>
</html>
