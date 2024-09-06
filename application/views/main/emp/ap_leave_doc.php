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

    <div class="container-fluid mt-2">
        <!-- ฟอร์มสำหรับ Filter -->
        <div class="card mb-2">
            <div class="card-body">
                <form id="filterForm" action="javascript:void(0)">
                    <div class="form-row">
                        <div class="form-group col-md-3">
                            <label for="dateRange">ช่วงวันที่</label>
                            <div class="input-group">
                                <div class="input-group-prepend">
                                    <div class="input-group-text">
                                        <input type="checkbox" id="selectAllDates" aria-label="Checkbox for selecting all dates" class="mr-2 auto-save">
                                    </div>
                                </div>
                                <input type="text" class="form-control" id="dateRange" name="dateRange" readonly>
                            </div>
                            <small id="dateRangeHelp" class="form-text text-muted">เลือกทั้งหมดทุกช่วงเวลา</small>
                        </div>
                        <div class="form-group col-md-3">
                            <label for="department">แผนก</label>
                            <select class="form-control" id="department" name="department">
                                <option value="">ทั้งหมด</option>
                                <?php
                                $department = $this->db->select('deptID, name')->from('Dept')->get()->result();
                                ?>
                                <?php foreach ($department as $dept) : ?>
                                    <option value="<?php echo $dept->deptID; ?>"><?php echo $dept->name; ?></option>
                                    <!-- เพิ่มตัวเลือกแผนกตามที่มีในระบบ -->
                                <?php endforeach; ?>
                            </select>
                        </div>
                        <div class="form-group col-md-3">
                            <label for="leaveType">ประเภทการลา</label>
                            <select class="form-control" id="leaveType" name="leaveType">
                                <option value="">ทั้งหมด</option>
                                <option value="sick">ลาป่วย</option>
                                <option value="personal">ลากิจ</option>
                                <option value="vacation">ลาพักร้อน</option>
                                <option value="other">อื่นๆ</option>
                            </select>
                        </div>
                        <div class="form-group col-md-3">
                            <label for="status">สถานะ</label>
                            <select class="form-control" id="status" name="status">
                                <option value="">ทั้งหมด</option>
                                <option value="pending">รออนุมัติ</option>
                                <option value="approved">อนุมัติแล้ว</option>
                                <option value="rejected">ปฏิเสธ</option>
                            </select>
                        </div>
                    </div>
                    <button type="submit" class="btn btn-primary" id="filterBtn" aria-describedby="filterHelpBlock">
                        <i class="fa fa-filter"></i> กรอง
                    </button>
                </form>
            </div>
        </div>

        <!-- ตาราง DataTable -->
        <div class="card">
            <!-- check box for hide column -->
            <div class="card-header">
                <div class="row">
                    <div class="hide" id="hideColumn">
                    </div>
                </div>
            </div>
            <div class="card-body">
                <table id="leaveTable" class="table table-striped table-bordered" style="width:100%"></table>
            </div>
        </div>
    </div>

    <!-- Modal แสดงรายละเอียดการลา iframes -->
    <div class="modal fade" id="leaveDetailModal" tabindex="-1" role="dialog" aria-labelledby="leaveDetailModalLabel" aria-hidden="true" data-backdrop="static" data-keyboard="false">
        <div class="modal-dialog modal-lg" role="document" style="max-width: 50%;">
            <div class="modal-content">
                <!-- <div class="modal-header">
                    <h5 class="modal-title" id="leaveDetailModalLabel">รายละเอียดการลา</h5>
                    <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                        <span aria-hidden="true">&times;</span>
                    </button>
                </div> -->
                <div class="modal-body">
                    <iframe id="leaveDetailIframe" src="" frameborder="0" width="100%" height="800" class="doctor"></iframe>
                </div>
                <!-- <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-dismiss="modal">ปิด</button>
                </div> -->
            </div>
        </div>
    </div>



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
        ! function(window, document, $) {
            "use strict";
            // $.fn.dataTable.ext.errMode = 'none';


        }(window, document, jQuery);

        $(document).ready(function() {
            // กำหนดค่าเริ่มต้นให้กับ Date Range Picker
            $('#dateRange').daterangepicker({
                "locale": {
                    "format": "DD/MM/YYYY",
                    "separator": " - ",
                    "applyLabel": "ตกลง",
                    "cancelLabel": "ยกเลิก",
                    "fromLabel": "จาก",
                    "toLabel": "ถึง",
                    "customRangeLabel": "กำหนดเอง",
                    "weekLabel": "W",
                    "daysOfWeek": [
                        "อา",
                        "จ",
                        "อ",
                        "พ",
                        "พฤ",
                        "ศ",
                        "ส"
                    ],
                    "monthNames": [
                        "มกราคม",
                        "กุมภาพันธ์",
                        "มีนาคม",
                        "เมษายน",
                        "พฤษภาคม",
                        "มิถุนายน",
                        "กรกฎาคม",
                        "สิงหาคม",
                        "กันยายน",
                        "ตุลาคม",
                        "พฤศจิกายน",
                        "ธันวาคม"
                    ],
                    "firstDay": 1
                },
                "opens": "center"
            });

            provTable = $('#leaveTable').DataTable({
                "autoWidth": false,
                "pageLength": <?php echo $this->session->userdata('user_profile')->cng_per_page; ?>,
                "processing": true,
                "stateSave": true,
                "serverSide": false,
                "bSort": true,
                "autoWidth": false,
                "language": {
                    "url": "<?php echo base_url() ?>dist/js/pages/datatable/<?php echo $this->session->userdata('user_profile')->cng_lang; ?>.json",
                    "headers": {
                        "Access-Control-Allow-Origin": "*"
                    },
                    searchPlaceholder: "<?php echo $lang_sys->lbl_search; ?>",
                },
                "ajax": {
                    "url": "<?php echo base_url('emp/ap_leave_doc/get_data') ?>",
                    "type": "POST",
                    "data": function(d) {
                        d.deptID = $('#department_id').val();
                        d.is_active = $('#is_active').val();
                        d.dateRange = $('#dateRange').val();
                        d.department = $('#department').val();
                        d.leaveType = $('#leaveType').val();
                        d.status = $('#status').val();
                        d.select_all_dates = $('#selectAllDates').is(':checked');
                    }
                },
                "deferRender": true,
                "aLengthMenu": [
                    [10, 25, 50, 100],
                    [10, 25, 50, 100]
                ],
                "columns": [{
                        "title": "#",
                        "data": "no",
                        "orderable": false,
                        "className": "text-center",
                        "width": "35px",
                        "render": function(data, type, row, meta) {
                            return meta.row + 1;
                        }
                    },
                    {
                        "title": "รหัสพนักงาน",
                        "data": "personid",
                        "className": "text-left",
                        "render": function(data, type, row) {
                            var html = '<a href="<?php echo base_url('emp/personal_info'); ?>/' + row.personid + '" class="text-info data" data-toggle="modal" data-target="#leaveDetailModal" data-id="' + row.id + '">' + data + '</a>';
                            // html += '<a class="text-info doctor" modal="true" data-toggle="modal" data-target="#leaveDetailModal" data-id="' + row.id + '" data-path="' + row.leavepath + '"><i class="fas fa-user-md ml-2" data-toggle="tooltip" title="ดูประวัติการรักษา"></i></a>';
                            html += '<a class="text-info doctor" modal="true" data-toggle="modal" data-target="#leaveDetailModal" data-id="' + row.id + '" data-path="' + row.leavepath + '">' + (row.leavepath ? '<i class="fas fa-user-md ml-2" data-toggle="tooltip" title="ดูประวัติการรักษา"></i>' : '') + '</a>';
                            return html;
                        }
                    },
                    {
                        "title": "ชื่อ-นามสกุล",
                        "data": "name",
                        "className": "text-center"
                    },
                    {
                        "title": "แผนก",
                        "data": "deptname",
                        "className": "text-center"
                    },
                    {
                        "title": "วันที่ลา",
                        "data": "leavedate",
                        "className": "text-center"
                    },
                    {
                        "title": "ประเภทการลา",
                        "data": "leavetype",
                        "className": "text-center"
                    },
                    {
                        "title": "เหตุผล",
                        "data": "leavereason",
                        "className": "text-center"
                    },
                    {
                        "title": "จำนวนวัน",
                        "data": "leaveduration",
                        "className": "text-center",
                        "render": function(data, type, row) {
                            return data + ' ' + row.leaveunit;
                        }
                    },
                    {
                        "title": "<i class='fa fa-cog'></i>",
                        "className": "text-center",
                        "data": "id",
                        "orderable": false,
                        "width": "auto",
                        "render": function(data, type, row) {
                            var html = '';
                            if (row.record_status === 'N') {
                                html += '<button class="btn btn-success btn-sm approve-btn" data-id="' + data + '"><i class="fa fa-check"></i> อนุมัติ</button> ';
                                html += '<button class="btn btn-danger btn-sm reject-btn" data-id="' + data + '"><i class="fa fa-times"></i> ปฏิเสธ</button>';
                            }
                            return html;
                        }
                    },
                    {
                        "title": "<i class='fas fa-ellipsis-v'></i>",
                        "className": "text-center",
                        "data": "id",
                        "orderable": false,
                        "render": function(data, type, row) {
                            var html = `
                            <div class="dropdown">
                                <button class="btn btn-link btn-sm p-0" type="button" id="dropdownMenuButton-` + data + `" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                                    <i class="fas fa-ellipsis-v"></i>
                                </button>
                                <div class="dropdown-menu dropdown-menu-right" aria-labelledby="dropdownMenuButton-` + data + `">
                                    <a class="dropdown-item" data-toggle="modal" data-target="#leaveDetailModal" data-id="` + data + `">
                                        <i class="fas fa-eye mr-2"></i>ดูประวัติการลา
                                    </a>
                                    <!-- เพิ่มรายการเมนูอื่นๆ ตามต้องการ -->
                                </div>
                            </div>
                        `;
                            return html;
                        }
                    }
                ],
                "order": [
                    [1, "asc"]
                ],
                "drawCallback": function(settings) {
                    // จัดการการแสดงผลของ DataTable
                    $('.approve-btn, .reject-btn').on('click', function() {
                        var id = $(this).data('id');
                        var action = $(this).hasClass('approve-btn') ? 'approve' : 'reject';
                        $.ajax({
                            url: '<?php echo base_url('leave/updateStatus'); ?>',
                            method: 'POST',
                            data: {
                                id: id,
                                action: action
                            },
                            success: function(response) {
                                if (response.success) {
                                    provTable.ajax.reload();
                                } else {
                                    alert('เกิดข้อผิดพลาด: ' + response.message);
                                }
                            }
                        });
                    });
                    // เมื่อเอาเมาส์ไปวางบนปุ่ม ให้แสดง tooltip
                    $('[data-toggle="tooltip"]').tooltip();

                },
                "initComplete": function(settings, json) {
                    var api = new $.fn.dataTable.Api(settings);

                    // สร้าง container สำหรับ checkboxes
                    var $checkboxContainer = $('<div>', {
                        id: 'columnToggle',
                        class: 'd-flex flex-wrap justify-content-start align-items-center mt-3 mb-3'
                    }).insertBefore($(api.table().container()));

                    // สร้าง label สำหรับ checkboxes
                    $('<label>', {
                        for: 'columnToggle',
                        class: 'mr-3 mb-2 font-weight-bold',
                        text: 'แสดง/ซ่อนคอลัมน์:'
                    }).appendTo($checkboxContainer);

                    // สร้าง checkboxes สำหรับแต่ละคอลัมน์
                    api.columns().every(function(index) {
                        var column = this;
                        var columnTitle = $(column.header()).text().trim();

                        // ตรวจสอบว่าคอลัมน์มีข้อความหรือไม่
                        if (columnTitle) {
                            var $checkboxDiv = $('<div>', {
                                class: 'form-check form-check-inline mb-2 mr-3'
                            }).appendTo($checkboxContainer);

                            var $checkbox = $('<input>', {
                                type: 'checkbox',
                                class: 'form-check-input',
                                id: 'toggle-col-' + index,
                                checked: column.visible()
                            }).appendTo($checkboxDiv);

                            $('<label>', {
                                class: 'form-check-label',
                                for: 'toggle-col-' + index,
                                text: columnTitle
                            }).appendTo($checkboxDiv);

                            // เพิ่ม event listener สำหรับการเปลี่ยนแปลงสถานะ checkbox
                            $checkbox.on('change', function() {
                                var isChecked = $(this).prop('checked');
                                column.visible(isChecked);
                            });
                        }
                    });

                    // เพิ่มปุ่มเลือกทั้งหมด/ยกเลิกทั้งหมด
                    var $selectAllDiv = $('<div>', {
                        class: 'ml-auto mb-2'
                    }).appendTo($checkboxContainer);

                    var $selectAllBtn = $('<button>', {
                        type: 'button',
                        class: 'btn btn-outline-primary btn-sm mr-2',
                        text: 'เลือกทั้งหมด'
                    }).appendTo($selectAllDiv);

                    var $deselectAllBtn = $('<button>', {
                        type: 'button',
                        class: 'btn btn-outline-secondary btn-sm',
                        text: 'ยกเลิกทั้งหมด'
                    }).appendTo($selectAllDiv);

                    // เพิ่ม event listeners สำหรับปุ่มเลือกทั้งหมด/ยกเลิกทั้งหมด
                    $selectAllBtn.on('click', function() {
                        $('.form-check-input').prop('checked', true).trigger('change');
                    });

                    $deselectAllBtn.on('click', function() {
                        $('.form-check-input').prop('checked', false).trigger('change');
                    });

                    if ($('#selectAllDates').is(':checked')) {
                        $('#dateRange').val('');
                    } else {
                        $('#dateRange').val('<?php echo date('d/m/Y', strtotime('-7 days')); ?> - <?php echo date('d/m/Y'); ?>');
                    }
                }
            });

            $(document).on('click', '.doctor, a.data', function(e) {
                e.preventDefault();
                var iframe = $('#leaveDetailIframe');
                if ($(this).hasClass('doctor')) {
                    var url = '<?php echo base_url(); ?>' + $(this).data('path');
                } else {
                    var url = '<?php echo base_url('emp/ap_leave_doc'); ?>/loadPdf/' + $(this).data('id');
                }
                iframe.attr('src', url);
            });

            $('#filterForm').on('submit', function(e) {
                e.preventDefault();
                provTable.ajax.reload();
            });

            // selectAllDates
            $('#selectAllDates').on('change', function() {
                if ($(this).is(':checked')) {
                    $('#dateRange').val('');
                } else {
                    $('#dateRange').val('<?php echo date('d/m/Y', strtotime('-7 days')); ?> - <?php echo date('d/m/Y'); ?>');
                }
                provTable.ajax.reload();
            });
            // filterForm
            $('#filterForm select').on('change', function() {
                provTable.ajax.reload();
            });
        });
    </script>
<?php
} /* Can View */ ?>