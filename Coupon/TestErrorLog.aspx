<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TestErrorLog.aspx.cs" Inherits="Coupon.TestErrorLog" %>

<!DOCTYPE html>
<html>
<head runat="server">
    <title>Test Error Logger</title>
    <style>
        body { font-family: Arial, sans-serif; margin: 40px; }
        .section { margin: 20px 0; padding: 20px; border: 1px solid #ccc; background: #f9f9f9; }
        .btn { padding: 10px 20px; margin: 10px; font-size: 16px; cursor: pointer; }
        .result { margin-top: 20px; padding: 15px; background: #e8f4f8; border-left: 4px solid #2196F3; }
        .error { background: #ffebee; border-left: 4px solid #f44336; }
        .success { background: #e8f5e9; border-left: 4px solid #4CAF50; }
        pre { background: #fff; padding: 10px; border: 1px solid #ddd; overflow-x: auto; }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <h1>Test Error Logger Configuration</h1>

        <div class="section">
            <h2>1. Check LogPath Configuration</h2>
            <asp:Button ID="btnCheckConfig" runat="server" Text="Check Config" CssClass="btn" OnClick="btnCheckConfig_Click" />
            <asp:Literal ID="litConfig" runat="server"></asp:Literal>
        </div>

        <div class="section">
            <h2>2. Test Log Write</h2>
            <asp:Button ID="btnTestLog" runat="server" Text="Test Write Log" CssClass="btn" OnClick="btnTestLog_Click" />
            <asp:Literal ID="litLogTest" runat="server"></asp:Literal>
        </div>

        <div class="section">
            <h2>3. Test Permission</h2>
            <asp:Button ID="btnTestPermission" runat="server" Text="Test File Write Permission" CssClass="btn" OnClick="btnTestPermission_Click" />
            <asp:Literal ID="litPermission" runat="server"></asp:Literal>
        </div>

        <div class="section">
            <h2>4. Read Log File</h2>
            <asp:Button ID="btnReadLog" runat="server" Text="Read Last 50 Lines" CssClass="btn" OnClick="btnReadLog_Click" />
            <asp:Literal ID="litLogContent" runat="server"></asp:Literal>
        </div>
    </form>
</body>
</html>
