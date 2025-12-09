# Deployment Checklist - Honda CX Coupon System

## Version: Production Ready v1.0
## Date: December 2025

---

## ✅ Pre-Deployment Checklist

### 1. Database Setup

- [ ] **Run Database Scripts**
  ```sql
  -- 1. User Cache and Login System
  Database/CreateUserCacheTable.sql

  -- 2. Error Logs (if not already created)
  -- Check if ErrorLogs table exists first
  ```

- [ ] **Verify Tables Created**
  ```sql
  SELECT * FROM INFORMATION_SCHEMA.TABLES
  WHERE TABLE_NAME IN ('User_Cache', 'Login_Audit', 'ErrorLogs')
  ```

- [ ] **Test Stored Procedures**
  ```sql
  -- Test user cache operations
  EXEC sp_GetUserCache @Username = 'testuser'
  ```

### 2. Configuration Updates

- [ ] **Update Web.config**
  ```xml
  <!-- CRITICAL: Update these values -->
  <add key="AuthenApiBaseUrl" value="https://YOUR-ACTUAL-API.com/api" />
  <add key="PasswordSalt" value="YOUR-UNIQUE-SALT-CHANGE-THIS-NOW" />

  <!-- Optional: Adjust these if needed -->
  <add key="CacheDurationDays" value="90" />
  <add key="MaxFailedAttempts" value="5" />
  <add key="LockoutDurationMinutes" value="15" />
  <add key="SessionTimeoutHours" value="8" />
  ```

- [ ] **Generate Unique Salt**
  ```powershell
  # PowerShell command to generate random salt
  -join ((65..90) + (97..122) + (48..57) | Get-Random -Count 32 | % {[char]$_})
  ```

- [ ] **Update Connection String** (if changed)

### 3. Build and Compile

- [ ] **Build Solution**
  - Visual Studio: Build → Build Solution (Ctrl+Shift+B)
  - Check for compilation errors
  - Verify 0 errors, 0 warnings

- [ ] **Check References**
  - Verify all NuGet packages restored
  - Check Newtonsoft.Json (for JSON handling)
  - Check System.Net.Http (for API calls)

### 4. File Deployment

- [ ] **Deploy Files to Server**
  ```
  Required Files:
  ├── Coupon/Helpers/AuthenticationHelper.cs
  ├── Coupon/Admin/Login.aspx + .cs + .designer.cs
  ├── Coupon/Admin/Logout.aspx + .cs + .designer.cs
  ├── Coupon/Admin/ImportCoupons.aspx + .cs + .designer.cs
  ├── Coupon/Admin/Report.aspx.cs (modified)
  └── Web.config (updated)
  ```

- [ ] **Deploy bin/ Directory**
  - Copy entire bin/ folder with compiled DLLs

### 5. IIS Configuration

- [ ] **Application Pool Settings**
  - .NET Framework version: 4.7.2
  - Managed Pipeline Mode: Integrated
  - Identity: ApplicationPoolIdentity or specific service account

- [ ] **Enable Session State**
  - Session Mode: InProc or StateServer
  - Timeout: Match SessionTimeoutHours config

- [ ] **SSL/HTTPS** (Recommended)
  - Install SSL certificate
  - Force HTTPS redirect
  - Update AuthenApiBaseUrl to use HTTPS

### 6. Permissions

- [ ] **Folder Permissions**
  ```
  Grant IIS_IUSRS and NETWORK SERVICE permissions:
  - /Logs - Write access
  - /AuditLogs - Write access
  - /Barcode - Write access
  ```

- [ ] **Database Permissions**
  - Application user has:
    - SELECT, INSERT, UPDATE on User_Cache
    - SELECT, INSERT on Login_Audit
    - EXECUTE on all stored procedures

---

## 🧪 Testing Checklist

### 1. Authentication Tests

- [ ] **First-Time Login**
  1. Navigate to `/Admin/Login.aspx`
  2. Enter valid AD credentials
  3. Verify successful login
  4. Check User_Cache table populated
  5. Check Login_Audit table logged success

- [ ] **Return Login (Cache Hit)**
  1. Logout
  2. Login again with same credentials
  3. Should be fast (cache hit)
  4. Check Login_Audit shows "cache" as AuthSource

- [ ] **Invalid Credentials**
  1. Enter wrong password
  2. Verify error message shown
  3. Check Login_Audit logged failure
  4. Check FailedAttempts incremented in User_Cache

- [ ] **Account Lockout**
  1. Fail login 5 times
  2. Verify account locked
  3. Try login again - should show lockout message
  4. Wait 15 minutes or run: `EXEC sp_ResetFailedAttempts @Username = 'testuser'`
  5. Verify can login again

- [ ] **Session Validation**
  1. Login successfully
  2. Navigate to `/Admin/Report.aspx`
  3. Verify access granted (no redirect)
  4. Clear session or wait for timeout
  5. Refresh page
  6. Verify redirect to login

### 2. Protected Pages Tests

- [ ] **Report Page**
  1. Without login: Navigate to `/Admin/Report.aspx`
  2. Verify redirect to `/Admin/Login.aspx?ReturnUrl=...`
  3. Login successfully
  4. Verify redirect back to Report page

- [ ] **Import Page**
  1. Without login: Navigate to `/Admin/ImportCoupons.aspx`
  2. Verify redirect to login
  3. Login successfully
  4. Verify redirect back to Import page

### 3. Import System Tests

- [ ] **CSV Upload**
  1. Login to system
  2. Navigate to `/Admin/ImportCoupons.aspx`
  3. Upload `sample_coupon_import.csv`
  4. Verify preview shows data
  5. Check column mapping auto-detected
  6. Enter Value and Expiration Date
  7. Click Import
  8. Verify success message
  9. Check database for imported coupons

- [ ] **Excel Upload** (if EPPlus installed)
  1. Create Excel file with sample data
  2. Upload .xlsx file
  3. Verify same flow as CSV

- [ ] **Duplicate Handling**
  1. Import same file twice
  2. Verify duplicates skipped
  3. Check results show duplicate count

### 4. API Integration Tests

- [ ] **API Connectivity**
  1. Verify AuthenApiBaseUrl is correct
  2. Test API endpoint manually (Postman/curl)
  3. Verify SSL certificate valid

- [ ] **API Timeout Handling**
  1. Temporarily make API unreachable (firewall/disconnect)
  2. Try login with valid cached user
  3. Verify cache fallback works
  4. Verify error logged

- [ ] **Password Change Detection**
  1. Login successfully (cache created)
  2. Change password in AD
  3. Login with new password
  4. Verify system detects change
  5. Verify cache updated

### 5. Security Tests

- [ ] **SQL Injection**
  - Try: `admin' OR '1'='1` as username
  - Verify rejected/sanitized

- [ ] **XSS Attack**
  - Try: `<script>alert('xss')</script>` in login fields
  - Verify encoded/sanitized

- [ ] **Session Fixation**
  - Verify session ID changes after login

- [ ] **Open Redirect**
  - Try: `/Admin/Login.aspx?ReturnUrl=http://evil.com`
  - Verify only local URLs accepted

---

## 📊 Monitoring Checklist

### 1. Database Monitoring

- [ ] **Check Login Audit Regularly**
  ```sql
  -- Recent failed logins
  SELECT TOP 50 * FROM Login_Audit
  WHERE Success = 0
  ORDER BY LoginTime DESC

  -- Locked accounts
  SELECT Username, FailedAttempts, LastFailedLogin, IsLocked
  FROM User_Cache
  WHERE IsLocked = 1
  ```

- [ ] **Check Error Logs**
  ```sql
  SELECT TOP 100 * FROM ErrorLogs
  WHERE LogLevel = 'ERROR'
  ORDER BY CreatedDateTime DESC
  ```

### 2. Performance Monitoring

- [ ] **Cache Hit Rate**
  ```sql
  SELECT
      AuthSource,
      COUNT(*) as Count,
      COUNT(*) * 100.0 / (SELECT COUNT(*) FROM Login_Audit) as Percentage
  FROM Login_Audit
  WHERE LoginTime >= DATEADD(DAY, -7, GETDATE())
  GROUP BY AuthSource
  ```

- [ ] **Session Metrics**
  - Average session duration
  - Active sessions count

### 3. Alerts Setup

- [ ] **Failed Login Alerts**
  - Alert if > 10 failed logins per minute
  - Alert if account locked

- [ ] **API Health**
  - Monitor API response time
  - Alert if > 50% API errors

---

## 🔄 Maintenance Tasks

### Daily

- [ ] Check Login_Audit for unusual activity
- [ ] Check ErrorLogs for critical errors
- [ ] Monitor disk space for log files

### Weekly

- [ ] Review locked accounts
- [ ] Check cache hit rate
- [ ] Review API error rate

### Monthly

- [ ] Clean up old audit logs (>90 days)
  ```sql
  DELETE FROM Login_Audit
  WHERE LoginTime < DATEADD(DAY, -90, GETDATE())
  ```

- [ ] Clean up expired cache
  ```sql
  EXEC sp_CleanupExpiredCache @DaysToKeepExpired = 30
  ```

- [ ] Review and update documentation

---

## 🆘 Troubleshooting Guide

### Issue: "Authentication service unavailable"

**Symptoms:** Users can't login, error message shown

**Diagnosis:**
```sql
-- Check recent API errors in logs
SELECT * FROM ErrorLogs
WHERE Message LIKE '%API%'
ORDER BY CreatedDateTime DESC
```

**Solutions:**
1. Check AuthenApiBaseUrl in Web.config
2. Test API manually: `curl https://your-api.com/api/ldap/test-connection`
3. Check network connectivity
4. Verify SSL certificate
5. Check if API is down (contact API team)

**Workaround:**
- If users have valid cache, they can still login
- System will use cache fallback automatically

### Issue: Account locked unexpectedly

**Symptoms:** User reports account locked without trying to login

**Diagnosis:**
```sql
-- Check user's recent login attempts
SELECT * FROM Login_Audit
WHERE Username = 'affected.user'
ORDER BY LoginTime DESC
```

**Solutions:**
1. Check if actual failed attempts (wrong password)
2. Check if bot/brute force attack
3. Reset manually:
   ```sql
   EXEC sp_ResetFailedAttempts @Username = 'affected.user'
   ```

### Issue: Session expires too quickly

**Symptoms:** Users logged out after short time

**Diagnosis:**
- Check SessionTimeoutHours in Web.config
- Check IIS Session State timeout
- Check Application Pool recycle settings

**Solutions:**
1. Increase SessionTimeoutHours in Web.config
2. Increase Session timeout in IIS
3. Adjust Application Pool recycle schedule

### Issue: Import fails with large files

**Symptoms:** Timeout or memory error during import

**Diagnosis:**
- Check file size
- Check row count

**Solutions:**
1. Split file into smaller batches (<1000 rows)
2. Increase upload timeout in Web.config
3. Increase maxRequestLength in Web.config

---

## 📝 Rollback Plan

### If Issues Occur After Deployment

1. **Restore Previous Code**
   - Revert to previous build
   - Restore Web.config

2. **Database Rollback** (if needed)
   - Don't delete new tables (keep for next attempt)
   - Restore previous stored procedures if modified

3. **Re-enable Old Login System** (if critical)
   - Comment out new authentication check in Report.aspx.cs:
     ```csharp
     // Temporarily disable new auth
     /*
     if (!AuthenticationHelper.ValidateSession(Session))
     {
         Response.Redirect("~/Admin/Login.aspx");
         return;
     }
     */
     ```

---

## ✅ Post-Deployment Verification

### Immediate (Within 1 hour)

- [ ] Test login flow end-to-end
- [ ] Verify all protected pages redirect properly
- [ ] Check first login creates cache
- [ ] Check logs are being written

### First Day

- [ ] Monitor error logs hourly
- [ ] Check user feedback
- [ ] Verify cache is working (fast logins)
- [ ] Monitor API call rate

### First Week

- [ ] Review all login attempts
- [ ] Check for any failed imports
- [ ] Verify session timeout working
- [ ] Check lockout mechanism working

---

## 📞 Support Contacts

**System Owner:** [Your Name]
**Database Admin:** [DBA Contact]
**API Team:** [API Support Contact]
**IIS Admin:** [IIS Admin Contact]

---

## 📚 Documentation Links

- **Login System Guide:** `LOGIN_SYSTEM_GUIDE.md`
- **Import System Guide:** `IMPORT_SYSTEM_GUIDE.md`
- **Configuration Sample:** `AuthenticationConfigSample.txt`
- **Database Scripts:** `Database/CreateUserCacheTable.sql`

---

## ✨ Success Criteria

System is considered successfully deployed when:

✅ All users can login with AD credentials
✅ Cache system working (fast return logins)
✅ Protected pages require authentication
✅ Import system functional
✅ No critical errors in logs
✅ API integration working
✅ Session management working
✅ Audit logging active

---

*Deployment Checklist v1.0 - December 2025*
*Honda CX Coupon System - Hybrid Authentication*
