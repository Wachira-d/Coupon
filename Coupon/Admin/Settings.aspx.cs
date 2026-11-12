using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using Coupon.Helpers;
using Newtonsoft.Json.Linq;

namespace Coupon.Admin
{
    /// <summary>
    /// Admin page to edit system settings (stored in App_Settings) and the allowed admin accounts (Admin_Users)
    /// </summary>
    public partial class Settings : Page
    {
        protected void Page_Init(object sender, EventArgs e)
        {
            // SECURITY: tie ViewState/postbacks to this session (prevents CSRF on the save/delete buttons)
            if (Session != null)
            {
                ViewStateUserKey = Session.SessionID;
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            // SECURITY: admins only
            if (!AuthenticationHelper.ValidateSession(Session))
            {
                Response.Redirect("~/Admin/Login.aspx?ReturnUrl=" + Server.UrlEncode(Request.RawUrl), false);
                Context.ApplicationInstance.CompleteRequest();
                return;
            }

            if (!IsPostBack)
            {
                UserInfo currentUser = AuthenticationHelper.GetCurrentUser(Session);
                if (currentUser != null)
                {
                    lblCurrentUser.Text = Server.HtmlEncode(currentUser.DisplayName ?? currentUser.Username);
                    lblCurrentEmail.Text = Server.HtmlEncode(currentUser.Email ?? "");
                }

                LoadSettings();
                BindAdmins(false);
            }

            ShowWarnings();
        }

        #region Settings

        private void LoadSettings()
        {
            txtApiBaseUrl.Text = SettingsHelper.Get(SettingsHelper.KeyApiBaseUrl) ?? "";
            txtApiKey.Text = "";
            chkClearApiKey.Checked = false;
            litApiKeyStatus.Text = string.IsNullOrEmpty(SettingsHelper.Get(SettingsHelper.KeyApiKey))
                ? "สถานะ: <strong>ยังไม่ได้ตั้งค่า</strong>"
                : $"สถานะ: <strong>ตั้งค่าแล้ว</strong> (จาก {Server.HtmlEncode(SourceOf(SettingsHelper.KeyApiKey))})";

            txtApiTimeout.Text = SettingsHelper.GetInt(SettingsHelper.KeyApiTimeout, 30).ToString();
            chkRestrictAdmin.Checked = SettingsHelper.GetBool(SettingsHelper.KeyRestrictAdminAccess, true);
            txtSessionHours.Text = SettingsHelper.GetInt(SettingsHelper.KeySessionTimeoutHours, 8).ToString();
            txtLockoutMinutes.Text = SettingsHelper.GetInt(SettingsHelper.KeyLockoutDurationMinutes, 15).ToString();
            txtCacheDays.Text = SettingsHelper.GetInt(SettingsHelper.KeyCacheDurationDays, 90).ToString();
            txtCouponIdLength.Text = SettingsHelper.CouponIdLength.ToString();

            // Email Relay API
            chkRelayEnabled.Checked = SettingsHelper.GetBool(SettingsHelper.KeyRelayEnabled, false);
            txtRelayUrl.Text = SettingsHelper.Get(SettingsHelper.KeyRelayBaseUrl) ?? "";
            txtRelayKey.Text = "";
            chkClearRelayKey.Checked = false;
            litRelayKeyStatus.Text = SecretStatus(SettingsHelper.KeyRelayApiKey);
            txtRelaySecret.Text = "";
            chkClearRelaySecret.Checked = false;
            litRelaySecretStatus.Text = SecretStatus(SettingsHelper.KeyRelaySigningSecret);
            txtRelayFrom.Text = SettingsHelper.Get(SettingsHelper.KeyRelayFromAddress) ?? "";
            txtRelayFromName.Text = SettingsHelper.Get(SettingsHelper.KeyRelayFromName) ?? "";
            txtRelayProfile.Text = SettingsHelper.Get(SettingsHelper.KeyRelaySenderProfile) ?? "";
            txtRelayTimeout.Text = SettingsHelper.GetInt(SettingsHelper.KeyRelayTimeout, 30).ToString();

            if (string.IsNullOrEmpty(txtTestEmailTo.Text))
            {
                txtTestEmailTo.Text = Convert.ToString(Session["Email"]);
            }
        }

        private string SecretStatus(string key)
        {
            return string.IsNullOrEmpty(SettingsHelper.Get(key))
                ? "สถานะ: <strong>ยังไม่ได้ตั้งค่า</strong>"
                : $"สถานะ: <strong>ตั้งค่าแล้ว</strong> (จาก {Server.HtmlEncode(SourceOf(key))})";
        }

        private static bool IsValidEmail(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 320)
            {
                return false;
            }

            try
            {
                var address = new System.Net.Mail.MailAddress(value.Trim());
                return address.Address == value.Trim();
            }
            catch (FormatException)
            {
                return false;
            }
        }

        private static bool IsValidHttpUrl(string value)
        {
            Uri parsed;
            return Uri.TryCreate(value, UriKind.Absolute, out parsed) &&
                   (parsed.Scheme == Uri.UriSchemeHttp || parsed.Scheme == Uri.UriSchemeHttps);
        }

        protected void btnSaveSettings_Click(object sender, EventArgs e)
        {
            if (!SettingsHelper.SettingsTableAvailable)
            {
                ShowMessage("บันทึกไม่ได้: ยังไม่มีตาราง App_Settings กรุณารัน Database/CreateSettingsTables.sql ก่อน", false);
                return;
            }

            var errors = new List<string>();

            string apiUrl = txtApiBaseUrl.Text.Trim();
            if (apiUrl.Length > 0 && !IsValidHttpUrl(apiUrl))
            {
                errors.Add("API Base URL ต้องขึ้นต้นด้วย http:// หรือ https://");
            }

            string relayUrl = txtRelayUrl.Text.Trim().TrimEnd('/');
            if (relayUrl.Length > 0 && !IsValidHttpUrl(relayUrl))
            {
                errors.Add("Relay Base URL ต้องขึ้นต้นด้วย http:// หรือ https://");
            }
            if (relayUrl.EndsWith("/api/v1", StringComparison.OrdinalIgnoreCase) || relayUrl.EndsWith("/api/v1/emails", StringComparison.OrdinalIgnoreCase))
            {
                errors.Add("Relay Base URL ไม่ต้องใส่ /api/v1 (ระบบเติมให้เอง)");
            }

            string relayFrom = txtRelayFrom.Text.Trim();
            if (relayFrom.Length > 0 && !IsValidEmail(relayFrom))
            {
                errors.Add("From address ไม่ใช่อีเมลที่ถูกต้อง");
            }

            if (chkRelayEnabled.Checked && relayUrl.Length == 0 && string.IsNullOrEmpty(ConfigValue(SettingsHelper.KeyRelayBaseUrl)))
            {
                errors.Add("เปิด Email Relay แล้วต้องใส่ Relay Base URL");
            }

            ValidateInt(txtApiTimeout, SettingsHelper.KeyApiTimeout, "API Timeout", errors);
            ValidateInt(txtSessionHours, SettingsHelper.KeySessionTimeoutHours, "Session timeout", errors);
            ValidateInt(txtLockoutMinutes, SettingsHelper.KeyLockoutDurationMinutes, "Lockout duration", errors);
            ValidateInt(txtCacheDays, SettingsHelper.KeyCacheDurationDays, "Login cache duration", errors);
            ValidateInt(txtCouponIdLength, SettingsHelper.KeyCouponIdLength, "Coupon ID length", errors);
            ValidateInt(txtRelayTimeout, SettingsHelper.KeyRelayTimeout, "Email Relay timeout", errors);

            if (!chkRestrictAdmin.Checked && SettingsHelper.GetBool(SettingsHelper.KeyRestrictAdminAccess, true))
            {
                // Allowed, but make sure it is intentional: it opens the admin area to every AD account
                ErrorLogger.LogWarning("Admin access restriction is being disabled", $"By: {CurrentUserName()}");
            }

            if (errors.Count > 0)
            {
                ShowMessage("กรุณาแก้ไข:<br/>" + string.Join("<br/>", errors.Select(s => HttpUtility.HtmlEncode(s))), false, true);
                return;
            }

            var changed = new List<string>();
            string by = CurrentUserName();

            try
            {
                SaveIfChanged(SettingsHelper.KeyApiBaseUrl, apiUrl, by, changed);

                SaveSecret(SettingsHelper.KeyApiKey, txtApiKey, chkClearApiKey, by, changed);
                SaveIfChanged(SettingsHelper.KeyApiTimeout, txtApiTimeout.Text.Trim(), by, changed);
                SaveBoolIfChanged(SettingsHelper.KeyRestrictAdminAccess, chkRestrictAdmin.Checked, by, changed);
                SaveIfChanged(SettingsHelper.KeySessionTimeoutHours, txtSessionHours.Text.Trim(), by, changed);
                SaveIfChanged(SettingsHelper.KeyLockoutDurationMinutes, txtLockoutMinutes.Text.Trim(), by, changed);
                SaveIfChanged(SettingsHelper.KeyCacheDurationDays, txtCacheDays.Text.Trim(), by, changed);
                SaveIfChanged(SettingsHelper.KeyCouponIdLength, txtCouponIdLength.Text.Trim(), by, changed);

                SaveBoolIfChanged(SettingsHelper.KeyRelayEnabled, chkRelayEnabled.Checked, by, changed, false);
                SaveIfChanged(SettingsHelper.KeyRelayBaseUrl, relayUrl, by, changed);
                SaveSecret(SettingsHelper.KeyRelayApiKey, txtRelayKey, chkClearRelayKey, by, changed);
                SaveSecret(SettingsHelper.KeyRelaySigningSecret, txtRelaySecret, chkClearRelaySecret, by, changed);
                SaveIfChanged(SettingsHelper.KeyRelayFromAddress, relayFrom, by, changed);
                SaveIfChanged(SettingsHelper.KeyRelayFromName, txtRelayFromName.Text.Trim(), by, changed);
                SaveIfChanged(SettingsHelper.KeyRelaySenderProfile, txtRelayProfile.Text.Trim(), by, changed);
                SaveIfChanged(SettingsHelper.KeyRelayTimeout, txtRelayTimeout.Text.Trim(), by, changed);

                if (changed.Count > 0)
                {
                    // Values are not logged (API key is secret); only which keys changed
                    AuditLogger.LogAdminAction(by, "Update Settings", "Settings", GetClientIpAddress(), "Changed: " + string.Join(", ", changed));
                    ShowMessage("บันทึกแล้ว: " + string.Join(", ", changed), true);
                }
                else
                {
                    ShowMessage("ไม่มีค่าที่เปลี่ยนแปลง", true);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error saving settings", ex, $"By: {by}");
                ShowMessage("บันทึกไม่สำเร็จ: " + ex.Message, false);
            }

            LoadSettings();
            ShowWarnings();
        }

        private static void ValidateInt(TextBox box, string key, string label, List<string> errors)
        {
            string text = box.Text.Trim();
            if (text.Length == 0)
            {
                return; // empty = use Web.config / default
            }

            SettingDefinition definition = SettingsHelper.FindDefinition(key);
            int value;
            if (!int.TryParse(text, out value) || (definition != null && (value < definition.Min || value > definition.Max)))
            {
                errors.Add(definition != null
                    ? $"{label} ต้องเป็นตัวเลข {definition.Min} - {definition.Max}"
                    : $"{label} ต้องเป็นตัวเลข");
            }
        }

        /// <summary>Empty value removes the database override; otherwise save only if different from the effective value</summary>
        private static void SaveIfChanged(string key, string newValue, string by, List<string> changed)
        {
            if (string.IsNullOrEmpty(newValue))
            {
                if (SettingsHelper.HasDatabaseValue(key))
                {
                    SettingsHelper.Remove(key);
                    changed.Add(key + " (reset to Web.config)");
                }
                return;
            }

            if (!string.Equals(newValue, SettingsHelper.Get(key), StringComparison.Ordinal))
            {
                SettingsHelper.Save(key, newValue, by);
                changed.Add(key);
            }
        }

        private static void SaveBoolIfChanged(string key, bool newValue, string by, List<string> changed, bool defaultValue = true)
        {
            if (newValue != SettingsHelper.GetBool(key, defaultValue))
            {
                SettingsHelper.Save(key, newValue ? "true" : "false", by);
                changed.Add(key);
            }
        }

        /// <summary>Secrets are never shown: change only when something was typed, or remove when "clear" is ticked</summary>
        private static void SaveSecret(string key, TextBox box, CheckBox clear, string by, List<string> changed)
        {
            if (clear.Checked)
            {
                if (SettingsHelper.HasDatabaseValue(key))
                {
                    SettingsHelper.Remove(key);
                    changed.Add(key + " (cleared)");
                }
            }
            else if (box.Text.Trim().Length > 0)
            {
                SettingsHelper.Save(key, box.Text.Trim(), by);
                changed.Add(key);
            }
        }

        private static string ConfigValue(string key)
        {
            return System.Configuration.ConfigurationManager.AppSettings[key];
        }

        protected void btnTestApi_Click(object sender, EventArgs e)
        {
            ApiTestResult result = AuthenticationHelper.TestApiConnection();
            var html = new StringBuilder();

            string summary;
            string css;
            if (!string.IsNullOrEmpty(result.Error))
            {
                css = "error";
                summary = "เชื่อมต่อ API ไม่ได้: " + result.Error;
            }
            else if (result.StatusCode >= 500)
            {
                css = "error";
                summary = $"API ตอบกลับ HTTP {result.StatusCode} {result.StatusText} - ฝั่ง Authen API มีปัญหา" +
                          (result.StatusCode == 502 ? " (502 = ตัว API ที่อยู่หลัง IIS ไม่ทำงาน/ไม่ตอบ)" : "");
            }
            else if (!result.IsJson)
            {
                css = "warning";
                summary = $"API ตอบกลับ HTTP {result.StatusCode} แต่ไม่ใช่ JSON - ตรวจสอบ URL ว่าถูกต้อง";
            }
            else if (result.StatusCode == 401 || result.StatusCode == 403)
            {
                css = "warning";
                summary = $"API ตอบกลับ HTTP {result.StatusCode} - API Key ไม่ถูกต้องหรือไม่ได้ใส่";
            }
            else
            {
                css = "success";
                summary = $"API ทำงานปกติ (HTTP {result.StatusCode}, ตอบเป็น JSON)";
            }

            html.Append($"<div class='alert-box {css}'><strong>{HttpUtility.HtmlEncode(summary)}</strong>");
            html.Append($"<br/>URL: {HttpUtility.HtmlEncode(result.Url)}");
            html.Append($"<br/>เวลา: {result.ElapsedMs:N0} ms (timeout {result.TimeoutSeconds} s)");
            if (!string.IsNullOrEmpty(result.BodyPreview))
            {
                html.Append($"<pre>{HttpUtility.HtmlEncode(result.BodyPreview)}</pre>");
            }
            html.Append("</div>");

            litApiTest.Text = html.ToString();

            AuditLogger.LogAdminAction(CurrentUserName(), "Test Authen API", "Settings", GetClientIpAddress(),
                string.IsNullOrEmpty(result.Error) ? $"HTTP {result.StatusCode}, {result.ElapsedMs} ms" : result.Error);
        }

        protected void btnTestRelay_Click(object sender, EventArgs e)
        {
            RelayResult result = EmailRelayHelper.TestConnection();

            string css;
            string summary;
            var details = new StringBuilder();

            if (result.Success)
            {
                css = "success";
                summary = "เชื่อมต่อ Email Relay ได้ และ API Key ถูกต้อง";
                JToken data = result.Data;
                if (data != null && data.Type == JTokenType.Object)
                {
                    // Show a few useful fields from /api/v1/me (whatever the relay returns)
                    foreach (JProperty property in ((JObject)data).Properties().Take(12))
                    {
                        if (property.Value.Type == JTokenType.Object || property.Value.Type == JTokenType.Array) continue;
                        details.Append($"<br/>{HttpUtility.HtmlEncode(property.Name)}: {HttpUtility.HtmlEncode(property.Value.ToString())}");
                    }
                }
            }
            else
            {
                css = result.StatusCode == 403 ? "warning" : "error";
                summary = "Email Relay: " + (result.Error ?? "ไม่ทราบสาเหตุ") + RelayHint(result.StatusCode);
            }

            litRelayTest.Text = RenderRelayResult(css, summary, details.ToString(), result);
            AuditLogger.LogAdminAction(CurrentUserName(), "Test Email Relay", "Settings", GetClientIpAddress(),
                result.Success ? $"OK, {result.ElapsedMs} ms" : $"HTTP {result.StatusCode}: {result.Error}");
        }

        protected void btnSendTestEmail_Click(object sender, EventArgs e)
        {
            string to = txtTestEmailTo.Text.Trim();
            if (!IsValidEmail(to))
            {
                litRelayTest.Text = "<div class='alert-box error'>กรุณาใส่อีเมลผู้รับที่ถูกต้อง</div>";
                return;
            }

            if (!EmailRelayHelper.IsEnabled)
            {
                litRelayTest.Text = "<div class='alert-box warning'>Email Relay ยังปิดอยู่: ติ๊ก Enable Email Relay แล้วกด Save Settings ก่อน</div>";
                return;
            }

            string by = CurrentUserName();
            RelayResult result = EmailRelayHelper.SendTestEmail(to, by);

            string css;
            string summary;
            string details = "";
            if (result.Success)
            {
                css = "success";
                summary = $"Relay รับอีเมลแล้ว ส่งไปที่ {to}";
                details = $"<br/>Message ID: {HttpUtility.HtmlEncode(result.MessageId)}" +
                          $"<br/>สถานะ: {HttpUtility.HtmlEncode(result.DeliveryStatus)} (queued = อยู่ในคิว จะส่งภายในไม่กี่วินาที ดูผลได้ใน console ของ relay)";
            }
            else
            {
                css = "error";
                summary = "ส่งไม่สำเร็จ: " + (result.Error ?? "ไม่ทราบสาเหตุ") + RelayHint(result.StatusCode);
            }

            litRelayTest.Text = RenderRelayResult(css, summary, details, result);
            AuditLogger.LogAdminAction(by, "Send Test Email", "Settings", GetClientIpAddress(),
                result.Success ? $"To: {to}, Id: {result.MessageId}" : $"To: {to}, HTTP {result.StatusCode}: {result.Error}");
        }

        private static string RelayHint(int statusCode)
        {
            switch (statusCode)
            {
                case 401: return " (401 = API Key ผิด/หมดอายุ/ถูก revoke หรือลายเซ็นไม่ถูกต้อง)";
                case 403: return " (403 = IP ของ server นี้ไม่อยู่ใน allow list, scope ไม่พอ, หรือ From/โดเมนผู้รับไม่อนุญาต)";
                case 409: return " (409 = ข้อความซ้ำถี่เกินไป)";
                case 422: return " (422 = ไม่มี sender profile หรือ profile ปิดอยู่)";
                case 429: return " (429 = เกิน rate limit หรือโควต้ารายวัน)";
                case 502: return " (502 = ตัว API หลัง IIS ไม่ทำงาน)";
                default: return "";
            }
        }

        private static string RenderRelayResult(string css, string summary, string extraHtml, RelayResult result)
        {
            var html = new StringBuilder();
            html.Append($"<div class='alert-box {css}'><strong>{HttpUtility.HtmlEncode(summary)}</strong>");
            html.Append(extraHtml);
            if (!string.IsNullOrEmpty(result.Url))
            {
                html.Append($"<br/>URL: {HttpUtility.HtmlEncode(result.Url)}");
            }
            html.Append($"<br/>เวลา: {result.ElapsedMs:N0} ms");
            if (!result.Success && !string.IsNullOrEmpty(result.ResponsePreview))
            {
                html.Append($"<pre>{HttpUtility.HtmlEncode(result.ResponsePreview)}</pre>");
            }
            html.Append("</div>");
            return html.ToString();
        }

        #endregion

        #region Admin accounts

        private void BindAdmins(bool forceReload)
        {
            gvAdmins.DataSource = SettingsHelper.GetAdminUsers(forceReload);
            gvAdmins.DataBind();

            bool editable = SettingsHelper.AdminTableAvailable;
            pnlAddAdmin.Visible = editable;
        }

        protected void btnAddAdmin_Click(object sender, EventArgs e)
        {
            if (!SettingsHelper.AdminTableAvailable)
            {
                ShowMessage("เพิ่มไม่ได้: ยังไม่มีตาราง Admin_Users กรุณารัน Database/CreateSettingsTables.sql ก่อน", false);
                return;
            }

            string username = txtNewAdmin.Text.Trim();
            if (!SettingsHelper.IsValidAdminUsername(username))
            {
                ShowMessage("Username/Email ไม่ถูกต้อง (ใช้ได้เฉพาะ a-z 0-9 . _ - @ \\)", false);
                return;
            }

            try
            {
                string by = CurrentUserName();
                if (SettingsHelper.AddAdmin(username, txtNewAdminName.Text, txtNewAdminNote.Text, by))
                {
                    AuditLogger.LogAdminAction(by, "Add Admin", "Admin_Users", GetClientIpAddress(), username);
                    ShowMessage("เพิ่ม admin แล้ว: " + username, true);
                    txtNewAdmin.Text = "";
                    txtNewAdminName.Text = "";
                    txtNewAdminNote.Text = "";
                }
                else
                {
                    ShowMessage("มีบัญชีนี้อยู่แล้ว: " + username, false);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error adding admin", ex, $"Username: {username}");
                ShowMessage("เพิ่มไม่สำเร็จ: " + ex.Message, false);
            }

            BindAdmins(true);
            ShowWarnings();
        }

        protected void gvAdmins_RowCommand(object sender, GridViewCommandEventArgs e)
        {
            if (e.CommandName != "ToggleActive" && e.CommandName != "DeleteAdmin")
            {
                return;
            }

            int id;
            if (!int.TryParse(Convert.ToString(e.CommandArgument), out id))
            {
                return;
            }

            AdminUser target = SettingsHelper.FindAdmin(id);
            if (target == null)
            {
                ShowMessage("ไม่พบบัญชีนี้ (อาจถูกลบไปแล้ว)", false);
                BindAdmins(true);
                return;
            }

            bool removesAccess = e.CommandName == "DeleteAdmin" || target.IsActive;

            // Safety: do not let an admin lock themselves (or everybody) out
            if (removesAccess && IsCurrentUser(target.Username))
            {
                ShowMessage("ไม่สามารถปิดหรือลบบัญชีของตัวเองได้", false);
                return;
            }

            if (removesAccess && target.IsActive && SettingsHelper.CountActiveAdmins() <= 1)
            {
                ShowMessage("ต้องมี admin ที่ใช้งานได้อย่างน้อย 1 บัญชี", false);
                return;
            }

            try
            {
                string by = CurrentUserName();
                if (e.CommandName == "DeleteAdmin")
                {
                    SettingsHelper.DeleteAdmin(id);
                    AuditLogger.LogAdminAction(by, "Delete Admin", "Admin_Users", GetClientIpAddress(), target.Username);
                    ShowMessage("ลบแล้ว: " + target.Username, true);
                }
                else
                {
                    SettingsHelper.SetAdminActive(id, !target.IsActive);
                    AuditLogger.LogAdminAction(by, target.IsActive ? "Disable Admin" : "Enable Admin", "Admin_Users",
                        GetClientIpAddress(), target.Username);
                    ShowMessage((target.IsActive ? "ปิดใช้งานแล้ว: " : "เปิดใช้งานแล้ว: ") + target.Username, true);
                }
            }
            catch (Exception ex)
            {
                ErrorLogger.LogError("Error updating admin", ex, $"Id: {id}");
                ShowMessage("ทำรายการไม่สำเร็จ: " + ex.Message, false);
            }

            BindAdmins(true);
            ShowWarnings();
        }

        private bool IsCurrentUser(string adminUsername)
        {
            string target = (adminUsername ?? "").Trim().ToLowerInvariant();
            return new[] { Session["LoginName"], Session["Username"], Session["Email"] }
                .Select(o => Convert.ToString(o)?.Trim().ToLowerInvariant())
                .Any(s => !string.IsNullOrEmpty(s) && s == target);
        }

        #endregion

        #region Helpers

        /// <summary>Used in markup: where a setting's value comes from</summary>
        protected string SourceOf(string key)
        {
            return SettingsHelper.GetSource(key);
        }

        protected string FormatDate(object value)
        {
            return value is DateTime ? ((DateTime)value).ToString("yyyy-MM-dd HH:mm", System.Globalization.CultureInfo.InvariantCulture) : "";
        }

        private void ShowWarnings()
        {
            var warnings = new List<string>();

            if (!SettingsHelper.SettingsTableAvailable || !SettingsHelper.AdminTableAvailable)
            {
                warnings.Add("ยังไม่มีตาราง App_Settings / Admin_Users ในฐานข้อมูล ระบบกำลังใช้ค่าจาก Web.config และรายชื่อ admin จาก ReportPassword (แก้ไขบนเว็บไม่ได้) " +
                             "กรุณารันสคริปต์ Database/CreateSettingsTables.sql");
            }

            if (!SettingsHelper.GetBool(SettingsHelper.KeyRestrictAdminAccess, true))
            {
                warnings.Add("ปิดการจำกัดสิทธิ์อยู่: ทุกคนที่มีบัญชี AD เข้าหน้า Admin ได้");
            }
            else if (SettingsHelper.CountActiveAdmins() == 0)
            {
                warnings.Add("ยังไม่มีรายชื่อ admin ที่ใช้งานได้: ทุกคนที่มีบัญชี AD เข้าหน้า Admin ได้ กรุณาเพิ่มรายชื่อ");
            }

            litWarnings.Text = warnings.Count == 0
                ? ""
                : "<div class='alert-box warning'>" + string.Join("<br/>", warnings.Select(s => HttpUtility.HtmlEncode(s))) + "</div>";
        }

        /// <param name="alreadyEncoded">true when message is already safe HTML</param>
        private void ShowMessage(string message, bool success, bool alreadyEncoded = false)
        {
            string text = alreadyEncoded ? message : HttpUtility.HtmlEncode(message);
            litMessage.Text = $"<div class='alert-box {(success ? "success" : "error")}'>{text}</div>";
        }

        private string CurrentUserName()
        {
            string email = Convert.ToString(Session["Email"]);
            if (!string.IsNullOrEmpty(email))
            {
                return email;
            }

            string username = Convert.ToString(Session["Username"]);
            return string.IsNullOrEmpty(username) ? "admin" : username;
        }

        private string GetClientIpAddress()
        {
            string ip = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (!string.IsNullOrEmpty(ip))
            {
                return ip.Split(',')[0].Trim();
            }
            return Request.ServerVariables["REMOTE_ADDR"] ?? "Unknown";
        }

        #endregion
    }
}
