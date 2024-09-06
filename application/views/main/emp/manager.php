<?php /* var_dump($permission); */ ?>
<?php (empty($permission) ? redirect(base_url("error404")) : ""); ?>
<!-- ============================================================== -->
<!-- Start Page Content -->
<!-- ============================================================== -->
<style>
    .modal-dialog.modal-lg {
        max-width: 100%;
        width: auto !important;
        display: inline-block;
    }

    .disabled-link {
        pointer-events: none;
        color: grey;
        text-decoration: none;
    }


    .bootstrap-switch-container {
        /* กำหนดความยาวคงให้ <?php echo $lang_module->lbl_status; ?> แก้ปัญหาย่อขยายตอน*/
        width: 90px !important;
    }

    .card {
        border: none;
        box-shadow: 0 0 20px rgba(0, 0, 0, 0.1);
        border-radius: 10px;
    }

    .card-header {
        background-color: #f8f9fa;
        border-bottom: 1px solid #e9ecef;
        padding: 20px;
        border-top-left-radius: 10px;
        border-top-right-radius: 10px;
    }

    .form-group label {
        font-weight: 600;
        color: #495057;
        margin-bottom: 5px;
    }

    .form-control {
        border-radius: 5px;
    }

    .btn-primary {
        background-color: #007bff;
        border-color: #007bff;
        padding: 10px 20px;
        font-weight: 600;
        transition: all 0.3s;
    }

    .btn-primary:hover {
        background-color: #0056b3;
        border-color: #0056b3;
    }

    .table {
        margin-top: 20px;
    }
</style>
<!-- This page plugin CSS -->
<link href="<?php echo base_url(); ?>assets/libs/datatables.net-bs4/css/dataTables.bootstrap4.css" rel="stylesheet">
<link rel="stylesheet" type="text/css" href="<?php echo base_url(); ?>assets/libs/bootstrap-switch/dist/css/bootstrap3/bootstrap-switch.min.css">
<link rel="stylesheet" href="<?php echo base_url(); ?>assets/libs/signature-pad/css/signature-pad.css">

<div class="row">
    <div class="col-12">
        <div class="card">
            <div class="card-header">
                <form id="formFilter" method="post" action="#" class="form-horizontal" role="form">
                    <div class="row mb-4">
                        <div class="col-md-6">
                            <div class="form-group">
                                <label for="selectedDate" class="form-label">
                                    <i class="mdi mdi-calendar me-2"></i><?php echo $lang_sys->lbl_select_date; ?>:
                                </label>
                                <input type="date" class="form-control" id="selectedDate" name="selectedDate" value="<?php echo date('Y-m-d'); ?>">
                                <br>
                                <!-- ปุ่มหน้าหลังเพื่อเพิ่มลดวันทีละวัน -->
                                <a href="javascript:void(0);" class="btn btn-primary" id="btnPrevDate">
                                    <i class="mdi mdi-arrow-left"></i>
                                </a>
                                <!-- ปุ่มหน้าหน้าเพื่อเพิ่มลดวันทีละวัน -->
                                <a href="javascript:void(0);" class="btn btn-primary" id="btnNextDate">
                                    <i class="mdi mdi-arrow-right"></i>
                                </a>
                            </div>
                        </div>
                        <div class="col-md-6">
                            <div class="form-group">
                                <label for="doorID" class="form-label">
                                    <i class="mdi mdi-account-card-details me-2"></i>ประตูที่ต้องการค้นหา:
                                </label>
                                <select class="form-control auto-save" id="doorID" name="doorID">
                                    <option value="all" selected>ทั้งหมด</option>
                                    <?php foreach ($Door as $key => $value) { ?>
                                        <option value="<?php echo $value->doorName; ?>"><?php echo $value->doorName; ?></option>
                                    <?php } ?>
                                </select>
                                <br>
                                <!-- ปุ่มหน้าหลังเพื่อค้นหาประตูถัดไป -->
                                <a href="javascript:void(0);" class="btn btn-primary" id="btnPrevDoor">
                                    <i class="mdi mdi-arrow-left"></i>
                                </a>
                                <!-- ปุ่มหน้าหน้าเพื่อค้นหาประตูถัดไป -->
                                <a href="javascript:void(0);" class="btn btn-primary" id="btnNextDoor">
                                    <i class="mdi mdi-arrow-right"></i>
                                </a>
                            </div>
                        </div>
                    </div>
                    <div class="row mb-4">
                        <div class="col-md-6 d-none">
                            <div class="form-group">
                                <label for="shiftType" class="form-label">
                                    <i class="mdi mdi-clock-outline me-2"></i>ประเภทกะ:
                                </label>
                                <select class="form-control auto-save" id="shiftType" name="shiftType">
                                    <option value="all" selected>ทั้งหมด</option>
                                    <option value="day">กะกลางวัน</option>
                                    <option value="night">กะกลางคืน</option>
                                    <option value="no_shift">ไม่เข้ากะ</option>
                                </select>
                            </div>
                        </div>
                        <div class="col-6">
                            <div class="row">
                                <!-- เลือก department -->
                                <div class="form-group col-md-6">
                                    <label for="selectDistrup" class="form-label text-center">
                                        <i class="mdi mdi-account-card-details me-2"></i>เลือกแผนก:
                                    </label>
                                    <select class="form-control auto-save" id="selectDistrup" name="selectDistrup">
                                        <option value="all" selected>ทั้งหมด</option>
                                        <?php foreach ($department as $key => $value) { ?>
                                            <option value="<?php echo $value->code; ?>"><?php echo $value->name; ?></option>
                                        <?php } ?>
                                    </select>
                                    <br>
                                    <!-- ปุ่มหน้าหลังเพื่อค้นหาประตูถัดไป -->
                                    <a href="javascript:void(0);" class="btn btn-primary" id="btnPrevDept">
                                        <i class="mdi mdi-arrow-left"></i>
                                    </a>
                                    <!-- ปุ่มหน้าหน้าเพื่อค้นหาประตูถัดไป -->
                                    <a href="javascript:void(0);" class="btn btn-primary" id="btnNextDept">
                                        <i class="mdi mdi-arrow-right"></i>
                                    </a>

                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="col-6">
                        <div class="row">
                            <button type="submit" class="btn btn-primary">
                                <i class="mdi mdi-filter me-2"></i><?php echo $lang_sys->lbl_apply_filter; ?>
                            </button> &nbsp;
                            <button type="button" class="btn btn-secondary btn-clear-filter">
                                <i class="mdi mdi-filter-remove-outline me-2"></i><?php echo $lang_sys->lbl_clear_filter; ?> <!-- lbl_clear_filter อังกฤษ Clear Filter จีน(ตัวย่อ) 清除过滤器 -->
                            </button>
                        </div>
                    </div>
                </form>
            </div>
            <div class="card-body">
                <div class="table-responsive">
                    <table id="mTable" class="table table-bordered table-striped table-hover"></table>
                </div>
            </div>
        </div>
    </div>
</div>

<script src="<?php echo base_url(); ?>assets/libs/jquery/dist/jquery.min.js"></script>
<!-- This Page JS -->
<script src="<?php echo base_url(); ?>assets/extra-libs/jqbootstrapvalidation/validation.js"></script>
<script src="<?php echo base_url(); ?>assets/libs/bootstrap-switch/dist/js/bootstrap-switch.min.js"></script>

<!-- Croppie JS -->
<link href="<?php echo base_url(); ?>assets/extra-libs/croppie/croppie.css" rel="stylesheet">
<script src="<?php echo base_url(); ?>assets/extra-libs/croppie/croppie.js"></script>

<!-- Datatable Page JS -->
<script src="<?php echo base_url(); ?>assets/libs/datatables/media/js/jquery.dataTables.min.js"></script>
<script src="<?php echo base_url(); ?>dist/js/pages/datatable/custom-datatable.js"></script>
<script src="<?php echo base_url(); ?>assets/libs/datatables/media/js/dataTables.rowReorder.min.js"></script>

<script src="<?php echo base_url(); ?>assets/libs/moment/min/moment.min.js"></script>
<!-- DateRangePicker JS -->
<script src="<?php echo base_url(); ?>assets/libs/daterangepicker/daterangepicker.min.js"></script>


<!-- Signature Pad JS -->
<script src="<?php echo base_url(); ?>assets/libs/signature-pad/js/signature_pad.umd.js"></script>
<script src="<?php echo base_url(); ?>assets/libs/signature-pad/js/app.js"></script>
<script type="text/javascript">
    var provTable;
    ! function(window, document, $) {
        "use strict";

    }(window, document, jQuery);

    $(document).ready(function() {
        var provTable = $('#mTable').DataTable({
            "autoWidth": false,
            "processing": true,
            "pageLength": <?php echo $this->session->userdata('user_profile')->cng_per_page; ?>,
            "bSort": true,
            "stateSave": true,
            "language": {
                "url": "<?php echo base_url() ?>dist/js/pages/datatable/<?php echo $this->session->userdata('user_profile')->cng_lang; ?>.json",
                "headers": {
                    "Access-Control-Allow-Origin": "*",
                },
                searchPlaceholder: "<?php echo $lang_sys->lbl_search; ?>",
                sSearch: "",
            },
            "ajax": {
                "url": "<?php echo base_url('emp/manager/getDataHQMS_IPS') ?>",
                "type": "POST",
                "data": function(d) {
                    return {
                        selectDate: $('#selectedDate').val(),
                        doorID: $("#doorID").val() || 'all',
                        selectInOut: $('input[name=selectInOut]:checked').val(),
                        selectShift: $('#shiftType').val(),
                        selectDistrup: $('#selectDistrup option:selected').val(),
                    };
                }
            },
            "dataType": "json",
            "deferRender": true,
            "aLengthMenu": [
                [10, 25, 50, 100],
                [10, 25, 50, 100]
            ],
            "columns": [{
                    "title": "<i class='mdi mdi-pound'></i> <?php echo $lang_sys->lbl_list; ?>",
                    "data": null,
                    "orderable": false,
                    "className": "text-center",
                    "render": function(data, type, row, meta) {
                        return meta.row + 1;
                    }
                },
                {
                    "title": "<i class='mdi mdi-account-card-details'></i> <?php echo $lang_module->lbl_emp_id; ?>",
                    "data": "personID",
                    "render": function(data, type, row) {
                        var idTypes = [];
                        if (row.fingerprintUsed) idTypes.push('ลายนิ้วมือ');
                        if (row.cardUsed) idTypes.push('บัตร');
                        return data + ' (' + idTypes.join(', ') + ')';
                    }
                },
                {
                    "title": "<i class='mdi mdi-account'></i> <?php echo $lang_module->lbl_emp_name; ?>",
                    "data": "personName",
                    "render": function(data, type, row) {
                        return data + ' (' + (row.shift == null ? 'ไม่เข้ากะ' : row.shift) + ')';
                    }
                },
                {
                    "title": "<i class='mdi mdi-domain'></i> <?php echo $lang_module->lbl_department; ?>",
                    "data": "deptName",
                    "render": function(data, type, row) {
                        return data;
                    }
                },
                {
                    "data": "FirstTime",
                    "title": "<i class='mdi mdi-clock'></i> <?php echo $lang_module->lbl_only_type; ?>",
                    "render": function(data, type, row) {
                        // return '<span class="badge bg-primary FirstTime" data-time="' + data + '">' +
                        return '<span class="badge bg-light showtime FirstTime" data-time="' + data + '" tooltip="' + row.FirstTime + '">' +
                            data + '</span>';
                    }
                },
                {
                    "data": "LastTime",
                    "title": "<i class='mdi mdi-clock'></i> <?php echo $lang_module->lbl_outWork; ?>",
                    "render": function(data, type, row) {
                        if (row.FirstTime === row.LastTime) {
                            return '<span class="bg-danger">ยังไม่ออก</span>';
                        } else {
                            var timeDiff = moment(row.LastTime).diff(moment(row.FirstTime), 'hours', true);
                            if (timeDiff < 1) {
                                return '<span class="bg-danger">ยังไม่ออก</span>';
                            } else {
                                return '<span class="badge bg-success LastTime" data-time="' + data + '">' +
                                    data + '</span>';
                            }
                        }
                    }
                },
                {
                    "title": "<i class='mdi mdi-calendar'></i> <?php echo $lang_module->lbl_date; ?>",
                    "data": "FirstTime",
                    "render": function(data, type, row) {
                        return moment(data).format('YYYY-MM-DD HH:mm:ss');
                    }
                },
                {
                    "title": "<i class='mdi mdi-door'></i> <?php echo $lang_module->lbl_device_name; ?>",
                    "data": "deviceName",
                    "render": function(data, type, row) {
                        return data;
                    }
                }
                <?php if ($this->session->userdata('user_profile')->id == 463) { ?>,
                    {
                        "title": "<i class='mdi mdi-account-key'></i>",
                        "orderable": false,
                        "className": "text-center",
                        "render": function(data, type, row) {
                            return '<dropdown class="btn-group">' +
                                '<button type="button" class="btn btn-primary dropdown-toggle" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">' +
                                '<i class="mdi mdi-settings"></i>' +
                                '</button>' +
                                '<div class="dropdown-menu">' +
                                '<a class="dropdown-item giveAccess" href="javascript:void(0);">Give Access</a>' +
                                '</div>' +
                                '</dropdown>';
                        }
                    }
                <?php } ?>
            ],
            "order": [
                [0, "asc"]
            ],
            "fnDrawCallback": function() {
                var doorID = $('#doorID').val();
                var isHRAttendance = doorID.indexOf('Build1 - HR Attendance') > -1;
                var isAllDoors = doorID === 'all'

                if (isHRAttendance) {
                    provTable.column(4).visible(!isAllDoors);
                    provTable.column(5).visible(!isAllDoors);
                    provTable.column(6).visible(false);
                    // provTable.column(7).visible(false);
                } else {
                    provTable.column(4).visible(false);
                    provTable.column(5).visible(false);
                    provTable.column(6).visible(true);
                }
            },
            "createdRow": function(row, data, dataIndex) {
                $(row).find('.showtime').tooltip({
                    title: function() {
                        return $(this).attr('tooltip');
                    },
                    html: true,
                    placement: 'top'
                });
            },
            "initComplete": function() {
                // ฟังก์ชันที่จะทำงานเมื่อ DataTable โหลดเสร็จสมบูรณ์
            }
        });

        $('#selectDistrup').on('change', function() {
            provTable.ajax.reload();
        });

        $('#doorID').on('change', function() {
            // console.log($(this).val());
            if ($(this).val().indexOf('Build1 - HR Attendance') > -1) {
                console.log('HR Attendance');
            }
            provTable.ajax.reload();
        });

        $('#startDate').val(moment().format('YYYY-MM-DD'));
        $('#endDate').val(moment().format('YYYY-MM-DD'));
        $('#formFilter').submit(function(e) {
            e.preventDefault();
            if ($('#startDate').val() == '') {
                swal.fire({
                    title: 'Warning',
                    text: 'Please select date range',
                    type: 'warning',
                    confirmButtonText: 'OK'
                });
            } else {
                provTable.ajax.reload();
            }
        });

        <?php if ($this->session->userdata('user_profile')->id == 463) { ?>

            // $(document).on('click', '.FirstTime', function(e) {
            //     e.preventDefault();
            //     dataRow = provTable.row($(this).closest('tr')).data();
            //     // console.log(dataRow);
            //     dataTime = dataRow.rDate + ' ' + dataRow.FirstTime;
            //     $.ajax({
            //         url: '<?php echo base_url('emp/manager/setTimeKhem') ?>',
            //         type: 'POST',
            //         data: {
            //             time: dataTime,
            //             doorName: dataRow.DEVICENAME,
            //             firstTime: dataRow.FirstTime
            //         },
            //         dataType: 'json',
            //         success: function(data) {
            //             if (data.status == 'success') {
            //                 provTable.ajax.reload();
            //             } else {
            //                 swal.fire({
            //                     'title': 'Error',
            //                     'text': data.message,
            //                     'type': 'error',
            //                     'confirmButtonText': 'OK'
            //                 });
            //             }
            //         }
            //     });

            //     // swal.fire('Saved', '', 'success');
            // });
            // $(document).on('click', '.LastTime', function(e) {
            //     e.preventDefault();
            //     dataRow = provTable.row($(this).closest('tr')).data();
            //     // console.log(dataRow);
            //     dataTime = dataRow.rDate + ' ' + dataRow.LastTime;
            //     $.ajax({
            //         url: '<?php echo base_url('emp/manager/editDataevning') ?>',
            //         type: 'POST',
            //         data: {
            //             time: dataTime,
            //             doorName: dataRow.DEVICENAME,
            //             lastTime: dataRow.LastTime
            //         },
            //         dataType: 'json',
            //         success: function(data) {
            //             if (data.status == 'success') {
            //                 provTable.ajax.reload();
            //             } else {
            //                 swal.fire({
            //                     'title': 'Error',
            //                     'text': data.message,
            //                     'type': 'error',
            //                     'confirmButtonText': 'OK'
            //                 });
            //             }
            //         }
            //     });
            // });


        <?php } ?>

        $(document).on('click', '.giveAccess', function() {
            dataRow = provTable.row($(this).closest('tr')).data();
            console.log(dataRow);
            dnFp = 0;
            if (dataRow.EMP_ID.indexOf('-2') > -1) {
                dnFp = 1;
            }
            swal.fire({
                title: 'Are you sure?',
                text: 'Do you want to give access to this person?',
                type: 'warning',
                showCancelButton: true,
                confirmButtonText: 'Yes',
                cancelButtonText: 'No'
            }).then(function(result) {
                if (result.value) {
                    $.ajax({
                        url: '<?php echo base_url('emp/manager/giveAccess') ?>',
                        type: 'POST',
                        data: {
                            id: dataRow.EMP_ID,
                            doorName: dataRow.DEVICENAME,
                            dnFp: dnFp
                        },
                        dataType: 'json',
                        success: function(data) {
                            if (data.status == 'success') {
                                provTable.ajax.reload();
                            } else {
                                swal.fire({
                                    'title': 'Error',
                                    'text': data.message,
                                    'type': 'error',
                                    'confirmButtonText': 'OK'
                                });
                            }
                        }
                    });
                }
            });
        });

        $(document).on('change', '#selectedDate', function() {
            provTable.ajax.reload();
        });

        $('#btnPrevDate, #btnNextDate').on('click', function() {
            var selectedDate = $('#selectedDate').val();
            var newDate = moment(selectedDate).add($(this).attr('id') == 'btnPrevDate' ? -1 : 1, 'days').format('YYYY-MM-DD');
            $('#selectedDate').val(newDate);
            provTable.ajax.reload();
        });

        $('#btnPrevDoor, #btnNextDoor').on('click', function() {
            var doorID = $('#doorID').val();
            var doorIndex = $('#doorID option').index($('#doorID option:selected'));
            var newDoorIndex = doorIndex + ($(this).attr('id') == 'btnPrevDoor' ? -1 : 1);
            if (newDoorIndex < 0) {
                newDoorIndex = $('#doorID option').length - 1;
            } else if (newDoorIndex >= $('#doorID option').length) {
                newDoorIndex = 0;
            }
            $('#doorID').val($('#doorID option').eq(newDoorIndex).val());
            provTable.ajax.reload();
        });

        $('#btnPrevDept, #btnNextDept').on('click', function() {
            var deptID = $('#selectDistrup').val();
            var deptIndex = $('#selectDistrup option').index($('#selectDistrup option:selected'));
            var newDeptIndex = deptIndex + ($(this).attr('id') == 'btnPrevDept' ? -1 : 1);
            if (newDeptIndex < 0) {
                newDeptIndex = $('#selectDistrup option').length - 1;
            } else if (newDeptIndex >= $('#selectDistrup option').length) {
                newDeptIndex = 0;
            }
            $('#selectDistrup').val($('#selectDistrup option').eq(newDeptIndex).val());
            provTable.ajax.reload();
        });

        $('.btn-clear-filter').on('click', function() {
            $('#formFilter').find('#doorID, #shiftType, #selectDistrup').val('all');
            $('#formFilter').find('#selectedDate').val(moment().format('YYYY-MM-DD'));
            provTable.ajax.reload();

        });
    });
</script>