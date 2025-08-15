# ระบบส่งอีเมล SMTP สำหรับ OT Approval

## ภาพรวม
ระบบนี้ถูกออกแบบมาเพื่อส่งอีเมลแจ้งเตือนการอนุมัติ OT ไปยัง NextAppr ผ่าน SMTP Server

## การตั้งค่า SMTP
- **Server**: 10.1.15.143
- **Port**: 25
- **SSL**: ปิด (EnableSsl = false)
- **Authentication**: UseDefaultCredentials = true

## ฟังก์ชันที่เพิ่มเข้ามา

### 1. GetNextApproverEmail()
```csharp
private async Task<string> GetNextApproverEmail(SqlConnection ces941Connection, string nextApprId)
```
- ดึงอีเมลของ NextAppr จากฐานข้อมูล
- ตาราง: EmpMaster
- เงื่อนไข: EmpNo = nextApprId และ RecordStatus = 'N'

### 2. SendOTApprovalEmail()
```csharp
private async Task SendOTApprovalEmail(string nextApprEmail, string otReqNo, string personId, string personName, 
    DateTime workDate, DateTime startDateTime, DateTime endDateTime, decimal calculatedHours, string remark)
```
- ส่งอีเมลแจ้งเตือนการอนุมัติ OT
- ใช้ HTML template ที่สวยงาม
- ข้อมูลที่ส่ง: เลขที่คำขอ, รหัสพนักงาน, ชื่อพนักงาน, วันที่ทำงาน, เวลาเริ่ม-สิ้นสุด, จำนวนชั่วโมง, หมายเหตุ

### 3. TestEmail() Actions
```csharp
[HttpGet]
public async Task<IActionResult> TestEmail()

[HttpPost]
public async Task<IActionResult> TestEmail(string toEmail = "test@ipsth.com")
```
- หน้าเว็บสำหรับทดสอบการส่งอีเมล
- URL: `/Attendance/TestEmail`
- สามารถระบุอีเมลผู้รับได้

## การใช้งาน

### 1. ทดสอบการส่งอีเมล
1. เข้าไปที่ `/Attendance/TestEmail`
2. ใส่อีเมลผู้รับ
3. กดปุ่ม "ส่งอีเมลทดสอบ"

### 2. การส่งอีเมลอัตโนมัติ
เมื่อมีการบันทึกข้อมูล OT ใหม่ ระบบจะ:
1. บันทึกข้อมูลลงฐานข้อมูล
2. ดึงอีเมลของ NextAppr
3. ส่งอีเมลแจ้งเตือนอัตโนมัติ

## โครงสร้างอีเมล
```
หัวข้อ: คำขออนุมัติ OT - [OTReqNo]

เนื้อหา:
- เลขที่คำขอ
- รหัสพนักงาน
- ชื่อพนักงาน
- วันที่ทำงาน
- เวลาเริ่ม-สิ้นสุด
- จำนวนชั่วโมง
- หมายเหตุ
```

## การจัดการข้อผิดพลาด
- หากไม่พบอีเมลของ NextAppr จะ log warning และไม่ส่งอีเมล
- หากเกิดข้อผิดพลาดในการส่งอีเมล จะ log error แต่ไม่กระทบการบันทึกข้อมูล
- การส่งอีเมลจะทำงานใน try-catch แยกต่างหาก

## การตั้งค่าเพิ่มเติม
หากต้องการเปลี่ยนการตั้งค่า SMTP สามารถแก้ไขในฟังก์ชัน `SendOTApprovalEmail()`:

```csharp
smtpClient.Host = "10.1.15.143";        // SMTP Server
smtpClient.Port = 25;                   // Port
smtpClient.EnableSsl = false;           // SSL
smtpClient.UseDefaultCredentials = true; // Authentication
```

## การทดสอบ
1. ใช้หน้า TestEmail เพื่อทดสอบการเชื่อมต่อ SMTP
2. ตรวจสอบ log เพื่อดูสถานะการส่งอีเมล
3. ตรวจสอบ inbox ของ NextAppr ว่าต้องอีเมลหรือไม่

## หมายเหตุ
- ระบบใช้ Bootstrap 5 สำหรับ UI
- อีเมลใช้ HTML template ที่สวยงาม
- มีการ log ข้อมูลเพื่อการ debug
- การส่งอีเมลไม่กระทบการทำงานหลักของระบบ 