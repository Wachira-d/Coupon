<%@ Page Title="System Settings" Language="C#" MasterPageFile="~/Site.Master" AutoEventWireup="true" CodeBehind="Settings.aspx.cs" Inherits="Coupon.Admin.Settings" %>

<asp:Content ID="Content1" ContentPlaceHolderID="MainContent" runat="server">
    <style>
        .settings-container { max-width: 1100px; margin: 0 auto; padding: 20px; }

        .user-header {
            background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
            color: white; padding: 20px 30px; border-radius: 12px; margin-bottom: 30px;
            display: flex; justify-content: space-between; align-items: center; flex-wrap: wrap; gap: 10px;
            box-shadow: 0 4px 15px rgba(102, 126, 234, 0.3);
        }
        .user-header h4 { margin: 0 0 5px 0; font-size: 18px; }
        .user-header p { margin: 0; opacity: 0.9; font-size: 14px; }
        .nav-buttons a {
            color: white; text-decoration: none; padding: 10px 20px; background: rgba(255, 255, 255, 0.2);
            border-radius: 8px; margin-left: 10px; display: inline-block;
        }
        .nav-buttons a:hover { background: rgba(255, 255, 255, 0.3); }
        .nav-buttons a.active { background: rgba(255, 255, 255, 0.4); font-weight: bold; }

        .panel { background: white; border-radius: 12px; padding: 30px; margin-bottom: 30px; box-shadow: 0 2px 10px rgba(0, 0, 0, 0.1); }
        .panel h5 { color: #333; margin: 0 0 20px 0; padding-bottom: 12px; border-bottom: 2px solid #667eea; font-size: 18px; }
        .panel .hint { color: #777; font-size: 13px; margin: -10px 0 20px 0; }

        .setting-row { display: grid; grid-template-columns: 280px 1fr; gap: 15px; align-items: start; padding: 12px 0; border-bottom: 1px solid #f0f0f0; }
        .setting-row:last-child { border-bottom: none; }
        .setting-row label { font-weight: 600; color: #444; margin: 0; padding-top: 8px; display: block; }
        .setting-row .desc { font-size: 12px; color: #888; margin-top: 4px; }
        .setting-row input[type=text], .setting-row input[type=password] {
            width: 100%; max-width: 480px; padding: 8px 12px; border: 1px solid #ccc; border-radius: 6px; font-size: 14px;
        }
        .setting-row input.short { max-width: 140px; }
        .source { display: inline-block; font-size: 11px; padding: 2px 8px; border-radius: 10px; margin-left: 6px; background: #eee; color: #555; font-weight: normal; }
        .source.Database { background: #e3f2fd; color: #1565c0; }
        .source.Web\.config { background: #fff3e0; color: #e65100; }

        .btn-primary-custom, .btn-secondary-custom, .btn-danger-link {
            padding: 10px 22px; border-radius: 8px; border: none; cursor: pointer; font-size: 14px; margin-right: 8px;
        }
        .btn-primary-custom { background: #667eea; color: white; }
        .btn-primary-custom:hover { background: #5a6fd6; }
        .btn-secondary-custom { background: white; color: #667eea; border: 1px solid #667eea; }
        .actions { margin-top: 20px; }

        .alert-box { padding: 12px 18px; border-radius: 8px; margin-bottom: 20px; font-size: 14px; }
        .alert-box.success { background: #e8f5e9; color: #2e7d32; border: 1px solid #c8e6c9; }
        .alert-box.error { background: #ffebee; color: #c62828; border: 1px solid #ffcdd2; }
        .alert-box.warning { background: #fff8e1; color: #8d6e00; border: 1px solid #ffe082; }
        .alert-box pre { white-space: pre-wrap; word-break: break-all; margin: 8px 0 0 0; font-size: 12px; background: rgba(0,0,0,0.04); padding: 8px; border-radius: 4px; }

        .admin-grid { width: 100%; border-collapse: collapse; font-size: 14px; }
        .admin-grid th { background: #f5f6fa; color: #555; text-align: left; padding: 10px; border-bottom: 2px solid #e0e0e0; }
        .admin-grid td { padding: 10px; border-bottom: 1px solid #f0f0f0; vertical-align: middle; }
        .status-active { color: #2e7d32; font-weight: 600; }
        .status-disabled { color: #999; }
        .admin-grid a { margin-right: 12px; color: #667eea; text-decoration: none; }
        .admin-grid a.danger { color: #c62828; }

        .add-admin { display: grid; grid-template-columns: 2fr 2fr 2fr auto; gap: 10px; margin-top: 20px; align-items: center; }
        .add-admin input[type=text] { width: 100%; padding: 8px 12px; border: 1px solid #ccc; border-radius: 6px; font-size: 14px; }

        @media (max-width: 768px) {
            .setting-row { grid-template-columns: 1fr; }
            .add-admin { grid-template-columns: 1fr; }
            .nav-buttons a { margin: 5px 5px 0 0; }
        }
    </style>

    <div class="settings-container">
        <div class="user-header">
            <div>
                <h4><asp:Label ID="lblCurrentUser" runat="server" /></h4>
                <p><asp:Label ID="lblCurrentEmail" runat="server" /></p>
            </div>
            <div class="nav-buttons">
                <a href="<%= ResolveUrl("~/Admin/Report.aspx") %>">Report</a>
                <a href="<%= ResolveUrl("~/Admin/ImportCoupons.aspx") %>">Import Coupons</a>
                <a class="active" href="<%= ResolveUrl("~/Admin/Settings.aspx") %>">Settings</a>
                <a href="<%= ResolveUrl("~/Admin/Logout.aspx") %>">Logout</a>
            </div>
        </div>

        <asp:Literal ID="litWarnings" runat="server" />
        <asp:Literal ID="litMessage" runat="server" />

        <!-- ===================== Authentication API ===================== -->
        <div class="panel">
            <h5>Authentication API</h5>
            <p class="hint">ค่าที่บันทึกที่นี่จะใช้แทนค่าใน Web.config ทันที (ไม่ต้อง restart) เว้นช่องว่างเพื่อกลับไปใช้ค่าจาก Web.config</p>

            <div class="setting-row">
                <div>
                    <label for="<%= txtApiBaseUrl.ClientID %>">API Base URL <span class="source <%= SourceOf("AuthenApiBaseUrl") %>"><%= SourceOf("AuthenApiBaseUrl") %></span></label>
                    <div class="desc">เช่น https://authen.hondacx.me/api</div>
                </div>
                <div><asp:TextBox ID="txtApiBaseUrl" runat="server" MaxLength="300" /></div>
            </div>

            <div class="setting-row">
                <div>
                    <label for="<%= txtApiKey.ClientID %>">API Key (X-API-Key) <span class="source <%= SourceOf("AuthenApiKey") %>"><%= SourceOf("AuthenApiKey") %></span></label>
                    <div class="desc">ไม่แสดงค่าเดิมเพื่อความปลอดภัย พิมพ์เฉพาะเมื่อต้องการเปลี่ยน</div>
                </div>
                <div>
                    <asp:TextBox ID="txtApiKey" runat="server" TextMode="Password" MaxLength="500" autocomplete="new-password" />
                    <div class="desc"><asp:Literal ID="litApiKeyStatus" runat="server" /></div>
                    <div class="desc"><asp:CheckBox ID="chkClearApiKey" runat="server" Text=" ลบ API Key ที่บันทึกบนเว็บ (กลับไปใช้ค่าจาก Web.config)" /></div>
                </div>
            </div>

            <div class="setting-row">
                <div>
                    <label for="<%= txtApiTimeout.ClientID %>">API Timeout (seconds) <span class="source <%= SourceOf("AuthenApiTimeout") %>"><%= SourceOf("AuthenApiTimeout") %></span></label>
                    <div class="desc">5 - 120 วินาที</div>
                </div>
                <div><asp:TextBox ID="txtApiTimeout" runat="server" CssClass="short" MaxLength="3" /></div>
            </div>

            <div class="actions">
                <asp:Button ID="btnTestApi" runat="server" Text="Test API Connection" CssClass="btn-secondary-custom" OnClick="btnTestApi_Click" CausesValidation="false" />
                <span class="desc">ทดสอบด้วยค่าที่บันทึกแล้ว (กด Save ก่อนถ้าเพิ่งแก้)</span>
            </div>
            <div style="margin-top:15px;"><asp:Literal ID="litApiTest" runat="server" /></div>
        </div>

        <!-- ===================== Login & Security ===================== -->
        <div class="panel">
            <h5>Login &amp; Security</h5>

            <div class="setting-row">
                <div>
                    <label>Allow only listed admin accounts <span class="source <%= SourceOf("RestrictAdminAccess") %>"><%= SourceOf("RestrictAdminAccess") %></span></label>
                    <div class="desc">ถ้าปิด ทุกคนที่มีบัญชี AD จะเข้าหน้า Admin ได้</div>
                </div>
                <div style="padding-top:8px;"><asp:CheckBox ID="chkRestrictAdmin" runat="server" Text=" เปิดใช้งาน (แนะนำ)" /></div>
            </div>

            <div class="setting-row">
                <div>
                    <label for="<%= txtSessionHours.ClientID %>">Session timeout (hours) <span class="source <%= SourceOf("SessionTimeoutHours") %>"><%= SourceOf("SessionTimeoutHours") %></span></label>
                    <div class="desc">1 - 24 ชั่วโมง หลังจากนั้นต้อง login ใหม่</div>
                </div>
                <div><asp:TextBox ID="txtSessionHours" runat="server" CssClass="short" MaxLength="2" /></div>
            </div>

            <div class="setting-row">
                <div>
                    <label for="<%= txtLockoutMinutes.ClientID %>">Lockout duration (minutes) <span class="source <%= SourceOf("LockoutDurationMinutes") %>"><%= SourceOf("LockoutDurationMinutes") %></span></label>
                    <div class="desc">ระยะเวลาล็อคบัญชีหลังใส่รหัสผิดหลายครั้ง (1 - 1440)</div>
                </div>
                <div><asp:TextBox ID="txtLockoutMinutes" runat="server" CssClass="short" MaxLength="4" /></div>
            </div>

            <div class="setting-row">
                <div>
                    <label for="<%= txtCacheDays.ClientID %>">Login cache duration (days) <span class="source <%= SourceOf("CacheDurationDays") %>"><%= SourceOf("CacheDurationDays") %></span></label>
                    <div class="desc">ระยะเวลาที่ login ได้จาก cache เมื่อ Authen API ล่ม (1 - 365)</div>
                </div>
                <div><asp:TextBox ID="txtCacheDays" runat="server" CssClass="short" MaxLength="3" /></div>
            </div>
        </div>

        <!-- ===================== Email Relay API ===================== -->
        <div class="panel">
            <h5>Email Relay API</h5>
            <p class="hint">ส่งอีเมลผ่าน EmailRelayAPI (ดูค่า API Key / Sender profile ได้จาก console ของ relay ที่ /admin/)</p>

            <div class="setting-row">
                <div>
                    <label>Enable Email Relay <span class="source <%= SourceOf("EmailRelayEnabled") %>"><%= SourceOf("EmailRelayEnabled") %></span></label>
                    <div class="desc">ปิดไว้ = ระบบจะไม่ส่งอีเมลใดๆ</div>
                </div>
                <div style="padding-top:8px;"><asp:CheckBox ID="chkRelayEnabled" runat="server" Text=" เปิดใช้งาน" /></div>
            </div>

            <div class="setting-row">
                <div>
                    <label for="<%= txtRelayUrl.ClientID %>">Relay Base URL <span class="source <%= SourceOf("EmailRelayBaseUrl") %>"><%= SourceOf("EmailRelayBaseUrl") %></span></label>
                    <div class="desc">ไม่ต้องใส่ /api/v1 เช่น https://relay.hondacx.me</div>
                </div>
                <div><asp:TextBox ID="txtRelayUrl" runat="server" MaxLength="300" /></div>
            </div>

            <div class="setting-row">
                <div>
                    <label for="<%= txtRelayKey.ClientID %>">API Key (X-API-Key) <span class="source <%= SourceOf("EmailRelayApiKey") %>"><%= SourceOf("EmailRelayApiKey") %></span></label>
                    <div class="desc">key ของ client (erk_...) ต้องมี scope <code>send</code> และ <code>read</code> ถ้าจะใช้ปุ่ม Test Connection</div>
                </div>
                <div>
                    <asp:TextBox ID="txtRelayKey" runat="server" TextMode="Password" MaxLength="500" autocomplete="new-password" />
                    <div class="desc"><asp:Literal ID="litRelayKeyStatus" runat="server" /></div>
                    <div class="desc"><asp:CheckBox ID="chkClearRelayKey" runat="server" Text=" ลบ API Key ที่บันทึกบนเว็บ" /></div>
                </div>
            </div>

            <div class="setting-row">
                <div>
                    <label for="<%= txtRelaySecret.ClientID %>">Signing secret (optional) <span class="source <%= SourceOf("EmailRelaySigningSecret") %>"><%= SourceOf("EmailRelaySigningSecret") %></span></label>
                    <div class="desc">ใส่เฉพาะเมื่อ client เปิด "Require signed requests" ระบบจะส่ง X-Timestamp / X-Signature (HMAC-SHA256) ให้</div>
                </div>
                <div>
                    <asp:TextBox ID="txtRelaySecret" runat="server" TextMode="Password" MaxLength="500" autocomplete="new-password" />
                    <div class="desc"><asp:Literal ID="litRelaySecretStatus" runat="server" /></div>
                    <div class="desc"><asp:CheckBox ID="chkClearRelaySecret" runat="server" Text=" ลบ signing secret ที่บันทึกบนเว็บ" /></div>
                </div>
            </div>

            <div class="setting-row">
                <div>
                    <label for="<%= txtRelayFrom.ClientID %>">From address (optional) <span class="source <%= SourceOf("EmailRelayFromAddress") %>"><%= SourceOf("EmailRelayFromAddress") %></span></label>
                    <div class="desc">เว้นว่าง = ใช้ From ของ sender profile ถ้าใส่ ต้องอยู่ใน Allowed From ของ client</div>
                </div>
                <div><asp:TextBox ID="txtRelayFrom" runat="server" MaxLength="320" placeholder="noreply@hondacx.me" /></div>
            </div>

            <div class="setting-row">
                <div>
                    <label for="<%= txtRelayFromName.ClientID %>">From name (optional) <span class="source <%= SourceOf("EmailRelayFromName") %>"><%= SourceOf("EmailRelayFromName") %></span></label>
                </div>
                <div><asp:TextBox ID="txtRelayFromName" runat="server" MaxLength="200" placeholder="Honda CX Coupon" /></div>
            </div>

            <div class="setting-row">
                <div>
                    <label for="<%= txtRelayProfile.ClientID %>">Sender profile (optional) <span class="source <%= SourceOf("EmailRelaySenderProfile") %>"><%= SourceOf("EmailRelaySenderProfile") %></span></label>
                    <div class="desc">เว้นว่าง = ใช้ profile ที่ผูกกับ client</div>
                </div>
                <div><asp:TextBox ID="txtRelayProfile" runat="server" MaxLength="100" /></div>
            </div>

            <div class="setting-row">
                <div>
                    <label for="<%= txtRelayTimeout.ClientID %>">Timeout (seconds) <span class="source <%= SourceOf("EmailRelayTimeout") %>"><%= SourceOf("EmailRelayTimeout") %></span></label>
                    <div class="desc">5 - 120 วินาที</div>
                </div>
                <div><asp:TextBox ID="txtRelayTimeout" runat="server" CssClass="short" MaxLength="3" /></div>
            </div>

            <div class="actions">
                <asp:Button ID="btnTestRelay" runat="server" Text="Test Connection" CssClass="btn-secondary-custom" OnClick="btnTestRelay_Click" CausesValidation="false" />
                <asp:TextBox ID="txtTestEmailTo" runat="server" MaxLength="320" placeholder="ส่งอีเมลทดสอบไปที่..." style="padding:8px 12px; border:1px solid #ccc; border-radius:6px; width:260px; margin-right:8px;" />
                <asp:Button ID="btnSendTestEmail" runat="server" Text="Send Test Email" CssClass="btn-secondary-custom" OnClick="btnSendTestEmail_Click" CausesValidation="false" />
                <div class="desc" style="margin-top:6px;">ทดสอบด้วยค่าที่บันทึกแล้ว (กด Save Settings ก่อนถ้าเพิ่งแก้) Send Test Email ทำงานเมื่อเปิด Enable Email Relay แล้ว</div>
            </div>
            <div style="margin-top:15px;"><asp:Literal ID="litRelayTest" runat="server" /></div>
        </div>

        <!-- ===================== Coupon ===================== -->
        <div class="panel">
            <h5>Coupon</h5>

            <div class="setting-row">
                <div>
                    <label for="<%= txtCouponIdLength.ClientID %>">Coupon ID length <span class="source <%= SourceOf("NumberofCode") %>"><%= SourceOf("NumberofCode") %></span></label>
                    <div class="desc">ใช้ทั้งตอน Import (สร้าง ID อัตโนมัติ) และหน้าใช้สิทธิ์ (4 - 32) ถ้าเปลี่ยน คูปองเดิมที่ ID ยาวไม่ตรงจะใช้สิทธิ์ไม่ได้</div>
                </div>
                <div><asp:TextBox ID="txtCouponIdLength" runat="server" CssClass="short" MaxLength="2" /></div>
            </div>

            <div class="actions">
                <asp:Button ID="btnSaveSettings" runat="server" Text="Save Settings" CssClass="btn-primary-custom" OnClick="btnSaveSettings_Click" />
            </div>
        </div>

        <!-- ===================== Allowed Admin Accounts ===================== -->
        <div class="panel">
            <h5>Allowed Admin Accounts</h5>
            <p class="hint">ใส่อีเมลหรือชื่อบัญชี AD ให้ตรงกับที่ใช้ login (ไม่สนตัวพิมพ์เล็ก-ใหญ่) ผู้ที่ถูกปิดหรือลบจะถูกออกจากระบบภายใน 1 นาที</p>

            <asp:GridView ID="gvAdmins" runat="server" AutoGenerateColumns="false" CssClass="admin-grid" GridLines="None"
                OnRowCommand="gvAdmins_RowCommand" EmptyDataText="ยังไม่มีรายชื่อ admin">
                <Columns>
                    <asp:TemplateField HeaderText="Username / Email">
                        <ItemTemplate><%# Server.HtmlEncode(Convert.ToString(Eval("Username"))) %></ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Name">
                        <ItemTemplate><%# Server.HtmlEncode(Convert.ToString(Eval("DisplayName"))) %></ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Note">
                        <ItemTemplate><%# Server.HtmlEncode(Convert.ToString(Eval("Note"))) %></ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Status">
                        <ItemTemplate>
                            <span class='<%# (bool)Eval("IsActive") ? "status-active" : "status-disabled" %>'><%# (bool)Eval("IsActive") ? "Active" : "Disabled" %></span>
                        </ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="Added">
                        <ItemTemplate><%# Server.HtmlEncode(Convert.ToString(Eval("AddedBy"))) %><br /><small><%# FormatDate(Eval("CreatedDateTime")) %></small></ItemTemplate>
                    </asp:TemplateField>
                    <asp:TemplateField HeaderText="">
                        <ItemTemplate>
                            <asp:LinkButton ID="lnkToggle" runat="server" CommandName="ToggleActive" CommandArgument='<%# Eval("Id") %>'
                                Visible='<%# !(bool)Eval("IsFromWebConfig") %>' CausesValidation="false"
                                Text='<%# (bool)Eval("IsActive") ? "Disable" : "Enable" %>' />
                            <asp:LinkButton ID="lnkDelete" runat="server" CommandName="DeleteAdmin" CommandArgument='<%# Eval("Id") %>'
                                Visible='<%# !(bool)Eval("IsFromWebConfig") %>' CausesValidation="false" CssClass="danger"
                                Text="Delete" OnClientClick="return confirm('ลบบัญชีนี้ออกจากรายชื่อ admin?');" />
                        </ItemTemplate>
                    </asp:TemplateField>
                </Columns>
            </asp:GridView>

            <asp:Panel ID="pnlAddAdmin" runat="server" CssClass="add-admin" DefaultButton="btnAddAdmin">
                <asp:TextBox ID="txtNewAdmin" runat="server" MaxLength="200" placeholder="email หรือ AD account เช่น name@ipsos.com" />
                <asp:TextBox ID="txtNewAdminName" runat="server" MaxLength="200" placeholder="ชื่อ (ไม่บังคับ)" />
                <asp:TextBox ID="txtNewAdminNote" runat="server" MaxLength="500" placeholder="หมายเหตุ (ไม่บังคับ)" />
                <asp:Button ID="btnAddAdmin" runat="server" Text="Add Admin" CssClass="btn-primary-custom" OnClick="btnAddAdmin_Click" CausesValidation="false" />
            </asp:Panel>
        </div>
    </div>
</asp:Content>
