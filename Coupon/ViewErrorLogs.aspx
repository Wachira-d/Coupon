<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ViewErrorLogs.aspx.cs" Inherits="Coupon.ViewErrorLogs" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title>Error Logs Viewer</title>
    <meta charset="utf-8">
    <style>
        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            margin: 0;
            padding: 20px;
            background: #f5f5f5;
        }
        .container {
            max-width: 1400px;
            margin: 0 auto;
            background: white;
            padding: 30px;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
        }
        h1 {
            color: #333;
            margin-bottom: 30px;
            border-bottom: 3px solid #2196F3;
            padding-bottom: 10px;
        }
        .filters {
            background: #f9f9f9;
            padding: 20px;
            border-radius: 5px;
            margin-bottom: 20px;
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 15px;
        }
        .filter-group {
            display: flex;
            flex-direction: column;
        }
        .filter-group label {
            font-weight: bold;
            margin-bottom: 5px;
            color: #555;
        }
        .filter-group select, .filter-group input {
            padding: 8px;
            border: 1px solid #ddd;
            border-radius: 4px;
            font-size: 14px;
        }
        .btn {
            padding: 10px 20px;
            margin: 5px;
            font-size: 14px;
            cursor: pointer;
            border: none;
            border-radius: 4px;
            transition: all 0.3s;
        }
        .btn-primary {
            background: #2196F3;
            color: white;
        }
        .btn-primary:hover {
            background: #0b7dda;
        }
        .btn-success {
            background: #4CAF50;
            color: white;
        }
        .btn-success:hover {
            background: #45a049;
        }
        .btn-danger {
            background: #f44336;
            color: white;
        }
        .btn-danger:hover {
            background: #da190b;
        }
        .stats {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
            gap: 15px;
            margin-bottom: 20px;
        }
        .stat-card {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 20px;
            border-radius: 8px;
            text-align: center;
        }
        .stat-card.error {
            background: linear-gradient(135deg, #f093fb 0%, #f5576c 100%);
        }
        .stat-card.warning {
            background: linear-gradient(135deg, #ffecd2 0%, #fcb69f 100%);
            color: #333;
        }
        .stat-card.info {
            background: linear-gradient(135deg, #4facfe 0%, #00f2fe 100%);
        }
        .stat-card h3 {
            margin: 0 0 10px 0;
            font-size: 16px;
        }
        .stat-card .number {
            font-size: 32px;
            font-weight: bold;
        }
        .log-table {
            width: 100%;
            border-collapse: collapse;
            margin-top: 20px;
            font-size: 14px;
        }
        .log-table th {
            background: #2196F3;
            color: white;
            padding: 12px 8px;
            text-align: left;
            position: sticky;
            top: 0;
        }
        .log-table td {
            padding: 10px 8px;
            border-bottom: 1px solid #eee;
        }
        .log-table tr:hover {
            background: #f5f5f5;
        }
        .log-level {
            display: inline-block;
            padding: 4px 8px;
            border-radius: 3px;
            font-weight: bold;
            font-size: 12px;
        }
        .log-level.ERROR {
            background: #ffebee;
            color: #c62828;
        }
        .log-level.WARNING {
            background: #fff3e0;
            color: #ef6c00;
        }
        .log-level.INFO {
            background: #e3f2fd;
            color: #1565c0;
        }
        .expandable {
            cursor: pointer;
            color: #2196F3;
            text-decoration: underline;
        }
        .expandable:hover {
            color: #0b7dda;
        }
        .details {
            display: none;
            background: #f9f9f9;
            padding: 15px;
            margin-top: 10px;
            border-left: 4px solid #2196F3;
            border-radius: 4px;
        }
        .details pre {
            margin: 5px 0;
            white-space: pre-wrap;
            word-wrap: break-word;
            font-size: 12px;
        }
        .no-data {
            text-align: center;
            padding: 40px;
            color: #999;
            font-size: 18px;
        }
        .pagination {
            text-align: center;
            margin-top: 20px;
        }
    </style>
    <script>
        function toggleDetails(id) {
            var details = document.getElementById('details_' + id);
            if (details.style.display === 'none' || details.style.display === '') {
                details.style.display = 'block';
            } else {
                details.style.display = 'none';
            }
        }
    </script>
</head>
<body>
    <form id="form1" runat="server">
        <div class="container">
            <h1>Error Logs Viewer (Database)</h1>

            <!-- Statistics -->
            <div class="stats">
                <div class="stat-card error">
                    <h3>Errors</h3>
                    <div class="number"><asp:Label ID="lblErrorCount" runat="server" Text="0"></asp:Label></div>
                </div>
                <div class="stat-card warning">
                    <h3>Warnings</h3>
                    <div class="number"><asp:Label ID="lblWarningCount" runat="server" Text="0"></asp:Label></div>
                </div>
                <div class="stat-card info">
                    <h3>Info</h3>
                    <div class="number"><asp:Label ID="lblInfoCount" runat="server" Text="0"></asp:Label></div>
                </div>
                <div class="stat-card">
                    <h3>Total Logs</h3>
                    <div class="number"><asp:Label ID="lblTotalCount" runat="server" Text="0"></asp:Label></div>
                </div>
            </div>

            <!-- Filters -->
            <div class="filters">
                <div class="filter-group">
                    <label>Log Level:</label>
                    <asp:DropDownList ID="ddlLogLevel" runat="server">
                        <asp:ListItem Value="" Selected="True">All Levels</asp:ListItem>
                        <asp:ListItem Value="ERROR">ERROR</asp:ListItem>
                        <asp:ListItem Value="WARNING">WARNING</asp:ListItem>
                        <asp:ListItem Value="INFO">INFO</asp:ListItem>
                    </asp:DropDownList>
                </div>
                <div class="filter-group">
                    <label>Time Range:</label>
                    <asp:DropDownList ID="ddlTimeRange" runat="server">
                        <asp:ListItem Value="1" Selected="True">Last 1 Hour</asp:ListItem>
                        <asp:ListItem Value="6">Last 6 Hours</asp:ListItem>
                        <asp:ListItem Value="24">Last 24 Hours</asp:ListItem>
                        <asp:ListItem Value="168">Last 7 Days</asp:ListItem>
                        <asp:ListItem Value="720">Last 30 Days</asp:ListItem>
                        <asp:ListItem Value="0">All Time</asp:ListItem>
                    </asp:DropDownList>
                </div>
                <div class="filter-group">
                    <label>IP Address:</label>
                    <asp:TextBox ID="txtIPFilter" runat="server" placeholder="Filter by IP..."></asp:TextBox>
                </div>
                <div class="filter-group">
                    <label>Records:</label>
                    <asp:DropDownList ID="ddlRecordCount" runat="server">
                        <asp:ListItem Value="50" Selected="True">50</asp:ListItem>
                        <asp:ListItem Value="100">100</asp:ListItem>
                        <asp:ListItem Value="200">200</asp:ListItem>
                        <asp:ListItem Value="500">500</asp:ListItem>
                    </asp:DropDownList>
                </div>
            </div>

            <!-- Action Buttons -->
            <div style="margin-bottom: 20px;">
                <asp:Button ID="btnSearch" runat="server" Text="Search" CssClass="btn btn-primary" OnClick="btnSearch_Click" />
                <asp:Button ID="btnRefresh" runat="server" Text="Refresh" CssClass="btn btn-success" OnClick="btnRefresh_Click" />
                <asp:Button ID="btnExport" runat="server" Text="Export CSV" CssClass="btn btn-success" OnClick="btnExport_Click" />
                <asp:Button ID="btnDeleteOld" runat="server" Text="Delete Logs >30 Days" CssClass="btn btn-danger" OnClick="btnDeleteOld_Click" OnClientClick="return confirm('Are you sure you want to delete logs older than 30 days?');" />
            </div>

            <!-- Results -->
            <asp:Literal ID="litResults" runat="server"></asp:Literal>
        </div>
    </form>
</body>
</html>
