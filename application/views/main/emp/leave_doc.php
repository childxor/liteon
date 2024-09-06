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

    <div class="card">
        <div class="card-body">
            <form id="searchForm">
                <div class="row mb-4">
                    <div class="col-md-4 col-lg-3 mb-3">
                        <label for="department_id" class="form-label">
                            <i class="fas fa-building"></i> <?php echo $lang_module->lbl_department; ?>
                        </label>
                        <select class="form-control" id="department_id" name="deptName">
                            <option value=""><?php echo $lang_module->lbl_all_departments; ?></option>
                            <?php foreach ($dept as $department) : ?>
                                <option value="<?php echo $department->deptID; ?>"><?php echo $department->name; ?></option>
                            <?php endforeach; ?>
                        </select>
                    </div>
                    <div class="col-md-4 col-lg-3 mb-3">
                        <label for="is_active" class="form-label">
                            <i class="fas fa-toggle-on"></i> <?php echo $lang_module->lbl_status; ?>
                        </label>
                        <select class="form-control" id="is_active" name="is_active">
                            <option value=""><?php echo $lang_module->lbl_all_statuses; ?></option>
                            <option value="N"><?php echo $lang_module->lbl_deactive; ?></option>
                            <option value="Y"><?php echo $lang_module->lbl_active; ?></option>
                        </select>
                    </div>
                    <div class="col-md-4 col-lg-3 mb-3">
                        <label for="dateRange" class="form-label">
                            <i class="far fa-calendar-alt"></i> <?php echo $lang_module->lbl_date_range; ?>
                        </label>
                        <input type="text" class="form-control" id="dateRange" name="dateRange">
                    </div>
                </div>
                <div class="row mb-4">
                    <div class="col-md-4 col-lg-3 mb-3">
                        <button type="submit" class="btn btn-primary btn-block">
                            <i class="fas fa-search"></i> <?php echo $lang_module->lbl_search; ?>
                        </button>
                    </div>
                    <?php if ($this->session->userdata('user_profile')->id == '463') : ?>
                        <div class="col-md-4 col-lg-3 mb-3">
                            <button type="button" class="btn btn-secondary btn-block" id="btnSync">
                                <i class="fas fa-sync"></i> Sync
                            </button>
                        </div>
                    <?php endif; ?>
                    <?php if ($this->efs_lib->is_can($permission, "add")) : ?>
                        <div class="col-md-4 col-lg-3 mb-3">
                            <button type="button" id="addBtn" class="btn btn-success btn-block" data-toggle="modal" data-target="#addModal">
                                <i class="fas fa-plus"></i> <?php echo $lang_module->lbl_add; ?>
                            </button>
                        </div>
                    <?php endif; ?>
                </div>
            </form>

            <div class="table-responsive">
                <table id="mTable" class="table table-bordered table-striped table-hover" style="width:100%"></table>
            </div>
        </div>
    </div>


    <?php if ($this->efs_lib->is_can($permission, "edit")) { ?>
        <!-- Button to trigger modal -->
        <button type="button" class="btn btn-primary" data-toggle="modal" data-target="#leaveModal">
            ยื่นใบลา
        </button>

        <!-- Leave Request Modal -->
        <div class="modal fade" id="leaveModal" tabindex="-1" role="dialog" aria-labelledby="leaveModalLabel" aria-hidden="true" style="display: none;" aria-modal="true">
            <div class="modal-dialog modal-lg" role="document" style="max-width: 600px;">
                <div class="modal-content">
                    <div class="modal-header bg-primary text-white">
                        <h5 class="modal-title" id="leaveModalLabel"><i class="fas fa-calendar-alt mr-2"></i>แบบฟอร์มขอลา</h5>
                        <button type="button" class="close text-white" data-dismiss="modal" aria-label="Close">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body">
                        <form id="leaveForm">
                            <div class="form-row">
                                <div class="form-group col-md-4">
                                    <label for="personID"><i class="fas fa-id-badge mr-2"></i>รหัสพนักงาน</label>
                                    <input type="text" class="form-control" id="personID" readonly name="personID" value="">
                                </div>
                                <div class="form-group col-md-4">
                                    <label for="cardNumber"><i class="fas fa-credit-card mr-2"></i>เลขหลังบัตร</label>
                                    <input type="text" class="form-control" id="cardNumber" readonly name="cardNumber" value="">
                                </div>
                                <div class="form-group col-md-4">
                                    <label for="name"><i class="fas fa-user mr-2"></i>ชื่อ-นามสกุล</label>
                                    <input type="text" class="form-control" id="name" readonly name="name" value="">
                                </div>
                            </div>
                            <div class="form-row">
                                <div class="form-group col-md-6">
                                    <label for="deptName"><i class="fas fa-building mr-2"></i>แผนก</label>
                                    <input type="text" class="form-control" id="deptName" readonly name="deptName" value="">
                                </div>
                                <div class="form-group col-md-6">
                                    <label for="deptID"><i class="fas fa-hashtag mr-2"></i>รหัสแผนก</label>
                                    <input type="text" class="form-control" id="deptID" readonly name="deptID" value="">
                                </div>
                            </div>
                            <div class="form-row">
                                <div class="form-group col-md-6">
                                    <label for="leaveDate"><i class="fas fa-calendar-day mr-2"></i>วันที่ลา</label>
                                    <input type="date" class="form-control" id="leaveDate" required value="<?php echo date('Y-m-d'); ?>">
                                </div>
                                <div class="form-group col-md-3">
                                    <label for="leaveDuration"><i class="fas fa-clock mr-2"></i>จำนวนลา</label>
                                    <input type="number" class="form-control" id="leaveDuration" min="0" step="0.01" required value="1">
                                </div>
                                <div class="form-group col-md-3">
                                    <label for="leaveUnit"><i class="fas fa-hourglass-half mr-2"></i>หน่วย</label>
                                    <select class="form-control" id="leaveUnit" required>
                                        <option value="day">วัน</option>
                                        <option value="hour">ชั่วโมง</option>
                                    </select>
                                </div>
                            </div>
                            <div class="form-group">
                                <label><i class="fas fa-list-ul mr-2"></i>ประเภทการลา</label>
                                <div class="d-flex flex-wrap">
                                    <div class="custom-control custom-radio mr-3 mb-2">
                                        <input type="radio" id="sickLeave" name="leaveType" class="custom-control-input" value="sick" required checked>
                                        <label class="custom-control-label" for="sickLeave"><i class="fas fa-user-injured mr-1"></i>ลาป่วย</label>
                                    </div>
                                    <div class="custom-control custom-radio mr-3 mb-2">
                                        <input type="radio" id="personalLeave" name="leaveType" class="custom-control-input" value="personal">
                                        <label class="custom-control-label" for="personalLeave"><i class="fas fa-home mr-1"></i>ลากิจ</label>
                                    </div>
                                    <div class="custom-control custom-radio mr-3 mb-2">
                                        <input type="radio" id="vacationLeave" name="leaveType" class="custom-control-input" value="vacation">
                                        <label class="custom-control-label" for="vacationLeave"><i class="fas fa-umbrella-beach mr-1"></i>ลาพักร้อน</label>
                                    </div>
                                    <div class="custom-control custom-radio mr-3 mb-2">
                                        <input type="radio" id="resignLeave" name="leaveType" class="custom-control-input" value="resign">
                                        <label class="custom-control-label" for="resignLeave"><i class="fas fa-door-open mr-1"></i>ลาออก</label>
                                    </div>
                                    <div class="custom-control custom-radio mb-2">
                                        <input type="radio" id="otherLeave" name="leaveType" class="custom-control-input" value="other">
                                        <label class="custom-control-label" for="otherLeave"><i class="fas fa-question-circle mr-1"></i>อื่นๆ</label>
                                    </div>
                                </div>
                            </div>
                            <div class="form-group">
                                <label for="leaveReason"><i class="fas fa-comment-alt mr-2"></i>เหตุผลการลา <small class="text-muted">(ไม่เกิน 500 ตัวอักษร)</small></label>
                                <textarea class="form-control" id="leaveReason" rows="3" required></textarea>
                            </div>
                            <div class="form-group">
                                <label for="approver"><i class="fas fa-user-tie mr-2"></i>หัวหน้าผู้อนุมัติ</label>
                                <input type="text" class="form-control" id="approver" readonly value="ชื่อหัวหน้า">
                            </div>
                            <div class="form-group">
                                <label for="attachments"><i class="fas fa-paperclip mr-2"></i>แนบเอกสาร (ใบรับรองแพทย์หรือเอกสารอื่นๆ)</label>
                                <div class="custom-file">
                                    <input type="file" class="custom-file-input" id="attachments" multiple>
                                    <label class="custom-file-label" for="attachments">เลือกไฟล์</label>
                                </div>
                                <small class="form-text text-muted">สามารถเลือกได้หลายไฟล์ (สูงสุด 5 MB ต่อไฟล์)</small>
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-dismiss="modal"><i class="fas fa-times mr-2"></i>ยกเลิก</button>
                                <button type="submit" class="btn btn-primary"><i class="fas fa-paper-plane mr-2"></i>ยื่นใบลา</button>
                            </div>
                        </form>
                    </div>
                </div>
            </div>
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

    <script src="<?php echo base_url(); ?>assets/libs/moment/min/moment.min.js"></script>
    <!-- DateRangePicker JS -->
    <script src="<?php echo base_url(); ?>assets/libs/daterangepicker/daterangepicker.min.js"></script>

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
                "language": {
                    "url": "<?php echo base_url() ?>dist/js/pages/datatable/<?php echo $this->session->userdata('user_profile')->cng_lang; ?>.json",
                    "headers": {
                        "Access-Control-Allow-Origin": "*"
                    },
                    searchPlaceholder: "<?php echo $lang_sys->lbl_search; ?>",
                },
                "ajax": {
                    "url": "<?php echo base_url('emp/leave_doc/getUserfromHQMS'); ?>",
                    "type": "POST",
                    "data": function(d) {
                        d.deptID = $('#department_id').val();
                        d.is_active = $('#is_active').val();
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
                            return row.cardNumber;
                        }
                    },
                    {
                        "data": "name",
                        "className": "text-left",
                        "title": "<?php echo $lang_module->lbl_fullname; ?>",
                        "render": function(data, type, row, meta) {
                            return row.name;
                        }
                    },
                    {
                        "data": "deptName",
                        "className": "text-left",
                        "title": "<?php echo $lang_module->lbl_department; ?>",
                        "render": function(data, type, row, meta) {
                            return row.deptName;
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
                            "data": "cardNumber",
                            "className": "text-center",
                            "orderable": false,
                            "title": "<i class='mdi mdi-account-key'></i>",
                            "width": "50px",
                            "render": function(data, type, row, meta) {
                                return '<button class="leaveFrom btn btn-sm btn-primary" data-personID="' + row.personID + '" data-cardNumber="' + row.cardNumber + '" data-name="' + row.name + '" data-deptName="' + row.deptName + '" data-deptID="' + row.deptID + '" data-toggle="modal" data-target="#leaveModal">Leave</button>';
                            }
                        },
                    <?php } ?>
                ],
            });

            $('#dateRange').daterangepicker({
                opens: 'left',
                locale: {
                    format: 'DD/MM/YYYY'
                }
            });


        }(window, document, jQuery);
        // range date


        $(document).ready(function() {

            $('#searchForm').submit(function(e) {
                e.preventDefault();
                provTable.ajax.reload();
            });

            $('.leaveFrom').click(function() {
                var dataRow = provTable.row($(this).parents('tr')).data();
                // console.log(dataRow); 
                var $form = $('#leaveForm');
                console.log(dataRow);
                // $form[0].reset();
                $form.find('input[type="file"]').val('').trigger('change');
                // clear form
                var PersonID = dataRow.cardNumber;
                var cardNumber2 = dataRow.cardNumber2;
                var name = dataRow.name;
                var deptName = dataRow.deptName;
                var deptID = dataRow.deptID;

                $form.find('#personID').val(PersonID);
                $form.find('#cardNumber').val(cardNumber2);
                $form.find('#name').val(name);
                $form.find('#deptName').val(deptName);
                $form.find('#deptID').val(deptID);
            });


            $('#leaveForm').submit(function(e) {
                e.preventDefault();
                swal.fire({
                    title: 'ยื่นใบลา',
                    text: 'ยืนยันการยื่นใบลา',
                    type: 'warning',
                    showCancelButton: true,
                    confirmButtonText: 'ยืนยัน',
                    cancelButtonText: 'ยกเลิก',
                    confirmButtonColor: '#3085d6',
                    cancelButtonColor: '#d33',
                }).then((result) => {
                    var formData = new FormData();
                    formData.append('leaveDate', $('#leaveDate').val());
                    formData.append('leaveDuration', $('#leaveDuration').val());
                    formData.append('leaveUnit', $('#leaveUnit').val());
                    formData.append('leaveType', $('input[name="leaveType"]:checked').val());
                    formData.append('leaveReason', $('#leaveReason').val());
                    formData.append('personID', $('#personID').val());
                    formData.append('cardNumber', $('#cardNumber').val());
                    formData.append('name', $('#name').val());
                    formData.append('deptName', $('#deptName').val());
                    formData.append('deptID', $('#deptID').val());
                    formData.append('approver', $('#approver').val());
                    var attachments = $('#attachments')[0].files;
                    for (var i = 0; i < attachments.length; i++) {
                        formData.append('attachments[]', attachments[i]);
                    }
                    $.ajax({
                        url: '<?php echo base_url('emp/leave_doc/leaveRequest'); ?>',
                        type: 'POST',
                        data: formData,
                        contentType: false,
                        processData: false,
                        success: function(response) {
                            console.log(response);
                            $('#leaveModal').modal('hide');
                        },
                        error: function(xhr, status, error) {
                            console.log(xhr.responseText);
                        }
                    });
                });
            });
        });
    </script>
<?php
} /* Can View */ ?>