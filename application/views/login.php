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
    <!-- เพิ่ม select เปลี่ยนภาษา -->

</head>

<div class="container" style="margin-top: 20px; position: absolute; top: 0; right: 0;">
    <div class="row justify-content-end">
        <div class="col-md-2">
            <div class="form-group">
                <select class="form-control auto-save" id="lang" name="lang">
                    <option value="en" href="<?php echo base_url("main/change_language/en"); ?>" data-id="en">English</option>
                    <option value="th" href="<?php echo base_url("main/change_language/th"); ?>" data-id="th">ไทย</option>
                    <option value="cn" href="<?php echo base_url("main/change_language/cn"); ?>" data-id="cn">中文</option>
                </select>
            </div>
        </div>
    </div>
</div>

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
                                            <label for="department_id"><?php echo $lang->sys_department; ?></label>
                                            <select class="form-control" id="department_id" name="department_id" required>
                                                <option value=""><?php echo $lang->sys_choose_department; ?></option>
                                                <?php foreach ($department as $row) { ?>
                                                    <option value="<?php echo $row->id; ?>"><?php echo $row->name; ?></option>
                                                <?php } ?>
                                            </select>
                                        </div>
                                        <div class="form-group col-md-6">
                                            <label for="sub_department_id"><?php echo $lang->system_sub_department; ?></label>
                                            <select class="form-control" id="sub_department_id" name="sub_department_id" disabled>
                                                <!-- Add dynamic sub department options here -->
                                            </select>
                                        </div>
                                    </div>
                                    <div class="form-row">
                                        <div class="form-group col-md-6">
                                            <label for="emp_code"><?php echo $lang->system_emp_code; ?></label>
                                            <input type="text" class="form-control" id="emp_code" name="emp_code" required>
                                        </div>
                                        <div class="form-group col-md-6">
                                            <label for="username"><?php echo $lang->system_username; ?></label>
                                            <input type="text" class="form-control" id="username" name="username" required>
                                        </div>
                                    </div>
                                    <div class="form-row">
                                        <div class="form-group col-md-4">
                                            <label for="gender_id"><?php echo $lang->system_gender; ?></label>
                                            <select class="form-control" id="gender_id" name="gender_id" required>
                                                <option value="M"><?php echo $lang->system_men; ?></option>
                                                <option value="F"><?php echo $lang->system_girl; ?></option>
                                                <option value="O"><?php echo $lang->system_other; ?></option>
                                            </select>
                                        </div>
                                        <div class="form-group col-md-4">
                                            <label for="prefix_name"><?php echo $lang->system_prefix_name; ?></label>
                                            <select class="form-control" id="prefix_name" name="prefix_name" required>
                                                <option value="นาย"><?php echo $lang->system_mister; ?></option>
                                                <option value="นาง"><?php echo $lang->system_mrs; ?></option>
                                                <option value="นางสาว"><?php echo $lang->system_miss; ?></option>
                                                <option value="Mr.">Mr.</option>
                                                <option value="Mrs.">Mrs.</option>
                                                <option value="Miss">Miss</option>
                                            </select>
                                        </div>
                                        <div class="form-group col-md-4">
                                            <label for="position_id"><?php echo $lang->system_position; ?></label>
                                            <select class="form-control" id="position_id" name="position_id" required>
                                                <option value=""><?php echo $lang->system_choose_position; ?></option>
                                                <?php foreach ($position as $row) { ?>
                                                    <option value="<?php echo $row->id; ?>"><?php echo $row->name; ?></option>
                                                <?php } ?>
                                            </select>
                                        </div>
                                    </div>
                                    <div class="form-row">
                                        <div class="form-group col-md-6">
                                            <label for="first_name"><?php echo $lang->system_first_name; ?></label>
                                            <input type="text" class="form-control" id="first_name" name="first_name" required>
                                        </div>
                                        <div class="form-group col-md-6">
                                            <label for="last_name"><?php echo $lang->system_last_name; ?></label>
                                            <input type="text" class="form-control" id="last_name" name="last_name" required>
                                        </div>
                                    </div>
                                    <div class="form-row">
                                        <div class="form-group col-md-6">
                                            <label for="tel"><?php echo $lang->system_tel; ?></label>
                                            <input type="tel" class="form-control" id="tel" name="tel" required>
                                        </div>
                                        <div class="form-group col-md-6">
                                            <label for="email"><?php echo $lang->system_email; ?></label>
                                            <input type="email" class="form-control" id="email" name="email" required>
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <label for="card_number"><?php echo $lang->system_card_number; ?></label>
                                        <input type="text" class="form-control" id="card_number" name="card_number" required>
                                    </div>
                                    <div class="form-row">
                                        <div class="form-group col-md-6">
                                            <label for="default_module_id"><?php echo $lang->system_default_module; ?></label>
                                            <select class="form-control" id="default_module_id" name="default_module_id" disabled>
                                                <!-- Add dynamic default module options here -->
                                            </select>
                                        </div>
                                        <div class="form-group col-md-6">
                                            <label for="cng_lang"><?php echo $lang->system_cng_lang; ?></label>
                                            <select class="form-control" id="cng_lang" name="cng_lang" required>
                                                <option value="EN"><?php echo $lang->system_eng; ?></option>
                                                <option value="TH"><?php echo $lang->system_thai; ?></option>
                                                <option value="CN"><?php echo $lang->system_china; ?></option>
                                            </select>
                                        </div>
                                    </div>
                                    <button type="submit" class="btn btn-primary"><?php echo $lang->system_register; ?></button>
                                </form>
                            </div>
                            <div class="tab-pane fade" id="report" role="tabpanel">
                                <form id="reportIssueForm" enctype="multipart/form-data">
                                    <div class="form-group">
                                        <label for="issueType"><?php echo $lang->system_issue_type; ?></label>
                                        <select class="form-control" id="issueType" name="issueType" required>
                                            <option value=""><?php echo $lang->system_choose_issue_type; ?></option>
                                            <option value="login"><?php echo $lang->system_login; ?></option>
                                            <option value="account"><?php echo $lang->system_account; ?></option>
                                            <option value="system"><?php echo $lang->system_system; ?></option>
                                            <option value="data"><?php echo $lang->system_data; ?></option>
                                            <option value="permission"><?php echo $lang->system_permission; ?></option>
                                            <option value="performance"><?php echo $lang->system_performance; ?></option>
                                            <option value="ui"><?php echo $lang->system_ui; ?></option>
                                            <option value="report"><?php echo $lang->system_report; ?></option>
                                            <option value="suggestion"><?php echo $lang->system_suggestion; ?></option>
                                            <option value="other"><?php echo $lang->system_other; ?></option>
                                        </select>
                                    </div>
                                    <div class="form-group">
                                        <label for="urgencyLevel"><?php echo $lang->system_urgency_level; ?></label>
                                        <select class="form-control" id="urgencyLevel" name="urgencyLevel" required>
                                            <option value=""><?php echo $lang->system_choose_urgency_level; ?></option>
                                            <option value="low"><?php echo $lang->system_low; ?></option>
                                            <option value="medium"><?php echo $lang->system_medium; ?></option>
                                            <option value="high"><?php echo $lang->system_high; ?></option>
                                            <option value="critical"><?php echo $lang->system_critical; ?></option>
                                        </select>
                                    </div>
                                    <div class="form-group">
                                        <label for="affectedSystem"><?php echo $lang->system_affected_system; ?></label>
                                        <select class="form-control" id="affectedSystem" name="affectedSystem" required>
                                            <option value=""><?php echo $lang->system_choose_affected_system; ?></option>
                                            <option value="hr"><?php echo $lang->system_hr; ?></option>
                                            <option value="finance"><?php echo $lang->system_finance; ?></option>
                                            <option value="inventory"><?php echo $lang->system_inventory; ?></option>
                                            <option value="production"><?php echo $lang->system_production; ?></option>
                                            <option value="sales"><?php echo $lang->system_sales; ?></option>
                                            <option value="crm"><?php echo $lang->system_crm; ?></option>
                                            <option value="website"><?php echo $lang->system_website; ?></option>
                                            <option value="other"><?php echo $lang->system_other; ?></option>
                                        </select>
                                    </div>
                                    <div class="form-group">
                                        <label for="issueDescription"><?php echo $lang->system_issue_description; ?></label>
                                        <textarea class="form-control" id="issueDescription" name="issueDescription" rows="4" required></textarea>
                                    </div>
                                    <div class="form-group">
                                        <label for="expectedResult"><?php echo $lang->system_expected_result; ?></label>
                                        <textarea class="form-control" id="expectedResult" name="expectedResult" rows="2"></textarea>
                                    </div>
                                    <div class="form-group">
                                        <label for="contactName"><?php echo $lang->system_contact_name; ?></label>
                                        <input type="text" class="form-control" id="contactName" name="contactName" required>
                                    </div>
                                    <div class="form-group">
                                        <label for="contactEmail"><?php echo $lang->system_contact_email; ?></label>
                                        <input type="email" class="form-control" id="contactEmail" name="contactEmail" required>
                                    </div>
                                    <div class="form-group">
                                        <label for="contactPhone"><?php echo $lang->system_contact_phone; ?></label>
                                        <input type="tel" class="form-control" id="contactPhone" name="contactPhone">
                                    </div>
                                    <div class="form-group">
                                        <label for="attachFile"><?php echo $lang->system_attach_file; ?></label>
                                        <input type="file" class="form-control-file" id="attachFile" name="attachFile">
                                        <small class="form-text text-muted"><?php echo $lang->system_allow_file; ?></small>
                                    </div>
                                    <button type="submit" class="btn btn-primary"><?php echo $lang->system_report_issue; ?></button>
                                </form>
                            </div>
                            </form>
                        </div>
                    </div>
                </div>
            </div>
            <div class="col-md-6">
                <div class="queue-display">
                    <h3 class="text-center mb-4"><?php echo $lang->system_queue_status; ?></h3>
                    <div class="row mb-4">
                        <div class="col-6 text-center">
                            <h5><?php echo $lang->system_current_queue; ?></h5>
                            <div id="currentQueue" class="queue-number">-</div>
                        </div>
                        <div class="col-6 text-center">
                            <h5><?php echo $lang->system_next_queue; ?></h5>
                            <div id="nextQueue" class="queue-number">-</div>
                        </div>
                    </div>
                    <div class="table-responsive">
                        <table class="table table-sm queue-table">
                            <thead>
                                <tr>
                                    <th><?php echo $lang->system_queue; ?></th>
                                    <th><?php echo $lang->system_type; ?></th>
                                    <th><?php echo $lang->system_urgency; ?></th>
                                    <th><?php echo $lang->system_status; ?></th>
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
</body>

</html>


<script src="https://code.jquery.com/jquery-3.5.1.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/@popperjs/core@2.5.3/dist/umd/popper.min.js"></script>
<script src="https://stackpath.bootstrapcdn.com/bootstrap/4.5.2/js/bootstrap.min.js"></script>
<script src="https://cdn.jsdelivr.net/npm/sweetalert2@10.16.3/dist/sweetalert2.all.min.js"></script>


<script>
    // test git
    function myFunction() {
        var x = document.getElementById("myInput");
        if (x.type === "password") {
            x.type = "text";
        } else {
            x.type = "password";
        }
    }

    $(document).ready(function() {
        // lang
        $('#lang').change(function() {
            var lang = $(this).val();
            var url = $(this).find('option:selected').attr('href');
            // console.log(url);
            window.location.href = url;
        });
        $('#registerForm').submit(function(e) {
            e.preventDefault();
            var formData = $(this).serialize();
            console.log(formData);

            $.ajax({
                url: '<?php echo base_url("authen/register"); ?>',
                type: 'POST',
                data: formData,
                dataType: 'json',
                success: function(response) {
                    if (response.status === 'success') {
                        // alert('ลงทะเบียนสำเร็จ: ' + response.message);
                        Swal.fire({
                            title: 'Success!',
                            text: 'ลงทะเบียนสำเร็จ: ' + response.message,
                            type: 'success',
                            confirmButtonText: 'OK'
                        });
                        $('#registerForm')[0].reset();
                        // อาจจะเพิ่มการเปลี่ยนแท็บไปยังหน้า login หรือแสดงข้อความเพิ่มเติม
                    } else {
                        // alert('เกิดข้อผิดพลาด: ' + response.message);
                        Swal.fire({
                            title: 'Error!',
                            text: 'เกิดข้อผิดพลาด: ' + response.message,
                            type: 'error',
                            confirmButtonText: 'OK'
                        });
                    }
                },
                error: function(xhr, status, error) {
                    // alert('เกิดข้อผิดพลาดในการส่งข้อมูล: ' + error);
                    Swal.fire({
                        title: 'Error!',
                        text: 'เกิดข้อผิดพลาดในการส่งข้อมูล: ' + error,
                        type: 'error',
                        confirmButtonText: 'OK'
                    });
                }
            });
        });

        // เพิ่มการตรวจสอบความถูกต้องของข้อมูลก่อนส่งฟอร์ม
        $('#registerForm input').on('blur', function() {
            var field = $(this);
            var fieldName = field.attr('name');
            var fieldValue = field.val();
            // ตรวจสอบ attribute ถ้าเป็นอันอื่นนอกจาก emp_code  username number email จะไม่ตรวจสอบ
            if (fieldName !== 'emp_code' && fieldName !== 'username' && fieldName !== 'tel' && fieldName !== 'email' && fieldName !== 'card_number') {
                return;
            }
            console.log(fieldName);

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

        $('#showPassword').change(function() {
            $('#password').attr('type', $(this).prop('checked') ? 'text' : 'password');
        });
    });
</script>