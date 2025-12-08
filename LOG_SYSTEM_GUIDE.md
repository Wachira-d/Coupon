# 📝 คู่มือระบบ Log Files - Coupon Management System

## 🎯 ภาพรวม

ระบบนี้มี log system 2 ประเภท:
1. **Error Logs** - บันทึก errors, warnings, และ info messages
2. **Audit Logs** - บันทึก security events และ user actions

---

## 📂 ตำแหน่งไฟล์ Log

### บน Production Server (IIS):
```
D:\Web Sites\wwwroot\Hondacx\Coupon\
├── App_Data\
│   ├── Logs\                     ← Error logs
│   │   ├── Error_20251208.log
│   │   ├── Warning_20251208.log
│   │   └── Info_20251208.log
│   └── AuditLogs\                ← Audit logs
│       └── Audit_20251208.log
└── Barcode\                      ← Barcode images
```

### บน Development (Local):
```
C:\Users\[YourName]\source\repos\Coupon\Coupon\
├── App_Data\
│   ├── Logs\
│   └── AuditLogs\
└── Barcode\
```

---

## 🚀 วิธีตั้งค่า (Production)

### ขั้นตอนที่ 1: รัน PowerShell Script

เปิด PowerShell **แบบ Administrator**:

```powershell
cd D:\Web Sites\wwwroot\Hondacx\Coupon

# รัน setup script
.\SetupLogFolders.ps1

# หรือระบุ path เอง
.\SetupLogFolders.ps1 -WebSitePath "D:\Web Sites\wwwroot\Hondacx\Coupon"

# ถ้าต้องการ force reset permissions
.\SetupLogFolders.ps1 -Force
```

Script จะ:
- ✅ สร้าง folders ที่จำเป็น
- ✅ ตั้งค่า permissions สำหรับ IIS
- ✅ ทดสอบการเขียนไฟล์
- ✅ สร้าง sample log files

### ขั้นตอนที่ 2: ทดสอบระบบ Log

เปิดหน้าทดสอบ:
```
http://hondacx.me/Coupon/TestLogs.aspx
```

หน้านี้จะให้คุณ:
- ✅ ทดสอบ Error logging (Error, Warning, Info)
- ✅ ทดสอบ Audit logging (Authentication, Security events, Coupon redemption)
- ✅ ตรวจสอบ folders และ permissions
- ✅ ดู log files ล่าสุด
- ✅ ดูข้อมูล system

**⚠️ หมายเหตุ:** หน้า TestLogs.aspx สามารถเข้าถึงได้เฉพาะใน development environment หรือ localhost เท่านั้น

---

## 📖 วิธีดู Log Files

### 1. ผ่าน File Explorer

เปิดไฟล์ด้วย Notepad:
```
D:\Web Sites\wwwroot\Hondacx\Coupon\App_Data\Logs\Error_20251208.log
D:\Web Sites\wwwroot\Hondacx\Coupon\App_Data\AuditLogs\Audit_20251208.log
```

### 2. ผ่าน PowerShell

```powershell
# ดู error log 50 บรรทัดล่าสุด
$today = Get-Date -Format "yyyyMMdd"
Get-Content "D:\Web Sites\wwwroot\Hondacx\Coupon\App_Data\Logs\Error_$today.log" -Tail 50

# ดู audit log 50 บรรทัดล่าสุด
Get-Content "D:\Web Sites\wwwroot\Hondacx\Coupon\App_Data\AuditLogs\Audit_$today.log" -Tail 50

# ดู log แบบ real-time
Get-Content "D:\Web Sites\wwwroot\Hondacx\Coupon\App_Data\Logs\Error_$today.log" -Tail 50 -Wait
```

### 3. ผ่าน TestLogs.aspx

```
http://hondacx.me/Coupon/TestLogs.aspx
```
คลิกปุ่ม "View Error Log" หรือ "View Audit Log"

---

## 📊 รูปแบบ Log Files

### Error Log Format
```
================================================================================
[2025-12-08 14:30:45] ERROR
--------------------------------------------------------------------------------
Message: Database connection failed
Exception: System.Data.SqlClient.SqlException
   at System.Data.SqlClient.SqlConnection.Open()
   at Coupon.Helpers.DatabaseHelper.ExecuteQuery(String query)

Additional Info: IP: 192.168.1.100, Coupon ID: ABC123
================================================================================
```

### Audit Log Format
```
2025-12-08 14:30:45 | AUTHENTICATION | SUCCESS | admin@example.com | 192.168.1.100 | Admin panel access
2025-12-08 14:31:20 | COUPON_REDEMPTION | INFO | ABC123 | Central | 200 | 192.168.1.50
2025-12-08 14:35:00 | SECURITY_EVENT | HIGH | Rate Limit Exceeded | 192.168.1.200 | 10 failed attempts
```

---

## 🔍 วิธีแก้ปัญหา Log Files

### ปัญหา: ไม่มี Log File ถูกสร้าง

**สาเหตุ:**
- Folder ไม่มี write permissions
- Folder ยังไม่ถูกสร้าง
- IIS Identity ไม่มีสิทธิ์

**วิธีแก้:**
```powershell
# 1. รัน setup script อีกครั้ง
.\SetupLogFolders.ps1 -Force

# 2. ตั้งค่า permissions ด้วยตนเอง
icacls "D:\Web Sites\wwwroot\Hondacx\Coupon\App_Data" /grant "IIS_IUSRS:(OI)(CI)M" /T
icacls "D:\Web Sites\wwwroot\Hondacx\Coupon\App_Data" /grant "NETWORK SERVICE:(OI)(CI)M" /T

# 3. Restart IIS
iisreset /restart

# 4. ทดสอบอีกครั้งที่ TestLogs.aspx
```

### ปัญหา: Log File ว่างเปล่า

**สาเหตุ:**
- Exception ถูก catch แต่ไม่ถูก log
- ErrorLogger ไม่ทำงาน

**วิธีแก้:**
```powershell
# ดู IIS Event Viewer
eventvwr.msc

# หรือดู Windows Event Log
Get-EventLog -LogName Application -Source "ASP.NET*" -Newest 50 |
    Where-Object {$_.TimeGenerated -gt (Get-Date).AddHours(-1)} |
    Format-Table TimeGenerated, EntryType, Message -AutoSize
```

### ปัญหา: Access Denied Errors

**วิธีแก้:**
```powershell
# ค้นหา IIS Application Pool ที่ใช้งาน
Import-Module WebAdministration
Get-IISAppPool | Where-Object { $_.Name -like "*Coupon*" }

# ให้สิทธิ์แก่ Application Pool นั้น
$appPoolName = "YourAppPoolName"  # เปลี่ยนตามที่ได้จากคำสั่งด้านบน
icacls "D:\Web Sites\wwwroot\Hondacx\Coupon\App_Data" /grant "IIS AppPool\${appPoolName}:(OI)(CI)M" /T
```

---

## 📝 การใช้งาน Log System ใน Code

### Error Logging
```csharp
using Coupon.Helpers;

// Log error with exception
try
{
    // Your code here
}
catch (Exception ex)
{
    ErrorLogger.LogError("Operation failed", ex, "Additional context info");
}

// Log warning
ErrorLogger.LogWarning("User tried invalid action", $"IP: {ipAddress}");

// Log info
ErrorLogger.LogInfo("Operation completed", "Details here");
```

### Audit Logging
```csharp
using Coupon.Helpers;

// Log authentication
AuditLogger.LogAuthentication(
    userEmail: "user@example.com",
    success: true,
    ipAddress: Request.UserHostAddress,
    details: "Login successful"
);

// Log security event
AuditLogger.LogSecurityEvent(
    eventName: "Suspicious Activity",
    ipAddress: Request.UserHostAddress,
    details: "Multiple failed login attempts",
    severity: SecuritySeverity.High
);

// Log coupon redemption
AuditLogger.LogCouponRedemption(
    couponId: "ABC123",
    brand: "Central",
    value: 200,
    ipAddress: Request.UserHostAddress
);

// Log admin action
AuditLogger.LogAdminAction(
    adminUser: "admin@example.com",
    action: "Report Generated",
    ipAddress: Request.UserHostAddress,
    details: "100 records exported"
);
```

---

## 🔧 การตั้งค่า Log Rotation

Log files จะถูกสร้างใหม่ทุกวันโดยอัตโนมัติ (format: `Error_YYYYMMDD.log`)

### ลบ Log Files เก่า

สร้าง scheduled task เพื่อลบ logs เก่า:

```powershell
# DeleteOldLogs.ps1
$logPath = "D:\Web Sites\wwwroot\Hondacx\Coupon\App_Data\Logs"
$auditPath = "D:\Web Sites\wwwroot\Hondacx\Coupon\App_Data\AuditLogs"
$daysToKeep = 90  # เก็บ 90 วัน

# ลบ error logs เก่า
Get-ChildItem -Path $logPath -Filter "*.log" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$daysToKeep) } |
    Remove-Item -Force

# ลบ audit logs เก่า
Get-ChildItem -Path $auditPath -Filter "*.log" |
    Where-Object { $_.LastWriteTime -lt (Get-Date).AddDays(-$daysToKeep) } |
    Remove-Item -Force
```

เพิ่ม scheduled task:
```
Task Scheduler → Create Basic Task
Name: Delete Old Coupon Logs
Trigger: Daily at 2:00 AM
Action: Start a Program
Program: PowerShell.exe
Arguments: -ExecutionPolicy Bypass -File "D:\Scripts\DeleteOldLogs.ps1"
```

---

## 📊 Monitoring & Alerts

### สคริปต์ตรวจสอบ Errors

```powershell
# CheckErrors.ps1 - ส่ง email alert เมื่อมี critical errors
$today = Get-Date -Format "yyyyMMdd"
$errorLog = "D:\Web Sites\wwwroot\Hondacx\Coupon\App_Data\Logs\Error_$today.log"

if (Test-Path $errorLog) {
    $errors = Get-Content $errorLog | Select-String "ERROR" | Measure-Object

    if ($errors.Count -gt 10) {
        # ส่ง email alert (ตั้งค่า SMTP settings)
        Send-MailMessage -To "admin@example.com" `
                         -From "server@example.com" `
                         -Subject "ALERT: High error count on Coupon system" `
                         -Body "Found $($errors.Count) errors today" `
                         -SmtpServer "smtp.example.com"
    }
}
```

---

## 📋 Checklist สำหรับ Production

- [ ] รัน `SetupLogFolders.ps1` สำเร็จ
- [ ] ทดสอบการเขียน log ผ่าน `TestLogs.aspx`
- [ ] ตรวจสอบว่า log files ถูกสร้าง
- [ ] ตั้งค่า log rotation (scheduled task)
- [ ] ตั้งค่า monitoring/alerts สำหรับ errors
- [ ] เพิ่ม folder exclusion ใน antivirus (ถ้าจำเป็น)
- [ ] Backup logs เป็นประจำ

---

## 🆘 ติดต่อ Support

หากพบปัญหา กรุณาแจ้ง:
1. Error message จาก log files
2. Output จาก `SetupLogFolders.ps1`
3. Screenshot จาก `TestLogs.aspx`
4. ข้อมูล IIS version และ .NET version

---

**เอกสารนี้อัพเดทล่าสุด: 2025-12-08**
