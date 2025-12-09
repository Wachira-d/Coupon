<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="TestDatabase.aspx.cs" Inherits="Coupon.TestDatabase" %>

<!DOCTYPE html>
<html>
<head>
    <title>Database Connection Test</title>
    <style>
        body { font-family: 'Consolas', monospace; padding: 20px; background: #1e1e1e; color: #d4d4d4; }
        .success { color: #4ec9b0; }
        .error { color: #f48771; }
        .info { color: #4fc1ff; }
        pre { background: #252526; padding: 10px; border-radius: 5px; overflow-x: auto; }
        h2 { color: #569cd6; }
    </style>
</head>
<body>
    <h1>🔍 Database Connection Test</h1>
    <form id="form1" runat="server">
        <asp:Button ID="btnTest" runat="server" Text="Test Connection" OnClick="btnTest_Click" />
        <hr />
        <asp:Literal ID="litResult" runat="server"></asp:Literal>
    </form>
</body>
</html>
