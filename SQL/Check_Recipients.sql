-- ตรวจสอบข้อมูลใน esmms_employees_receive_mail
SELECT * FROM esmms_employees_receive_mail;

-- นับจำนวนข้อมูล
SELECT COUNT(*) AS TotalRecipients FROM esmms_employees_receive_mail;

-- ลบข้อมูลทั้งหมด (ถ้าต้องการเริ่มใหม่)
-- DELETE FROM esmms_employees_receive_mail;

-- ลบเฉพาะข้อมูลที่ไม่ active
-- DELETE FROM esmms_employees_receive_mail WHERE is_active = 0;

