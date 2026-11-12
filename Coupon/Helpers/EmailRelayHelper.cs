using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Coupon.Helpers
{
    /// <summary>An email to send through the Email Relay API</summary>
    public class RelayEmail
    {
        public List<string> To { get; set; } = new List<string>();
        public List<string> Cc { get; set; }
        public List<string> Bcc { get; set; }
        public string Subject { get; set; }
        public string Text { get; set; }
        public string Html { get; set; }
        public string ReplyTo { get; set; }
        public List<string> Tags { get; set; }

        /// <summary>Same key within 24h is not sent twice by the relay (safe to retry)</summary>
        public string IdempotencyKey { get; set; }

        /// <summary>Stored template name on the relay (optional)</summary>
        public string Template { get; set; }
        public Dictionary<string, string> Variables { get; set; }
    }

    /// <summary>Result of a call to the Email Relay API</summary>
    public class RelayResult
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; }
        public string MessageId { get; set; }
        public string DeliveryStatus { get; set; }
        public bool Duplicate { get; set; }
        public string Error { get; set; }
        public string Url { get; set; }
        public long ElapsedMs { get; set; }
        public string ResponsePreview { get; set; }
        public JToken Data { get; set; }
    }

    /// <summary>
    /// Client for EmailRelayAPI (https://github.com/Wachira-d/EmailRelayAPI, docs/API_GUIDE.md).
    /// Settings are read from Admin/Settings.aspx (App_Settings) with Web.config fallback.
    ///
    /// - Auth: API key in X-API-Key header (scope "send" to send, "read" for /api/v1/me)
    /// - Optional HMAC signing when a signing secret is set:
    ///   X-Timestamp + X-Signature: sha256=hex(HMAC_SHA256(secret, "{ts}.{METHOD}.{path}.{sha256hex(body)}"))
    /// - Response envelope: { success, message, data, error, timestamp }
    /// </summary>
    public static class EmailRelayHelper
    {
        private const string SendPath = "/api/v1/emails";
        private const string MePath = "/api/v1/me";

        private static readonly HttpClient _httpClient = new HttpClient();

        static EmailRelayHelper()
        {
            // Timeout applied per request so it can be changed from the Settings page
            _httpClient.Timeout = Timeout.InfiniteTimeSpan;
        }

        public static bool IsEnabled => SettingsHelper.GetBool(SettingsHelper.KeyRelayEnabled, false);

        private static string BaseUrl => (SettingsHelper.Get(SettingsHelper.KeyRelayBaseUrl) ?? "").Trim();
        private static string ApiKey => SettingsHelper.Get(SettingsHelper.KeyRelayApiKey) ?? "";
        private static string SigningSecret => SettingsHelper.Get(SettingsHelper.KeyRelaySigningSecret) ?? "";
        private static int TimeoutSeconds => SettingsHelper.GetInt(SettingsHelper.KeyRelayTimeout, 30);

        /// <summary>Queue an email for delivery. Does not throw; check RelayResult.Success.</summary>
        public static RelayResult Send(RelayEmail email)
        {
            if (!IsEnabled)
            {
                return new RelayResult { Success = false, Error = "Email Relay is disabled (Admin/Settings.aspx)" };
            }

            if (email == null || email.To == null || email.To.Count(t => !string.IsNullOrWhiteSpace(t)) == 0)
            {
                return new RelayResult { Success = false, Error = "No recipient" };
            }

            string json = JsonConvert.SerializeObject(BuildPayload(email), new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore
            });

            RelayResult result = Execute(HttpMethod.Post, SendPath, json);

            if (result.Success && result.Data != null)
            {
                result.MessageId = result.Data["id"]?.ToString();
                result.DeliveryStatus = result.Data["status"]?.ToString();
                result.Duplicate = result.Data["duplicate"]?.Type == JTokenType.Boolean && result.Data["duplicate"].Value<bool>();
            }

            if (!result.Success)
            {
                ErrorLogger.LogWarning("Email Relay send failed",
                    $"HTTP {result.StatusCode}, Error: {result.Error}, Subject: {email.Subject}, To: {string.Join(";", email.To)}");
            }

            return result;
        }

        /// <summary>Check URL + API key using GET /api/v1/me (needs scope "read")</summary>
        public static RelayResult TestConnection()
        {
            return Execute(HttpMethod.Get, MePath, null);
        }

        /// <summary>Send a short test email to the given address</summary>
        public static RelayResult SendTestEmail(string to, string requestedBy)
        {
            string now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            return Send(new RelayEmail
            {
                To = new List<string> { to },
                Subject = "[Coupon] Email Relay test",
                Text = $"This is a test email from the Coupon admin Settings page.\r\nSent at: {now}\r\nRequested by: {requestedBy}",
                Html = "<p>This is a test email from the <strong>Coupon</strong> admin Settings page.</p>" +
                       $"<p>Sent at: {System.Web.HttpUtility.HtmlEncode(now)}<br/>Requested by: {System.Web.HttpUtility.HtmlEncode(requestedBy)}</p>",
                Tags = new List<string> { "coupon", "test" },
                IdempotencyKey = "coupon-settings-test-" + Guid.NewGuid().ToString("N")
            });
        }

        private static Dictionary<string, object> BuildPayload(RelayEmail email)
        {
            var payload = new Dictionary<string, object>
            {
                ["to"] = ToAddresses(email.To),
                ["subject"] = email.Subject,
                ["text"] = email.Text,
                ["html"] = email.Html,
                ["idempotencyKey"] = email.IdempotencyKey,
                ["template"] = string.IsNullOrWhiteSpace(email.Template) ? null : email.Template,
                ["variables"] = email.Variables,
                ["tags"] = email.Tags
            };

            var cc = ToAddresses(email.Cc);
            if (cc.Count > 0) payload["cc"] = cc;
            var bcc = ToAddresses(email.Bcc);
            if (bcc.Count > 0) payload["bcc"] = bcc;

            if (!string.IsNullOrWhiteSpace(email.ReplyTo))
            {
                payload["replyTo"] = new Dictionary<string, object> { ["address"] = email.ReplyTo.Trim() };
            }

            // From: optional - when empty the relay uses the From of the sender profile
            string fromAddress = (SettingsHelper.Get(SettingsHelper.KeyRelayFromAddress) ?? "").Trim();
            if (fromAddress.Length > 0)
            {
                var from = new Dictionary<string, object> { ["address"] = fromAddress };
                string fromName = (SettingsHelper.Get(SettingsHelper.KeyRelayFromName) ?? "").Trim();
                if (fromName.Length > 0) from["name"] = fromName;
                payload["from"] = from;
            }

            string profile = (SettingsHelper.Get(SettingsHelper.KeyRelaySenderProfile) ?? "").Trim();
            if (profile.Length > 0)
            {
                payload["senderProfile"] = profile;
            }

            // Leave out empty fields (NullValueHandling does not apply to dictionary entries)
            foreach (string key in payload.Where(p => p.Value == null).Select(p => p.Key).ToList())
            {
                payload.Remove(key);
            }

            return payload;
        }

        private static List<Dictionary<string, object>> ToAddresses(IEnumerable<string> addresses)
        {
            return (addresses ?? Enumerable.Empty<string>())
                .Where(a => !string.IsNullOrWhiteSpace(a))
                .Select(a => new Dictionary<string, object> { ["address"] = a.Trim() })
                .ToList();
        }

        private static RelayResult Execute(HttpMethod method, string path, string jsonBody)
        {
            var result = new RelayResult();
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                string baseUrl = BaseUrl;
                if (baseUrl.Length == 0)
                {
                    result.Error = "Email Relay Base URL is not set";
                    return result;
                }

                result.Url = baseUrl.TrimEnd('/') + path;
                var request = new HttpRequestMessage(method, result.Url);

                byte[] bodyBytes = jsonBody == null ? new byte[0] : Encoding.UTF8.GetBytes(jsonBody);
                if (jsonBody != null)
                {
                    // Send exactly the bytes that were signed
                    request.Content = new ByteArrayContent(bodyBytes);
                    request.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json") { CharSet = "utf-8" };
                }

                string apiKey = ApiKey;
                if (!string.IsNullOrEmpty(apiKey))
                {
                    request.Headers.Add("X-API-Key", apiKey);
                }

                string secret = SigningSecret;
                if (!string.IsNullOrEmpty(secret))
                {
                    // The relay signs Request.Path, which excludes any IIS application path in front of /api
                    string timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
                    string canonical = $"{timestamp}.{method.Method.ToUpperInvariant()}.{path}.{Sha256Hex(bodyBytes)}";
                    request.Headers.Add("X-Timestamp", timestamp);
                    request.Headers.Add("X-Signature", "sha256=" + HmacSha256Hex(secret, canonical));
                }

                int timeout = TimeoutSeconds;
                using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout)))
                {
                    HttpResponseMessage response = _httpClient.SendAsync(request, cts.Token).Result;
                    string body = response.Content.ReadAsStringAsync().Result ?? "";

                    result.StatusCode = (int)response.StatusCode;
                    result.ResponsePreview = body.Length > 1000 ? body.Substring(0, 1000) : body;

                    JObject envelope = null;
                    try
                    {
                        envelope = JObject.Parse(body);
                    }
                    catch (JsonException)
                    {
                        // not JSON (e.g. IIS 502 page)
                    }

                    if (envelope != null)
                    {
                        result.Success = response.IsSuccessStatusCode && envelope["success"]?.Type == JTokenType.Boolean && envelope["success"].Value<bool>();
                        result.Message = envelope["message"]?.ToString();
                        result.Data = envelope["data"];
                        if (!result.Success)
                        {
                            string error = envelope["error"]?.Type == JTokenType.String ? envelope["error"].ToString() : null;
                            result.Error = !string.IsNullOrEmpty(error) ? error : (result.Message ?? $"HTTP {result.StatusCode}");
                        }
                    }
                    else
                    {
                        result.Success = false;
                        result.Error = $"HTTP {result.StatusCode} {response.StatusCode} (response is not JSON)";
                    }
                }
            }
            catch (AggregateException ex) when (ex.InnerException is TaskCanceledException)
            {
                result.Error = $"Timeout: no response within {TimeoutSeconds} seconds";
            }
            catch (AggregateException ex)
            {
                Exception inner = ex.GetBaseException();
                result.Error = $"{inner.GetType().Name}: {inner.Message}";
            }
            catch (Exception ex)
            {
                result.Error = $"{ex.GetType().Name}: {ex.Message}";
            }
            finally
            {
                stopwatch.Stop();
                result.ElapsedMs = stopwatch.ElapsedMilliseconds;
            }

            return result;
        }

        private static string Sha256Hex(byte[] data)
        {
            using (var sha = SHA256.Create())
            {
                return ToHex(sha.ComputeHash(data));
            }
        }

        private static string HmacSha256Hex(string secret, string message)
        {
            using (var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret)))
            {
                return ToHex(hmac.ComputeHash(Encoding.UTF8.GetBytes(message)));
            }
        }

        private static string ToHex(byte[] bytes)
        {
            var sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
            {
                sb.Append(b.ToString("x2", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }
    }
}
