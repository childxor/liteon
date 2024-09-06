<?php /* var_dump($permission); */ ?>
<?php (empty($permission) ? redirect(base_url("error404")) : ""); ?>
<?php if (!empty($this->efs_lib->is_can($permission, "view"))) { /* Can View */ ?>
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
                        <?php if ($this->efs_lib->is_can($permission, "add")) { ?>
                            <a id="addBtn" class="btn btn-primary mb-2" data-toggle="modal" data-backdrop="static" data-keyboard="false" data-target="#addModal" href="javascript:void(0);"><span><i class="fa fa-plus"></i> <?php echo $lang_module->lbl_add; ?></span></a>
                        <?php } ?>
                    </div>
                    <div class="table-responsive m-t-40">
                        <table id="mTable" class="table table-bordered table-striped table-hover datatable" style="width:100%">
                            <thead>
                                <tr>
                                    <th><?php echo $lang_module->lbl_order; ?></th>
                                    <th class="is-small"><?php echo $lang_module->lbl_icon; ?></th>
                                    <th><?php echo $lang_module->lbl_module_name; ?></th>
                                    <th class="is-small text-center"><i class="fa fa-eye"></i></th>
                                    <th class="no-sort text-center"><i class="fas fa-bars"></i></th>
                                    <?php if ($this->efs_lib->is_can($permission, "edit")) { ?>
                                        <th class="no-sort text-center"><i class="fas fa-edit"></i></th>
                                    <?php } ?>
                                    <?php if ($this->efs_lib->is_can($permission, "delete")) { ?>
                                        <th class="no-sort text-center"><i class="fas fa-trash-alt"></i></th>
                                    <?php } ?>
                                </tr>
                            </thead>
                            <tbody></tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <?php if ($this->efs_lib->is_can($permission, "add")) { ?>
        <div class="modal fade" id="addModal" tabindex="-1" role="dialog" aria-labelledby="addModalLabel">
            <form id="addForm" method="post" action="<?php echo base_url('sys/module/add'); ?>" novalidate>
                <div class="modal-dialog" role="document">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h4 class="modal-title" id="addModalLabel"><?php echo $lang_module->lbl_add; ?></h4>
                            <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                        </div>
                        <div class="modal-body">
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_module_name_th; ?><span class="text-danger">*</span></label>
                                <div class="controls">
                                    <input type="text" class="form-control validated" name="name_th" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>" autocomplete="off">
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_module_name_en; ?><span class="text-danger">*</span></label>
                                <div class="controls">
                                    <input type="text" class="form-control validated" name="name_en" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>" autocomplete="off">
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_module_name_jp; ?></label>
                                <div class="controls">
                                    <input type="text" class="form-control validated" name="name_jp" autocomplete="off">
                                </div>
                            </div>
                            <input type="hidden" name="module" value="-">
                            <input type="hidden" name="parent_module_id" value="0">
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_order; ?><span class="text-danger">*</span></label>
                                <div class="controls">
                                    <input type="number" min="0" max="99" class="form-control validated" name="sort" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>" data-validation-number-message="<?php echo $lang_sys->msg_number_only; ?>" data-validation-max-message="<?php echo $lang_sys->msg_max_99; ?>">
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_icon; ?></label>
                                <select id="create_icon" name="icon" style="width: 100%">
                                    <option value=""></option>
                                    <?php $this->load->view('main/layout/icon_ti'); ?>
                                </select>
                            </div>
                            <div class="form-group">
                                <label class="control-label">PERMISSION</label>
                                <div class="container">
                                    <div class="row">
                                        <?php
                                        $result = $this->db->query("SELECT * FROM sys_permission WHERE record_status ='N' ORDER BY sort ASC")->result();
                                        foreach ($result as $item) {
                                            if ($item->id == 1) {
                                        ?>
                                                <div class="form-check col-3">
                                                    <input type="checkbox" class="form-check-input" id="add_permission_<?php echo $item->id; ?>" name="permission[]" value="<?php echo $item->id; ?>" checked disabled>
                                                    <label class="form-check-label" for="add_permission_<?php echo $item->id; ?>"><?php echo strtoupper($item->name); ?></label>
                                                </div>
                                            <?php
                                            } else {
                                            ?>
                                                <div class="form-check col-3">
                                                    <input type="checkbox" class="form-check-input" id="add_permission_<?php echo $item->id; ?>" name="permission[]" value="<?php echo $item->id; ?>">
                                                    <label class="form-check-label" for="add_permission_<?php echo $item->id; ?>"><?php echo strtoupper($item->name); ?></label>
                                                </div>
                                        <?php
                                            }
                                        }
                                        ?>
                                    </div>
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
    <?php } ?>

    <?php if ($this->efs_lib->is_can($permission, "edit")) { ?>
        <div class="modal fade" id="editModal" tabindex="-1" role="dialog" aria-labelledby="editModalLabel">
            <form id="editForm" method="post" action="<?php echo base_url('sys/module/edit'); ?>" novalidate>
                <div class="modal-dialog" role="document">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h4 class="modal-title" id="addModalLabel"><?php echo $lang_module->lbl_edit; ?></h4>
                            <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                        </div>
                        <div class="modal-body">
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_module_name_th; ?><span class="text-danger">*</span></label>
                                <div class="controls">
                                    <input type="text" class="form-control validated" name="name_th" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>" autocomplete="off">
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_module_name_en; ?><span class="text-danger">*</span></label>
                                <div class="controls">
                                    <input type="text" class="form-control validated" name="name_en" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>" autocomplete="off">
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_module_name_jp; ?></label>
                                <div class="controls">
                                    <input type="text" class="form-control validated" name="name_jp" autocomplete="off">
                                </div>
                                <input type="hidden" name="id">
                                <input type="hidden" name="module" value="-">
                                <input type="hidden" name="parent_module_id" value="0">
                                <div class="form-group">
                                    <label class="control-label"><?php echo $lang_module->lbl_order; ?><span class="text-danger">*</span></label>
                                    <div class="controls">
                                        <input type="number" min="0" max="99" class="form-control validated" name="sort" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>" data-validation-number-message="<?php echo $lang_sys->msg_number_only; ?>" data-validation-max-message="<?php echo $lang_sys->msg_max_99; ?>">
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="control-label"><?php echo $lang_module->lbl_icon; ?></label>
                                    <select id="edit_icon" name="icon" style="width: 100%">
                                        <option value=""></option>
                                        <?php $this->load->view('main/layout/icon_ti'); ?>
                                    </select>
                                </div>
                                <div class="form-group">
                                    <label class="control-label">PERMISSION</label>
                                    <div class="container">
                                        <div class="row">
                                            <?php
                                            $result = $this->db->query("SELECT * FROM sys_permission WHERE record_status ='N' ORDER BY sort ASC")->result();
                                            foreach ($result as $item) {
                                                if ($item->id == 1) {
                                            ?>
                                                    <div class="form-check col-3">
                                                        <input type="checkbox" class="form-check-input" id="edit_permission_<?php echo $item->id; ?>" name="permission[]" value="<?php echo $item->id; ?>" checked disabled>
                                                        <label class="form-check-label" for="edit_permission_<?php echo $item->id; ?>"><?php echo strtoupper($item->name); ?></label>
                                                    </div>
                                                <?php
                                                } else {
                                                ?>
                                                    <div class="form-check col-3">
                                                        <input type="checkbox" class="form-check-input" id="edit_permission_<?php echo $item->id; ?>" name="permission[]" value="<?php echo $item->id; ?>">
                                                        <label class="form-check-label" for="edit_permission_<?php echo $item->id; ?>"><?php echo strtoupper($item->name); ?></label>
                                                    </div>
                                            <?php
                                                }
                                            }
                                            ?>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="modal-footer">
                                <button type="reset" class="btn btn-sm btn-default" data-dismiss="modal"><?php echo $lang_module->lbl_cancel; ?></button>
                                <button type="submit" class="btn btn-sm btn-primary"><?php echo $lang_module->lbl_save; ?></button>
                            </div>
                        </div>
                    </div>
                </div>
            </form>
        </div>

        <!-- /.modal -->
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
    <script src="<?php echo base_url(); ?>assets/libs/datatables/media/js/dataTables.rowReorder.min.js"></script>

    <script type="text/javascript">
        var provTable;
        ! function(window, document, $) {
            "use strict";
            provTable = $('#mTable').DataTable({
                "autoWidth": false,
                "rowReorder": true,
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
                "ajax": "<?php echo base_url('sys/module/ajax_list'); ?>",
                "deferRender": true,
                "aLengthMenu": [
                    [10, 25, 50, 100],
                    [10, 25, 50, 100]
                ],
                "columns": [{
                        "data": {
                            "id": "id",
                            "is_active": "is_active"
                        },
                        render: function(data, type, row, meta) {
                            return '<div class="text-center">' + (meta.row + 1) + '</div>';
                        }
                    },
                    {
                        "data": {
                            "icon": "icon"
                        },
                        render: function(data, type) {
                            return '<div class="text-center"><i class="text-center ' + data.icon + '" style="font-size: 20px;"></i></div>';
                        }
                    },
                    {
                        "data": {
                            "name_th": "name_th",
                            "name_th": "name_th",
                            "name_en": "name_jp"
                        },
                        render: function(data, type) {
                            return data.name_th + ' / ' + data.name_en + ' / ' + data.name_jp;
                        }
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
                    {
                        "data": {
                            "id": "id"
                        },
                        render: function(data, type) {
                            return '<a href="<?php echo base_url('sys/module/child/'); ?>' + data.id + '" class="btn btn-info btn-xs"><i class="fas fa-align-justify"></i></a>';
                        }
                    },
                    <?php if ($this->efs_lib->is_can($permission, "edit")) { ?> {
                            "data": {
                                "id": "id"
                            },
                            render: function(data, type) {
                                return '<button type="button" class="btn btn-warning btn-xs" id="editBtn" data-id="' + data.id + '" data-toggle="modal" data-backdrop="static" data-keyboard="false" data-target="#editModal"><i class="fas fa-pencil-alt"></i></button>';
                            }
                        },
                    <?php } ?>
                    <?php if ($this->efs_lib->is_can($permission, "delete")) { ?> {
                            "data": {
                                "id": "id",
                                "created_username": "created_username"
                            },
                            render: function(data, type) {
                                if (data.created_username == 'system') {
                                    return '<button type="button" class="btn btn-default btn-xs" data-toggle="tooltip" title="<?php echo $lang_sys->msg_not_delete; ?>" disabled><i class="fas fa-trash-alt"></i></button>';
                                } else {
                                    return '<button type="button" class="btn btn-danger btn-xs confirmDelete" data-href="<?php echo base_url('sys/module/delete/'); ?>' + data.id + '"><i class="fas fa-trash-alt"></i></button>';
                                }
                            }
                        },
                    <?php } ?>
                ],
                "columnDefs": [{
                        orderable: true,
                        className: 'reorder',
                        targets: 0
                    },
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
                        $(location).attr('href', '<?php echo base_url() ?>sys/module/change_active/' + $(this).data('id') + '.' + $(this).data('active'));
                    });
                },
            });
            provTable.on('row-reorder', function(e, diff, edit) {
                for (var i = 0, ien = diff.length; i < ien; i++) {
                    var rowData = provTable.row(diff[i].node).data();
                    $.post('<?php echo base_url('sys/module/sort'); ?>', {
                        newPos: diff[i].newPosition + 1,
                        id: rowData.id
                    }, function(data) {});
                }
                reloadTable();
            });

            function reloadTable() {
                setTimeout(provTable.ajax.reload, 100);
            }
        }(window, document, jQuery);
        $(document).ready(function() {
            <?php if ($this->efs_lib->is_can($permission, "add")) { ?>
                var create_icon = $('#create_icon').select2({
                    dropdownParent: $('#addModal'),
                    escapeMarkup: function(m) {
                        return m;
                    },
                    placeholder: "",
                    allowClear: true
                });
                $('#addBtn').click(function() {
                    $('#addForm').trigger("reset");
                    var $form = $('#addForm');
                    $form.find('.error,.valid').css('border-color', '').removeClass('error').removeClass('valid');
                    $form.find('.form-error').remove();
                    $form.find('.help-block').html('');
                    $form.find('[name="sort"]').val(provTable.data().count() + 1);
                });
            <?php } ?>

            <?php if ($this->efs_lib->is_can($permission, "edit")) { ?>
                var edit_icon = $('#edit_icon').select2({
                    dropdownParent: $('#editModal'),
                    escapeMarkup: function(m) {
                        return m;
                    },
                    placeholder: "",
                    allowClear: true
                });
                $('#mTable tbody').on('click', 'tr td #editBtn', function() {
                    var $form = $('#editForm');
                    $form.find('.error,.valid').css('border-color', '').removeClass('error').removeClass('valid');
                    $form.find('.form-error').remove();
                    $form.find('.help-block').html('');
                    $('#editForm').trigger("reset");
                    $.post("<?php echo base_url('sys/module/ajax_data'); ?>", {
                            id: $(this).data('id')
                        },
                        function(data) {
                            $form.find('[name="id"]').val(data['id']);
                            $form.find('[name="name_th"]').val(data['name_th']);
                            $form.find('[name="name_en"]').val(data['name_en']);
                            $form.find('[name="name_jp"]').val(data['name_jp']);
                            $form.find('[name="sort"]').val(data['sort']);
                            edit_icon.val(data['icon']).trigger('change');
                            $.each(data['permission'], function(index, val) {
                                $form.find('[id="edit_permission_' + val + '"]').prop('checked', true);
                            });
                        }, "json");
                });
            <?php } ?>

        });
        $(function() {
            <?php if ($this->session->flashdata('msg')) { ?>
                toastr.<?php echo $this->session->flashdata('type') ?>('<?php echo $this->session->flashdata('type') ?>', '<?php echo $this->session->flashdata('msg') ?>', "top-right", "#ff6849", "5000");
            <?php } ?>

        });
    </script>
<?php
} /* Can View */ ?>