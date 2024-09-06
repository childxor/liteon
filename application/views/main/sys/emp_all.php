<?php /* var_dump($permission); */ ?>
<?php (empty($permission) ? redirect(base_url("error404")) : ""); ?>
<?php if (!empty($this->efs_lib->is_can($permission, "view"))) { /* Can View */ ?>
    <?php $this->db =  $this->load->database('HQMS_IPS', true); ?>

    <!-- ============================================================== -->
    <!-- Start Page Content -->
    <!-- ============================================================== -->
    <style>
        .help-block {
            font-size: 0.9rem !important;
        }

        .bootstrap-switch-container {
            /* กำหนดความยาวคงให้ <?php echo $lang_module->lbl_status; ?> แก้ปัญหาย่อขยายตอน*/
            width: 90px !important;
        }

        /*  */
    </style>
    <!-- This page plugin CSS -->
    <link href="<?php echo base_url(); ?>assets/libs/datatables.net-bs4/css/dataTables.bootstrap4.css" rel="stylesheet">
    <link rel="stylesheet" type="text/css" href="<?php echo base_url(); ?>assets/libs/bootstrap-switch/dist/css/bootstrap3/bootstrap-switch.min.css">
    <div class="card">
        <div class="card-body">
            <div class="mb-3">
                <div class="row">
                    <div class="col-md-3 col-lg-3 mb-2">
                        <select class="form-control" id="department_id" name="deptName">
                            <option value=""><?php echo $lang_module->lbl_department; ?></option>
                            <?php
                            foreach ($dept as $department) : ?>
                                <option value="<?php echo $department->deptID; ?>"><?php echo $department->name; ?></option>
                            <?php endforeach; ?>
                        </select>
                    </div>
                    <div class="col-md-3 col-lg-3 mb-2">
                        <select class="form-control auto-save" id="is_active">
                            <option value=""><?php echo $lang_module->lbl_status; ?></option>
                            <option value="N"><?php echo $lang_module->lbl_deactive; ?></option>
                            <option value="Y"><?php echo $lang_module->lbl_active; ?></option>
                        </select>
                    </div>
                    <div class="col-md-3 col-lg-3 mb-2">
                        <select class="form-control auto-save" id="shift_type">
                            <option value=""><?php echo $lang_module->lbl_shift; ?></option>
                            <option value="A"><?php echo $lang_module->lbl_shift_a; ?></option>
                            <option value="B"><?php echo $lang_module->lbl_shift_b; ?></option>
                            <option value="N"><?php echo $lang_module->lbl_no_shift; ?></option>
                        </select>
                    </div>
                    <div class="col-md-3 col-lg-3 mb-2">
                        <button class="btn btn-info btn-block" id="btnSearch" onclick="provTable.ajax.reload();">
                            <i class="fas fa-search"></i> <?php echo $lang_module->lbl_search; ?>
                        </button>
                    </div>
                    <!-- <?php if ($this->session->userdata('user_profile')->id == '463') : ?>
                        <div class="col-md-4 col-lg-3 mb-2">
                            <button class="btn btn-secondary btn-block" id="btnSync">
                                <i class="fas fa-sync"></i> Sync
                            </button>
                        </div>
                        <div class="col-md-4 col-lg-3 mb-2">
                            <button class="btn btn-secondary btn-block" id="btnSyncName">
                                <i class="fas fa-sync"></i> Sync Name
                            </button>
                        </div>
                    <?php endif; ?> -->
                </div>
            </div>

            <?php if ($this->efs_lib->is_can($permission, "add")) : ?>
                <div class="text-right mb-3">
                    <button id="addBtn" class="btn btn-primary" data-toggle="modal" data-target="#addModal">
                        <i class="fa fa-plus"></i> <?php echo $lang_module->lbl_add; ?>
                    </button>
                </div>
            <?php endif; ?>

            <div class="table-responsive">
                <table id="mTable" class="table table-bordered table-striped table-hover" style="width:100%"></table>
            </div>
        </div>
    </div>

    <?php if ($this->efs_lib->is_can($permission, "edit")) { ?>
        <div class="modal fade" id="addModal" tabindex="-1" role="dialog" aria-labelledby="addModalLabel">
            <form id="addForm" method="post" action="<?php echo base_url('sys/user/add'); ?>" novalidate>
                <div class="modal-dialog" role="document">
                    <div class="modal-content">
                        <div class="modal-body">
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label class="control-label">Employee ID<span class="text-danger">*</span></label>
                                        <div class="controls">
                                            <input type="text" class="form-control validated" name="cardNo" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>" autocomplete="off">
                                        </div>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label class="control-label">Card Number<span class="text-danger">*</span></label>
                                        <div class="controls">
                                            <input type="text" class="form-control validated" name="cardNumber" autocomplete="off">
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label class="control-label">Person ID Card<span class="text-danger">*</span></label>
                                        <div class="controls">
                                            <input type="text" class="form-control validated" readonly name="personIDc" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>" autocomplete="off">
                                        </div>
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <div class="form-group">
                                        <label class="control-label">Person ID Finger<span class="text-danger">*</span></label>
                                        <div class="controls">
                                            <input type="text" class="form-control validated" readonly name="personIDfp" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>" autocomplete="off">
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-md-8">
                                    <div class="form-group">
                                        <label class="control-label">Name<span class="text-danger">*</span></label>
                                        <div class="controls">
                                            <input type="text" class="form-control validated" name="name" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>" autocomplete="off">
                                        </div>
                                    </div>
                                </div>
                                <div class="col-md-4">
                                    <div class="form-group">
                                        <label class="control-label">Shift<span class="text-danger">*</span></label>
                                        <div class="controls">
                                            <select class="form-control form-control-line validated" name="shift">
                                                <option value=""><?php echo $lang_module->lbl_shift; ?></option>
                                                <option value="no">ไม่เข้ากะ</option>
                                                <option value="A">A</option>
                                                <option value="B">B</option>
                                            </select>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label">Department Name<span class="text-danger">*</span></label>
                                <div class="controls">
                                    <select class="form-control form-control-line validated" name="deptName" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>">
                                        <option value=""><?php echo $lang_module->lbl_department; ?></option>
                                        <?php foreach ($dept as $department) { ?>
                                            <option value="<?php echo $department->deptID; ?>"><?php echo $department->name; ?></option>
                                        <?php } ?>
                                    </select>
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label">Department ID<span class="text-danger">*</span></label>
                                <div class="controls">
                                    <input type="text" class="form-control validated" name="deptID" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>" autocomplete="off" readonly>
                                </div>
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="reset" class="btn btn-sm btn-default" data-dismiss="modal"><?php echo $lang_module->lbl_cancel; ?></button>
                            <button type="submit" class="btn btn-sm btn-primary"><?php echo $lang_module->lbl_save; ?></button>
                        </div>
                    </div>
                </div>
            </form>
        </div>
    <?php } ?>

    <?php if ($this->efs_lib->is_can($permission, "edit")) { ?>
        <div class="modal fade" id="addAccessModal" role="dialog" aria-labelledby="addAccessModalLabel" aria-hidden="true" data-backdrop="static" data-keyboard="false">
            <form id="addAccessForm" method="post" action="<?php echo base_url('sys/user/addAccess'); ?>" novalidate>
                <div class="modal-dialog" role="document" style="max-width: 75%;">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title" id="addAccessModalLabel">Access Control</h5>
                            <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                        </div>
                        <div class="modal-body">
                            <table id="mTableAccess" class="table table-bordered table-striped table-hover" style="width:100%"></table>
                        </div>
                        <div class="modal-footer">
                            <button class="btn btn-sm btn-primary addAccessAll"><i class="fas fa-check"></i> เพิ่มสิทธิ์ทั้งหมด</button>
                            <button class="btn btn-sm btn-danger addAccessHR"><i class="fas fa-times"></i> เพิ่มสิทธิ์เฉพาะ HR</button>
                            <button type="button" class="btn btn-sm btn-default delAccessAll"><i class="fas fa-trash"></i> ลบสิทธิ์ทั้งหมด</button>
                        </div>
                    </div>
                </div>
            </form>
        </div>
    <?php } ?>

    <!-- ============================================================== -->
    <!-- End Page Content -->
    <!-- ============================================================== -->
    <script src="<?php echo base_url(); ?>assets/libs/jquery/dist/jquery.min.js"></script>
    <!-- ============================================================== -->
    <!-- This page plugins -->
    <!-- ============================================================== -->
    <!-- This Page JS -->
    <script src="<?php echo base_url(); ?>assets/extra-libs/jqbootstrapvalidation/validation.js"></script>
    <script src="<?php echo base_url(); ?>assets/libs/bootstrap-switch/dist/js/bootstrap-switch.min.js"></script>

    <!-- Datatable Page JS -->
    <script src="<?php echo base_url(); ?>assets/libs/datatables/media/js/jquery.dataTables.min.js"></script>
    <script src="<?php echo base_url(); ?>dist/js/pages/datatable/custom-datatable.js"></script>
    <script src="<?php echo base_url(); ?>assets/libs/select2/dist/js/dataTables.rowReorder.min.js"></script>

    <script type="text/javascript">
        var provTable;
        var provTableAccess;
        ! function(window, document, $) {
            "use strict";
            $.fn.dataTable.ext.errMode = 'none';
            provTable = $('#mTable').DataTable({
                "autoWidth": false,
                "pageLength": <?php echo $this->session->userdata('user_profile')->cng_per_page; ?>,
                "processing": false,
                "bSort": true,
                "stateSave": false,
                "autoWidth": false,
                "cache": false,
                "language": {
                    "url": "<?php echo base_url() ?>dist/js/pages/datatable/<?php echo $this->session->userdata('user_profile')->cng_lang; ?>.json",
                    "headers": {
                        "Access-Control-Allow-Origin": "*"
                    },
                    searchPlaceholder: "<?php echo $lang_sys->lbl_search; ?>",
                },
                "ajax": {
                    "url": "<?php echo base_url('sys/emp_all/getUserfromHQMS'); ?>",
                    "type": "POST",
                    "data": function(d) {
                        d.deptID = $('#department_id').val();
                        d.is_active = $('#is_active').val();
                        d.shift_type = $('#shift_type').val();
                    }
                },
                "deferRender": true,
                "aLengthMenu": [
                    [10, 25, 50, 100],
                    [10, 25, 50, 100]
                ],
                "columns": [{
                        "data": "rowAutoID",
                        "className": "text-center",
                        "width": "20px",
                        "title": "<?php echo $lang_module->lbl_order; ?>",
                        "render": function(data, type, row, meta) {
                            return meta.row + 1;
                        }
                    },
                    {
                        "data": "cardNumber",
                        "className": "text-left",
                        "title": "cardNumber",
                        "render": function(data, type, row, meta) {
                            return '<div class="d-flex align-items-center">' + row.cardNumber + '  | <i class="' + (row.cardNumber2 == '' ? 'mdi mdi-account-off' : '') + '"></i><i class="' + (row.isFp == '0' ? 'mdi mdi-fingerprint' : '') + '"></i></div>';
                        }
                    },
                    {
                        "data": "name",
                        "className": "text-left",
                        "title": "<?php echo $lang_module->lbl_fullname; ?>",
                        "render": function(data, type, row, meta) {
                            return row.name + ' (' + row.isAuthDoor + ')';
                        }
                    },
                    {
                        "data": "deptName",
                        "className": "text-left",
                        "width": "140px",
                        "title": "<?php echo $lang_module->lbl_department; ?>",
                        "render": function(data, type, row, meta) {
                            var html = "<select class='form-control form-control-line validated' name='deptName' required data-validation-required-message='<?php echo $lang_sys->msg_require; ?>'>";
                            html += "<option value=''><?php echo $lang_module->lbl_department; ?></option>";
                            <?php foreach ($dept as $department) { ?>
                                html += "<option value='<?php echo $department->deptID; ?>' " + (row.deptID == '<?php echo $department->deptID; ?>' ? 'selected' : '') + "><?php echo $department->name; ?></option>";
                            <?php } ?>
                            html += "</select>";
                            return html;
                        }
                    },
                    {
                        "data": "deptID",
                        "className": "text-left",
                        "title": "<?php echo $lang_module->lbl_position; ?>",
                        "render": function(data, type, row, meta) {
                            return row.deptID;
                        }
                    },
                    <?php if ($this->efs_lib->is_can($permission, "edit")) { ?> {
                            "data": "isActive",
                            "className": "text-center",
                            "orderable": false,
                            "title": "<?php echo $lang_module->lbl_status; ?>",
                            "width": "90px",
                            "render": function(data, type, row, meta) {
                                var ch = function() {
                                    return row.isActive == '1' ? 'checked' : '';
                                };
                                return '<div class="custom-control custom-switch">' +
                                    '<input type="checkbox" class="status custom-control-input" id="switch' + row.cardNumber + '" ' + ch() + '>' +
                                    '<label class="custom-control-label" for="switch' + row.cardNumber + '"></label>' +
                                    '</div>';
                            }
                        },
                    <?php } ?>
                    <?php if ($this->efs_lib->is_can($permission, "edit")) { ?> {
                            "data": "shiftMent",
                            "className": "text-center",
                            "orderable": false,
                            "title": "<?php echo $lang_module->lbl_shift; ?>",
                            "width": "80px",
                            "render": function(data, type, row, meta) {
                                var isShiftA = (row.shiftMent == '' || row.shiftMent == 'N') ? false : true;
                                var html = '<div class="custom-control custom-switch">' +
                                    '<input type="checkbox" class="disableCa custom-control-input" ' + (isShiftA ? 'checked' : '') + ' id="' + row.cardNumber + 'Shca" data-personID="' + row.cardNumber + '-3">' +
                                    '<label class="custom-control-label" for="' + row.cardNumber + 'Shca"></label>' +
                                    '<span class="ml-2">' + (isShiftA ? 'On' : 'Off') + '</span>' +
                                    '</div>';
                                return (row.isActive == '0' ? '' : html);
                            }
                        },
                    <?php } ?>
                    // ถ้า shiftMent null ให้ disable switch
                    <?php if ($this->efs_lib->is_can($permission, "edit")) { ?> {
                            "data": "shiftMent",
                            "className": "text-center",
                            "orderable": false,
                            "title": "<?php echo $lang_module->lbl_shift; ?>",
                            "width": "80px",
                            "render": function(data, type, row, meta) {
                                var isShiftA = row.shiftMent == 'A';
                                // ถ้า shiftMent null ให้ disable switch
                                var html = '<div class="custom-control custom-switch">' +
                                    '<input type="checkbox" class="shiftMent custom-control-input" ' + (isShiftA ? 'checked' : '') + ' id="' + row.cardNumber + 'Sh" data-personID="' + row.cardNumber + '-3">' +
                                    '<label class="custom-control-label" for="' + row.cardNumber + 'Sh"></label>' +
                                    '<span class="ml-2">' + (isShiftA ? 'A' : 'B') + '</span>' +
                                    '</div>';
                                return (row.shiftMent == '' ? '' : html);
                            }
                        },
                    <?php } ?>
                    <?php if ($this->efs_lib->is_can($permission, "edit")) { ?> {
                            "data": "cardNumber",
                            "className": "text-center",
                            "orderable": false,
                            "title": "<i class='mdi mdi-account-key'></i>",
                            "width": "100px",
                            "render": function(data, type, row, meta) {
                                var html = '<div class="dropdown">' +
                                    '<button class="btn btn-secondary dropdown-toggle" type="button" id="dropdownMenu' + row.cardNumber + '" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">' +
                                    '<i class="fas fa-ellipsis-v"></i>' +
                                    '</button>' +
                                    '<div class="dropdown-menu" aria-labelledby="dropdownMenu' + row.cardNumber + '">';

                                <?php if ($this->efs_lib->is_can($permission, "edit")) { ?>
                                    html += '<a class="dropdown-item showAccess" href="#" data-card="' + row.cardNumber + '" data-toggle="modal" data-backdrop="static" data-keyboard="false" data-target="#addAccessModal">สิทธิ์การเข้าถึง</a>' +
                                        // '<a class="dropdown-item showAttendance" href="#" data-card="' + row.cardNumber + '">ข้อมูลการเข้างาน</a>' +
                                        // '<a class="dropdown-item showLeave" href="#" data-card="' + row.cardNumber + '">ข้อมูลการลา</a>' +
                                        '<a class="dropdown-item edit" href="#" data-card="' + row.cardNumber + '">แก้ไขข้อมูล</a>';
                                <?php } ?>

                                <?php if ($this->efs_lib->is_can($permission, "delete")) { ?>
                                    html += '<a class="dropdown-item delrow" href="#" data-id="' + row.cardNumber + '">ลบข้อมูล</a>';
                                <?php } ?>

                                html += '</div></div>';
                                return html;
                            }
                        },
                    <?php } ?>
                ],
                "fnRowCallback": function(nRow, aData, iDisplayIndex, iDisplayIndexFull) {
                    // deptName ให้เปลี่ยนเป็น select
                    var deptName = $(nRow).find('td:eq(3)');
                    deptName.html(deptName.find('select').val(aData.deptID).prop('outerHTML'));

                },
                "initComplete": function(settings, json) {
                    //    ถ้าแถวไหน isFp = 1 ให้เปลี่ยนสีคอลัมป์แรก
                }
            });
        }(window, document, jQuery);

        $(document).ready(function() {
            // addAccessModal
            // $(document).on('click', '.additionalInfo', function() {
            //     //    modal.show
            //     var dataRow = provTable.row($(this).parents('tr')).data();
            //     var value = $(this).val();
            //     if (value == 'access') {
            //         $('h5#addAccessModalLabel').text('Access Control: ' + dataRow.name);
            //         provTableAccess.clear().draw();
            //         $.ajax({
            //             url: '<?php echo base_url('sys/emp_all/getAccess'); ?>',
            //             type: 'POST',
            //             data: {
            //                 cardNumber: $(this).data('card')
            //             },
            //             success: function(data) {
            //                 provTableAccess.rows.add(data.data).draw();
            //             }
            //         });
            //     } else if (value == 'attendance') {
            //         window.location.href = '<?php echo base_url('sys/emp_all/attendance'); ?>?cardNumber=' + dataRow.cardNumber;
            //     } else if (value == 'leave') {
            //         window.location.href = '<?php echo base_url('sys/emp_all/leave'); ?>?cardNumber=' + dataRow.cardNumber;
            //     }
            // });

            $(document).on('click', '.addAccessAll', function(e) {
                e.preventDefault();
                $('#mTableAccess input[type="checkbox"]').each(function() {
                    if (!$(this).prop('checked')) {
                        $(this).prop('checked', true);
                        $(this).change();
                    }
                });
            });

            // deptName in row select 
            $(document).on('change', '#mTable select[name="deptName"]', function() {
                var deptID = $(this).val();
                var depName = $(this).find('option:selected').text();
                var dataRow = provTable.row($(this).parents('tr')).data();
                $.ajax({
                    url: '<?php echo base_url('sys/emp_all/updateDept'); ?>',
                    type: 'POST',
                    data: {
                        personName: dataRow.name,
                        cardNumber: dataRow.cardNumber,
                        deptID: deptID,
                        deptName: depName
                    },
                    success: function(data) {
                        if (data.status == 'success') {

                        } else {
                            swal.fire({
                                title: 'Error',
                                text: 'An error occurred while updating data',
                                type: 'error',
                                timer: 1000,
                            });
                            provTable.ajax.reload();
                        }
                    }
                });
            });

            $(document).on('click', '.addAccessHR', function(e) {
                e.preventDefault();
                $('#mTableAccess').find('td').each(function() {
                    if ($(this).text().indexOf('HR Attendance') > -1) {
                        var row = $(this).closest('tr');
                        var input = row.find('input[type="checkbox"]');
                        if (!input.prop('checked')) {
                            input.prop('checked', true);
                            input.change();
                        }
                    }
                });
            });
            $(document).on('click', '.delAccessAll', function(e) {
                e.preventDefault();
                $('#mTableAccess input[type="checkbox"]').each(function() {
                    if ($(this).prop('checked')) {
                        $(this).prop('checked', false);
                        $(this).change();
                    }
                });
            });
            provTableAccess = $('#mTableAccess').DataTable({
                "autoWidth": false,
                "pageLength": <?php echo $this->session->userdata('user_profile')->cng_per_page; ?>,
                "processing": false,
                "bSort": true,
                "stateSave": false,
                "autoWidth": false,
                "language": {
                    "url": "<?php echo base_url() ?>dist/js/pages/datatable/<?php echo $this->session->userdata('user_profile')->cng_lang; ?>.json",
                    "headers": {
                        "Access-Control-Allow-Origin": "*"
                    },
                    searchPlaceholder: "<?php echo $lang_sys->lbl_search; ?>",
                },
                "data": [],
                "deferRender": true,
                "aLengthMenu": [
                    [10, 25, 50, 100],
                    [10, 25, 50, 100]
                ],
                "columns": [{
                        "title": "Door Name",
                        "data": "doorName",
                    },
                    {
                        "title": "<i class='mdi mdi-account-key'></i>",
                        "data": "accessCard",
                        "orderable": false,
                        "width": "100px",
                        "render": function(data, type, row, meta) {
                            return '<div class="custom-control custom-switch">' +
                                '<input type="checkbox" class="custom-control-input" ' + (row.accessCard == '0' ? '' : 'checked') + ' id="' + meta.row + 'Cd" data-personID="' + row.personID + '-1">' +
                                '<label class="custom-control-label" for="' + meta.row + 'Cd"></label>' +
                                '</div>';
                        }
                    },
                    {
                        "title": "<i class='mdi mdi-fingerprint'></i>",
                        "data": "accessFinger",
                        "orderable": false,
                        "width": "100px",
                        "render": function(data, type, row, meta) {
                            return '<div class="custom-control custom-switch">' +
                                '<input type="checkbox" class="custom-control-input" ' + (row.accessFinger == '0' ? '' : 'checked') + ' id="' + meta.row + 'Fp" data-personID="' + row.personID + '-2">' +
                                '<label class="custom-control-label" for="' + meta.row + 'Fp"></label>' +
                                '</div>';
                        }
                    },
                ],
            });
            $('button#btnSync').click(function() {
                $.ajax({
                    url: '<?php echo base_url('sys/emp_all/syncData'); ?>',
                    type: 'POST',
                    success: function(data) {
                        if (data.status == 'success') {
                            swal.fire({
                                title: 'Success',
                                text: 'Data updated successfully',
                                type: 'success',
                                timer: 1000,
                            });
                            provTable.ajax.reload();
                        } else {
                            swal.fire({
                                title: 'Error',
                                text: 'An error occurred while updating data',
                                type: 'error',
                                timer: 1000,
                            });
                            provTable.ajax.reload();
                        }
                    }
                });
            });
            $('button#btnSyncName').click(function() {
                $.ajax({
                    url: '<?php echo base_url('sys/emp_all/checkNorow'); ?>',
                    type: 'POST',
                    success: function(data) {
                        if (data.status == 'success') {
                            swal.fire({
                                title: 'Success',
                                text: 'Data updated successfully',
                                type: 'success',
                                timer: 1000,
                            });
                            provTable.ajax.reload();
                        } else {
                            swal.fire({
                                title: 'Error',
                                text: 'An error occurred while updating data',
                                type: 'error',
                                timer: 1000,
                            });
                            provTable.ajax.reload();
                        }
                    }
                });
            });

            // $(document).on('click', '.addAcess', function() {
            // var html = "<select class='form-control form-control-sm additionalInfo' data-card='" + row.cardNumber + "'>";
            //                 html += "<option value=''>เลือกดูข้อมูล</option>";
            //                 // html += "<option value='access'>สิทธิ์การเข้าถึง</option>";
            //                 html += "<option value='access' data-toggle='modal' data-backdrop='static' data-keyboard='false' data-target='#addAccessModal' data-card='" + row.cardNumber + "'>สิทธิ์การเข้าถึง</option>";
            //                 html += "<option value='attendance'>ข้อมูลการเข้างาน</option>";
            //                 html += "<option value='leave'>ข้อมูลการลา</option>";
            //                 html += "</select>";
            //                 return html;                e.preventDefault();
            $(document).on('click', '.showAccess', function(e) {
                e.preventDefault();
                $('#addAccessModal').modal('show');
                var dataRow = provTable.row($(this).parents('tr')).data();
                $('h5#addAccessModalLabel').text('Access Control: ' + dataRow.name);
                provTableAccess.clear().draw();
                $.ajax({
                    url: '<?php echo base_url('sys/emp_all/getAccess'); ?>',
                    type: 'POST',
                    data: {
                        cardNumber: $(this).data('card')
                    },
                    success: function(data) {
                        provTableAccess.rows.add(data.data).draw();

                    }
                });
            });

            provTable.ajax.reload();

            $(document).on('change', '#mTable input.shiftMent', function() {
                var personID = this.id.replace('Sh', '');
                var shift = this.checked ? 'A' : 'B';
                $(this).closest('td').find('span').text(shift);
                $.ajax({
                    url: '<?php echo base_url('sys/emp_all/updateShift'); ?>',
                    type: 'POST',
                    data: {
                        personID: personID,
                        shift: shift
                    },
                    success: function(data) {
                        if (data.status == 'success') {
                            swal.fire({
                                title: 'Success',
                                text: 'Data updated successfully',
                                type: 'success',
                                timer: 500,
                            });
                            // provTable.ajax.reload();
                        } else {
                            swal.fire({
                                title: 'Error',
                                text: 'An error occurred while updating data',
                                type: 'error',
                                timer: 1000,
                            });
                            provTable.ajax.reload();
                        }
                    }
                });
            });

            // disableCa
            $(document).on('change', '#mTable input.disableCa', function() {
                var personID = this.id.replace('Shca', '');
                var shift = this.checked ? 'On' : 'Off';
                var row = $(this).closest('td').find('input.disableCa');
                $.ajax({
                    url: '<?php echo base_url('sys/emp_all/offShift'); ?>',
                    type: 'POST',
                    data: {
                        personID: personID,
                        shift: shift
                    },
                    success: function(data) {
                        if (data.status == 'success') {
                            row.closest('td').find('span').text(shift);
                            row.closest('td').find('input.shiftMent').prop('checked', true);
                        } else {
                            swal.fire({
                                title: 'Error',
                                text: 'An error occurred while updating data',
                                type: 'error',
                                timer: 1000,
                            });
                            provTable.ajax.reload();
                        }
                    }
                });
            });

            $(document).on('change', '#mTable input.status', function() {
                var cardNumber = this.id.replace('switch', '');
                var timeNotDisable = this.checked ? '2999-12-31 00:00:00.000' : new Date().toISOString().slice(0, 10) + ' 00:00:00.000';
                var now = function() {
                    return new Date().toISOString().slice(0, 10);
                };
                var val = this.checked ? '2999-12-31 00:00:00.000' : now();
                var isActived = this.checked ? '1' : '0';
                $.ajax({
                    url: '<?php echo base_url('sys/emp_all/updateStatus'); ?>',
                    type: 'POST',
                    data: {
                        cardNumber: cardNumber,
                        isActive: isActived
                    },
                    success: function(data) {
                        if (data.status == 'success') {
                            // swal.fire({
                            //     title: 'Success',
                            //     text: 'Data updated successfully',
                            //     type: 'success',
                            //     timer: 1000,
                            // });
                            provTable.ajax.reload();
                        } else {
                            swal.fire({
                                title: 'Error',
                                text: 'An error occurred while updating data',
                                type: 'error',
                                timer: 1000,
                            });
                            provTable.ajax.reload();
                        }
                    }
                });
            });

            $(document).on('change', '#mTableAccess input[type="checkbox"]', function() {
                var dataRow = provTableAccess.row($(this).parents('tr')).data();
                var state = this.checked ? '1' : '0';
                var dnfp = 0;
                if ($(this).data('personid').indexOf('-2') > -1) {
                    dnfp = 1;
                }
                $.ajax({
                    url: '<?php echo base_url('sys/emp_all/updateAccess'); ?>',
                    type: 'POST',
                    data: {
                        personId: $(this).data('personid'),
                        doorId: dataRow.doorID,
                        state: state,
                        weekTimeID: dataRow.weekTimeID,
                        dnfp: dnfp
                    },
                    success: function(data) {
                        if (data.status == 'success') {
                            // swal.fire({
                            //     title: 'Success',
                            //     text: 'Data updated successfully',
                            //     type: 'success',
                            //     timer: 1000,
                            // });
                        } else {
                            swal.fire({
                                title: 'Error',
                                text: 'An error occurred while updating data',
                                type: 'error',
                                timer: 1000,
                            });
                        }
                    }
                });
            });

            $('#addForm select[name="deptName"]').change(function() {
                $('#addForm input[name="deptID"]').val($(this).val());
            });
            $('#addForm input[name="cardNo"]').change(function() {
                $('#addForm input[name="personIDc"]').val($(this).val() + '-1');
                $('#addForm input[name="personIDfp"]').val($(this).val() + '-2');
            });

            // submit form add
            $('#addForm').submit(function(e) {
                e.preventDefault();
                $department = $('#addForm select[name="deptName"] option:selected').text();
                data = $(this).serialize();
                data += '&deptName=' + $department;
                console.log($department);
                $.ajax({
                    url: '<?php echo base_url('sys/emp_all/addDataHQMS'); ?>',
                    type: 'POST',
                    // data: $(this).serialize(),
                    data: data,
                    success: function(data) {
                        if (data.status == 'success') {
                            $('#addModal input').val('');
                            $('#addModal select').change();
                            provTable.ajax.reload();
                        } else {
                            // alert(data.message);
                        }
                    }
                });
            });

            // modal show then reset form
            $('button#addBtn').click(function() {
                $('#addForm')[0].reset();
            });

            $('#mTable').on('click', 'button.delrow', function() {
                swal.fire({
                    title: 'ยืนยันการลบข้อมูล',
                    text: 'คุณต้องการลบข้อมูล ' + $(this).data('id') + ' ใช่หรือไม่',
                    type: 'warning',
                    showCancelButton: true,
                    confirmButtonText: 'ใช่',
                    cancelButtonText: 'ไม่ใช่',
                }).then((result) => {
                    console.log(result);
                    if (result.value) {
                        $.ajax({
                            url: '<?php echo base_url('sys/emp_all/deleteDataHQMS'); ?>',
                            type: 'POST',
                            data: {
                                cardNumber: $(this).data('id')
                            },
                            success: function(data) {
                                if (data.status == 'success') {
                                    provTable.ajax.reload();
                                } else {
                                    swal.fire({
                                        title: 'Error',
                                        text: 'An error occurred while deleting data',
                                        type: 'error',
                                    });
                                }
                            }
                        });
                    }
                });
            });

            $('#mTable').on('click', 'a.edit', function() {
                var dataRow = provTable.row($(this).parents('tr')).data();
                $('#addForm input[name="cardNo"]').val(dataRow.cardNumber);
                $('#addForm input[name="cardNumber"]').val(dataRow.cardNumber2);
                $('#addForm input[name="personIDfp"]').val(dataRow.cardNumber + '-2');
                $('#addForm input[name="personIDc"]').val(dataRow.cardNumber + '-1');
                $('#addForm input[name="name"]').val(dataRow.name);
                $('#addForm select[name="deptName"]').val(dataRow.deptID);
                $('#addForm select[name="shift"]').val(dataRow.shiftMent);
                $('#addForm select[name="deptName"]').change();
                $('#addModal').modal('show');
            });

        });
    </script>
<?php
} /* Can View */ ?>