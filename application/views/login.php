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
                                    <!-- Registration form fields here -->
                                </form>
                            </div>
                            <div class="tab-pane fade" id="report" role="tabpanel">
                                <form id="reportIssueForm">
                                    <!-- Report issue form fields here -->
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
    $(document).ready(function() {
        $('#showPassword').change(function() {
            $('#password').attr('type', $(this).prop('checked') ? 'text' : 'password');
        });
    });
    $(".preloader").fadeOut();
    $(':input:enabled:visible:first').focus();

    $(function() {
        // $("#includedContent").load('<?PHP echo base_url(); ?>assets/cookie/cookies.html');
    });

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
            var password = $('#registerPassword').val();
            var confirm_password = $('#registerConfirmPassword').val();
            if (password != confirm_password) {
                Swal.fire({
                    title: 'Error!',
                    text: 'Password and Confirm Password do not match.',
                    type: 'error',
                    confirmButtonText: 'OK'
                });
                return;
            }
            $.ajax({
                type: "POST",
                url: "<?php echo base_url('authen/register') ?>",
                data: $(this).serialize(),
                success: function(data) {
                    console.log(data);

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