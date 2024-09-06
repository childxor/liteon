<?php /* var_dump($permission); */ ?>
<?php (empty($permission) ? redirect(base_url("error404")) : ""); ?>
<?php if (!empty($this->efs_lib->is_can($permission, "view"))) { /* Can View */ ?>
    <!-- ============================================================== -->
    <!-- Start Page Content -->
    <!-- ============================================================== -->
    <style>
        .modal-dialog.modal-lg {
            max-width: 100%;
            width: auto !important;
            display: inline-block;
        }

        .bootstrap-switch-container {
            /* กำหนดความยาวคงให้ <?php echo $lang_module->lbl_status; ?> แก้ปัญหาย่อขยายตอน*/
            width: 90px !important;
        }
    </style>
    <!-- This page plugin CSS -->
    <link href="<?php echo base_url(); ?>assets/libs/datatables.net-bs4/css/dataTables.bootstrap4.css" rel="stylesheet">
    <link rel="stylesheet" type="text/css" href="<?php echo base_url(); ?>assets/libs/bootstrap-switch/dist/css/bootstrap3/bootstrap-switch.min.css">
    <link rel="stylesheet" href="<?php echo base_url(); ?>assets/libs/signature-pad/css/signature-pad.css">
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
                                    <th><?php echo $lang_module->lbl_fullname; ?></th>
                                    <th><?php echo $lang_module->lbl_department; ?></th>
                                    <th><?php echo $lang_module->lbl_description; ?></th>
                                    <th class="is-small text-center"><i class="fa fa-eye"></i></th>
                                    <th class="no-sort text-center"><i class="fas fa-search"></i></th>
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
            <form id="addForm" method="post" action="<?php echo base_url('mas/signature/add'); ?>" novalidate>
                <div class="modal-dialog" role="document">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h4 class="modal-title" id="addModalLabel"><?php echo $lang_module->lbl_add; ?></h4>
                            <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                        </div>
                        <div class="modal-body">
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_fullname; ?><span class="text-danger">*</span></label>
                                <div class="controls">
                                    <select class="validated" id="add_user_id" name="user_id" style="width: 100%" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>">
                                        <option value=""></option>
                                        <?php
                                        $sql = "SELECT DISTINCT id, first_name, last_name FROM vw_user ";
                                        foreach ($this->db->query($sql)->result() as $row) {
                                            echo '<option value="' . $row->id . '">' . $row->first_name . ' ' . $row->last_name . '</option>';
                                        }
                                        ?>
                                    </select>
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_signature; ?><span class="text-danger">*</span></label>
                                <div class="controls">
                                    <div id="add-signature" class="signature-pad">
                                        <div class="signature-pad--body">
                                            <canvas id="add-signature-pad"></canvas>
                                        </div>
                                        <div class="signature-pad--footer">
                                            <div class="description">Sign above</div>

                                            <div class="signature-pad--actions">
                                                <div class="col-12">
                                                    <button type="button" class="button clear" data-action="clear">Clear</button>
                                                    <button type="button" class="button" data-action="undo">Undo</button>
                                                    <button type="button" class="button" data-action="ok">Ok</button>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                    <input type="hide" id="add-sign" name="sign" class="validated" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>">
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_description; ?></label>
                                <div class="controls">
                                    <textarea name="description" class="form-control" rows="3" placeholder="<?php echo $lang_module->lbl_description_placeholder; ?>"></textarea>
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
            <form id="editForm" method="post" action="<?php echo base_url('mas/signature/edit'); ?>" novalidate>
                <input type="hidden" name="id">
                <div class="modal-dialog" role="document">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h4 class="modal-title" id="editModalLabel"><?php echo $lang_module->lbl_edit; ?></h4>
                            <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                        </div>
                        <div class="modal-body">
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_fullname; ?><span class="text-danger">*</span></label>
                                <div class="controls">
                                    <select class="validated" id="edit_user_id" name="user_id" style="width: 100%" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>">
                                        <option value=""></option>
                                        <?php
                                        $sql = "SELECT id, first_name, last_name FROM vw_user ";
                                        foreach ($this->db->query($sql)->result() as $row) {
                                            echo '<option value="' . $row->id . '">' . $row->first_name . ' ' . $row->last_name . '</option>';
                                        }
                                        ?>
                                    </select>
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_signature; ?><span class="text-danger">*</span></label>
                                <div class="controls">
                                    <div id="edit-signature" class="signature-pad">
                                        <div class="signature-pad--body">
                                            <canvas id="edit-signature-pad"></canvas>
                                        </div>
                                        <div class="signature-pad--footer">
                                            <div class="description">Sign above</div>

                                            <div class="signature-pad--actions">
                                                <div class="col-12">
                                                    <button type="button" class="button clear" data-action="clear">Clear</button>
                                                    <button type="button" class="button" data-action="undo">Undo</button>
                                                    <button type="button" class="button" data-action="ok">Ok</button>
                                                </div>
                                            </div>
                                        </div>
                                    </div>
                                    <input type="hidden" id="edit-sign" name="sign">
                                    <div class="controls">
                                       <div class="custom-file">
                                           <input type="file" class="custom-file-input" id="edit-signlink" name="signlink" accept="image/png, image/jpeg">
                                           <label class="custom-file-label" for="edit-sign">Choose file</label>
                                       </div>
                                    </div>
                                </div>
                                <!-- openfile dialog png jpg -->
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_description; ?></label>
                                <div class="controls">
                                    <textarea name="description" class="form-control" rows="3" placeholder="<?php echo $lang_module->lbl_description_placeholder; ?>"></textarea>
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

    <div id="signModal" class="modal" tabindex="-1" role="dialog" style="text-align: center;">
        <div class="modal-dialog" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h5 class="modal-title"><?php echo $lang_module->lbl_signature; ?></h5>
                </div>
                <div class="modal-body">
                    <img id="sign-image" src="#" style="width:100%" />
                </div>
                <div class="modal-footer">
                    <button id="reset" type="button" class="btn btn-xs btn-default" data-dismiss="modal"><?php echo $lang_module->lbl_cancel; ?></button>
                </div>
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

    <!-- Croppie JS -->
    <link href="<?php echo base_url(); ?>assets/extra-libs/croppie/croppie.css" rel="stylesheet">
    <script src="<?php echo base_url(); ?>assets/extra-libs/croppie/croppie.js"></script>

    <!-- Datatable Page JS -->
    <script src="<?php echo base_url(); ?>assets/libs/datatables/media/js/jquery.dataTables.min.js"></script>
    <script src="<?php echo base_url(); ?>dist/js/pages/datatable/custom-datatable.js"></script>
    <script src="<?php echo base_url(); ?>assets/libs/datatables/media/js/dataTables.rowReorder.min.js"></script>

    <!-- Signature Pad JS -->
    <script src="<?php echo base_url(); ?>assets/libs/signature-pad/js/signature_pad.umd.js"></script>
    <script src="<?php echo base_url(); ?>assets/libs/signature-pad/js/app.js"></script>
    <script type="text/javascript">
        var provTable;
        ! function(window, document, $) {
            "use strict";
            provTable = $('#mTable').DataTable({
                "autoWidth": false,
                "processing": true,
                "pageLength": <?php echo $this->session->userdata('user_profile')->cng_per_page; ?>,
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
                "ajax": "<?php echo base_url('mas/signature/ajax_list'); ?>",
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
                        "data": {
                            "first_name": "first_name",
                            "last_name": "last_name"
                        },
                        render: function(data, type) {
                            return data.first_name + ' ' + data.last_name;
                        }
                    },
                    {
                        "data": {
                            "department_name": "department_name",
                            "sub_department_name": "sub_department_name"
                        },
                        render: function(data, type) {
                            return data.department_name + ' (' + data.sub_department_name + ')';
                        }
                    },
                    {
                        "data": "description"
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
                            return '<button type="button" id="signBtn" class="btn btn-info btn-xs" data-id="' + data.id + '" data-toggle="modal" data-backdrop="static" data-keyboard="false" data-target="#signModal"><i class="fas fa-search"></i></button>';
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
                                    return '<button type="button" class="btn btn-danger btn-xs confirmDelete" data-href="<?php echo base_url('mas/signature/delete/'); ?>' + data.id + '"><i class="fas fa-trash-alt"></i></button>';
                                }
                            }
                        },
                    <?php } ?>
                ],
                "columnDefs": [{
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
                        $(location).attr('href', '<?php echo base_url() ?>mas/signature/change_active/' + $(this).data('id') + '.' + $(this).data('active'));
                    });
                },
            });

            function reloadTable() {
                setTimeout(provTable.ajax.reload, 100);
            }
        }(window, document, jQuery);
        $(document).ready(function() {

            /* Set Class .select2 */
            $('#add_user_id').select2({
                dropdownParent: $("#addModal"),
                placeholder: "",
                allowClear: true
            });
            $('#edit_user_id').select2({
                dropdownParent: $("#editModal"),
                placeholder: "",
                allowClear: true
            });
            <?php if ($this->efs_lib->is_can($permission, "add")) { ?>
                $('#addModal').on('shown.bs.modal', function(e) {
                    resizeCanvas_add();
                });
                $('#addBtn').click(function() {
                    $('#addForm').trigger("reset");
                    var $form = $('#addForm');
                    $form.find('.error,.valid').css('border-color', '').removeClass('error').removeClass('valid');
                    $form.find('.form-error').remove();
                    $form.find('.help-block').html('');
                });
            <?php } ?>

            $('#mTable tbody').on('click', 'tr td #signBtn', function() {
                $.post("<?php echo base_url('mas/signature/ajax_data'); ?>", {
                        id: $(this).data('id')
                    },
                    function(data) {
                        $('#sign-image').attr('src', data['sign']);
                    }, "json");
            });
            <?php if ($this->efs_lib->is_can($permission, "edit")) { ?>
                $('#editModal').on('shown.bs.modal', function(e) {
                    resizeCanvas_edit();
                });
                $('#mTable tbody').on('click', 'tr td #editBtn', function() {
                    var $form = $('#editForm');
                    $form.find('.error,.valid').css('border-color', '').removeClass('error').removeClass('valid');
                    $form.find('.form-error').remove();
                    $form.find('.help-block').html('');
                    $.post("<?php echo base_url('mas/signature/ajax_data'); ?>", {
                            id: $(this).data('id')
                        },
                        function(data) {
                            $form.find('[name="id"]').val(data['id']);
                            $form.find('[name="user_id"]').val(data['user_id']).trigger('change');
                            $form.find('[name="description"]').val(data['description']);
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