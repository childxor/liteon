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
<link rel="stylesheet" type="text/css"
    href="<?php echo base_url(); ?>assets/libs/bootstrap-switch/dist/css/bootstrap3/bootstrap-switch.min.css">
<div class="row">
    <div class="col-12">
        <div class="card">
            <div class="card-body">
                <div class="text-right">
                    <?php if ($this->efs_lib->is_can($permission, "add")) { ?>
                    <a id="addBtn" class="btn btn-primary mb-2" data-toggle="modal" data-backdrop="static"
                        data-keyboard="false" data-target="#addModal" href="javascript:void(0);"><span><i
                                class="fa fa-plus"></i> <?php echo $lang_module->lbl_add; ?></span></a>
                    <?php } ?>
                </div>
                <div class="table-responsive m-t-40">
                    <table id="mTable" class="table table-bordered table-striped table-hover datatable"
                        style="width:100%">
                        <thead>
                            <tr>
                                <th><?php echo $lang_module->lbl_order; ?></th>
                                <th><?php echo $lang_module->lbl_supplier; ?></th>
                                <th><?php echo $lang_module->lbl_contact; ?></th>
                                <th><?php echo $lang_module->lbl_tel; ?></th>
                                <th class="is-small text-center"><i class="fa fa-eye"></i></th>
                                <th class="no-sort text-center"><i class="fas fa-search"></th>
                                <?php if ($this->efs_lib->is_can($permission, "edit")) { ?>
                                <th class="no-sort text-center"><i class="fas fa-address-book"></i></th>
                                <th class="no-sort text-center"><i class="fas fa-money-bill-alt"></i></th>
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
    <form id="addForm" method="post" action="<?php echo base_url('mas/supplier/add'); ?>" novalidate>
        <div class="modal-dialog modal-lg" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h4 class="modal-title" id="addModalLabel"><?php echo $lang_module->lbl_add; ?></h4>
                    <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span
                            aria-hidden="true">&times;</span></button>
                </div>
                <div class="modal-body">
                    <div class="form-group">
                        <label class="control-label"><?php echo $lang_module->lbl_supplier; ?><span
                                class="text-danger">*</span></label>
                        <div class="controls">
                            <input type="text" class="form-control validated" name="name" required
                                data-validation-required-message="<?php echo $lang_sys->msg_require; ?>"
                                autocomplete="off">
                        </div>
                    </div>
                    <div class="form-group">
                        <label class="control-label"><?php echo $lang_module->lbl_address; ?><span
                                class="text-danger">*</span></label>
                        <div class="controls">
                            <textarea name="address" class="form-control validated" rows="3"
                                placeholder="<?php echo $lang_module->lbl_address; ?>" required
                                data-validation-required-message="<?php echo $lang_sys->msg_require; ?>"></textarea>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-6">
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_tel; ?><span
                                        class="text-danger">*</span></label>
                                <div class="controls">
                                    <input type="text" class="form-control validated" name="tel" required
                                        data-validation-required-message="<?php echo $lang_sys->msg_require; ?>"
                                        autocomplete="off">
                                </div>
                            </div>
                        </div>
                        <div class="col-6">
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_fax; ?><span
                                        class="text-danger">*</span></label>
                                <div class="controls">
                                    <input type="text" class="form-control validated" name="fax" required
                                        data-validation-required-message="<?php echo $lang_sys->msg_require; ?>"
                                        autocomplete="off">
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="form-group">
                        <label class="control-label"><?php echo $lang_module->lbl_contact; ?><span
                                class="text-danger">*</span></label>
                        <div class="controls">
                            <input type="text" class="form-control validated" name="contact" required
                                data-validation-required-message="<?php echo $lang_sys->msg_require; ?>"
                                autocomplete="off">
                        </div>
                    </div>
                    <div class="form-group">
                        <label class="control-label"><?php echo $lang_module->lbl_description; ?></label>
                        <div class="controls">
                            <textarea name="description" class="form-control" rows="2" placeholder=""></textarea>
                        </div>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="reset" class="btn btn-sm btn-default"
                        data-dismiss="modal"><?php echo $lang_module->lbl_cancel; ?></button>
                    <button type="submit" class="btn btn-sm btn-primary"><?php echo $lang_module->lbl_save; ?></button>
                </div>
            </div>
        </div>
    </form>
</div>
<!-- /.modal -->
<?php } ?>

<div class="modal fade" id="viewModal" tabindex="-1" role="dialog" aria-labelledby="editModalLabel">
    <div class="modal-dialog" role="document">
        <div class="modal-content">
            <div class="modal-body">
                <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span
                        aria-hidden="true">&times;</span></button>
                <div class="card-body">
                    <small class="text-muted"><?php echo $lang_module->lbl_supplier; ?></small>
                    <h6 id="name"></h6>
                    <small class="text-muted pt-4 db"><?php echo $lang_module->lbl_address; ?></small>
                    <h6 id="address"></h6>
                    <small class="text-muted pt-4 db"><?php echo $lang_module->lbl_tel; ?></small>
                    <h6 id="tel"></h6>
                    <small class="text-muted pt-4 db"><?php echo $lang_module->lbl_fax; ?></small>
                    <h6 id="fax"></h6>
                    <small class="text-muted pt-4 db"><?php echo $lang_module->lbl_contact; ?></small>
                    <h6 id="contact"></h6>
                    <small class="text-muted pt-4 db"><?php echo $lang_module->lbl_description; ?></small>
                    <p id="description" class="mt-2"></p>
                </div>
            </div>
        </div>
    </div>
</div>
<!-- /.modal -->

<?php if ($this->efs_lib->is_can($permission, "edit")) { ?>
<div class="modal fade" id="editModal" tabindex="-1" role="dialog" aria-labelledby="editModalLabel">
    <form id="editForm" method="post" action="<?php echo base_url('mas/supplier/edit'); ?>" novalidate>
        <div class="modal-dialog modal-lg" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h4 class="modal-title" id="addModalLabel"><?php echo $lang_module->lbl_edit; ?></h4>
                    <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span
                            aria-hidden="true">&times;</span></button>
                </div>
                <input type="hidden" name="id">
                <div class="modal-body">
                    <div class="form-group">
                        <label class="control-label"><?php echo $lang_module->lbl_supplier; ?><span
                                class="text-danger">*</span></label>
                        <div class="controls">
                            <input type="text" class="form-control validated" name="name" required
                                data-validation-required-message="<?php echo $lang_sys->msg_require; ?>"
                                autocomplete="off">
                        </div>
                    </div>
                    <div class="form-group">
                        <label class="control-label"><?php echo $lang_module->lbl_address; ?><span
                                class="text-danger">*</span></label>
                        <div class="controls">
                            <textarea name="address" class="form-control validated" rows="3"
                                placeholder="<?php echo $lang_module->lbl_address; ?>" required
                                data-validation-required-message="<?php echo $lang_sys->msg_require; ?>"></textarea>
                        </div>
                    </div>
                    <div class="row">
                        <div class="col-6">
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_tel; ?><span
                                        class="text-danger">*</span></label>
                                <div class="controls">
                                    <input type="text" class="form-control validated" name="tel" required
                                        data-validation-required-message="<?php echo $lang_sys->msg_require; ?>"
                                        autocomplete="off">
                                </div>
                            </div>
                        </div>
                        <div class="col-6">
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_fax; ?><span
                                        class="text-danger">*</span></label>
                                <div class="controls">
                                    <input type="text" class="form-control validated" name="fax" required
                                        data-validation-required-message="<?php echo $lang_sys->msg_require; ?>"
                                        autocomplete="off">
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="form-group">
                        <label class="control-label"><?php echo $lang_module->lbl_contact; ?><span
                                class="text-danger">*</span></label>
                        <div class="controls">
                            <input type="text" class="form-control validated" name="contact" required
                                data-validation-required-message="<?php echo $lang_sys->msg_require; ?>"
                                autocomplete="off">
                        </div>
                    </div>
                    <div class="form-group">
                        <label class="control-label"><?php echo $lang_module->lbl_description; ?></label>
                        <div class="controls">
                            <textarea name="description" class="form-control" rows="2" placeholder=""></textarea>
                        </div>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="reset" class="btn btn-sm btn-default"
                        data-dismiss="modal"><?php echo $lang_module->lbl_cancel; ?></button>
                    <button type="submit" class="btn btn-sm btn-primary"><?php echo $lang_module->lbl_save; ?></button>
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
        "ajax": "<?php echo base_url('mas/supplier/ajax_list'); ?>",
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
                    "code": "code",
                    "name": "name"
                },
                render: function(data, type, row, meta) {
                    return data.name + ' <small class="text-muted">(' + data.code + ')</small>';
                }
            },
            {
                "data": "contact"
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
                        return '<input data-id="' + data.id + '" data-active="' + data.is_active +
                            '" type="checkbox" class="switch" <?php echo ($this->efs_lib->is_can($permission, "edit") ? '' : 'disabled'); ?> ' +
                            (data.is_active == 1 ? 'checked' : '') +
                            ' data-size="mini" data-on-color="success" data-off-color="warning" data-on-text="Yes" data-off-text="No"  disabled />';
                    } else {
                        return '<input data-id="' + data.id + '" data-active="' + data.is_active +
                            '" type="checkbox" class="switch" <?php echo ($this->efs_lib->is_can($permission, "edit") ? '' : 'disabled'); ?> ' +
                            (data.is_active == 1 ? 'checked' : '') +
                            ' data-size="mini" data-on-color="success" data-off-color="warning" data-on-text="Yes" data-off-text="No"/>';
                    }
                }
            },
            {
                "data": {
                    "id": "id"
                },
                render: function(data, type) {
                    return '<button type="button" class="btn btn-success btn-xs" id="viewBtn" data-id="' +
                        data.id +
                        '" data-toggle="modal" data-backdrop="static" data-keyboard="false" data-target="#viewModal"><i class="fas fa-search"></i></button>';
                }
            },
            <?php if ($this->efs_lib->is_can($permission, "edit")) { ?> {
                "data": {
                    "id": "id"
                },
                render: function(data, type) {
                    return '<a href="<?php echo base_url('mas/supplier/address_book/'); ?>' + data.id +
                        '" class="btn btn-info btn-xs text-white"><i class="fas fa-address-book"></i></a>';
                }
            }, {
                "data": {
                    "id": "id"
                },
                render: function(data, type) {
                    return '<a href="<?php echo base_url('mas/supplier/money_book/'); ?>' + data.id +
                        '" class="btn btn-primary btn-xs text-white"><i class="fas fa-money-bill-alt"></i></a>';
                }
            }, {
                "data": {
                    "id": "id"
                },
                render: function(data, type) {
                    return '<button type="button" class="btn btn-warning btn-xs" id="editBtn" data-id="' +
                        data.id +
                        '" data-toggle="modal" data-backdrop="static" data-keyboard="false" data-target="#editModal"><i class="fas fa-pencil-alt"></i></button>';
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
                        return '<button type="button" class="btn btn-danger btn-xs confirmDelete" data-href="<?php echo base_url('mas/supplier/delete/'); ?>' +
                            data.id + '"><i class="fas fa-trash-alt"></i></button>';
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
                $(location).attr('href', '<?php echo base_url() ?>mas/supplier/change_active/' + $(
                    this).data('id') + '.' + $(this).data('active'));
            });
        },
    });

    function reloadTable() {
        setTimeout(provTable.ajax.reload, 100);
    }
}(window, document, jQuery);
$(document).ready(function() {
    <?php if ($this->efs_lib->is_can($permission, "add")) { ?>
    $('#addBtn').click(function() {
        $('#addForm').trigger("reset");
        var $form = $('#addForm');
        $form.find('.error,.valid').css('border-color', '').removeClass('error').removeClass('valid');
        $form.find('.form-error').remove();
        $form.find('.help-block').html('');
    });
    <?php } ?>

    $('#mTable tbody').on('click', 'tr td #viewBtn', function() {
        $.post("<?php echo base_url('mas/supplier/ajax_data'); ?>", {
                id: $(this).data('id')
            },
            function(data) {
                $('#name').html(data['name']);
                $('#address').html(data['address']);
                $('#contact').html(data['contact']);
                $('#tel').html(data['tel']);
                $('#fax').html(data['fax']);
                $('#description').html(data['description']);
            }, "json");
    });
    <?php if ($this->efs_lib->is_can($permission, "edit")) { ?>
    $('#mTable tbody').on('click', 'tr td #editBtn', function() {
        var $form = $('#editForm');
        $form.find('.error,.valid').css('border-color', '').removeClass('error').removeClass('valid');
        $form.find('.form-error').remove();
        $form.find('.help-block').html('');
        $.post("<?php echo base_url('mas/supplier/ajax_data'); ?>", {
                id: $(this).data('id')
            },
            function(data) {
                $form.find('[name="id"]').val(data['id']);
                $form.find('[name="name"]').val(data['name']);
                $form.find('[name="address"]').val(data['address']);
                $form.find('[name="contact"]').val(data['contact']);
                $form.find('[name="tel"]').val(data['tel']);
                $form.find('[name="fax"]').val(data['fax']);
                $form.find('[name="description"]').val(data['description']);
            }, "json");
    });
    <?php } ?>

});
$(function() {
    <?php if ($this->session->flashdata('msg')) { ?>
    toastr.<?php echo $this->session->flashdata('type') ?>('<?php echo $this->session->flashdata('type') ?>',
        '<?php echo $this->session->flashdata('msg') ?>', "top-right", "#ff6849", "5000");
    <?php } ?>

});
</script>
<?php
} /* Can View */ ?>