<%@ Page Title="Coupon Report Dashboard" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Report.aspx.cs" Inherits="Coupon.Admin.Report" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        /* Modern Dashboard Styling */
        .report-container {
            max-width: 1400px;
            margin: 0 auto;
            padding: 20px;
        }

        .user-header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 20px 30px;
            border-radius: 12px;
            margin-bottom: 30px;
            display: flex;
            justify-content: space-between;
            align-items: center;
            box-shadow: 0 4px 15px rgba(102, 126, 234, 0.3);
        }

        .user-info h4 {
            margin: 0 0 5px 0;
            font-size: 18px;
        }

        .user-info p {
            margin: 0;
            opacity: 0.9;
            font-size: 14px;
        }

        .nav-buttons a {
            color: white;
            text-decoration: none;
            padding: 10px 20px;
            background: rgba(255, 255, 255, 0.2);
            border-radius: 8px;
            margin-left: 10px;
            display: inline-block;
            transition: all 0.3s;
        }

        .nav-buttons a:hover {
            background: rgba(255, 255, 255, 0.3);
            transform: translateY(-2px);
        }

        /* Dashboard Cards */
        .dashboard-cards {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
            gap: 20px;
            margin-bottom: 30px;
        }

        .stat-card {
            background: white;
            border-radius: 12px;
            padding: 25px;
            box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
            border-left: 4px solid #667eea;
            transition: transform 0.3s, box-shadow 0.3s;
        }

        .stat-card:hover {
            transform: translateY(-5px);
            box-shadow: 0 5px 20px rgba(0, 0, 0, 0.15);
        }

        .stat-card.sent {
            border-left-color: #4caf50;
        }

        .stat-card.used {
            border-left-color: #ff9800;
        }

        .stat-card.expired {
            border-left-color: #f44336;
        }

        .stat-card h6 {
            color: #666;
            font-size: 14px;
            margin: 0 0 10px 0;
            text-transform: uppercase;
            letter-spacing: 0.5px;
        }

        .stat-card .value {
            font-size: 32px;
            font-weight: bold;
            color: #333;
            margin: 0;
        }

        .stat-card .label {
            font-size: 14px;
            color: #999;
            margin-top: 5px;
        }

        /* Search Panels */
        .search-panel {
            background: white;
            border-radius: 12px;
            padding: 30px;
            margin-bottom: 30px;
            box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
        }

        .search-panel h5 {
            color: #333;
            margin: 0 0 25px 0;
            padding-bottom: 15px;
            border-bottom: 2px solid #667eea;
            font-size: 18px;
        }

        .form-row {
            display: flex;
            flex-wrap: wrap;
            gap: 20px;
            margin-bottom: 20px;
        }

        .form-group {
            flex: 1;
            min-width: 280px;
            display: block;
        }

        .form-group label {
            display: block;
            font-weight: 600;
            color: #555;
            margin-bottom: 8px;
            font-size: 14px;
            text-align: left;
        }

        .form-group input[type="text"],
        .form-group input[type="date"],
        .form-group select,
        .form-group .form-control {
            display: block;
            width: 100%;
            padding: 10px 15px;
            border: 1px solid #ddd;
            border-radius: 8px;
            font-size: 14px;
            transition: border-color 0.3s;
            box-sizing: border-box;
        }

        .form-group input:focus,
        .form-group select:focus {
            outline: none;
            border-color: #667eea;
            box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
        }

        /* Buttons */
        .btn-custom {
            padding: 12px 30px;
            border: none;
            border-radius: 8px;
            font-size: 14px;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.3s;
            display: inline-flex;
            align-items: center;
            gap: 8px;
        }

        .btn-primary-custom {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
        }

        .btn-primary-custom:hover {
            transform: translateY(-2px);
            box-shadow: 0 5px 15px rgba(102, 126, 234, 0.4);
        }

        .btn-secondary-custom {
            background: #6c757d;
            color: white;
        }

        .btn-success-custom {
            background: #28a745;
            color: white;
        }

        .btn-actions {
            display: flex;
            gap: 10px;
            justify-content: flex-end;
            margin-top: 20px;
        }

        /* GridView Styling */
        .gridview-container {
            background: white;
            border-radius: 12px;
            padding: 30px;
            box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1);
            overflow-x: auto;
        }

        .gridview-container h5 {
            margin: 0 0 20px 0;
            color: #333;
        }

        .grid-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 20px;
        }

        .grid-info {
            color: #666;
            font-size: 14px;
        }

        /* GridView Enhanced Styling */
        .table-enhanced {
            width: 100%;
            border-collapse: collapse;
            font-size: 14px;
        }

        .table-enhanced thead {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
        }

        .table-enhanced th {
            padding: 15px;
            text-align: left;
            font-weight: 600;
            border: none;
        }

        .table-enhanced td {
            padding: 12px 15px;
            border-bottom: 1px solid #f0f0f0;
        }

        .table-enhanced tbody tr:hover {
            background-color: #f8f9fa;
        }

        .table-enhanced tbody tr:nth-child(even) {
            background-color: #fafafa;
        }

        /* Responsive */
        @media (max-width: 768px) {
            .user-header {
                flex-direction: column;
                gap: 15px;
            }

            .nav-buttons {
                display: flex;
                flex-direction: column;
                width: 100%;
            }

            .nav-buttons a {
                margin: 5px 0;
                text-align: center;
            }

            .form-row {
                grid-template-columns: 1fr;
            }

            .btn-actions {
                flex-direction: column;
            }

            .btn-custom {
                width: 100%;
                justify-content: center;
            }
        }

        /* Login Form */
        .login-container {
            max-width: 450px;
            margin: 50px auto;
            background: white;
            padding: 40px;
            border-radius: 12px;
            box-shadow: 0 5px 20px rgba(0, 0, 0, 0.1);
        }

        .login-container h3 {
            text-align: center;
            color: #333;
            margin-bottom: 30px;
        }

        .login-form-group {
            margin-bottom: 20px;
        }

        .login-form-group input {
            width: 100%;
            padding: 12px 15px;
            border: 1px solid #ddd;
            border-radius: 8px;
            font-size: 14px;
        }

        .login-btn {
            width: 100%;
            padding: 12px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            border: none;
            border-radius: 8px;
            font-size: 16px;
            font-weight: 600;
            cursor: pointer;
        }

        .date-presets {
            display: flex;
            gap: 10px;
            margin-top: 10px;
            flex-wrap: wrap;
        }

        .date-preset-btn {
            padding: 5px 12px;
            background: #f0f0f0;
            border: 1px solid #ddd;
            border-radius: 5px;
            font-size: 12px;
            cursor: pointer;
            transition: all 0.2s;
        }

        .date-preset-btn:hover {
            background: #667eea;
            color: white;
            border-color: #667eea;
        }
    </style>

    <div class="report-container">
        <!-- Dashboard Content (Authentication checked in code-behind) -->
        <!-- User Header -->
        <div class="user-header">
            <div class="user-info">
                <h4><asp:Label ID="lblCurrentUser" runat="server" /></h4>
                <p><asp:Label ID="lblCurrentEmail" runat="server" /></p>
            </div>
            <div class="nav-buttons">
                <a href="<%= ResolveUrl("~/Admin/ImportCoupons.aspx") %>">Import Coupons</a>
                <a href="<%= ResolveUrl("~/Admin/Settings.aspx") %>">Settings</a>
                <a href="<%= ResolveUrl("~/Admin/Logout.aspx") %>">Logout</a>
            </div>
        </div>

            <!-- Dashboard Statistics Cards -->
            <div class="dashboard-cards">
                <div class="stat-card">
                    <h6>Total Coupons</h6>
                    <p class="value"><asp:Label ID="lblTotalCoupons" runat="server" Text="0" /></p>
                    <p class="label">All registered coupons</p>
                </div>
                <div class="stat-card sent">
                    <h6>Coupons Sent</h6>
                    <p class="value"><asp:Label ID="lblCouponsSent" runat="server" Text="0" /></p>
                    <p class="label"><asp:Label ID="lblSentPercentage" runat="server" /></p>
                </div>
                <div class="stat-card used">
                    <h6>Coupons Used</h6>
                    <p class="value"><asp:Label ID="lblCouponsUsed" runat="server" Text="0" /></p>
                    <p class="label"><asp:Label ID="lblUsedPercentage" runat="server" /></p>
                </div>
                <div class="stat-card expired">
                    <h6>Expired Coupons</h6>
                    <p class="value"><asp:Label ID="lblExpiredCoupons" runat="server" Text="0" /></p>
                    <p class="label"><asp:Label ID="lblExpiredPercentage" runat="server" /></p>
                </div>
            </div>

            <!-- Coupon Search Panel -->
            <div class="search-panel">
                <h5>Search Coupons</h5>

                <div class="form-row">
                    <div class="form-group">
                        <label>Coupon ID</label>
                        <asp:TextBox ID="txtCouponId" runat="server" placeholder="Enter coupon ID" />
                    </div>
                    <div class="form-group">
                        <label>Coupon Code</label>
                        <asp:TextBox ID="txtCouponCode" runat="server" placeholder="Enter coupon code" />
                    </div>
                </div>

                <div class="form-row">
                    <div class="form-group">
                        <label>Register Date From</label>
                        <asp:TextBox ID="txtRegisterFrom" runat="server" TextMode="Date" />
                    </div>
                    <div class="form-group">
                        <label>Register Date To</label>
                        <asp:TextBox ID="txtRegisterTo" runat="server" TextMode="Date" />
                    </div>
                </div>

                <div class="form-row">
                    <div class="form-group">
                        <label>Send Date From</label>
                        <asp:TextBox ID="txtSendFrom" runat="server" TextMode="Date" />
                    </div>
                    <div class="form-group">
                        <label>Send Date To</label>
                        <asp:TextBox ID="txtSendTo" runat="server" TextMode="Date" />
                    </div>
                </div>

                <div class="form-row">
                    <div class="form-group">
                        <label>Used Date From</label>
                        <asp:TextBox ID="txtUsedFrom" runat="server" TextMode="Date" />
                    </div>
                    <div class="form-group">
                        <label>Used Date To</label>
                        <asp:TextBox ID="txtUsedTo" runat="server" TextMode="Date" />
                    </div>
                </div>

                <div class="form-row">
                    <div class="form-group">
                        <label>Send Status</label>
                        <asp:DropDownList ID="ddlSendStatus" runat="server" CssClass="form-control">
                            <asp:ListItem Value="">All</asp:ListItem>
                            <asp:ListItem Value="True">Sent</asp:ListItem>
                            <asp:ListItem Value="False">Not Sent</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                    <div class="form-group">
                        <label>Used Status</label>
                        <asp:DropDownList ID="ddlUsedStatus" runat="server" CssClass="form-control">
                            <asp:ListItem Value="">All</asp:ListItem>
                            <asp:ListItem Value="True">Used</asp:ListItem>
                            <asp:ListItem Value="False">Not Used</asp:ListItem>
                        </asp:DropDownList>
                    </div>
                </div>

                <div class="btn-actions">
                    <asp:Button ID="btnClearCouponSearch" runat="server" Text="Clear" CssClass="btn-custom btn-secondary-custom" OnClick="btnClearCouponSearch_Click" />
                    <asp:Button ID="btnSearchCoupons" runat="server" Text="Search Coupons" CssClass="btn-custom btn-primary-custom" OnClick="btnSearchCoupons_Click" />
                    <asp:Button ID="btnExportCoupons" runat="server" Text="Export to Excel" CssClass="btn-custom btn-success-custom" OnClick="btnExportCoupons_Click" />
                </div>
            </div>

            <!-- Access Logs Search Panel -->
            <div class="search-panel">
                <h5>Search Access Logs</h5>

                <div class="form-row">
                    <div class="form-group">
                        <label>Log Date From</label>
                        <asp:TextBox ID="txtLogFrom" runat="server" TextMode="Date" />
                    </div>
                    <div class="form-group">
                        <label>Log Date To</label>
                        <asp:TextBox ID="txtLogTo" runat="server" TextMode="Date" />
                    </div>
                </div>

                <div class="form-row">
                    <div class="form-group">
                        <label>Coupon ID / URL</label>
                        <asp:TextBox ID="txtLogCouponId" runat="server" placeholder="Enter coupon ID or URL" />
                    </div>
                    <div class="form-group">
                        <label>Filter Options</label>
                        <div style="display: flex; gap: 20px; margin-top: 10px;">
                            <label style="display: flex; align-items: center; gap: 5px;">
                                <asp:CheckBox ID="chkFoundCoupon" runat="server" Checked="true" />
                                Found Coupon
                            </label>
                            <label style="display: flex; align-items: center; gap: 5px;">
                                <asp:CheckBox ID="chkRedeemAction" runat="server" />
                                Redeem Action
                            </label>
                        </div>
                    </div>
                </div>

                <div class="btn-actions">
                    <asp:Button ID="btnClearLogSearch" runat="server" Text="Clear" CssClass="btn-custom btn-secondary-custom" OnClick="btnClearLogSearch_Click" />
                    <asp:Button ID="btnSearchLogs" runat="server" Text="Search Logs" CssClass="btn-custom btn-primary-custom" OnClick="btnSearchLogs_Click" />
                    <asp:Button ID="btnExportLogs" runat="server" Text="Export to Excel" CssClass="btn-custom btn-success-custom" OnClick="btnExportLogs_Click" />
                </div>
            </div>

            <!-- Results Grid -->
            <asp:Panel ID="pnlResults" runat="server" Visible="false">
                <div class="gridview-container">
                    <div class="grid-header">
                        <h5>Search Results</h5>
                        <div class="grid-info">
                            <asp:Label ID="lblResultCount" runat="server" />
                        </div>
                    </div>
                    <asp:GridView ID="gvResults" runat="server"
                        CssClass="table-enhanced"
                        AutoGenerateColumns="true"
                        AllowPaging="true"
                        AllowSorting="true"
                        PageSize="50"
                        OnPageIndexChanging="gvResults_PageIndexChanging"
                        OnSorting="gvResults_Sorting"
                        EmptyDataText="No records found">
                        <PagerStyle CssClass="pagination" HorizontalAlign="Center" />
                        <PagerSettings Mode="NumericFirstLast" PageButtonCount="10" FirstPageText="First" LastPageText="Last" />
                    </asp:GridView>
                </div>
            </asp:Panel>
    </div>
</asp:Content>
