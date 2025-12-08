# คู่มือแก้ปัญหา Log ไม่เขียนไฟล์

## สาเหตุที่พบบ่อย

เมื่อกดปุ่ม "ใช้สิทธิ์" แล้วเกิด error แต่ไม่มี log ออกมา มักเกิดจาก:

1. **IIS ไม่มี Permission เขียนไฟล์** ✅ (สาเหตุหลัก 90%)
2. **Path ใน config ผิด** หรือ **folder ไม่มีอยู่จริง**
3. **Disk Full** - พื้นที่ hard disk เต็ม

---

## วิธีแก้ปัญหาแบบง่าย (แนะนำ)

### ขั้นตอนที่ 1: เปิดหน้าทดสอบ

เปิด browser ไปที่:
```
http://hondacx.me/Coupon/TestErrorLog.aspx
```

### ขั้นตอนที่ 2: ตรวจสอบ Config

1. คลิก **"Check Config"**
2. ดูว่า `LogPath` ชี้ไปที่ไหน
3. ตรวจสอบว่า Directory/File Exists เป็น `True` หรือไม่

### ขั้นตอนที่ 3: ทดสอบ Permission

1. คลิก **"Test File Write Permission"**
2. ถ้าขึ้น ❌ Permission Denied แสดงว่าไม่มีสิทธิ์เขียนไฟล์

### ขั้นตอนที่ 4: แก้ไข Permission (ถ้าขั้นตอนที่ 3 fail)

วิธีที่ 1 - ใช้ PowerShell Script (แนะนำ):
```powershell
# เปิด PowerShell แบบ Administrator แล้วรันคำสั่ง:
cd C:\inetpub\wwwroot\Hondacx\Coupon
.\FixLogPermissions.ps1
```

วิธีที่ 2 - แก้ไขด้วยมือ:
```powershell
# เปิด PowerShell แบบ Administrator แล้วรันคำสั่ง:
$logPath = "C:\inetpub\wwwroot\Hondacx\Coupon"
icacls $logPath /grant "IIS_IUSRS:(OI)(CI)M" /T
icacls $logPath /grant "NETWORK SERVICE:(OI)(CI)M" /T
```

### ขั้นตอนที่ 5: ทดสอบอีกครั้ง

กลับไปที่ TestErrorLog.aspx:
1. คลิก **"Test Write Log"** - ควรขึ้น ✅ Log Write Successful
2. คลิก **"Read Last 50 Lines"** - ควรเห็น log ที่เขียนไว้

---

## วิธีแก้ปัญหาด้วย File Explorer

ถ้าไม่อยากใช้ PowerShell สามารถทำแบบ GUI ได้:

1. เปิด File Explorer ไปที่ `C:\inetpub\wwwroot\Hondacx\Coupon`
2. คลิกขวาที่ folder → **Properties**
3. เลือกแท็บ **Security**
4. คลิก **Edit**
5. คลิก **Add**
6. พิมพ์ `IIS_IUSRS` แล้วคลิก **Check Names** → **OK**
7. เลือก `IIS_IUSRS` แล้วติ๊กถูก **Modify** และ **Write**
8. ทำซ้ำสำหรับ `NETWORK SERVICE`
9. คลิก **Apply** → **OK**

---

## การตรวจสอบ Log หลังแก้ไข

### วิธีที่ 1: ใช้หน้า TestErrorLog.aspx
```
http://hondacx.me/Coupon/TestErrorLog.aspx
```
คลิก "Read Last 50 Lines" เพื่อดู log ล่าสุด

### วิธีที่ 2: ใช้ PowerShell
```powershell
# ดู log แบบ real-time
Get-Content "C:\inetpub\wwwroot\Hondacx\Coupon\log.txt" -Tail 20 -Wait

# ดู log ทั้งไฟล์
Get-Content "C:\inetpub\wwwroot\Hondacx\Coupon\log.txt"

# ค้นหา error ล่าสุด
Get-Content "C:\inetpub\wwwroot\Hondacx\Coupon\log.txt" | Select-String "ERROR" | Select-Object -Last 10
```

### วิธีที่ 3: เปิดด้วย Notepad
```
C:\inetpub\wwwroot\Hondacx\Coupon\log.txt
```

---

## ทดสอบว่าแก้ไขสำเร็จหรือยัง

1. เปิดหน้าคูปอง: `http://hondacx.me/Coupon/Central.aspx?id=TEST`
2. คลิกปุ่ม **"กดใช้สิทธิ์"**
3. ถ้าเกิด error:
   - ไปดู log ที่ `C:\inetpub\wwwroot\Hondacx\Coupon\log.txt`
   - ควรเห็น error message พร้อม stack trace

---

## Format ของ Log File

Log จะมีหน้าตาประมาณนี้:

```
[2025-12-08 14:30:15] ERROR: Error during coupon redemption
Exception Type: SqlException
Message: Cannot insert duplicate key row in object 'dbo.Coupon'
Stack Trace: at System.Data.SqlClient.SqlConnection.OnError(...)
Additional Info: Coupon ID: ABC123, IP: 203.154.xx.xx
URL: http://hondacx.me/Central.aspx?id=ABC123
User IP: 203.154.xx.xx
User Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64)...
--------------------------------------------------------------------------------

[2025-12-08 14:35:22] ERROR: Error during coupon redemption
...
```

---

## คำถามที่พบบ่อย (FAQ)

### Q: Log ออกมาแล้ว แต่ไฟล์ใหญ่เกินไป ทำยังไง?
A: มี 2 ทางเลือก:
1. ใช้ log rotation แบบ daily (เปลี่ยน config `LogPath` เป็น folder แทน file)
2. ใช้ PowerShell script ลบ log เก่า:
```powershell
# เก็บเฉพาะ log 7 วันล่าสุด
Get-ChildItem "C:\inetpub\wwwroot\Hondacx\Coupon" -Filter "*.log" |
    Where-Object {$_.LastWriteTime -lt (Get-Date).AddDays(-7)} |
    Remove-Item
```

### Q: แก้ไข permission แล้วยังไม่ได้ ทำยังไง?
A: ลองเช็คเพิ่มเติม:
1. ตรวจสอบว่า Disk มีพื้นที่เหลือหรือไม่
2. ตรวจสอบว่า Antivirus หรือ Firewall block การเขียนไฟล์หรือไม่
3. Restart IIS: `iisreset /restart`
4. ตรวจสอบ Event Viewer (Windows Logs → Application) มี error จาก IIS หรือไม่

### Q: ต้องการ log เฉพาะ error ตอนใช้คูปอง ไม่ต้องการ log อื่น
A: สามารถสร้าง log file แยกได้โดย:
1. เพิ่ม config ใหม่ใน Web.config:
```xml
<add key="CouponErrorLogPath" value="C:\inetpub\wwwroot\Hondacx\Coupon\coupon_errors.txt" />
```
2. แก้ไข code ใน Central.aspx.cs บรรทัด 123 เป็น:
```csharp
// บันทึก log ไปทั้ง 2 ที่
ErrorLogger.LogError("Error during coupon redemption", ex, $"Coupon ID: {id}");
File.AppendAllText(ConfigurationManager.AppSettings["CouponErrorLogPath"],
    $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Coupon: {id}, Error: {ex.Message}\n");
```

---

## สรุป

การแก้ปัญหาส่วนใหญ่คือเรื่อง **Permission** ให้:

1. ✅ ใช้ `FixLogPermissions.ps1` script
2. ✅ ทดสอบด้วย `TestErrorLog.aspx`
3. ✅ ตรวจสอบ log ด้วย PowerShell หรือ Notepad

ถ้ายังมีปัญหาให้ดูใน Event Viewer หรือติดต่อ System Administrator
