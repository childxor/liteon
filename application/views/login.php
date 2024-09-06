<!DOCTYPE html>
<html lang="en">

<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>Login Page</title>
    <link rel="stylesheet" href="https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/css/bootstrap.min.css">
    <link rel="stylesheet" href="https://cdnjs.cloudflare.com/ajax/libs/font-awesome/5.15.3/css/all.min.css">
    <style>
        body {
            background: linear-gradient(135deg, #1a237e, #283593);
            min-height: 100vh;
            display: flex;
            align-items: center;
        }

        .card {
            border-radius: 1rem;
            box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15);
        }

        .card-header {
            background-color: transparent;
            border-bottom: none;
            text-align: center;
            padding-bottom: 0;
        }

        .logo {
            max-width: 180px;
            margin-bottom: 1rem;
        }

        .nav-tabs {
            border-bottom: none;
        }

        .nav-tabs .nav-link {
            border: none;
            color: #495057;
            font-weight: 500;
        }

        .nav-tabs .nav-link.active {
            color: #007bff;
            background-color: transparent;
            border-bottom: 2px solid #007bff;
        }

        .form-control {
            border-radius: 0.5rem;
        }

        .btn-primary {
            border-radius: 0.5rem;
            padding: 0.5rem 2rem;
        }

        .queue-display {
            background: linear-gradient(135deg, #283593, #1a237e);
            color: white;
            border-radius: 1rem;
            padding: 1.5rem;
            box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15);
        }

        .queue-number {
            font-size: 2.5rem;
            font-weight: bold;
        }

        .queue-table {
            background-color: rgba(255, 255, 255, 0.1);
            border-radius: 0.5rem;
        }

        .queue-table th,
        .queue-table td {
            color: white;
            border-color: rgba(255, 255, 255, 0.2);
        }

        .queue-table thead {
            background-color: rgba(255, 255, 255, 0.2);
        }
    </style>
</head>

<body>
    <div class="container">
        <div class="row justify-content-center">
            <div class="col-md-6 mb-4">
                <div class="card">
                    <div class="card-header">
                        <img src="<?php echo base_url('assets/images/logo-text.png'); ?>" alt="<?php echo $var->project; ?>" class="logo">
                        <ul class="nav nav-tabs card-header-tabs" id="myTab" role="tablist">
                            <li class="nav-item">
                                <a class="nav-link active" id="login-tab" data-toggle="tab" href="#login" role="tab"><?php echo $lang->sys_login_submit; ?></a>
                            </li>
                            <li class="nav-item">
                                <a class="nav-link" id="register-tab" data-toggle="tab" href="#register" role="tab"><?php echo $lang->sys_login_register; ?></a>
                            </li>
                            <li class="nav-item">
                                <a class="nav-link" id="report-tab" data-toggle="tab" href="#report" role="tab"><?php echo $lang->sys_report_issue; ?></a>
                            </li>
                        </ul>
                    </div>
                    <div class="card-body">
                        <div class="tab-content" id="myTabContent">
                            <div class="tab-pane fade show active" id="login" role="tabpanel">
                                <form action="<?php echo base_url('authen/login') ?>" method="post">
                                    <div class="form-group">
                                        <label for="username"><i class="fas fa-user"></i> <?php echo $lang->sys_login_username; ?></label>
                                        <input type="text" class="form-control" id="username" name="username" required>
                                    </div>
                                    <div class="form-group">
                                        <label for="password"><i class="fas fa-lock"></i> <?php echo $lang->sys_login_password; ?></label>
                                        <input type="password" class="form-control" id="password" name="passwd" required>
                                    </div>
                                    <div class="form-group form-check">
                                        <input type="checkbox" class="form-check-input" id="showPassword">
                                        <label class="form-check-label" for="showPassword"><?php echo $lang->sys_login_seepass; ?></label>
                                    </div>
                                    <?php if ($this->session->flashdata("msg") != "") { ?>
                                        <div class="alert alert-<?php echo $this->session->flashdata("type"); ?> alert-dismissible fade show" role="alert">
                                            <?php echo $this->session->flashdata("msg"); ?>
                                            <button type="button" class="close" data-dismiss="alert" aria-label="Close">
                                                <span aria-hidden="true">&times;</span>
                                            </button>
                                        </div>
                                    <?php } ?>
                                    <button type="submit" class="btn btn-primary btn-block"><?php echo $lang->sys_login_submit; ?></button>
                                </form>
                            </div>
                            <div class="tab-pane fade" id="register" role="tabpanel">
                                <form id="registerForm" action="#" method="post">
                                    <div class="form-row">
                                        <div class="form-group col-md-6">
                                            <label for="department_id">แผนก</label>
                                            <select class="form-control" id="department_id" name="department_id" required>
                                                <option value="">เลือกแผนก</option>
                                                <?php foreach ($department as $row) { ?>
                                                    <option value="<?php echo $row->id; ?>"><?php echo $row->name; ?></option>
                                                <?php } ?>
                                            </select>
                                        </div>
                                        <div class="form-group col-md-6">
                                            <label for="sub_department_id">แผนกย่อย</label>
                                            <select class="form-control" id="sub_department_id" name="sub_department_id" disabled>
                                                <!-- เพิ่มตัวเลือกแผนกย่อยโดยไดนามิก -->
                                            </select>
                                        </div>
                                    </div>
                                    <div class="form-row">
                                        <div class="form-group col-md-6">
                                            <label for="emp_code">รหัสพนักงาน</label>
                                            <input type="text" class="form-control" id="emp_code" name="emp_code" required>
                                        </div>
                                        <div class="form-group col-md-6">
                                            <label for="username">ชื่อผู้ใช้</label>
                                            <input type="text" class="form-control" id="username" name="username" required>
                                        </div>
                                    </div>
                                    <div class="form-row">
                                        <div class="form-group col-md-4">
                                            <label for="gender_id">เพศ</label>
                                            <select class="form-control" id="gender_id" name="gender_id" required>
                                                <option value="M">ชาย</option>
                                                <option value="F">หญิง</option>
                                                <option value="O">อื่นๆ</option>
                                            </select>
                                        </div>
                                        <div class="form-group col-md-4">
                                            <label for="prefix_name">คำนำหน้าชื่อ</label>
                                            <select class="form-control" id="prefix_name" name="prefix_name" required>
                                                <option value="นาย">นาย</option>
                                                <option value="นาง">นาง</option>
                                                <option value="นางสาว">นางสาว</option>
                                                <option value="Mr.">Mr.</option>
                                                <option value="Mrs.">Mrs.</option>
                                                <option value="Miss">Miss</option>
                                            </select>
                                        </div>
                                        <div class="form-group col-md-4">
                                            <label for="position_id">ตำแหน่ง</label>
                                            <select class="form-control" id="position_id" name="position_id" required>
                                                <option value="">เลือกตำแหน่ง</option>
                                                <?php foreach ($position as $row) { ?>
                                                    <option value="<?php echo $row->id; ?>"><?php echo $row->name; ?></option>
                                                <?php } ?>
                                            </select>
                                        </div>
                                    </div>
                                    <div class="form-row">
                                        <div class="form-group col-md-6">
                                            <label for="first_name">ชื่อ</label>
                                            <input type="text" class="form-control" id="first_name" name="first_name" required>
                                        </div>
                                        <div class="form-group col-md-6">
                                            <label for="last_name">นามสกุล</label>
                                            <input type="text" class="form-control" id="last_name" name="last_name" required>
                                        </div>
                                    </div>
                                    <div class="form-row">
                                        <div class="form-group col-md-6">
                                            <label for="tel">เบอร์โทรศัพท์</label>
                                            <input type="tel" class="form-control" id="tel" name="tel" required>
                                        </div>
                                        <div class="form-group col-md-6">
                                            <label for="email">อีเมล</label>
                                            <input type="email" class="form-control" id="email" name="email" required>
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <label for="card_number">หมายเลขบัตร</label>
                                        <input type="text" class="form-control" id="card_number" name="card_number" required>
                                    </div>
                                    <div class="form-row">
                                        <div class="form-group col-md-6">
                                            <label for="default_module_id">โมดูลเริ่มต้น</label>
                                            <select class="form-control" id="default_module_id" name="default_module_id" disabled>
                                                <!-- เพิ่มตัวเลือกโมดูลโดยไดนามิก -->
                                            </select>
                                        </div>
                                        <div class="form-group col-md-6">
                                            <label for="cng_lang">ภาษาที่ต้องการ</label>
                                            <select class="form-control" id="cng_lang" name="cng_lang" required>
                                                <option value="EN">อังกฤษ</option>
                                                <option value="TH">ไทย</option>
                                                <option value="CN">จีน</option>
                                            </select>
                                        </div>
                                    </div>
                                    <button type="submit" class="btn btn-primary">ลงทะเบียน</button>
                                </form>
                            </div>
                            <div class="tab-pane fade" id="report" role="tabpanel">
                                <form id="reportIssueForm" enctype="multipart/form-data">
                                    <div class="form-group">
                                        <label for="issueType">ประเภทปัญหา</label>
                                        <select class="form-control" id="issueType" name="issueType" required>
                                            <option value="">เลือกประเภทปัญหา</option>
                                            <option value="login">ปัญหาการเข้าสู่ระบบ</option>
                                            <option value="account">ปัญหาเกี่ยวกับบัญชีผู้ใช้</option>
                                            <option value="system">ระบบทำงานผิดพลาด</option>
                                            <option value="data">ข้อมูลไม่ถูกต้องหรือหาย</option>
                                            <option value="permission">ปัญหาสิทธิ์การใช้งาน</option>
                                            <option value="performance">ระบบทำงานช้า</option>
                                            <option value="ui">ปัญหาการแสดงผลหน้าจอ</option>
                                            <option value="report">รายงานไม่ถูกต้อง</option>
                                            <option value="suggestion">ข้อเสนอแนะ/ความคิดเห็น</option>
                                            <option value="other">อื่นๆ</option>
                                        </select>
                                    </div>
                                    <div class="form-group">
                                        <label for="urgencyLevel">ระดับความเร่งด่วน</label>
                                        <select class="form-control" id="urgencyLevel" name="urgencyLevel" required>
                                            <option value="">เลือกระดับความเร่งด่วน</option>
                                            <option value="low">ต่ำ - สามารถรอได้</option>
                                            <option value="medium">ปานกลาง - ควรได้รับการแก้ไขเร็วๆนี้</option>
                                            <option value="high">สูง - จำเป็นต้องได้รับการแก้ไขโดยเร็ว</option>
                                            <option value="critical">วิกฤต - ต้องได้รับการแก้ไขทันที</option>
                                        </select>
                                    </div>
                                    <div class="form-group">
                                        <label for="affectedSystem">ระบบที่ได้รับผลกระทบ</label>
                                        <select class="form-control" id="affectedSystem" name="affectedSystem" required>
                                            <option value="">เลือกระบบที่ได้รับผลกระทบ</option>
                                            <option value="hr">ระบบบุคลากร</option>
                                            <option value="finance">ระบบการเงิน</option>
                                            <option value="inventory">ระบบคลังสินค้า</option>
                                            <option value="production">ระบบการผลิต</option>
                                            <option value="sales">ระบบขาย</option>
                                            <option value="crm">ระบบลูกค้าสัมพันธ์</option>
                                            <option value="website">เว็บไซต์</option>
                                            <option value="other">อื่นๆ</option>
                                        </select>
                                    </div>
                                    <div class="form-group">
                                        <label for="issueDescription">รายละเอียดปัญหา</label>
                                        <textarea class="form-control" id="issueDescription" name="issueDescription" rows="4" required></textarea>
                                    </div>
                                    <div class="form-group">
                                        <label for="expectedResult">ผลลัพธ์ที่คาดหวัง</label>
                                        <textarea class="form-control" id="expectedResult" name="expectedResult" rows="2"></textarea>
                                    </div>
                                    <div class="form-group">
                                        <label for="contactName">ชื่อผู้แจ้งปัญหา</label>
                                        <input type="text" class="form-control" id="contactName" name="contactName" required>
                                    </div>
                                    <div class="form-group">
                                        <label for="contactEmail">อีเมลติดต่อกลับ</label>
                                        <input type="email" class="form-control" id="contactEmail" name="contactEmail" required>
                                    </div>
                                    <div class="form-group">
                                        <label for="contactPhone">เบอร์โทรศัพท์ (ไม่บังคับ)</label>
                                        <input type="tel" class="form-control" id="contactPhone" name="contactPhone">
                                    </div>
                                    <div class="form-group">
                                        <label for="attachFile">แนบไฟล์ (ไม่บังคับ)</label>
                                        <input type="file" class="form-control-file" id="attachFile" name="attachFile">
                                        <small class="form-text text-muted">อนุญาตไฟล์: gif, jpg, png, pdf, doc, docx, xls, xlsx, ppt, pptx (ขนาดไม่เกิน 5MB)</small>
                                    </div>
                                    <button type="submit" class="btn btn-primary">ส่งรายงานปัญหา</button>
                                </form>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
            <div class="col-md-6">
                <div class="queue-display">
                    <h3 class="text-center mb-4">สถานะคิว</h3>
                    <div class="row mb-4">
                        <div class="col-6 text-center">
                            <h5>คิวปัจจุบัน</h5>
                            <div id="currentQueue" class="queue-number">-</div>
                        </div>
                        <div class="col-6 text-center">
                            <h5>คิวถัดไป</h5>
                            <div id="nextQueue" class="queue-number">-</div>
                        </div>
                    </div>
                    <div class="table-responsive">
                        <table class="table table-sm queue-table">
                            <thead>
                                <tr>
                                    <th>คิว</th>
                                    <th>ประเภท</th>
                                    <th>ความเร่งด่วน</th>
                                    <th>สถานะ</th>
                                </tr>
                            </thead>
                            <tbody id="queueTableBody">
                                <!-- Queue data will be added here with JavaScript -->
                            </tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <script src="https://code.jquery.com/jquery-3.5.1.slim.min.js"></script>
    <script src="https://cdn.jsdelivr.net/npm/@popperjs/core@2.5.3/dist/umd/popper.min.js"></script>
    <script src="https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js"></script>
</body>

</html>
<script>
    // test git
    $(document).ready(function() {
        $('#showPassword').change(function() {
            $('#password').attr('type', $(this).prop('checked') ? 'text' : 'password');
        });
    });
    $(".preloader").fadeOut();
    $(':input:enabled:visible:first').focus();

    function myFunction() {
        var x = document.getElementById("myInput");
        if (x.type === "password") {
            x.type = "text";
        } else {
            x.type = "password";
        }
    }

    $(document).ready(function() {
        $('#registerForm').submit(function(e) {
            e.preventDefault();
            var formData = $(this).serialize();

            $.ajax({
                url: '<?php echo base_url("authen/register"); ?>',
                type: 'POST',
                data: formData,
                dataType: 'json',
                success: function(response) {
                    if (response.status === 'success') {
                        alert('ลงทะเบียนสำเร็จ: ' + response.message);
                        $('#registerForm')[0].reset();
                        // อาจจะเพิ่มการเปลี่ยนแท็บไปยังหน้า login หรือแสดงข้อความเพิ่มเติม
                    } else {
                        alert('เกิดข้อผิดพลาด: ' + response.message);
                    }
                },
                error: function(xhr, status, error) {
                    alert('เกิดข้อผิดพลาดในการส่งข้อมูล: ' + error);
                }
            });
        });

        // เพิ่มการตรวจสอบความถูกต้องของข้อมูลก่อนส่งฟอร์ม
        $('#registerForm input').on('blur', function() {
            var field = $(this);
            var fieldName = field.attr('name');
            var fieldValue = field.val();

            if (fieldValue.trim() === '') {
                return; // ไม่ตรวจสอบถ้าฟิลด์ว่างเปล่า
            }

            $.ajax({
                url: '<?php echo base_url("authen/checkField"); ?>',
                type: 'POST',
                data: {
                    field: fieldName,
                    value: fieldValue
                },
                dataType: 'json',
                success: function(response) {
                    if (response.status === 'error') {
                        field.addClass('is-invalid');
                        field.next('.invalid-feedback').remove();
                        field.after('<div class="invalid-feedback">' + response.message + '</div>');
                    } else {
                        field.removeClass('is-invalid');
                        field.next('.invalid-feedback').remove();
                    }
                }
            });
        });

        // form report issue submit then ajax to send email
        $('#submitIssue').click(function() {
            var formData = new FormData();
            formData.append('issueType', $('#issueType').val());
            formData.append('urgencyLevel', $('#urgencyLevel').val());
            formData.append('affectedSystem', $('#affectedSystem').val());
            formData.append('issueDescription', $('#issueDescription').val());
            formData.append('expectedResult', $('#expectedResult').val());
            formData.append('contactName', $('#contactName').val());
            formData.append('contactEmail', $('#contactEmail').val());
            formData.append('contactPhone', $('#contactPhone').val());
            formData.append('attachFile', $('#attachFile')[0].files[0]);

            $.ajax({
                type: "POST",
                url: "<?php echo base_url('authen/reportIssue') ?>",
                data: formData,
                contentType: false,
                processData: false,
                success: function(data) {
                    if (data.status == 'success') {
                        Swal.fire({
                            title: 'Success!',
                            text: 'Your issue has been reported successfully.',
                            type: 'success',
                            confirmButtonText: 'OK'
                        });
                        $('#reportIssueModal').modal('hide');
                    } else {
                        Swal.fire({
                            title: 'Error!',
                            text: 'An error occurred while reporting the issue. Please try again later.',
                            type: 'error',
                            confirmButtonText: 'OK'
                        });
                    }
                }
            });
        });
    });
</script>