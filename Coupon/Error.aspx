<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Error.aspx.cs" Inherits="Coupon.Error" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Error - Coupon System</title>
    <style>
        * {
            margin: 0;
            padding: 0;
            box-sizing: border-box;
        }

        body {
            font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            min-height: 100vh;
            display: flex;
            align-items: center;
            justify-content: center;
            padding: 20px;
        }

        .error-container {
            background: white;
            border-radius: 20px;
            box-shadow: 0 25px 50px -12px rgba(0, 0, 0, 0.25);
            padding: 60px 40px;
            text-align: center;
            max-width: 500px;
            width: 100%;
        }

        .error-icon {
            width: 120px;
            height: 120px;
            margin: 0 auto 30px;
            background: linear-gradient(135deg, #ff6b6b 0%, #ee5a5a 100%);
            border-radius: 50%;
            display: flex;
            align-items: center;
            justify-content: center;
            animation: pulse 2s ease-in-out infinite;
        }

        @keyframes pulse {
            0%, 100% { transform: scale(1); }
            50% { transform: scale(1.05); }
        }

        .error-icon svg {
            width: 60px;
            height: 60px;
            fill: white;
        }

        .error-title {
            font-size: 28px;
            font-weight: 700;
            color: #2d3748;
            margin-bottom: 15px;
        }

        .error-message {
            font-size: 16px;
            color: #718096;
            margin-bottom: 30px;
            line-height: 1.6;
        }

        .error-code {
            display: inline-block;
            background: #f7fafc;
            border: 1px solid #e2e8f0;
            border-radius: 8px;
            padding: 10px 20px;
            font-family: 'Courier New', monospace;
            font-size: 14px;
            color: #4a5568;
            margin-bottom: 30px;
        }

        .btn-container {
            display: flex;
            gap: 15px;
            justify-content: center;
            flex-wrap: wrap;
        }

        .btn {
            display: inline-block;
            padding: 14px 28px;
            border-radius: 10px;
            font-size: 16px;
            font-weight: 600;
            text-decoration: none;
            transition: all 0.3s ease;
            cursor: pointer;
            border: none;
        }

        .btn-primary {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
        }

        .btn-primary:hover {
            transform: translateY(-2px);
            box-shadow: 0 10px 20px rgba(102, 126, 234, 0.4);
        }

        .btn-secondary {
            background: white;
            color: #667eea;
            border: 2px solid #667eea;
        }

        .btn-secondary:hover {
            background: #667eea;
            color: white;
        }

        .debug-info {
            margin-top: 30px;
            padding: 20px;
            background: #fef3cd;
            border: 1px solid #ffc107;
            border-radius: 10px;
            text-align: left;
            font-size: 12px;
            font-family: 'Courier New', monospace;
            color: #856404;
            display: none;
        }

        .debug-info.show {
            display: block;
        }

        .debug-info h4 {
            margin-bottom: 10px;
            font-size: 14px;
        }

        .debug-info pre {
            white-space: pre-wrap;
            word-wrap: break-word;
            max-height: 200px;
            overflow-y: auto;
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="error-container">
            <div class="error-icon">
                <svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24">
                    <path d="M12 2C6.48 2 2 6.48 2 12s4.48 10 10 10 10-4.48 10-10S17.52 2 12 2zm1 15h-2v-2h2v2zm0-4h-2V7h2v6z"/>
                </svg>
            </div>

            <h1 class="error-title">
                <asp:Literal ID="litErrorTitle" runat="server" Text="Oops! Something went wrong"></asp:Literal>
            </h1>

            <p class="error-message">
                <asp:Literal ID="litErrorMessage" runat="server"
                    Text="We apologize for the inconvenience. The page you requested encountered an error. Please try again or contact support if the problem persists."></asp:Literal>
            </p>

            <div class="error-code" runat="server" id="divErrorCode" visible="false">
                Error Code: <asp:Literal ID="litErrorCode" runat="server"></asp:Literal>
            </div>

            <div class="btn-container">
                <a href="javascript:history.back()" class="btn btn-secondary">Go Back</a>
                <asp:HyperLink ID="lnkHome" runat="server" NavigateUrl="~/Central.aspx" CssClass="btn btn-primary">Home Page</asp:HyperLink>
            </div>

            <div class="debug-info" runat="server" id="divDebugInfo" visible="false">
                <h4>Debug Information (Development Only):</h4>
                <pre><asp:Literal ID="litDebugInfo" runat="server"></asp:Literal></pre>
            </div>
        </div>
    </form>
</body>
</html>
