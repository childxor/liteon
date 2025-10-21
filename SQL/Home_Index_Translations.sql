-- INSERT statements สำหรับตาราง sys_language สำหรับหน้า Home/Index
-- ข้อความหลัก
INSERT INTO sys_language (Keyword, ModuleId, Th, En, Cn) VALUES 
('employee_statistics_title', '1', 'สรุปข้อมูลพนักงาน', 'Employee Statistics Overview', '员工统计概览'),
('employee_statistics_subtitle', '1', 'Employee Statistics Overview', 'Employee Statistics Overview', '员工统计概览'),
('all_employees', '1', 'พนักงานทั้งหมด', 'All Employees', '所有员工'),
('current_employees', '1', 'พนักงานปัจจุบัน', 'Current Employees', '当前员工'),
('new_employees_this_month', '1', 'พนักงานใหม่เดือนนี้', 'New Employees This Month', '本月新员工'),
('resigned_this_month', '1', 'ลาออกเดือนนี้', 'Resigned This Month', '本月离职'),

-- ข้อความเบอร์โทรศัพท์
('phone_list_title', '1', 'รายชื่อที่มีเบอร์โทรศัพท์', 'Phone Number List', '电话号码列表'),
('phone_list_subtitle', '1', 'เบอร์โทรศัพท์ที่มีอยู่ในระบบเป็นเบอร์โต๊ะทำงาน', 'Phone numbers in the system are desk phone numbers', '系统中的电话号码是办公桌电话号码'),
('phone_count', '1', 'รายการ', 'Items', '项目'),

-- ข้อความตู้เก็บของ
('cabinet_management_title', '1', 'สรุปข้อมูลตู้เก็บของ', 'Cabinet Management Overview', '储物柜管理概览'),
('cabinet_management_subtitle', '1', 'Cabinet Management Overview', 'Cabinet Management Overview', '储物柜管理概览'),
('all_cabinets', '1', 'ตู้ทั้งหมด', 'All Cabinets', '所有储物柜'),
('available_cabinets', '1', 'ตู้ว่าง', 'Available Cabinets', '可用储物柜'),
('occupied_cabinets', '1', 'ตู้ที่ใช้งาน', 'Occupied Cabinets', '已使用储物柜'),
('damaged_cabinets', '1', 'ตู้ชำรุด', 'Damaged Cabinets', '损坏储物柜'),
('cabinets_to_return', '1', 'ตู้ต้องคืน', 'Cabinets to Return', '需要归还的储物柜'),
('cabinets_to_return_note', '1', '(เจ้าของลาออก)', '(Owner resigned)', '（所有者已离职）'),
('employees_without_cabinet', '1', 'พนักงานไม่มีตู้', 'Employees Without Cabinet', '没有储物柜的员工'),
('employees_without_cabinet_note', '1', '(ยังทำงานอยู่)', '(Still working)', '（仍在工作）'),
('cabinet_utilization_rate', '1', 'อัตราการใช้ตู้', 'Cabinet Utilization Rate', '储物柜使用率'),
('cabinet_coverage', '1', 'ความครอบคลุมตู้', 'Cabinet Coverage', '储物柜覆盖率'),

-- ข้อความคอมพิวเตอร์
('computer_management_title', '1', 'สรุปข้อมูลคอมพิวเตอร์', 'Computer Asset Management Overview', '计算机资产管理概览'),
('computer_management_subtitle', '1', 'Computer Asset Management Overview', 'Computer Asset Management Overview', '计算机资产管理概览'),
('all_computers', '1', 'คอมพิวเตอร์ทั้งหมด', 'All Computers', '所有计算机'),
('working_computers', '1', 'ใช้งานได้', 'Working', '正常工作'),
('repairing_computers', '1', 'กำลังซ่อม', 'Under Repair', '维修中'),
('damaged_computers', '1', 'ชำรุด', 'Damaged', '损坏'),
('disposed_computers', '1', 'จำหน่าย', 'Disposed', '已处置'),
('inactive_computers', '1', 'ไม่ใช้งาน', 'Inactive', '未使用'),
('spare_parts', '1', 'อะไหล่', 'Spare Parts', '备件'),
('computers_with_mac', '1', 'มี Mac Address', 'With Mac Address', '有Mac地址'),

-- ข้อความฟอร์ม
('search_add_phone', '1', 'ค้นหา/เพิ่มเบอร์ (PersonID)', 'Search/Add Phone (Person ID)', '搜索/添加电话（人员ID）'),
('desk_phone', '1', 'เบอร์โทรศัพท์โต๊ะทำงาน', 'Desk Phone Number', '办公桌电话号码'),
('search', '1', 'ค้นหา', 'Search', '搜索'),
('save_phone', '1', 'บันทึกเบอร์', 'Save Phone', '保存电话'),
('clear_form', '1', 'ล้างฟอร์ม', 'Clear Form', '清空表单'),
('edit_in_window', '1', 'แก้ไขในหน้าต่าง', 'Edit in Window', '在窗口中编辑'),
('loading_data', '1', 'กำลังโหลดข้อมูลเบอร์โทรศัพท์...', 'Loading phone data...', '正在加载电话数据...'),
('loading_error', '1', 'เกิดข้อผิดพลาดในการโหลดข้อมูล', 'Error loading data', '加载数据时出错'),
('try_again', '1', 'ลองใหม่', 'Try Again', '重试'),

-- ข้อความตาราง
('card_number', '1', 'CardNumber', 'Card Number', '卡号'),
('name', '1', 'ชื่อ', 'Name', '姓名'),
('department', '1', 'แผนก', 'Department', '部门'),
('phone_number', '1', 'เบอร์โทรศัพท์', 'Phone Number', '电话号码'),
('management', '1', 'การจัดการ', 'Management', '管理'),
('edit_phone', '1', 'แก้ไขเบอร์โทรศัพท์', 'Edit Phone Number', '编辑电话号码'),
('person_id', '1', 'PersonID', 'Person ID', '人员ID'),
('confirm_delete_phone', '1', 'ยืนยันการลบเบอร์', 'Confirm Phone Deletion', '确认删除电话'),
('confirm_delete_message', '1', 'ต้องการลบเบอร์ของ', 'Do you want to delete the phone number of', '您要删除的电话号码'),
('confirm_question', '1', 'ใช่หรือไม่', '?', '？'),

-- ข้อความ DataTable
('search_placeholder', '1', 'ค้นหา:', 'Search:', '搜索：'),
('length_menu', '1', 'แสดง _MENU_ รายการต่อหน้า', 'Show _MENU_ entries per page', '每页显示 _MENU_ 条记录'),
('info', '1', 'แสดง _START_ ถึง _END_ จาก _TOTAL_ รายการ', 'Showing _START_ to _END_ of _TOTAL_ entries', '显示第 _START_ 到 _END_ 条，共 _TOTAL_ 条记录'),
('info_empty', '1', 'แสดง 0 ถึง 0 จาก 0 รายการ', 'Showing 0 to 0 of 0 entries', '显示第 0 到 0 条，共 0 条记录'),
('info_filtered', '1', '(กรองจาก _MAX_ รายการทั้งหมด)', '(filtered from _MAX_ total entries)', '（从 _MAX_ 条记录中筛选）'),
('first_page', '1', 'หน้าแรก', 'First', '首页'),
('previous_page', '1', 'ก่อนหน้า', 'Previous', '上一页'),
('next_page', '1', 'ถัดไป', 'Next', '下一页'),
('last_page', '1', 'หน้าสุดท้าย', 'Last', '最后一页'),
('empty_table', '1', 'ไม่มีข้อมูลในตาราง', 'No data available in table', '表中没有数据'),
('zero_records', '1', 'ไม่พบข้อมูลที่ค้นหา', 'No matching records found', '未找到匹配的记录'),
('all_items', '1', 'ทั้งหมด', 'All', '全部'),

-- ข้อความ SweetAlert
('please_enter_person_id', '1', 'กรุณาระบุ PersonID', 'Please enter Person ID', '请输入人员ID'),
('name_not_found', '1', 'ไม่พบชื่อ', 'Name not found', '未找到姓名'),
('search_error', '1', 'เกิดข้อผิดพลาดในการค้นหา', 'Error occurred while searching', '搜索时发生错误'),
('person_id_not_found', '1', 'ไม่พบ PersonID', 'Person ID not found', '未找到人员ID'),
('save_success', '1', 'บันทึกสำเร็จ', 'Save successful', '保存成功'),
('save_failed', '1', 'บันทึกไม่สำเร็จ', 'Save failed', '保存失败'),
('save_error', '1', 'เกิดข้อผิดพลาดในการบันทึก', 'Error occurred while saving', '保存时发生错误'),
('delete_success', '1', 'ลบสำเร็จ', 'Delete successful', '删除成功'),
('delete_failed', '1', 'ลบไม่สำเร็จ', 'Delete failed', '删除失败'),
('delete_error', '1', 'เกิดข้อผิดพลาดในการลบ', 'Error occurred while deleting', '删除时发生错误'),

-- ข้อความ Placeholder
('person_id_placeholder', '1', 'เช่น 10129029', 'e.g. 10129029', '例如 10129029'),
('phone_placeholder', '1', 'เช่น 344', 'e.g. 344', '例如 344'),
('phone_edit_placeholder', '1', 'เช่น 0812345678', 'e.g. 0812345678', '例如 0812345678'),
('search_phone_placeholder', '1', 'ค้นหา: บัตร / ชื่อ / แผนก / ประตู / เหตุการณ์', 'Search: Card / Name / Department / Door / Event', '搜索：卡片/姓名/部门/门/事件'),

-- ข้อความอื่นๆ
('show_details', '1', 'แสดงรายละเอียด', 'Show Details', '显示详情'),
('hide_details', '1', 'ซ่อนรายละเอียด', 'Hide Details', '隐藏详情'),
('last_update', '1', 'อัปเดตล่าสุด:', 'Last update:', '最后更新：'),
('just_now', '1', 'เมื่อสักครู่', 'Just now', '刚刚'),
('success', '1', 'สำเร็จ', 'Success', '成功'),
('error', '1', 'เกิดข้อผิดพลาด', 'Error occurred', '发生错误'),
('ok', '1', 'ตกลง', 'OK', '确定'),
('not_specified', '1', 'ไม่ระบุ', 'Not specified', '未指定'),
('close', '1', 'ปิด', 'Close', '关闭'),
('save', '1', 'บันทึก', 'Save', '保存'),
('cancel', '1', 'ยกเลิก', 'Cancel', '取消'),
('delete', '1', 'ลบ', 'Delete', '删除'),
('details', '1', 'รายละเอียด: ', 'Details: ', '详情：');
