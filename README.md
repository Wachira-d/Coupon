# Honda CX Coupon System

> Complete Coupon Management System with Hybrid Authentication and Bulk Import

[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.7.2-blue)](https://dotnet.microsoft.com/)
[![Status](https://img.shields.io/badge/status-Production%20Ready-brightgreen)]()

---

## 📋 Overview

Honda CX Coupon System is a comprehensive web-based platform for digital coupon distribution, redemption, and management featuring:

### ✨ Key Features

- 🔐 **Hybrid Authentication** - Local cache + Active Directory via AuthenAPI
- 📥 **Bulk Import System** - CSV/Excel upload with smart column mapping
- 🛡️ **Enterprise Security** - SQL injection prevention, brute force protection, comprehensive audit trail
- 📊 **Admin Dashboard** - Real-time statistics and advanced reporting
- 🎫 **Multi-Brand Support** - Lotus, Central, Starbucks redemption pages
- 📴 **Offline Capability** - Works even when API unavailable (cache fallback)

---

## 🚀 Quick Start

### Prerequisites

```
✅ .NET Framework 4.7.2+
✅ IIS 7.5+
✅ SQL Server 2012+ OR PostgreSQL 10+
✅ Windows Server 2012+ / Windows 10+
```

### Installation (3 Steps)

#### 1. Database Setup
```sql
-- Run this script
Database/CreateUserCacheTable.sql
```

#### 2. Configure Web.config
```xml
<appSettings>
  <!-- CRITICAL: Update these -->
  <add key="AuthenApiBaseUrl" value="https://your-api.com/api" />
  <add key="PasswordSalt" value="YOUR-UNIQUE-SALT-CHANGE-THIS" />
  <add key="ConnectionString" value="..." />
</appSettings>
```

#### 3. Build & Deploy
```bash
# Visual Studio
Build → Build Solution (Ctrl+Shift+B)

# Deploy to IIS
- Copy bin/ + all .aspx files
- Set permissions (IIS_IUSRS)
- Test: /Admin/Login.aspx
```

**🎉 Done!** Access `/Admin/Login.aspx` to get started.

---

## 📁 Project Structure

```
/Coupon/
├── Admin/
│   ├── Login.aspx            # 🔐 Authentication page
│   ├── Logout.aspx           # 🚪 Logout page
│   ├── Report.aspx           # 📊 Admin dashboard (protected)
│   └── ImportCoupons.aspx    # 📥 Bulk import (protected)
│
├── Helpers/
│   ├── AuthenticationHelper.cs  # Hybrid authentication
│   ├── DatabaseHelper.cs        # Data access layer
│   ├── ErrorLogger.cs           # Error & audit logging
│   ├── InputValidator.cs        # Input sanitization
│   ├── SessionManager.cs        # Session management
│   └── AuditLogger.cs           # Audit trail
│
├── Database/
│   └── CreateUserCacheTable.sql # Database schema
│
├── Lotus.aspx, Central.aspx, Starbucks.aspx  # 🎫 Redemption pages
│
└── Documentation/
    ├── LOGIN_SYSTEM_GUIDE.md       # Authentication guide
    ├── IMPORT_SYSTEM_GUIDE.md      # Import system guide
    ├── DEPLOYMENT_CHECKLIST.md     # Deployment steps
    └── README.md                   # This file
```

---

## 🔐 Authentication System

### Hybrid Authentication Flow

```
User Login
    ↓
Check Local Cache (90-day validity)
    ├─ Valid + Password Match → ✅ Fast Local Authentication
    └─ Invalid/Expired → Call AuthenAPI
                            ↓
                         AD Verification
                            ↓
                         Update Cache
                            ↓
                      Create Session (8hr)
```

### Security Features

- ✅ SHA256 + Salt password hashing (no plain text storage)
- ✅ Brute force protection (5 attempts → 15 min lockout)
- ✅ Session timeout (8 hours, configurable)
- ✅ Comprehensive audit logging (all login attempts)
- ✅ API fallback (works offline with valid cache)
- ✅ SQL injection prevention (parameterized queries)
- ✅ XSS protection (input encoding)
- ✅ CSRF protection tokens
- ✅ Open redirect prevention

**📖 Full Guide:** [LOGIN_SYSTEM_GUIDE.md](LOGIN_SYSTEM_GUIDE.md)

---

## 📥 Bulk Import System

### Import Workflow

1. **Upload File** - CSV or Excel (.xlsx)
2. **Preview Data** - View first 10 rows
3. **Map Columns** - Auto-detect or manual selection
4. **Set Properties** - Batch value & expiration date
5. **Import** - Process with duplicate detection
6. **Results** - Success/Error/Duplicate counts

### Supported Formats

- ✅ **CSV** - Always supported (built-in)
- ✅ **Excel** - .xlsx, .xls (requires EPPlus)

### Example CSV

```csv
CouponID,BarcodeCode,Notes
ABC001,1234567890123,Campaign 2025
ABC002,1234567890124,Campaign 2025
ABC003,1234567890125,Campaign 2025
```

### Limits

- **File Size:** 10MB max
- **Rows:** 10,000 per import
- **Features:** Duplicate detection, error handling, batch operations

**📖 Full Guide:** [IMPORT_SYSTEM_GUIDE.md](IMPORT_SYSTEM_GUIDE.md)

---

## 📊 Admin Dashboard

### Features

- 📈 **Dashboard Stats** - Total/Used/Sent coupon counts
- 🔍 **Advanced Search** - Filter by ID, date, status
- 📋 **Logs Viewer** - Access logs with filters
- 👤 **User Info** - Current user display
- 🔗 **Quick Links** - Import, Logout navigation

### Access

1. Login at `/Admin/Login.aspx`
2. Dashboard at `/Admin/Report.aspx`
3. Import at `/Admin/ImportCoupons.aspx`

**All admin pages require authentication** - automatic redirect to login if not authenticated.

---

## 🎫 Coupon Redemption

### Customer Flow

1. Customer receives link: `/Lotus.aspx?id=ABC001`
2. View coupon details and barcode
3. Click "กดใช้สิทธิ์" (Use Coupon)
4. Barcode generated for checkout
5. Coupon marked as used in database

### Supported Brands

- 🛒 **Lotus** - `/Lotus.aspx`
- 🏬 **Central** - `/Central.aspx`
- ☕ **Starbucks** - `/Starbucks.aspx`

---

## ⚙️ Configuration

### Essential Settings

```xml
<!-- Web.config -->
<appSettings>
  <!-- Authentication API -->
  <add key="AuthenApiBaseUrl" value="https://api.company.com/api" />
  <add key="AuthenApiTimeout" value="30" />

  <!-- Security -->
  <add key="PasswordSalt" value="YOUR-UNIQUE-32-CHAR-SALT" />
  <add key="MaxFailedAttempts" value="5" />
  <add key="LockoutDurationMinutes" value="15" />
  <add key="SessionTimeoutHours" value="8" />

  <!-- Cache -->
  <add key="CacheDurationDays" value="90" />

  <!-- Database -->
  <add key="ConnectionString" value="Server=...;Database=Coupon;..." />
  <add key="DatabaseType" value="sqlserver" /> <!-- or "postgresql" -->

  <!-- Paths -->
  <add key="Path" value="C:\inetpub\wwwroot\Coupon" />
  <add key="LogPath" value="C:\inetpub\wwwroot\Coupon\log.txt" />
</appSettings>
```

**⚠️ Security:**
- **MUST** change `PasswordSalt` to unique value
- **MUST** use HTTPS in production
- **MUST** secure connection strings

---

## 📚 Documentation

| Document | Description |
|----------|-------------|
| [LOGIN_SYSTEM_GUIDE.md](LOGIN_SYSTEM_GUIDE.md) | Complete authentication guide |
| [IMPORT_SYSTEM_GUIDE.md](IMPORT_SYSTEM_GUIDE.md) | Bulk import & Excel setup |
| [DEPLOYMENT_CHECKLIST.md](DEPLOYMENT_CHECKLIST.md) | Deployment step-by-step |
| [AuthenticationConfigSample.txt](AuthenticationConfigSample.txt) | Config examples |
| [SECURITY_ANALYSIS.md](SECURITY_ANALYSIS.md) | Security review |

---

## 🐛 Troubleshooting

### "Authentication service unavailable"

**Solution:** Check `AuthenApiBaseUrl` in Web.config. System will use cache fallback if available.

### "Account temporarily locked"

**Solution:** Wait 15 minutes or run:
```sql
EXEC sp_ResetFailedAttempts @Username = 'user.name'
```

### Excel import fails

**Solution:** Install EPPlus (run `InstallEPPlus.ps1`) or convert to CSV.

### Session expires quickly

**Solution:** Increase `SessionTimeoutHours` in Web.config.

**📖 More:** See [DEPLOYMENT_CHECKLIST.md](DEPLOYMENT_CHECKLIST.md) troubleshooting section.

---

## 🔒 Security

### Implemented Protections

✅ SQL Injection Prevention (parameterized queries)
✅ XSS Protection (input encoding/validation)
✅ CSRF Protection (token validation)
✅ Brute Force Protection (rate limiting)
✅ Session Security (timeout, regeneration)
✅ Secure Password Storage (SHA256 + Salt)
✅ Comprehensive Audit Trail
✅ Input Validation & Sanitization

### Compliance

- OWASP Top 10 mitigation
- Secure credential storage
- Complete audit logging
- Regular security reviews

**📖 Details:** [SECURITY_ANALYSIS.md](SECURITY_ANALYSIS.md)

---

## 💻 System Requirements

### Server
- Windows Server 2012 R2+ / Windows 10+
- IIS 7.5+
- .NET Framework 4.7.2+
- SQL Server 2012+ OR PostgreSQL 10+
- 4GB+ RAM, 10GB+ disk

### Client
- Modern browser (Chrome 90+, Firefox 88+, Edge 90+, Safari 14+)
- JavaScript enabled
- Cookies enabled

---

## 📝 Database Tables

### Core Tables

| Table | Purpose |
|-------|---------|
| `Coupon` | Coupon data (ID, code, value, expiration, status) |
| `User_Cache` | Cached AD credentials and user info |
| `Login_Audit` | Login attempt logs |
| `ErrorLogs` | Application error logs |
| `Logs` | Access logs |
| `Admin_Logs` | Admin activity logs |

### Stored Procedures

- `sp_GetUserCache` - Retrieve cached user
- `sp_UpsertUserCache` - Update/insert cache
- `sp_UpdateLastLogin` - Update login timestamp
- `sp_RecordFailedLogin` - Record failed attempt
- `sp_ResetFailedAttempts` - Reset lockout
- `sp_LogLoginAttempt` - Audit logging
- `sp_CleanupExpiredCache` - Maintenance

---

## 🚀 Deployment

### Quick Deployment

1. **Database**
   ```sql
   RUN: Database/CreateUserCacheTable.sql
   ```

2. **Configure**
   - Update Web.config (API URL, Salt, ConnectionString)

3. **Build**
   ```bash
   Build → Build Solution
   ```

4. **Deploy**
   - Copy to IIS directory
   - Set permissions
   - Test login

**📖 Complete Guide:** [DEPLOYMENT_CHECKLIST.md](DEPLOYMENT_CHECKLIST.md)

---

## 🔄 Version History

| Version | Date | Changes |
|---------|------|---------|
| **1.0.0** | Dec 2025 | ✅ Production ready release |
| | | - Hybrid authentication system |
| | | - Bulk import (CSV/Excel) |
| | | - Security hardening |
| | | - User navigation & logout |
| | | - Complete documentation |

---

## 📞 Support

**Documentation:** See [Documentation](#-documentation) section above

**Error Logs:** `/Admin/ViewErrorLogs.aspx` (requires login)

**Database Logs:**
```sql
-- Recent errors
SELECT TOP 100 * FROM ErrorLogs
WHERE LogLevel = 'ERROR'
ORDER BY CreatedDateTime DESC

-- Failed logins
SELECT * FROM Login_Audit
WHERE Success = 0
ORDER BY LoginTime DESC
```

---

## 🙏 Technologies

- ASP.NET Web Forms 4.7.2
- SQL Server / PostgreSQL
- IIS
- Newtonsoft.Json
- BarcodeLib
- EPPlus (optional, for Excel)
- Bootstrap 3

---

## 📄 License

Proprietary - All Rights Reserved

Copyright © 2025 Honda CX Coupon System

---

**Made with ❤️ for Honda CX**

*Last Updated: December 2025*
*Status: Production Ready*
