<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Login.aspx.cs" Inherits="Coupon.Admin.Login" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1.0" />
    <title>Login - Honda CX Coupon System</title>
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
            justify-content: center;
            align-items: center;
            padding: 20px;
        }

        .login-container {
            background: white;
            border-radius: 20px;
            box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
            overflow: hidden;
            max-width: 450px;
            width: 100%;
            animation: slideIn 0.5s ease-out;
        }

        @keyframes slideIn {
            from {
                opacity: 0;
                transform: translateY(-30px);
            }
            to {
                opacity: 1;
                transform: translateY(0);
            }
        }

        .login-header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            padding: 40px 30px;
            text-align: center;
        }

        .login-header h1 {
            font-size: 28px;
            font-weight: 600;
            margin-bottom: 10px;
        }

        .login-header p {
            font-size: 14px;
            opacity: 0.9;
        }

        .login-body {
            padding: 40px 30px;
        }

        .form-group {
            margin-bottom: 25px;
        }

        .form-group label {
            display: block;
            font-weight: 500;
            margin-bottom: 8px;
            color: #333;
            font-size: 14px;
        }

        .form-group input {
            width: 100%;
            padding: 12px 15px;
            border: 2px solid #e0e0e0;
            border-radius: 8px;
            font-size: 15px;
            transition: all 0.3s;
        }

        .form-group input:focus {
            outline: none;
            border-color: #667eea;
            box-shadow: 0 0 0 3px rgba(102, 126, 234, 0.1);
        }

        .form-group input:disabled {
            background-color: #f5f5f5;
            cursor: not-allowed;
        }

        .btn-login {
            width: 100%;
            padding: 14px;
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white;
            border: none;
            border-radius: 8px;
            font-size: 16px;
            font-weight: 600;
            cursor: pointer;
            transition: all 0.3s;
            margin-top: 10px;
        }

        .btn-login:hover:not(:disabled) {
            transform: translateY(-2px);
            box-shadow: 0 5px 20px rgba(102, 126, 234, 0.4);
        }

        .btn-login:disabled {
            opacity: 0.6;
            cursor: not-allowed;
            transform: none;
        }

        .alert {
            padding: 12px 15px;
            border-radius: 8px;
            margin-bottom: 20px;
            font-size: 14px;
            display: none;
        }

        .alert.show {
            display: block;
            animation: fadeIn 0.3s ease-in;
        }

        @keyframes fadeIn {
            from { opacity: 0; }
            to { opacity: 1; }
        }

        .alert-error {
            background-color: #fee;
            border: 1px solid #fcc;
            color: #c33;
        }

        .alert-success {
            background-color: #efe;
            border: 1px solid #cfc;
            color: #3c3;
        }

        .alert-info {
            background-color: #eef;
            border: 1px solid #ccf;
            color: #33c;
        }

        .remember-me {
            display: flex;
            align-items: center;
            margin-bottom: 20px;
        }

        .remember-me input[type="checkbox"] {
            margin-right: 8px;
            width: auto;
        }

        .remember-me label {
            margin-bottom: 0;
            font-weight: normal;
            cursor: pointer;
            user-select: none;
        }

        .login-footer {
            text-align: center;
            padding: 20px 30px;
            background-color: #f8f9fa;
            font-size: 13px;
            color: #666;
        }

        .login-footer a {
            color: #667eea;
            text-decoration: none;
        }

        .login-footer a:hover {
            text-decoration: underline;
        }

        .icon {
            display: inline-block;
            margin-right: 8px;
        }

        .loading {
            display: inline-block;
            width: 16px;
            height: 16px;
            border: 2px solid rgba(255,255,255,.3);
            border-radius: 50%;
            border-top-color: white;
            animation: spin 1s linear infinite;
            margin-right: 8px;
        }

        @keyframes spin {
            to { transform: rotate(360deg); }
        }

        @media (max-width: 480px) {
            .login-container {
                margin: 10px;
            }

            .login-header h1 {
                font-size: 24px;
            }

            .login-body {
                padding: 30px 20px;
            }
        }
    </style>
</head>
<body>
    <form id="form1" runat="server">
        <div class="login-container">
            <div class="login-header">
                <h1>Honda CX Coupon</h1>
                <p>Admin Login Portal</p>
            </div>

            <div class="login-body">
                <asp:Panel ID="panelAlert" runat="server" CssClass="alert" Visible="false">
                    <asp:Literal ID="litMessage" runat="server"></asp:Literal>
                </asp:Panel>

                <div class="form-group">
                    <label for="txtUsername">Username</label>
                    <asp:TextBox ID="txtUsername" runat="server"
                                 placeholder="Enter your username"
                                 autocomplete="username"
                                 CssClass="form-control">
                    </asp:TextBox>
                </div>

                <div class="form-group">
                    <label for="txtPassword">Password</label>
                    <asp:TextBox ID="txtPassword" runat="server"
                                 TextMode="Password"
                                 placeholder="Enter your password"
                                 autocomplete="current-password"
                                 CssClass="form-control">
                    </asp:TextBox>
                </div>

                <div class="remember-me">
                    <asp:CheckBox ID="chkRememberMe" runat="server" />
                    <label for="<%= chkRememberMe.ClientID %>">Remember me for 30 days</label>
                </div>

                <asp:Button ID="btnLogin" runat="server"
                           Text="Login"
                           CssClass="btn-login"
                           OnClick="btnLogin_Click"
                           OnClientClick="return showLoading();" />

                <!-- Loading overlay -->
                <div id="loadingOverlay" style="display:none; position:fixed; top:0; left:0; width:100%; height:100%; background:rgba(0,0,0,0.5); z-index:9999; justify-content:center; align-items:center;">
                    <div style="background:white; padding:30px 50px; border-radius:10px; text-align:center; box-shadow:0 5px 30px rgba(0,0,0,0.3);">
                        <div class="loading" style="width:40px; height:40px; border-width:4px; margin:0 auto 15px;"></div>
                        <div style="font-size:16px; color:#333;">Authenticating...</div>
                        <div style="font-size:13px; color:#666; margin-top:5px;">Please wait</div>
                    </div>
                </div>
            </div>

            <div class="login-footer">
                <p>
                    Powered by AuthenAPI • Hybrid Authentication<br />
                    <small>© <%= DateTime.Now.Year %> Honda CX Coupon System</small>
                </p>
            </div>
        </div>
    </form>

    <script type="text/javascript">
        // Auto-focus username field on page load
        window.onload = function () {
            var usernameField = document.getElementById('<%= txtUsername.ClientID %>');
            if (usernameField) {
                usernameField.focus();
            }
        };

        // Show loading overlay when login button is clicked
        function showLoading() {
            var username = document.getElementById('<%= txtUsername.ClientID %>').value;
            var password = document.getElementById('<%= txtPassword.ClientID %>').value;

            // Validate before showing loading
            if (!username || !password) {
                return true; // Let server-side validation handle it
            }

            // Show loading overlay
            var overlay = document.getElementById('loadingOverlay');
            overlay.style.display = 'flex';

            // Disable login button
            var btn = document.getElementById('<%= btnLogin.ClientID %>');
            btn.disabled = true;
            btn.value = 'Logging in...';

            return true; // Allow form submission
        }
    </script>
</body>
</html>
