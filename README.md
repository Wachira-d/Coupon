# Coupon Management System

ASP.NET Web Forms application for e-voucher/coupon redemption across Central Group retail brands (Central, Lotus, Starbucks).

## Quick Start

### Prerequisites
- .NET Framework 4.7.2
- SQL Server OR PostgreSQL
- IIS / IIS Express

### Configuration
1. Update `Web.config`:
   - Set `DatabaseType` (PostgreSQL or MSSQL)
   - Configure connection strings (**SECURITY WARNING:** See below)
   - Set `Path` for barcode storage
   - Configure `LogPath` for error logs (optional)

2. Database Setup:
   - Create database: `Coupon_Honda`
   - Tables: `Coupon`, `Logs`, `Admin_Logs`

3. Run:
   - Open `Coupon.sln` in Visual Studio
   - Press F5 to run

## Project Structure

```
/Coupon/
├── *.aspx              # Public coupon pages (Central, Lotus, Starbucks)
├── Admin/
│   └── Report.aspx     # Admin dashboard
├── Helpers/            # NEW: Security helper classes
│   ├── DatabaseHelper.cs    # Parameterized queries
│   ├── ErrorLogger.cs       # Error logging
│   └── InputValidator.cs    # Input validation
├── Images/             # Brand images
├── Content/            # CSS files
├── Scripts/            # JavaScript files
└── Web.config          # Configuration
```

## Security Improvements (2025-12-08)

### ✅ Implemented
- **SQL Injection Prevention:** Parameterized queries in Central.aspx.cs
- **Input Validation:** Alphanumeric-only coupon IDs, length limits
- **Error Logging:** File-based logging system (App_Data/Logs)
- **Path Traversal Protection:** Safe file path validation
- **Code Quality:** Removed empty catch blocks

### ⚠️ Critical Warnings

**🔴 SECURITY ALERT: Hardcoded Credentials**

The `Web.config` file contains **exposed database credentials**:
- SQL Server: `admin / Racc00n@@306`
- PostgreSQL: `postgres / Spider@@306`
- Server IP: `192.168.80.31`

**IMMEDIATE ACTIONS REQUIRED:**
1. **DO NOT** commit real credentials to version control
2. Migrate to secure storage:
   - Azure Key Vault (recommended for Azure)
   - AWS Secrets Manager (recommended for AWS)
   - Environment variables (local development)
3. Rotate all exposed passwords
4. Remove credentials from Web.config after migration

### ⚠️ Pending Security Tasks

**High Priority:**
- [ ] Apply security fixes to Lotus.aspx.cs, Starbucks.aspx.cs
- [ ] Update Admin/Report.aspx.cs with parameterized queries
- [ ] Implement CSRF protection
- [ ] Migrate credentials to secure storage
- [ ] Enhance admin authentication (use ASP.NET Identity)

**Medium Priority:**
- [ ] Add unit tests
- [ ] Implement repository pattern
- [ ] Add performance monitoring
- [ ] Enable HTTPS-only with HSTS

**Long-term:**
- [ ] Migrate to ASP.NET Core 8.0
- [ ] Replace Web Forms with MVC/Razor Pages
- [ ] Implement Entity Framework Core

## Usage

### Customer Redemption Flow
1. Customer receives coupon code (4 digits)
2. Visit: `https://yoursite.com/Central.aspx?id=XXXX`
3. System validates coupon (expiration, usage status)
4. Click "กดใช้สิทธิ์" (Redeem)
5. Barcode generated for checkout

### Admin Reporting
1. Visit: `https://yoursite.com/Admin/Report.aspx`
2. Login with authorized email + LDAP password
3. View statistics and search coupons/logs

## Helper Classes

### DatabaseHelper
```csharp
var dbHelper = new DatabaseHelper();
string query = "SELECT * FROM coupon WHERE ID = @id";
var param = dbHelper.CreateParameter("@id", couponId);
DataTable result = dbHelper.ExecuteQuery(query, param);
```

### ErrorLogger
```csharp
try {
    // Operation
} catch (Exception ex) {
    ErrorLogger.LogError("Operation failed", ex, "Context info");
}
```

### InputValidator
```csharp
if (!InputValidator.IsValidCouponId(id, 4)) {
    ErrorLogger.LogWarning("Invalid coupon ID", $"ID: {id}");
    return;
}
```

## Logging

**Error Logs Location:**
- Default: `App_Data/Logs/`
- Configurable via `Web.config` → `LogPath`

**Log Files:**
- `Error_YYYYMMDD.log` - Errors and exceptions
- `Warning_YYYYMMDD.log` - Warnings and validation failures
- `Info_YYYYMMDD.log` - Informational messages

**Database Logs:**
- `Logs` table - Access attempts, IP tracking
- `Admin_Logs` table - Admin login attempts

## Features

### Rate Limiting
- Max 10 invalid coupon attempts per IP per hour
- Logged to database for analysis
- Warning displayed on violation

### Barcode Generation
- Algorithm: CODE128
- Format: JPEG (450x140px)
- Storage: Configured `Path` + `{couponId}.jpg`
- Web access: `/Barcode/{couponId}.jpg`

### Multi-Database Support
- SQL Server (default)
- PostgreSQL (configure via `DatabaseType` setting)

## Known Issues

1. **Framework EOL:** .NET Framework 4.7.2 out of support (plan migration to .NET 8)
2. **No CSRF Protection:** Forms vulnerable to cross-site request forgery
3. **Weak Admin Auth:** Plain-text password comparison
4. **Code Duplication:** Central/Lotus/Starbucks pages are 95% identical
5. **No Unit Tests:** 0% test coverage

## Detailed Documentation

See `SECURITY_ANALYSIS.md` for:
- Comprehensive security analysis
- Vulnerability details and fixes
- Deployment checklist
- Monitoring recommendations
- Compliance considerations

## Support

**Issues:** Report security issues immediately to system administrator

**Development:**
- Framework: ASP.NET Web Forms 4.7.2
- IDE: Visual Studio 2019+
- Database: SQL Server 2016+ OR PostgreSQL 12+

## License

Internal use only - Central Group retail systems

---

**Last Updated:** 2025-12-08
**Security Review:** 2025-12-08 (SQL injection fixes applied to Central.aspx.cs)
