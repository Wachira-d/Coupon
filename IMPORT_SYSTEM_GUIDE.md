# คู่มือการใช้งานระบบ Import คูปอง / Coupon Import System Guide

## ภาษาไทย

### ภาพรวมระบบ
ระบบนี้ช่วยให้คุณสามารถนำเข้าคูปองจำนวนมากพร้อมกันผ่านไฟล์ CSV

### ขั้นตอนการใช้งาน

#### 1. เตรียมไฟล์ CSV
- เปิดไฟล์ตัวอย่าง: `sample_coupon_import.csv`
- ไฟล์ต้องมีแถวแรกเป็น Header (ชื่อคอลัมน์)
- ต้องมีอย่างน้อย 2 คอลัมน์:
  - **คอลัมน์ ID คูปอง** (เช่น CouponID, ID, Code)
  - **คอลัมน์บาร์โค้ด** (เช่น BarcodeCode, Barcode, Code)

**ตัวอย่างไฟล์ CSV:**
```csv
CouponID,BarcodeCode,Notes
ABC001,1234567890123,Test coupon 1
ABC002,1234567890124,Test coupon 2
ABC003,1234567890125,Test coupon 3
```

#### 2. เข้าสู่หน้าระบบ Import
- URL: `http://yourdomain/Coupon/Admin/ImportCoupons.aspx`
- คลิก "Browse" หรือ "เลือกไฟล์"
- เลือกไฟล์ CSV ที่เตรียมไว้
- คลิก "📤 Upload & Preview"

#### 3. ตรวจสอบข้อมูลและแมปคอลัมน์
- ระบบจะแสดงข้อมูล 10 แถวแรกให้ตรวจสอบ
- ระบบจะพยายามตรวจจับคอลัมน์อัตโนมัติ
- หากไม่ถูกต้อง ให้เลือกคอลัมน์ที่ถูกต้องจาก dropdown:
  - **Coupon ID Column**: เลือกคอลัมน์ที่เป็น ID ของคูปอง
  - **Barcode Column**: เลือกคอลัมน์ที่เป็นรหัสบาร์โค้ด

#### 4. ตั้งค่า Lot Properties
- **💰 Coupon Value (บาท)**: ระบุมูลค่าคูปอง (เช่น 200, 300, 500)
- **📅 Expiration Date**: เลือกวันหมดอายุของ lot นี้
- **📝 Lot Description (Optional)**: ระบุรายละเอียดเพิ่มเติม (เช่น "Honda CX Campaign 2025")

#### 5. Import เข้าระบบ
- คลิก "🚀 Start Import"
- ระบบจะแสดงผลลัพธ์:
  - ✅ จำนวนที่ Import สำเร็จ
  - ℹ️ จำนวนที่ข้าม (มีอยู่แล้วในระบบ)
  - ❌ จำนวนที่เกิดข้อผิดพลาด

#### 6. Import ไฟล์ใหม่
- คลิก "🔄 Import Another File" เพื่อเริ่มต้นใหม่

### ข้อจำกัดและข้อควรระวัง

1. **รองรับเฉพาะ CSV**: ขณะนี้รองรับเฉพาะไฟล์ .csv เท่านั้น
   - หาก Excel (.xlsx, .xls): ให้ Save As → CSV (Comma delimited) ก่อน

2. **ขนาดไฟล์**: สูงสุด 10MB

3. **จำนวนแถว**: สูงสุด 10,000 แถวต่อครั้ง

4. **ID ซ้ำ**: หาก ID มีอยู่แล้วในระบบ จะข้ามไม่ Import

5. **ข้อมูลว่าง**: แถวที่มี ID หรือ Barcode ว่างจะถูกข้าม

### แก้ไขปัญหา

#### ปัญหา: ไม่สามารถ Upload ไฟล์ได้
- ตรวจสอบว่าไฟล์เป็น .csv
- ตรวจสอบขนาดไฟล์ไม่เกิน 10MB
- ตรวจสอบว่าไฟล์ไม่ได้เปิดอยู่ใน Excel

#### ปัญหา: ระบบตรวจจับคอลัมน์ผิด
- เลือกคอลัมน์ที่ถูกต้องจาก dropdown ด้วยตนเอง
- ชื่อคอลัมน์ควรเป็น: ID, Code, CouponID, BarcodeCode เพื่อให้ตรวจจับอัตโนมัติ

#### ปัญหา: Import ล้มเหลว
- ตรวจสอบ Log ที่: `http://yourdomain/Coupon/ViewErrorLogs.aspx`
- ตรวจสอบว่าข้อมูลในไฟล์ถูกต้องและไม่มีตัวอักษรพิเศษ

---

## English

### System Overview
This system allows you to import multiple coupons at once via CSV file upload.

### Usage Steps

#### 1. Prepare CSV File
- Open sample file: `sample_coupon_import.csv`
- File must have header row (column names)
- Must have at least 2 columns:
  - **Coupon ID Column** (e.g., CouponID, ID, Code)
  - **Barcode Column** (e.g., BarcodeCode, Barcode, Code)

**CSV Example:**
```csv
CouponID,BarcodeCode,Notes
ABC001,1234567890123,Test coupon 1
ABC002,1234567890124,Test coupon 2
ABC003,1234567890125,Test coupon 3
```

#### 2. Access Import Page
- URL: `http://yourdomain/Coupon/Admin/ImportCoupons.aspx`
- Click "Browse" or "Select File"
- Select your prepared CSV file
- Click "📤 Upload & Preview"

#### 3. Review Data and Map Columns
- System displays first 10 rows for preview
- System attempts auto-detection of columns
- If incorrect, manually select correct columns from dropdowns:
  - **Coupon ID Column**: Select column containing coupon IDs
  - **Barcode Column**: Select column containing barcode values

#### 4. Set Lot Properties
- **💰 Coupon Value (บาท)**: Enter coupon value (e.g., 200, 300, 500)
- **📅 Expiration Date**: Select expiration date for this lot
- **📝 Lot Description (Optional)**: Enter additional details (e.g., "Honda CX Campaign 2025")

#### 5. Import to System
- Click "🚀 Start Import"
- System displays results:
  - ✅ Successfully imported count
  - ℹ️ Skipped count (already exists)
  - ❌ Error count

#### 6. Import New File
- Click "🔄 Import Another File" to start over

### Limitations and Warnings

1. **CSV Only**: Currently supports .csv files only
   - For Excel (.xlsx, .xls): Save As → CSV (Comma delimited) first

2. **File Size**: Maximum 10MB

3. **Row Count**: Maximum 10,000 rows per import

4. **Duplicate IDs**: Existing IDs will be skipped

5. **Empty Data**: Rows with empty ID or Barcode will be skipped

### Troubleshooting

#### Problem: Cannot upload file
- Verify file is .csv format
- Check file size is under 10MB
- Ensure file is not open in Excel

#### Problem: System detects wrong columns
- Manually select correct columns from dropdowns
- For auto-detection, name columns: ID, Code, CouponID, BarcodeCode

#### Problem: Import fails
- Check logs at: `http://yourdomain/Coupon/ViewErrorLogs.aspx`
- Verify data is valid and contains no special characters

---

## Technical Details

### Database Columns Inserted
- **ID**: Coupon ID from selected column
- **Code**: Barcode from selected column
- **Value**: Value entered in form (applies to all rows)
- **Expire_DateTime**: Expiration date entered in form (applies to all rows)
- **Used_Status**: Set to False (0) for all new coupons
- **Date_Use**: Set to NULL for all new coupons

### CSV Format Requirements
- **Encoding**: UTF-8 (recommended)
- **Delimiter**: Comma (,)
- **Quoted Values**: Supported (e.g., "Value with, comma")
- **Line Endings**: Both Windows (CRLF) and Unix (LF) supported

### Security Features
- File type validation
- File size limits
- Parameterized SQL queries (SQL injection prevention)
- Duplicate checking before insert
- Audit logging
- Error logging

### Files Created
- `/Coupon/Admin/ImportCoupons.aspx` - UI page
- `/Coupon/Admin/ImportCoupons.aspx.cs` - Code-behind
- `/sample_coupon_import.csv` - Sample template
