<style>
    #exampleModal .modal-dialog {
        width: 70%;
        max-width: 100%;
        height: auto;
        margin: 10%;
        position: absolute;
        z-index: 9999;
        display: block;
    }

    #EditPrModal .modal-dialog {
        width: 70%;
        max-width: 100%;
        height: auto;
        margin: 10%;
        position: absolute;
        z-index: 9999;
        display: block;
    }


    #registerModal .modal-dialog {
        position: absolute;
        opacity: 0.9;
        left: 50%;
        top: 35%;
        transform: translate(-50%, -50%);
    }

    #dataTable_filter {
        text-align: right;
        margin-right: 10px;
    }

    /* หากต้องการปรับขนาดของ input Search */
    #dataTable_filter input {
        width: 150px;
        /* ปรับความกว้างของ input Search ตามที่คุณต้องการ */
    }

    tbody tr td {
        padding: 0px 0px 0px 0px;
        font-size: 16px;
    }
</style>
<!DOCTYPE html>
<html>

<head>
    <title>Remote Control</title>
</head>

<body>
    <div class="container">
        <h1>Remote Control</h1>
        <div class="row">
            <div class="col-md-4">
                <button class="btn btn-info btn-lg btn-block" data-toggle="modal" data-target="#EditPrModal"><i class="fa fa-table"></i> EditPrModal</button>
            </div>
            <div class="col-md-4">
                <button class="btn btn-success btn-lg btn-block" data-toggle="modal" data-target="#exampleModal"><i class="fa fa-table"></i> DataTable</button>
            </div>
            <div class="col-md-4">
                <button class="btn btn-success btn-lg btn-block" data-toggle="modal" data-target="#registerModal"><i class="fa fa-exclamation"></i> Register</button>
            </div>
        </div>
        <div class="row mt-3">
            <div class="col-md-4">
                <button id="searchID" class="btn btn-primary btn-lg btn-block"><i class="fa fa-7x"></i> หาไอดี PR PO</button>
            </div>
            <div class="col-md-4">
                <button class="btn btn-primary btn-lg btn-block" id="exec_spupdatestate"><i class="fa fa-9x"></i> exec_spupdatestate</button>
            </div>
            <div class="col-md-4">
                <button class="btn btn-primary btn-lg btn-block reset_bbf"><i class="fa fa-9x"></i> Reset BBF</button>
            </div>
        </div>
        <div class="row mt-3">
            <div class="col-md-4">
                <button id="del_pr_khem" class="btn btn-primary btn-lg btn-block"><i class="fa fa-7x"></i> ลบข้อมูล Pr เทส</button>
            </div>
            <div class="col-md-4">
                <button class="btn btn-primary btn-lg btn-block" id="exec_spupdatestate"><i class="fa fa-9x"></i> 7</button>
            </div>
            <div class="col-md-4">
                <button class="btn btn-primary btn-lg btn-block reset_bbf"><i class="fa fa-9x"></i> 8</button>
            </div>
        </div>
    </div>

    <!-- Modal button 1 -->
    <div class="modal fade" id="exampleModal" role="dialog" aria-labelledby="exampleModalLabel" aria-hidden="false" data-backdrop="static" data-keyboard="false">
        <div class="modal-dialog" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" id="dataTableModalLabel">Data Table</h5>
                    <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                        <span aria-hidden="true">&times;</span>
                    </button>
                </div>
                <div class="modal-body">
                    <div class="row">
                        <div class="col-md-6">
                            <button class="btn btn-success btn-block" id="importExcel"><i class="fa fa-file-excel"></i> Import Excel</button>
                        </div>
                        <div class="col-md-6">
                            <button class="btn btn-danger btn-block" id="exportExcel"><i class="fa fa-file-excel"></i> Export Excel</button>
                        </div>
                    </div>
                </div>
                <div class="modal-body">
                    <table id="dataTable" class="table table-striped table-bordered" style="width:100%;">
                    </table>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-dismiss="modal">Close</button>
                </div>
            </div>
        </div>
    </div>
    <!-- End Modal -->

    <!-- Modal button  -->
    <div class="modal fade" id="EditPrModal" role="dialog" aria-labelledby="EditPrLabel" aria-hidden="false" data-backdrop="static" data-keyboard="false">
        <div class="modal-dialog modal-dialog-top modal-lg" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" id="EditPrLabel">Edit PR PO</h5>
                    <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                        <span aria-hidden="false">&times;</span>
                    </button>
                </div>
                <div class="card">
                    <div class="card-body">
                        <div class="row">
                            <div class="col-sm-12 col-lg-6">
                                <div class="form-group row">
                                    <label for="fname2" class="col-sm-4 text-right control-label col-form-label">ใบเสนอซื้อเลขที่</label>
                                    <div class="col-sm-8">
                                        <input type="text" class="form-control" name="pr_no" placeholder="PR202312XXXX" readonly="">
                                    </div>
                                </div>
                            </div>
                            <div class="col-sm-12 col-lg-6">
                                <div class="form-group row">
                                    <label for="lname2" class="col-sm-4 text-right control-label col-form-label">วันที่เอกสาร<span class="text-danger">*</span></label>
                                    <div class="col-sm-8">
                                        <input type="text" name="date" class="form-control" placeholder="YYYY-MM-DD" id="date" required="" data-dtp="dtp_bP0FK">
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-sm-12 col-lg-6">
                                <div class="form-group row">
                                    <label for="uname1" class="col-sm-4 text-right control-label col-form-label">หน่วยงาน</label>
                                    <div class="col-sm-8">
                                        <input type="text" class="form-control" placeholder="IT" readonly="">
                                    </div>
                                </div>
                            </div>
                            <div class="col-sm-12 col-lg-6">
                                <div class="form-group row">
                                    <label class="col-sm-4 text-right control-label col-form-label">ผู้ขอเสนอซื้อ</label>
                                    <div class="col-sm-8">
                                        <input type="text" class="form-control" placeholder="อดิศักดิ์ สวนดอกไม้" readonly="">
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-sm-12 col-lg-6">
                                <div class="form-group row">
                                    <label class="col-sm-4 text-right control-label col-form-label">แผนก<span class="text-danger">*</span></label>
                                    <div class="col-sm-8">
                                        <input type="text" name="sub_department" id="sub_department" autofocus="" class="form-control" placeholder="" required="" autocomplete="off">
                                    </div>
                                </div>
                            </div>
                            <div class="col-sm-12 col-lg-6">
                                <div class="form-group row">
                                    <label class="col-sm-4 text-right control-label col-form-label">วันที่ต้องการ<span class="text-danger">*</span></label>
                                    <div class="col-sm-8">
                                        <input type="text" name="within" class="form-control" placeholder="YYYY-MM-DD" id="within" required="" data-dtp="dtp_ks9Rd">
                                    </div>
                                </div>
                            </div>
                        </div>
                        <div class="row">
                            <div class="col-sm-12 col-lg-6">
                                <div class="form-group row">
                                    <label class="col-sm-4 text-right control-label col-form-label">สถานที่จัดเก็บ<span class="text-danger">*</span></label>
                                    <div class="col-sm-8">
                                        <select class="form-control" name="location_id" id="location_id" required="">
                                            <option value=""></option>
                                            <option value="3">Server Room - ชั้น 1 Office</option>
                                            <option value="5">DIRECT USE - ซื้อมาแล้วนำใช้ทันที</option>
                                        </select>
                                    </div>
                                </div>
                            </div>
                            <div class="col-sm-12 col-lg-6">
                                <div class="form-group row">
                                    <label class="col-sm-4 text-right control-label col-form-label">ผู้ผลิต/บริษัท</label>
                                    <div class="col-sm-8">
                                        <select class="form-control" id="supplier_id" name="supplier_id" required="">
                                            <option value=""></option>
                                        </select>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="card-body table-responsive">
                        <h4 class="card-title">รายการเสนอซื้อ</h4>
                        <table id="list_table" class="table table-bordered" style="width:100%"></table>
                        <div class="form-group m-t-20">
                            <label class="control-label">คำอธิบาย </label>
                            <div class="controls">
                                <textarea id="description" name="description" class="form-control" rows="3" placeholder=""></textarea>
                            </div>
                        </div>
                        <div class="card-body">
                            <div class="row">
                                <div class="col-6 text-left">
                                    <button id="draftBtn" type="submit" class="btn btn-light">บันทึกร่างรายการเสนอซื้อ</button>
                                </div>
                                <div class="col-6 text-right">
                                    <button type="button" class="btn btn-danger" data-dismiss="modal">ยกเลิก</button>
                                    <button id="saveBtn" type="submit" class="btn btn-primary">บันทึกข้อมูล</button>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>
    <!-- End Modal -->

    <!-- Modal Register -->
    <div class="modal fade" id="registerModal" tabindex="-1" role="dialog" aria-labelledby="registerModalLabel" aria-hidden="true">
        <div class="modal-dialog fade" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title" id="registerModalLabel">ลงทะเบียน</h5>
                    <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                        <span aria-hidden="true">&times;</span>
                    </button>
                </div>
                <div class="modal-body">
                    <form>
                        <div class="form-row">
                            <img src="<?php echo base_url('assets/images/logo-text.png'); ?>" alt="logo" class="img-fluid mx-auto d-block" style="width: 50%;">
                        </div>
                        <div class="form-row">
                            <div class="form-group col-md-6">
                                <input type="text" class="form-control" id="firstName" placeholder="ชื่อ" required>
                            </div>
                            <div class="form-group col-md-6">
                                <input type="text" class="form-control" id="lastName" placeholder="นามสกุล" required>
                            </div>
                        </div>
                        <div class="form-group">
                            <div class="input-group">
                                <input type="email" class="form-control" id="email" placeholder="อีเมล" required>
                                <div class="input-group-append">
                                    <select class="form-control" id="emailDomain">
                                        <option value="@gmail.com">@gmail.com</option>
                                        <option value="@yahoo.com">@yahoo.com</option>
                                        <option value="@hotmail.com">@hotmail.com</option>
                                    </select>
                                </div>
                            </div>
                        </div>
                        <div class="form-group">
                            <textarea class="form-control" id="address" placeholder="ที่อยู่" required></textarea>
                        </div>
                        <div class="form-group">
                            <div class="form-row">
                                <div class="col-md-6">
                                    <span class="input-group-text"><i class="fab fa-line"><input type="text" class="form-control" id="lineId" placeholder="Line ID"></i></span>
                                </div>
                                <div class="col-md-6">
                                    <span class="input-group-text"><i class="fab fa-facebook"><input type="text" class="form-control" id="facebook" placeholder="Facebook"></i></span>
                                </div>
                            </div>
                        </div>
                        <div class="form-row">
                            <div class="form-group col-md-6">
                                <input type="password" class="form-control" id="password" placeholder="รหัสผ่าน" required>
                            </div>
                            <div class="form-group col-md-6">
                                <input type="password" class="form-control" id="confirmPassword" placeholder="ยืนยันรหัสผ่าน" required>
                            </div>
                            <div class="form-group col-md-12">
                                <span id='message'></span>
                            </div>
                        </div>
                        <div class="form-row">
                            <div class="form-group col-md-6">
                                <button type="button" class="btn btn-warning" id="generatePassword"><i class="fa fa-key btn-warning"></i>สร้างรหัสผ่าน</button>
                            </div>
                            <div class="form-group col-md-6">
                                <button type="button" class="btn btn-danger" id="showPassword"><i class="fa fa-eye btn-danger"></i>แสดงรหัสผ่าน</button>
                            </div>
                        </div>
                        <div class="form-row">
                            <div class="col-6">
                                <button type="submit" class="btn btn-success"><i class="fa fa-check"></i> ลงทะเบียน</button>
                            </div>
                            <div class="col-6">
                                <button type="button" class="btn btn-danger" id="clearForm"><i class="fa fa-trash"></i> ล้างข้อมูล</button>
                            </div>
                        </div>
                    </form>
                </div>
            </div>
        </div>
    </div>

    <!-- End Page Content -->
    <!-- ============================================================== -->
    <script src="<?php echo base_url(); ?>assets/libs/jquery/dist/jquery.min.js"></script>
    <!-- This Page JS -->
    <script src="<?php echo base_url(); ?>assets/extra-libs/jqbootstrapvalidation/validation.js"></script>
    <script src="<?php echo base_url(); ?>assets/libs/bootstrap-switch/dist/js/bootstrap-switch.min.js"></script>

    <!-- This Page JS -->
    <script src="<?php echo base_url(); ?>assets/libs/moment/moment.js"></script>
    <link rel="stylesheet" type="text/css" href="<?php echo base_url(); ?>assets/libs/bootstrap-material-datetimepicker/css/bootstrap-material-datetimepicker.css">
    <script src="<?php echo base_url(); ?>assets/libs/bootstrap-material-datetimepicker/js/bootstrap-material-datetimepicker-custom.js"></script>

    <!-- swal2 -->
    <!-- <script src="<?php echo base_url(); ?>assets/libs/sweetalert2/dist/swal.js"></script> -->
    <!-- <link rel="stylesheet" type="text/css" href="<?php echo base_url(); ?>assets/libs/sweetalert2/dist/sweetalert2.min.css"> -->

    <!-- Datatable Page JS -->
    <script src="<?php echo base_url(); ?>assets/libs/datatables/media/js/jquery.dataTables.min.js"></script>
    <script src="<?php echo base_url(); ?>dist/js/pages/datatable/custom-datatable.js"></script>
    <script src="<?php echo base_url(); ?>assets/libs/datatables/media/js/dataTables.rowReorder.min.js"></script>
    <!-- <script src="<?php echo base_url(); ?>assets/libs/datatables/media/event.js"></script> -->

    <!-- DataTables Buttons CSS -->
    <link rel="stylesheet" type="text/css" href="<?php echo base_url(); ?>assets/libs/button_datatable_ajax/buttons.dataTables.min.css">
    <!-- DataTables Buttons JavaScript -->
    <script type="text/javascript" charset="utf8" src="<?php echo base_url(); ?>assets/libs/button_datatable_ajax/dataTables.buttons.min.js"></script>
    <script type="text/javascript" charset="utf8" src="<?php echo base_url(); ?>assets/libs/button_datatable_ajax/jszip.min.js"></script>
    <script type="text/javascript" charset="utf8" src="<?php echo base_url(); ?>assets/libs/button_datatable_ajax/pdfmake.min.js"></script>
    <script type="text/javascript" charset="utf8" src="<?php echo base_url(); ?>assets/libs/button_datatable_ajax/vfs_fonts.js"></script>
    <script type="text/javascript" charset="utf8" src="<?php echo base_url(); ?>assets/libs/button_datatable_ajax/buttons.html5.min.js"></script>

    <script type="text/javascript" src="https://cdnjs.cloudflare.com/ajax/libs/xlsx/0.17.4/xlsx.full.min.js"></script>



    <script>
        var table;
        ! function(window, document, $) {
            "use strict";
            table = $('#list_table').DataTable({
                "bFilter": false,
                "bInfo": false,
                "destroy": true,
                "processing": false,
                "bAutoWidth": false,
                "serverSide": false,
                "displayLength": 25,
                "paging": false,
                "data": [{
                    "nrow": "",
                    "material_name_th": "",
                    "material_type_id": "",
                    "unit_id": "",
                    "formula": "",
                    "amount": "",
                    "budget_code": "",
                    "nrow": ""
                }],
                "columns": [{
                        "title": "ลำดับ",
                        "data": "nrow",
                        "width": "2%",
                        "className": "text-center",
                        "render": function(data, type, row, meta) {
                            return meta.row + 1;
                        },
                    },
                    {
                        "title": "รายการ/ชื่อวัสดุ",
                        "width": "30%",
                        "data": "material_name_th",
                        "render": function(data, type, row, meta) {
                            var html = '<input type="hidden" id="material_id_' + (parseInt(meta.row + 1)) + '" name="material_id[]">';
                            html += '<span class="twitter-typeahead" style="position: relative; display: inline-block;">';
                            html += '<input type="text" class="form-control tt-input" id="material_name_th_' + (parseInt(meta.row + 1)) + '" name="material_name_th[]" placeholder="รายการ/ชื่อวัสดุ" required="" autocomplete="off" spellcheck="false" dir="auto" style="position: relative; vertical-align: top;">';
                            html += '<pre aria-hidden="true" style="position: absolute; visibility: hidden; white-space: pre; font-family: Prompt, sans-serif; font-size: 16px; font-style: normal; font-variant: normal; font-weight: 400; word-spacing: 0px; letter-spacing: 0px; text-indent: 0px; text-rendering: auto; text-transform: none;"></pre>';
                            html += '<div class="tt-menu" style="position: absolute; top: 100%; left: 0px; z-index: 100; display: none;">';
                            html += '<div class="tt-dataset tt-dataset-0"></div>';
                            html += '</div>';
                            html += '</span>';
                            return html;
                        }
                    },
                    {
                        "title": "ประเภทวัสดุ",
                        "width": "15%",
                        "data": "material_type_id",
                        "data": "material_type",
                        "render": function(data, type, row, meta) {
                            var html = '<select class="form-control select" data-id="material_type_id_' + (parseInt(meta.row + 1)) + '"  id="material_type_id' + (parseInt(meta.row + 1)) + '"  name="material_type_id[]" style="width:100%" required="">';
                            html += (row.material_type == undefined) ? '<option value=""></option>' : row.material_type;
                            html += '</select>';
                            return html;
                        }
                    },
                    {
                        "title": "หน่วย",
                        "width": "10%",
                        "data": "unit_id",
                        "render": function(data, type, row, meta) {
                            var html = '<select class="form-control select" data-id="unit_id_' + (parseInt(meta.row + 1)) + '"  id="unit_id_' + (parseInt(meta.row + 1)) + '"  name="unit_id[]" style="width:100%" required="">'
                            html += (row.unit == undefined) ? '<option value=""></option>' : row.unit;
                            html += '</select>'
                            return html;
                        }
                    },
                    {
                        "title": "สูตรคิดราคา",
                        "width": "15%",
                        "data": "formula",
                        "render": function(data, type, row, meta) {
                            var html = '<input type="text" class="form-control" id="formula' + (parseInt(meta.row + 1)) + '"  name="formula[]" placeholder="สูตรคิดราคา" autocomplete="off">'
                            return html;
                        }
                    },
                    {
                        "title": "จำนวน",
                        "width": "10%",
                        "data": "amount",
                        "render": function(data, type, row, meta) {
                            var html = '<input type="number" step="any" class="form-control" id="amount' + (parseInt(meta.row + 1)) + '"  name="amount[]" placeholder="จำนวน" required="" min="1" autocomplete="off">'
                            return html;
                        }
                    },
                    {
                        "title": "รหัสงบประมาณ",
                        "data": "budget_code",
                        "render": function(data, type, row, meta) {
                            var html = '<input type="text" class="form-control" id="budget_code' + (parseInt(meta.row + 1)) + '"  name="budget_code[]" placeholder="รหัสงบประมาณ" autocomplete="off">'
                            return html;
                        }
                    },
                    {
                        "title": "<button class='btn btn-xs btn-success material_table_row' type='button'><i class='fa fa-plus'></i></button>",
                        "data": "nrow",
                        "className": "text-center",
                        "render": function(data, type, row, meta) {
                            if (meta.row == 0) {
                                var html = '<button class="btn btn-xs btn-default erase_material_fields" type="button"><i class="fa fa-eraser"></i></button>';
                            } else {
                                var html = '<button class="btn btn-xs btn-danger erase_material_fields" type="button"><i class="fa fa-trash"></i></button>';
                            }
                            return html;
                        }
                    },
                ],
                "createdRow": function(row, data, dataIndex) {
                    // $(row).find('select').select2({
                    //     width: '100%',
                    // });
                },
                "initComplete": function(settings, json) {
                    $.ajax({
                        url: "<?php echo base_url(); ?>remote/remote/get_material_type_id",
                        method: "POST",
                        data: {
                            material_type_id: 1,
                        },
                        datatype: "json",
                        success: function(data) {
                            $.each(data.sto_material_type, function(key, value) {
                                $('select[data-id="material_type_id_1"]').append('<option value="' + value.id + '">' + value.code + ' ' + value.name + '</option>');
                            });
                            $.each(data.mas_unit, function(key, value) {
                                $('select[data-id="unit_id_1"]').append('<option value="' + value.id + '">' + value.name_th + '</option>');
                            });
                        }
                    });
                },

            });


        }(window, document, window.jQuery),

        // ตัวแปร url 
        $(document).ready(function() {

            $('button#del_pr_khem').click(function(e) {
                e.preventDefault();
                swal.fire({
                    title: 'แน่ใจหรือไม่ว่าต้องการลบข้อมูล',
                }).then((result) => {
                    var url = window.location.pathname.split('/')[1];
                    if (result.value) {
                        if (url == 'efins') {
                            swal.fire({
                                title: 'เว็บไซต์นี้ไม่สามารถลบข้อมูลได้',
                                icon: 'error',
                                confirmButtonText: 'ตกลง',
                            })
                            return false;

                        } else {
                            $.ajax({
                                url: "<?php echo base_url(); ?>remote/remote/del_pr_khem",
                                method: "POST",
                                datatype: "json",
                                success: function(data) {
                                    if (data.status == 'success') {
                                        swal.fire({
                                            title: 'ลบข้อมูลเรียบร้อย',
                                            icon: 'success',
                                            confirmButtonText: 'ตกลง',
                                        })
                                    }
                                }

                            });

                        }

                    }
                });
            });

            $('select[data-id="material_type_id_1"]').select2({
                placeholder: "เลือกประเภทวัสดุ",
                width: '100%',
            });
            $('select[data-id="unit_id_1"]').select2({
                placeholder: "เลือกหน่วย",
                width: '100%',
            });

            $('button#exec_spupdatestate').click(function(e) {
                e.preventDefault();
                $.ajax({
                    url: "<?php echo base_url(); ?>remote/remote/exec_spupdatestate",
                    method: "POST",
                    data: {},
                    datatype: "json",
                    success: function(data) {
                        if (data.status == 'success') {
                            swal.fire({
                                title: 'อัพเดทสถานะเรียบร้อย',
                                icon: 'success',
                                confirmButtonText: 'ตกลง',
                            })
                        }
                    }
                });
            });

            $(document).on('click', 'button.erase_material_fields', function(e) {
                e.preventDefault();
                if ($(this).closest('tr').find('td').eq(0).text() == 1) {
                    $(this).closest('tr').each(function() {
                        $(this).find('input').val('');
                        $(this).find('select').val('');
                    });
                } else {
                    table.row($(this).parents('tr')).remove().draw();
                }
                table.rows().every(function(rowIdx, tableLoop, rowLoop) {
                    table.cell(rowIdx, 0).data(rowIdx + 1).draw();
                });
            });

            $('button.material_table_row').click(function(e) {
                var data = table.row.add({
                    "nrow": "",
                    "material_name_th": "",
                    "material_type_id": "",
                    "material_type": $(table.row(0).node()).find('select[data-id="material_type_id_1"]').html(),
                    "unit_id": "",
                    "unit": $(table.row(0).node()).find('select[data-id="unit_id_1"]').html(),
                    "formula": "",
                    "amount": "",
                    "budget_code": "",
                    "nrow": ""
                }).draw();
                table.columns.adjust().draw();

                $(table.row(data).node()).find('select[data-id="material_type_id_' + (parseInt(data[0]) + 1) + '"]').select2({
                    placeholder: "เลือกประเภทวัสดุ",
                    width: '100%',
                });
                $(table.row(data).node()).find('select[data-id="unit_id_' + (parseInt(data[0]) + 1) + '"]').select2({
                    placeholder: "เลือกหน่วย",
                    width: '100%',
                });
            });

            $('button#searchID').click(function(e) {
                swal.fire({
                    title: 'กรุณากรอกเลขที่ PR PO',
                    // input: 'text',
                    html: '<input id="PRsearch" class="swal2-input" placeholder="PR">' + '<br>' +
                        '<input id="POsearch" class="swal2-input" placeholder="PO">' + '<br>' +
                        '<span class="swal2-input-pr"></span>' + '<br>' +
                        '<span class="swal2-input-po"></span>',
                    inputAttributes: {
                        autocapitalize: 'off'
                    },
                    showCancelButton: false,
                    showLoaderOnConfirm: false,
                    confirmButton: false,
                })
            });

            $(document).on('change', '#PRsearch, #POsearch', function() {
                var pr = $('#PRsearch').val();
                var po = $('#POsearch').val();
                var spanPr = $(this).closest('.swal2-content').find('span.swal2-input-pr');
                var spanPo = $(this).closest('.swal2-content').find('span.swal2-input-po');
                $.ajax({
                    url: "<?php echo base_url(); ?>remote/remote/see_id",
                    method: "POST",
                    data: {
                        PRsearch: pr,
                        POsearch: po,
                    },
                    datatype: "json",
                    success: function(data) {
                        if (data.status == 'success') {
                            if (data.sto_pr == '') {
                                spanPr.addClass('text-danger').html('<span class="text-danger">ไม่พบข้อมูล PR</span>');
                            } else {
                                spanPr.removeClass('text-danger').html('<span class="text-success">PR : <button class="btn btn-success btn-sm" data-toggle="modal" data-target="#EditPrModal">' + data.sto_pr + '</button></span>');
                            }
                            if (data.pur_po == '') {
                                spanPo.addClass('text-danger').html('<span class="text-danger">ไม่พบข้อมูล PO</span>');
                            } else {
                                spanPo.removeClass('text-danger').html('<span class="text-success">PO : <button class="btn btn-success btn-sm" data-toggle="modal" data-target="#EditPrModal">' + data.pur_po + '</button></span>');
                            }
                        }
                    }
                });
            });

            $('#exampleModal').on('shown.bs.modal', function() {
                $('#dataTable').DataTable({
                    "bFilter": true,
                    "bInfo": false,
                    "destroy": true,
                    "processing": false,
                    "serverSide": false,
                    "displayLength": 25,
                    "paging": true,
                    "ajax": {
                        "url": "<?php echo base_url(); ?>remote/remote/get_dataTable",
                        "type": "POST",
                        "dataType": "json",
                        "dataSrc": "",
                    },
                    "columns": [
                        // meta + 1
                        {
                            "title": "#",
                            "className": "text-center",
                            "data": null,
                            "render": function(data, type, row, meta) {
                                return meta.row + meta.settings._iDisplayStart + 1;
                            },
                        },
                        {
                            "data": "material_code",
                            "title": "Material Code",
                            "className": "text-center"
                        },
                        {
                            "data": "material_name",
                            "title": "Material Name"
                        },
                        {
                            "data": "material_type_id",
                            "title": "Material Type ID",
                            "className": "text-center"
                        },
                        {
                            "data": "code",
                            "title": "Code"
                        },
                        {
                            "data": "name",
                            "title": "Name"
                        },
                        {
                            "data": "unit_name",
                            "title": "Unit Name",
                            "className": "text-center"
                        }
                    ],
                });
            });

            $('#EditPrModal').on('shown.bs.modal', function() {
                $('#dataTablePR').DataTable({
                    "bFilter": true,
                    "bInfo": false,
                    "destroy": true,
                    "processing": false,
                    "serverSide": false,
                    "displayLength": 25,
                    "paging": true,
                    "ajax": {
                        "url": "<?php echo base_url(); ?>remote/remote/get_dataTable",
                        "type": "POST",
                        "dataType": "json",
                        "dataSrc": "",
                    },
                    "columns": [{
                            "title": "#",
                            "className": "text-center",
                            "data": null,
                            "render": function(data, type, row, meta) {
                                return meta.row + meta.settings._iDisplayStart + 1;
                            },
                        },
                        {
                            "data": "material_code",
                            "title": "Material Code",
                            "className": "text-center"
                        },
                        {
                            "data": "material_name",
                            "title": "Material Name"
                        },
                        {
                            "data": "material_type_id",
                            "title": "Material Type ID",
                            "className": "text-center"
                        },
                        {
                            "data": "code",
                            "title": "Code"
                        },
                        {
                            "data": "name",
                            "title": "Name"
                        },
                        {
                            "data": "unit_name",
                            "title": "Unit Name",
                            "className": "text-center"
                        }
                    ],
                });
            });

        });

        $(document).ready(function() {
            // Z:\htdocs\efins\application\controllers\Main.php
            $('button.reset_bbf').click(function(e) {
                e.preventDefault();
                swal.fire({
                    title: 'กรอก material_id หรือ material_code',
                    icon: 'warning',
                    input: 'text',
                    inputAttributes: {
                        autocapitalize: 'off'
                    },
                    showCancelButton: true,
                    confirmButtonText: 'ตกลง',
                    cancelButtonText: 'ยกเลิก',
                    showLoaderOnConfirm: true,
                }).then((result) => {
                    if (result.value) {
                        $.ajax({
                            url: "<?php echo base_url(); ?>main/reset_bbf",
                            method: "POST",
                            data: {
                                material_id: result.value,
                            },
                            datatype: "json",
                            success: function(data) {
                                if (data.status == 'success') {
                                    swal.fire({
                                        title: 'Reset BBF เรียบร้อย',
                                        icon: 'success',
                                        confirmButtonText: 'ตกลง',
                                    })
                                }
                            }
                        });
                    }
                });
            });
        });
    </script>



</body>