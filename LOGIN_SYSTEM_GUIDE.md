# Login System Guide - Honda CX Coupon System

## Overview

ระบบ Login แบบ Hybrid Authentication ที่ใช้ Local Cache + AuthenAPI สำหรับตรวจสอบ credentials กับ Active Directory

## Features

✅ **Hybrid Authentication** - Local Cache + AD API Verification
✅ **Offline Capability** - Login ได้แม้ API ขัดข้อง (ใช้ cache)
✅ **Auto-Sync** - Sync กับ AD อัตโนมัติตามกำหนด
✅ **Brute Force Protection** - Lock account หลังจาก failed attempts
✅ **Session Management** - Session timeout และ security
✅ **Audit Logging** - บันทึก login/logout ทุกครั้ง
✅ **Remember Me** - จดจำ username เป็นเวลา 30 วัน

---

## Installation Guide

### 1. Database Setup

รัน SQL script เพื่อสร้างตารางที่จำเป็น:

```bash
# รันใน SQL Server Management Studio หรือ Azure Data Studio
Database/CreateUserCacheTable.sql
```

สิ่งที่จะถูกสร้าง:
- **User_Cache** table - เก็บ cached credentials และ user info
- **Login_Audit** table - บันทึก login attempts
- **Stored Procedures** - sp_GetUserCache, sp_UpsertUserCache, etc.

### 2. Web.config Configuration

เพิ่ม settings ลงใน `Web.config` → `<appSettings>` section:

```xml
<!-- Authentication API Settings -->
<add key="AuthenApiBaseUrl" value="https://your-authen-api.com/api" />
<add key="AuthenApiTimeout" value="30" />

<!-- Cache Settings -->
<add key="CacheDurationDays" value="90" />
<add key="PasswordSalt" value="YOUR_UNIQUE_SALT_CHANGE_THIS" />

<!-- Security Settings -->
<add key="MaxFailedAttempts" value="5" />
<add key="LockoutDurationMinutes" value="15" />
<add key="SessionTimeoutHours" value="8" />
```

⚠️ **สำคัญ!** เปลี่ยน `PasswordSalt` เป็นค่าที่ unique สำหรับระบบของคุณ

### 3. Project Files

ตรวจสอบว่าไฟล์ทั้งหมดถูก add เข้า project แล้ว:

**Database:**
- `Database/CreateUserCacheTable.sql`

**Helpers:**
- `Coupon/Helpers/AuthenticationHelper.cs`

**Admin Pages:**
- `Coupon/Admin/Login.aspx`
- `Coupon/Admin/Login.aspx.cs`
- `Coupon/Admin/Login.aspx.designer.cs`
- `Coupon/Admin/Logout.aspx`
- `Coupon/Admin/Logout.aspx.cs`
- `Coupon/Admin/Logout.aspx.designer.cs`

**Modified Files:**
- `Coupon/Admin/Report.aspx.cs` - เพิ่ม authentication check
- `Coupon/Admin/ImportCoupons.aspx.cs` - เพิ่ม authentication check

### 4. Build and Test

```bash
# Build project
dotnet build

# หรือใน Visual Studio
Build → Build Solution (Ctrl+Shift+B)
```

---

## Usage

### Login Flow

1. **User Access Admin Page**
   - เข้า `/Admin/Report.aspx` หรือ `/Admin/ImportCoupons.aspx`
   - ถ้ายังไม่ login → redirect ไป `/Admin/Login.aspx`

2. **User Login**
   - ใส่ Username + Password
   - เลือก "Remember me" (optional)
   - คลิก "Login"

3. **Authentication Process**
   ```
   IF user in cache AND cache valid AND password match:
       → Authenticate locally (fast)
   ELSE:
       → Call AuthenAPI for AD verification
       → Update cache
       → Create session
   ```

4. **Redirect to Original Page**
   - หลัง login สำเร็จ → redirect กลับไปหน้าเดิม
   - หรือไป default page (Report.aspx)

### Logout

```
/Admin/Logout.aspx
```

- Clear session
- Clear cookies
- Log logout event
- Redirect to login page

---

## Authentication Flow Diagram

```
┌─────────────────────┐
│  User Login Request │
└──────────┬──────────┘
           │
           ▼
    ┌──────────────┐
    │ Check Cache  │
    └──────┬───────┘
           │
     ┌─────┴─────┐
     │           │
  VALID      INVALID/EXPIRED
     │           │
     ▼           ▼
┌─────────┐  ┌──────────────┐
│ Verify  │  │  Call API    │
│ Locally │  │  Authenticate│
└────┬────┘  └──────┬───────┘
     │              │
     └──────┬───────┘
            │
            ▼
    ┌───────────────┐
    │ Create Session│
    └───────┬───────┘
            │
            ▼
    ┌───────────────┐
    │   Redirect    │
    └───────────────┘
```

---

## Security Features

### 1. Password Hashing
- SHA256 + Salt
- Password ไม่ถูกเก็บแบบ plain text
- Salt แยกต่างหากจาก password hash

### 2. Brute Force Protection
```
Failed Attempts → Lockout
5 attempts      → 15 minutes lock
```

### 3. Session Security
- Session timeout (default 8 hours)
- Session validation ทุกครั้งที่เข้าหน้า
- Session regeneration หลัง login

### 4. Audit Logging
- บันทึก login attempts (success/fail)
- บันทึก IP address และ User Agent
- บันทึก auth source (api/cache/error)

### 5. API Fallback
- ถ้า API ขัดข้อง → ใช้ cache (ถ้ามี)
- Graceful degradation
- Error logging

---

## Configuration Options

### Cache Duration
```xml
<add key="CacheDurationDays" value="90" />
```
- ระยะเวลาที่ cache valid (default: 90 days)
- หลังหมดอายุ → re-verify กับ AD
- แนะนำ: 30-90 days

### Session Timeout
```xml
<add key="SessionTimeoutHours" value="8" />
```
- ระยะเวลาที่ session valid (default: 8 hours)
- หลัง timeout → ต้อง login ใหม่
- แนะนำ: 4-12 hours

### Failed Attempts
```xml
<add key="MaxFailedAttempts" value="5" />
<add key="LockoutDurationMinutes" value="15" />
```
- จำนวน attempts ก่อน lock (default: 5)
- ระยะเวลา lock (default: 15 minutes)
- แนะนำ: 3-5 attempts, 15-30 minutes

---

## API Integration

### Authentication Endpoint

**URL:** `POST /ldap/authenticate`

**Request:**
```json
{
  "username": "john.doe",
  "password": "user_password"
}
```

**Response (Success):**
```json
{
  "success": true,
  "message": "Authentication successful",
  "data": {
    "samAccountName": "john.doe",
    "displayName": "John Doe",
    "email": "john.doe@company.com",
    "department": "IT",
    "title": "Developer",
    "groups": ["IT Staff", "Developers"],
    "isEnabled": true
  }
}
```

**Response (Failed):**
```json
{
  "success": false,
  "message": "Authentication failed. Invalid username or password."
}
```

---

## Database Schema

### User_Cache Table

| Column | Type | Description |
|--------|------|-------------|
| Id | INT | Primary key |
| Username | NVARCHAR(100) | AD username |
| PasswordHash | NVARCHAR(256) | SHA256 hash |
| Salt | NVARCHAR(64) | Password salt |
| DisplayName | NVARCHAR(200) | Full name |
| Email | NVARCHAR(200) | Email address |
| Department | NVARCHAR(100) | Department |
| Title | NVARCHAR(100) | Job title |
| Groups | NVARCHAR(MAX) | JSON array of groups |
| IsEnabled | BIT | Account enabled |
| IsLocked | BIT | Account locked |
| FailedAttempts | INT | Failed login count |
| LastLogin | DATETIME2 | Last successful login |
| LastAdSync | DATETIME2 | Last AD verification |
| CacheExpires | DATETIME2 | Cache expiration |

### Login_Audit Table

| Column | Type | Description |
|--------|------|-------------|
| LogId | BIGINT | Primary key |
| Username | NVARCHAR(100) | Username |
| LoginTime | DATETIME2 | Login timestamp |
| Success | BIT | Success/Fail |
| AuthSource | NVARCHAR(20) | api/cache/error |
| FailureReason | NVARCHAR(500) | Reason for failure |
| IpAddress | NVARCHAR(50) | Client IP |
| UserAgent | NVARCHAR(500) | Browser info |

---

## Troubleshooting

### Problem: "Authentication service unavailable"

**Cause:** API ไม่สามารถเชื่อมต่อได้

**Solution:**
1. ตรวจสอบ `AuthenApiBaseUrl` ใน Web.config
2. ตรวจสอบ network connectivity
3. ตรวจสอบ API logs
4. ถ้ามี valid cache → ระบบจะใช้ cache แทน

### Problem: "Account temporarily locked"

**Cause:** Failed login attempts มากเกินไป

**Solution:**
1. รอ lockout duration (default 15 minutes)
2. หรือ admin reset: `EXEC sp_ResetFailedAttempts @Username = 'username'`

### Problem: "Password changed but can't login"

**Cause:** Cache ยังเป็นรหัสเดิม

**Solution:**
- ระบบจะตรวจจับ password mismatch
- จะ call API ทันที
- Cache จะถูก update อัตโนมัติ

### Problem: Session expired quickly

**Cause:** `SessionTimeoutHours` ต่ำเกินไป

**Solution:**
- เพิ่มค่า `SessionTimeoutHours` ใน Web.config
- Rebuild และ restart application

---

## Maintenance

### Clean up expired cache

```sql
-- Manual cleanup
EXEC sp_CleanupExpiredCache @DaysToKeepExpired = 30;

-- Or setup as SQL Agent Job (weekly)
```

### View login audit logs

```sql
-- Recent logins
SELECT TOP 100 *
FROM Login_Audit
ORDER BY LoginTime DESC;

-- Failed logins
SELECT *
FROM Login_Audit
WHERE Success = 0
ORDER BY LoginTime DESC;

-- Locked accounts
SELECT Username, FailedAttempts, LastFailedLogin
FROM User_Cache
WHERE IsLocked = 1;
```

### Reset user's failed attempts

```sql
EXEC sp_ResetFailedAttempts @Username = 'john.doe';
```

---

## Best Practices

1. **PasswordSalt**
   - ใช้ค่า unique สำหรับแต่ละ environment
   - อย่าใช้ default value
   - เก็บไว้เป็นความลับ

2. **HTTPS**
   - ใช้ HTTPS สำหรับ production
   - ตรวจสอบ SSL certificate

3. **API Endpoint**
   - ตั้งค่า API URL ที่ถูกต้อง
   - ตรวจสอบ connectivity

4. **Backup**
   - Backup User_Cache table
   - Backup Login_Audit logs

5. **Monitoring**
   - ตรวจสอบ failed login attempts
   - ตรวจสอบ locked accounts
   - ตรวจสอบ API errors

---

## Migration from Old System

### Step 1: Deploy New System

1. Run database script
2. Update Web.config
3. Build and deploy

### Step 2: Test

1. Test login with AD credentials
2. Test session management
3. Test logout
4. Test Report and Import pages

### Step 3: Remove Old Code (Optional)

Old login code in Report.aspx.cs:
- `Button1_Click` (LDAP validation)
- TextBox1, TextBox13 controls

These are now bypassed by session check in `Page_Load`.

---

## Support

### Log Files

- **Application Logs:** ErrorLogger.cs
- **Audit Logs:** AuditLogger.cs
- **Database Logs:** Login_Audit table

### Debug Mode

Set breakpoints:
- `AuthenticationHelper.Authenticate()`
- `Login.aspx.cs → btnLogin_Click()`
- `Report.aspx.cs → Page_Load()`

---

## Summary

✅ Hybrid authentication system deployed
✅ Secure password hashing with salt
✅ Brute force protection enabled
✅ Session management implemented
✅ Audit logging active
✅ API fallback mechanism ready

**Next Steps:**
1. Update `AuthenApiBaseUrl` with actual API endpoint
2. Change `PasswordSalt` to unique value
3. Test login flow end-to-end
4. Monitor logs for issues

---

*Document Version: 1.0*
*Last Updated: December 2025*
*System: Honda CX Coupon - Admin Authentication*
