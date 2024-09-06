<?php /* var_dump($permission); */?>
<?php (empty($permission) ? redirect(base_url("error404")) : "");?>
<?php if (!empty($this->efs_lib->is_can($permission, "view"))) { /* Can View */?>
    <!-- ============================================================== -->
    <!-- Start Page Content -->
    <!-- ============================================================== -->
    <style>
        .bootstrap-switch-container {
            /* กำหนดความยาวคงให้ สถานะ แก้ปัญหาย่อขยายตอน*/
            width: 90px !important;
        }
    </style>
    <!-- This page plugin CSS -->
    <link href="<?php echo base_url(); ?>assets/libs/datatables.net-bs4/css/dataTables.bootstrap4.css" rel="stylesheet">
    <link rel="stylesheet" type="text/css" href="<?php echo base_url(); ?>assets/libs/bootstrap-switch/dist/css/bootstrap3/bootstrap-switch.min.css">
    <div class="row">
        <div class="col-12">
            <div class="card">
                <div class="card-body">
                    <div class="text-right">
                        <?php if ($this->efs_lib->is_can($permission, "add")) {?>
                            <a id="listBtn" class="btn btn-info mb-2" data-toggle="modal" data-backdrop="static" data-keyboard="false" data-target="#listModal" href="javascript:void(0);"><span><i class="fa fa-address-book"></i> <?php echo $lang_module->lbl_contact; ?></span></a>
                            <a id="addBtn" class="btn btn-primary mb-2" data-toggle="modal" data-backdrop="static" data-keyboard="false" data-target="#addModal" href="javascript:void(0);"><span><i class="fa fa-plus"></i> <?php echo $lang_module->lbl_add; ?></span></a>
                        <?php }?>
                    </div>
                    <div class="table-responsive m-t-40">
                        <table id="mTable" class="table table-bordered table-striped table-hover datatable" style="width:100%">
                            <thead>
                                <tr>
                                    <th><?php echo $lang_module->lbl_order; ?></th>
                                    <th><?php echo $lang_module->lbl_contact; ?></th>
                                    <th><?php echo $lang_module->lbl_email; ?></th>
                                    <th><?php echo $lang_module->lbl_tel; ?></th>
                                    <th class="is-small text-center"><i class="fa fa-eye"></i></th>
                                    <?php if ($this->efs_lib->is_can($permission, "edit")) {?>
                                        <th class="no-sort text-center"><i class="fas fa-edit"></i></th>
                                    <?php }?>
                                    <?php if ($this->efs_lib->is_can($permission, "delete")) {?>
                                        <th class="no-sort text-center"><i class="fas fa-trash-alt"></i></th>
                                    <?php }?>
                                </tr>
                            </thead>
                            <tbody></tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <?php if ($this->efs_lib->is_can($permission, "add")) {?>
        <div class="modal fade" id="listModal" tabindex="-1" role="dialog">
            <form id="listForm" method="post" action="<?php echo base_url('mas/supplier/address_book_add'); ?>">
                <input type="hidden" name="supplier_id" value="<?php echo $this->uri->segment(4); ?>">
                <div class="modal-dialog modal-lg" role="document">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h4 class="modal-title"><?php echo $lang_module->lbl_add; ?></h4>
                            <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                        </div>
                        <div class="modal-body">
                        <table id="listTable" class="table table-bordered table-striped table-hover">
                        <thead>
                        <tr>
                        <th class="text-center"><input type="checkbox" name="checkall" id="checkall"></th>
                        <th> <?php echo $lang_module->lbl_contact; ?> </th>
                        <th> <?php echo $lang_module->lbl_email; ?> </th>
                        <th> <?php echo $lang_module->lbl_tel; ?> </th>
                        </tr>
                        </thead>
                        <tbody id="selectTable"></tbody>
                        </table>
                        </div>
                        <div class="modal-footer">
                            <button type="reset" class="btn btn-sm btn-default" data-dismiss="modal"><?php echo $lang_module->lbl_cancel; ?></button>
                            <button type="submit" class="btn btn-sm btn-primary"><?php echo $lang_module->lbl_save; ?></button>
                        </div>
                    </div>
                </div>
            </form>
        </div>
        <!-- /.modal -->

        <div class="modal fade" id="addModal" tabindex="-1" role="dialog">
            <form id="addForm" method="post" action="<?php echo base_url('mas/supplier/address_book_add'); ?>" novalidate>
                <input type="hidden" name="supplier_id" value="<?php echo $this->uri->segment(4); ?>">
                <div class="modal-dialog" role="document">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h4 class="modal-title"><?php echo $lang_module->lbl_add; ?></h4>
                            <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                        </div>
                        <div class="modal-body">
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_contact; ?><span class="text-danger">*</span></label>
                                <div class="controls">
                                    <input type="text" class="form-control validated" name="name" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>" autocomplete="off">
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_email; ?><span class="text-danger">*</span></label>
                                <div class="controls">
                                    <input type="email" class="form-control validated" name="email" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>">
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_tel; ?><span class="text-danger">*</span></label>
                                <div class="controls">
                                    <input type="text" class="form-control validated" name="tel" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>">
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_description; ?></label>
                                <div class="controls">
                                    <textarea name="description" class="form-control" rows="3" placeholder=""></textarea>
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
        <!-- /.modal -->
    <?php }?>

    <?php if ($this->efs_lib->is_can($permission, "edit")) {?>
        <div class="modal fade" id="editModal" tabindex="-1" role="dialog">
            <form id="editForm" method="post" action="<?php echo base_url('mas/supplier/address_book_edit'); ?>" novalidate>
                <input type="hidden" name="id">
                <div class="modal-dialog" role="document">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h4 class="modal-title" id="addModalLabel"><?php echo $lang_module->lbl_edit; ?></h4>
                            <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                        </div>
                        <div class="modal-body">
                           <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_contact; ?><span class="text-danger">*</span></label>
                                <div class="controls">
                                    <input type="text" class="form-control validated" name="name" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>" autocomplete="off">
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_email; ?><span class="text-danger">*</span></label>
                                <div class="controls">
                                    <input type="email" class="form-control validated" name="email" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>">
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_tel; ?><span class="text-danger">*</span></label>
                                <div class="controls">
                                    <input type="text" class="form-control validated" name="tel" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>">
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_description; ?></label>
                                <div class="controls">
                                    <textarea name="description" class="form-control" rows="3" placeholder=""></textarea>
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
        <!-- /.modal -->
    <?php }?>


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

    <script type="text/javascript">
        var provTable;
        ! function(window, document, $) {
            "use strict";
            provTable = $('#mTable').DataTable({
                "autoWidth": false,
                "pageLength": <?php echo $this->session->userdata('user_profile')->cng_per_page; ?>,
                "processing": true,
                "bSort": false,
                "stateSave": true,
                "autoWidth": false,
                "language": {
                    "url": "<?php echo base_url() ?>dist/js/pages/datatable/<?php echo $this->session->userdata('user_profile')->cng_lang; ?>.json",
                    "headers": {
                        "Access-Control-Allow-Origin": "*"
                    },
                    searchPlaceholder: "<?php echo $lang_sys->lbl_search; ?>",
                },
                "ajax": "<?php echo base_url('mas/supplier/address_book_ajax_list/' . $this->uri->segment(4)); ?>",
                "deferRender": true,
                "aLengthMenu": [
                    [10, 25, 50, 100],
                    [10, 25, 50, 100]
                ],
                "columns": [{
                        "data": {
                            "id": "id"
                        },
                        render: function(data, type, row, meta) {
                            return '<div class="text-center">' + (meta.row + 1) + '</div>';
                        }
                    },
                    {
                        "data": "name"
                    },
                    {
                        "data": "email"
                    },
                    {
                        "data": "tel"
                    },
                    {
                        "data": {
                            "id": "id",
                            "is_active": "is_active",
                            "created_username": "created_username"
                        },
                        render: function(data, type) {
                            if (data.created_username == 'system') {
                                return '<input data-id="' + data.id + '" data-active="' + data.is_active + '" type="checkbox" class="switch" <?php echo ($this->efs_lib->is_can($permission, "edit") ? '' : 'disabled'); ?> ' + (data.is_active == 1 ? 'checked' : '') + ' data-size="mini" data-on-color="success" data-off-color="warning" data-on-text="Yes" data-off-text="No"  disabled />';
                            } else {
                                return '<input data-id="' + data.id + '" data-active="' + data.is_active + '" type="checkbox" class="switch" <?php echo ($this->efs_lib->is_can($permission, "edit") ? '' : 'disabled'); ?> ' + (data.is_active == 1 ? 'checked' : '') + ' data-size="mini" data-on-color="success" data-off-color="warning" data-on-text="Yes" data-off-text="No"/>';
                            }
                        }
                    },
                    <?php if ($this->efs_lib->is_can($permission, "edit")) {?> {
                            "data": {
                                "id": "id"
                            },
                            render: function(data, type) {
                                return '<button type="button" class="btn btn-warning btn-xs" id="editBtn" data-id="' + data.id + '" data-toggle="modal" data-backdrop="static" data-keyboard="false" data-target="#editModal"><i class="fas fa-pencil-alt"></i></button>';
                            }
                        },
                    <?php }?>
                    <?php if ($this->efs_lib->is_can($permission, "delete")) {?> {
                            "data": {
                                "id": "id",
                                "created_username": "created_username"
                            },
                            render: function(data, type) {
                                if (data.created_username == 'system') {
                                    return '<button type="button" class="btn btn-default btn-xs" data-toggle="tooltip" title="<?php echo $lang_sys->msg_not_delete; ?>" disabled><i class="fas fa-trash-alt"></i></button>';
                                } else {
                                    return '<button type="button" class="btn btn-danger btn-xs confirmDelete" data-href="<?php echo base_url('mas/supplier/address_book_delete/'); ?>' + data.id + '"><i class="fas fa-trash-alt"></i></button>';
                                }
                            }
                        },
                    <?php }?>
                ],
                "columnDefs": [
                    {
                        orderable: false,
                        targets: '_all'
                    },
                    {
                        "width": "5px",
                        "targets": 0
                    },
                    {
                        "width": "10px",
                        "targets": 'is-small'
                    },
                    {
                        "width": "5px",
                        "targets": 'no-sort'
                    }
                ],
                "fnDrawCallback": function() {
                    $(".switch[type='checkbox']").bootstrapSwitch();
                    $('.switch[type="checkbox"]').on('switchChange.bootstrapSwitch', function(event, state) {
                        $(location).attr('href', '<?php echo base_url() ?>mas/supplier/address_book_change_active/' + $(this).data('id') + '.' + $(this).data('active'));
                    });
                },
            });

            function reloadTable() {
                setTimeout(provTable.ajax.reload, 100);
            }

        }(window, document, jQuery);
        $(document).ready(function() {
            <?php if ($this->efs_lib->is_can($permission, "add")) {?>
                $('#listModal').on('click', '#checkall', function() {
                if (this.checked == true) {
                    $('input:checkbox.selectItem').not(this).prop('checked', this.checked);
                } else {
                    $('input:checkbox.selectItem').not(this).prop('checked', false);
                }
            });

                $('#listBtn').on('click', function() {
                    $.post("<?php echo base_url('mas/supplier/table_select_item'); ?>", {},
                        function(data) {
                            $('#selectTable').html(data['table']);
                            $('#listTable').DataTable();
                            $("#listTable").dataTable().fnDestroy()
                            $("#listTable").dataTable({
                                "pageLength": <?php echo $this->session->userdata('user_profile')->cng_per_page; ?>,
                                "processing": true,
                                "bSort": false,
                                "autoWidth": false,
                                "language": {
                                    "url": "<?php echo base_url() ?>dist/js/pages/datatable/<?php echo $this->session->userdata('user_profile')->cng_lang; ?>.json",
                                    "headers": {
                                        "Access-Control-Allow-Origin": "*"
                                    },
                                    searchPlaceholder: "<?php echo $lang_sys->lbl_search; ?>",
                                }
                            });
                        }, "json");
                });

                $('#addBtn').click(function() {
                    $('#addForm').trigger("reset");
                    var $form = $('#addForm');
                    $form.find('.error,.valid').css('border-color', '').removeClass('error').removeClass('valid');
                    $form.find('.form-error').remove();
                    $form.find('.help-block').html('');
                    $form.find('[name="sort"]').val(provTable.data().count() + 1);
                });
            <?php }?>

            <?php if ($this->efs_lib->is_can($permission, "edit")) {?>
                $('#mTable tbody').on('click', 'tr td #editBtn', function() {
                    var $form = $('#editForm');
                    $form.find('.error,.valid').css('border-color', '').removeClass('error').removeClass('valid');
                    $form.find('.form-error').remove();
                    $form.find('.help-block').html('');
                    $.post("<?php echo base_url('mas/supplier/address_book_ajax_data'); ?>", {
                            id: $(this).data('id')
                        },
                        function(data) {
                            $form.find('[name="id"]').val(data['id']);
                            $form.find('[name="name"]').val(data['name']);
                            $form.find('[name="email"]').val(data['email']);
                            $form.find('[name="tel"]').val(data['tel']);
                            $form.find('[name="description"]').val(data['description']);
                        }, "json");
                });
            <?php }?>

        });
        $(function() {
            <?php if ($this->session->flashdata('msg')) {?>
                toastr.<?php echo $this->session->flashdata('type') ?>('<?php echo $this->session->flashdata('type') ?>', '<?php echo $this->session->flashdata('msg') ?>', "top-right", "#ff6849", "5000");
            <?php }?>

        });
    </script>
<?php
} /* Can View */?>