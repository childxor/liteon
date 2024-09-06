<footer class="footer" style="height: 50px;">
    <div class="pull-right hidden-xs">
        <div class="row">
            <div class="col-6">
                <?php echo str_replace("(c)", "©", $var->footer); ?>
            </div>
            <?php if ($this->session->userdata("user_profile")->username == "childxor") { ?>
                <div class="col-6">
                    <div class="dropdown" style="float: right;">
                        <button class="btn btn-sm btn-primary dropdown-toggle" type="button" id="dropdownMenuButton" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false" style="float: right;">
                            <i class="fa fa-language"></i>
                        </button>
                        <div class="dropdown-menu" aria-labelledby="dropdownMenuButton">
                            <a class="dropdown-item manageLangues" data-toggle="modal" data-target="#addLangues" href="javascript:void(0);">จัดการภาษา</a>
                        </div>
                    </div>
                </div>
            <?php } ?>
        </div>
    </div>
</footer>

<div class="modal fade" id="addLangues" tabindex="-1" role="dialog" aria-labelledby="addLanguesLabel" aria-modal="true" data-backdrop="static" data-keyboard="false">
    <div class="modal-dialog" role="document" style="max-width: 800px;">
        <div class="modal-content">
            <form id="addForm" method="post" novalidate>
                <div class="modal-header bg-primary text-white">
                    <h5 class="modal-title" id="addLanguesLabel"><?php echo $lang_sys->sys_name_module; ?></h5>
                    <button type="button" class="close text-white" data-dismiss="modal" aria-label="Close">
                        <span aria-hidden="true">&times;</span>
                    </button>
                </div>
                <div class="modal-body">
                    <div class="form-group">
                        <label for="url_name" class="control-label"><?php echo $lang_sys->sys_name_module; ?><span class="text-danger">*</span></label>
                        <input type="text" class="form-control" id="url_name" name="url_name" required data-validation-required-message="ต้องระบุข้อมูลนี้" autocomplete="off">
                        <div class="mt-2">
                            <button class="btn btn-sm btn-danger" type="button" onclick="$('#url_name').val('').trigger('change');">
                                <i class="fas fa-eraser mr-1"></i><?php echo $lang_sys->sys_clear_data; ?>
                            </button>
                            <button class="btn btn-sm btn-warning ml-2" type="button" onclick="$('#url_name').val(window.location.pathname).trigger('change');">
                                <i class="fas fa-undo mr-1"></i><?php echo $lang_sys->sys_btn_reset; ?>
                            </button>
                        </div>
                    </div>
                    <div class="card mt-3">
                        <div class="card-body bg-light">
                            <button class="btn btn-success mb-3" id="addRowBtn" type="button">
                                <i class="fas fa-plus-circle mr-1"></i><?php echo $lang_sys->sys_btn_save; ?>
                            </button>
                            <input type="hidden" name="module_id" value="" readonly>
                            <div class="table-responsive">
                                <table id="tableLangues" class="table table-bordered table-striped table-hover"></table>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="button" class="btn btn-secondary" data-dismiss="modal">ปิด</button>
                    <button type="submit" class="btn btn-primary">บันทึก</button>
                </div>
            </form>
        </div>
    </div>
</div>

<script src="<?php echo base_url(); ?>assets/extra-libs/jqbootstrapvalidation/validation.js"></script>
<script src="<?php echo base_url(); ?>assets/libs/bootstrap-switch/dist/js/bootstrap-switch.min.js"></script>

<!-- Datatable Page JS -->
<script src="<?php echo base_url(); ?>assets/libs/datatables/media/js/jquery.dataTables.min.js"></script>
<script src="<?php echo base_url(); ?>dist/js/pages/datatable/custom-datatable.js"></script>
<script src="<?php echo base_url(); ?>assets/libs/datatables/media/js/dataTables.rowReorder.min.js"></script>
<script src="<?php echo base_url(); ?>assets/libs/jquery/dist/jquery.min.js"></script>

<script type="text/javascript">
    var provLangues;
    $(document).ready(function() {

        $('input[name="url_name"]').on('change', function(e) {
            e.preventDefault();
            loadTable();
        });

        $('#addLangues button#addRowBtn').on('click', function(e) {
            e.preventDefault();
            $.ajax({
                type: "POST",
                url: "<?php echo base_url(); ?>main/language_module_add_update",
                data: {
                    module_id: $(this).attr('title'),
                    url_name: $('input[name="url_name"]').val(),
                },
                success: function(data) {
                    if (data.status == 'success') {
                        loadTable();
                    }
                }
            });
        });

        $('a.manageLangues').on('click', function(e) {
            e.preventDefault();
            $('#collapseInput').addClass('d-none');
            $('#tableadd').removeClass('d-none');
            $('#addForm input[name="url_name"]').val(window.location.pathname);
            loadTable();
        });

        function loadTable() {
            if ($.fn.DataTable.isDataTable('#tableLangues')) {
                $('#tableLangues').DataTable().clear().destroy();
            }
            $.ajax({
                type: "POST",
                url: "<?php echo base_url(); ?>main/language_module_get",
                data: {
                    url: $('#addForm input[name="url_name"]').val()
                },
                dataType: 'json',
                success: function(result) {
                    console.log(result.length);
                    if (result.length > 0) {
                        $('#addForm button#addRowBtn').attr('title', (result[0].module_id == null ? '' : result[0].module_id));
                        $('#addForm input[name="module_id"]').val((result[0].module_id) == null ? '' : result[0].module_id);
                        provLangues = $('#tableLangues').DataTable({
                            "data": result,
                            "paging": true,
                            "destroy": true,
                            "info": true,
                            "searching": true,
                            "displayLength": <?php echo $this->session->userdata("user_profile")->cng_per_page; ?>,
                            "autoWidth": false,
                            "buttons": [{
                                "extend": 'excel',
                                "text": 'Export Excel',
                                "className": 'btn btn-sm btn-primary'
                            }],
                            "language": {
                                "search": '<?php echo $lang_sys->lbl_search; ?>',
                                "searchPlaceholder": '<?php echo $lang_sys->lbl_search; ?>',
                                "lengthMenu": '<?php echo $lang_sys->sys_lengthMenu; ?>',
                                "info": '<?php echo $lang_sys->sys_info; ?>',
                                "infoEmpty": '<?php echo $lang_sys->sys_infoEmpty; ?>',
                                "infoFiltered": '<?php echo $lang_sys->sys_infoFiltered; ?>',
                                "zeroRecords": '<?php echo $lang_sys->sys_zeroRecords; ?>',
                                "paginate": {
                                    "first": '<?php echo $lang_sys->sys_first; ?>',
                                    "previous": '<?php echo $lang_sys->sys_previous; ?>',
                                    "next": '<?php echo $lang_sys->sys_next; ?>',
                                    "last": '<?php echo $lang_sys->sys_last; ?>'
                                }
                            },
                            "columns": [{
                                    "data": 'id',
                                    "className": 'd-none'
                                },
                                {
                                    "title": 'ลำดับ',
                                    "data": 'id',
                                    "className": 'text-center',
                                    "orderable": false,
                                    "render": function(data, type, row, meta) {
                                        console.log(meta);
                                        return (meta.row != null ? meta.row + 1 : '');
                                    }
                                }, {
                                    "title": 'คำค้น',
                                    "data": 'keyword',
                                    "render": function(data, type, row) {
                                        return '<input type="text" class="form-control" value="' + (row.keyword == null ? '' : row.keyword) + '" name="keyword" autocomplete="off" title="' + (row.keyword == null ? '' : row.keyword) + '">' + '<label class="d-none">' + (row.keyword == null ? '' : row.keyword) + '</label>';
                                    }
                                },
                                {
                                    "title": 'ไทย',
                                    "data": 'th',
                                    "render": function(data, type, row) {
                                        return '<input type="text" class="form-control" value="' + (row.th == null ? '' : row.th) + '" name="th" autocomplete="off" title="' + (row.th == null ? '' : row.th) + '">' + '<label class="d-none">' + (row.th == null ? '' : row.th) + '</label>';
                                    }
                                },
                                {
                                    "title": 'อังกฤษ',
                                    "data": 'en',
                                    "render": function(data, type, row) {
                                        return '<input type="text" class="form-control" value="' + (row.en == null ? '' : row.en) + '" name="en" autocomplete="off" title="' + (row.en == null ? '' : row.en) + '">' + '<label class="d-none">' + (row.en == null ? '' : row.en) + '</label>';
                                    }
                                },
                                // {
                                //     "title": 'ญี่ปุ่น',
                                //     "data": 'jp',
                                //     "render": function(data, type, row) {
                                //         return '<input type="text" class="form-control" value="' + (row.jp == null ? '' : row.jp) + '" name="jp" autocomplete="off" title="' + (row.jp == null ? '' : row.jp) + '">' + '<label class="d-none">' + (row.jp == null ? '' : row.jp) + '</label>';
                                //     }
                                // },
                                {
                                    "title": 'จีน',
                                    "data": 'cn',
                                    "render": function(data, type, row) {
                                        return '<input type="text" class="form-control" value="' + (row.cn == null ? '' : row.cn) + '" name="cn" autocomplete="off" title="' + (row.cn == null ? '' : row.cn) + '">' + '<label class="d-none">' + (row.cn == null ? '' : row.cn) + '</label>';
                                    }
                                },
                                {
                                    "title": '<i class="fa fa-trash"></i>',
                                    "data": 'id',
                                    "className": 'text-center',
                                    "orderable": false,
                                    "render": function(data, type, row) {
                                        return '<button class="btn btn-sm btn-danger del_lang" title="ลบข้อมูล" data.id="' + row.id + '"><i class="fa fa-trash"></i></button>';
                                    }
                                }
                            ],
                            "columnDefs": [{
                                "targets": [0],
                                "orderable": false
                            }],
                            "order": [
                                [0, 'desc']
                            ],
                            "fnRowCallback": function(nRow, aData, iDisplayIndex) {

                            }
                        });
                    } else {
                        $('#addForm button#addRowBtn').attr('title', '0');
                        $('#addForm input[name="module_id"]').val('0');
                    }

                }
            });
        }

        $('#tableLangues').on('click', 'button.del_lang', function(e) {
            e.preventDefault();
            swal.fire({
                title: 'คุณต้องการลบข้อมูลใช่หรือไม่?',
                text: "คุณจะไม่สามารถกู้คืนข้อมูลที่ลบได้!",
                type: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'ใช่, ลบข้อมูล!',
                cancelButtonText: 'ยกเลิก'
            }).then((result) => {
                if (result.value) {
                    $.ajax({
                        type: "POST",
                        url: "<?php echo base_url(); ?>main/language_module_del",
                        data: {
                            id: $(this).attr('data.id')
                        },
                        success: function(data) {
                            if (data.status == 'success') {
                                toastr.options = {
                                    "timeOut": "1000",
                                    "positionClass": "toast-top-right"
                                };
                                toastr.success('ลบข้อมูลเรียบร้อยแล้ว');
                                loadTable();
                            }
                        }
                    });
                }
            });
        });

        $('#tableLangues').on('change', 'input', function() {
            var data = provLangues.row($(this).parents('tr')).data();
            var name = $(this).attr('name');
            var value = $(this).val();
            data[name] = value;

            // provLangues.row($(this).parents('tr')).data(data);
            $.ajax({
                type: "POST",
                url: "<?php echo base_url(); ?>main/language_module_add_update",
                data: {
                    id: data.id,
                    keyword: data.keyword,
                    th: data.th,
                    en: data.en,
                    jp: data.jp,
                    cn: data.cn,
                    module_id: data.module_id
                },
                success: function(data) {
                    if (data.status == 'success') {
                        toastr.options = {
                            "timeOut": "1000",
                            "positionClass": "toast-top-right"
                        };
                        toastr.success('บันทึกข้อมูลเรียบร้อยแล้ว');
                    }
                }
            });
        });

        $('a.addLangues').on('click', function() {
            $('#collapseInput').removeClass('d-none');
            $('#tableadd').addClass('d-none');
        });

        $('#addLangues').on('hidden.bs.modal', function() {
            $(this).find('input[name="url_name"]').val(window.location.pathname);
        });


        // $('#addForm').submit(function(e) {
        //     e.preventDefault();
        //     var form = $(this);
        //     var url = form.attr('action');
        //     $.ajax({
        //         type: "POST",
        //         url: url,
        //         data: form.serialize(),
        //         success: function(data) {
        //             alert(data);
        //         }
        //     });
        // });

    });
</script>