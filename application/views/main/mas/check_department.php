<?php /* var_dump($permission); */ ?>
<?php (empty($permission) ? redirect(base_url("error404")) : ""); ?>
<?php if (!empty($this->efs_lib->is_can($permission, "view"))) { /* Can View */ ?>
<?php
    $can_check = array('ADVISOR', 'DIRECTOR', 'MANAGING DIRECTOR', 'GOLF MANAGER', 'ASSISTANT MANAGER', 'MT MANAGER', 'SUPERVISOR', 'Junior Assistant Manager', 'ENG-DESIGN-DIE MANAGER', 'QMS MANAGER', 'PC MANAGER', 'GENERAL MANAGER', 'LEADER');
    ?>
<!-- ============================================================== -->
<!-- Start Page Content -->
<!-- ============================================================== -->
<style>
.bootstrap-switch-container {
    /* กำหนดความยาวคงให้ สถานะ แก้ปัญหาย่อขยายตอน*/
    width: 90px !important;
}

/* The switch - the box around the slider */
.switch {
    /* Variables */
    --switch_width: 2em;
    --switch_height: 1em;
    --thumb_color: #e8e8e8;
    --track_color: #e8e8e8;
    --track_active_color: #888;
    --outline_color: #000;
    font-size: 17px;
    position: relative;
    display: inline-block;
    width: var(--switch_width);
    height: var(--switch_height);
}

/* Hide default HTML checkbox */
.switch input {
    opacity: 0;
    width: 0;
    height: 0;
}

/* The slider */
.slider {
    box-sizing: border-box;
    border: 2px solid var(--outline_color);
    position: absolute;
    cursor: pointer;
    top: 0;
    left: 0;
    right: 0;
    bottom: 0;
    background-color: var(--track_color);
    transition: .15s;
    border-radius: var(--switch_height);
}

.slider:before {
    box-sizing: border-box;
    position: absolute;
    content: "";
    height: var(--switch_height);
    width: var(--switch_height);
    border: 2px solid var(--outline_color);
    border-radius: 100%;
    left: -2px;
    bottom: -2px;
    background-color: var(--thumb_color);
    transform: translateY(-0.2em);
    box-shadow: 0 0.2em 0 var(--outline_color);
    transition: .15s;
}

input:checked+.slider {
    background-color: var(--track_active_color);
}

input:focus-visible+.slider {
    box-shadow: 0 0 0 2px var(--track_active_color);
}

/* Raise thumb when hovered */
input:hover+.slider:before {
    transform: translateY(-0.3em);
    box-shadow: 0 0.3em 0 var(--outline_color);
}

input:checked+.slider:before {
    transform: translateX(calc(var(--switch_width) - var(--switch_height))) translateY(-0.2em);
}

/* Raise thumb when hovered & checked */
input:hover:checked+.slider:before {
    transform: translateX(calc(var(--switch_width) - var(--switch_height))) translateY(-0.3em);
    box-shadow: 0 0.3em 0 var(--outline_color);
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

                    <label class="switch">
                        <input type="checkbox" id="is_must" checked>
                        <span class="slider"></span>
                    </label>
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
                                <th style="width: 100px;"><?php echo $lang_module->lbl_step; ?></th>
                                <th><?php echo $lang_module->lbl_checker; ?></th>
                                <th><?php echo $lang_module->lbl_description; ?></th>
                                <th class="is-small"><?php echo $lang_module->lbl_require; ?></th>
                                <th class="is-small text-center"><i class="fa fa-eye"></i></th>
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
<div class="modal fade" id="addModal" tabindex="-1" role="dialog">
    <form id="addForm" method="post" action="<?php echo base_url('mas/department/check_add'); ?>" novalidate>
        <input type="hidden" name="department_id" value="<?php echo $this->uri->segment(4); ?>">
        <div class="modal-dialog" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h4 class="modal-title"><?php echo $lang_module->lbl_add; ?></h4>
                    <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span
                            aria-hidden="true">&times;</span></button>
                </div>
                <div class="modal-body">
                    <div class="form-group">
                        <label class="control-label"><?php echo $lang_module->lbl_checker; ?><span
                                class="text-danger">*</span></label>
                        <div class="controls">
                            <select class="validated" id="add_user" name="user_id" style="width: 100%" required
                                data-validation-required-message="<?php echo $lang_sys->msg_require; ?>">
                                <option value=""></option>
                                <?php
                                        $sql = "SELECT sys_user.id, sys_user.first_name, sys_user.last_name, mas_department.name AS department_name, mas_position.name AS position_name "
                                            . "FROM sys_user INNER JOIN mas_position ON mas_position.id = sys_user.position_id "
                                            . "INNER JOIN mas_department ON sys_user.department_id = mas_department.id "
                                            . "WHERE sys_user.record_status ='N' AND sys_user.is_active='1' "
                                            . "ORDER BY mas_department.sort, mas_position.sort ASC ";
                                        foreach ($this->db->query($sql)->result() as $row) {
                                            echo '<option value="' . $row->id . '">' . $row->first_name . ' ' . $row->last_name . ' (' . $row->department_name . ' : ' . $row->position_name . ')</option>';
                                        }
                                        ?>
                            </select>
                        </div>
                    </div>
                    <div class="form-group">
                        <label class="control-label"><?php echo $lang_module->lbl_step; ?><span
                                class="text-danger">*</span></label>
                        <div class="controls">
                            <select class="validated" name="step" style="width: 100%" required
                                data-validation-required-message="<?php echo $lang_sys->msg_require; ?>">
                                <option value=""></option>
                                <option value="1">Check</option>
                                <option value="2">Re-Check</option>
                                <option value="3">Verify</option>
                            </select>
                        </div>
                    </div>
                    <div class="form-group">
                        <label class="control-label"><?php echo $lang_module->lbl_require; ?><span
                                class="text-danger">*</span></label>
                        <div class="controls">
                            <select class="validated" name="is_must" style="width: 100%" required
                                data-validation-required-message="<?php echo $lang_sys->msg_require; ?>">
                                <option value="1"><?php echo $lang_module->lbl_yes; ?></option>
                                <option value="0"><?php echo $lang_module->lbl_no; ?></option>
                            </select>
                        </div>
                    </div>
                    <div class="form-group">
                        <label class="control-label"><?php echo $lang_module->lbl_order; ?><span
                                class="text-danger">*</span></label>
                        <div class="controls">
                            <input type="number" min="0" max="99" class="form-control validated" name="sort" required
                                data-validation-required-message="<?php echo $lang_sys->msg_require; ?>"
                                data-validation-number-message="<?php echo $lang_sys->msg_number_only; ?>"
                                data-validation-max-message="<?php echo $lang_sys->msg_max_99; ?>">
                        </div>
                    </div>
                    <div class="form-group">
                        <label class="control-label"><?php echo $lang_module->lbl_description; ?><span
                                class="text-danger">*</span></label>
                        <div class="controls">
                            <textarea name="description" class="form-control validated" rows="3" placeholder="" required
                                data-validation-required-message="<?php echo $lang_sys->msg_require; ?>"></textarea>
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

<?php if ($this->efs_lib->is_can($permission, "edit")) { ?>
<div class="modal fade" id="editModal" tabindex="-1" role="dialog">
    <form id="editForm" method="post" action="<?php echo base_url('mas/department/check_edit'); ?>" novalidate>
        <input type="hidden" name="id">
        <div class="modal-dialog" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h4 class="modal-title"><?php echo $lang_module->lbl_edit; ?></h4>
                    <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span
                            aria-hidden="true">&times;</span></button>
                </div>
                <div class="modal-body">
                    <div class="form-group">
                        <label class="control-label"><?php echo $lang_module->lbl_checker; ?><span
                                class="text-danger">*</span></label>
                        <div class="controls">
                            <select class="validated" id="edit_user" name="user_id" style="width: 100%" required
                                data-validation-required-message="<?php echo $lang_sys->msg_require; ?>">
                                <option value=""></option>
                                <?php
                                        $sql = "SELECT sys_user.id, sys_user.first_name, sys_user.last_name, mas_department.name AS department_name, mas_position.name AS position_name "
                                            . "FROM sys_user INNER JOIN mas_position ON mas_position.id = sys_user.position_id "
                                            . "INNER JOIN mas_department ON sys_user.department_id = mas_department.id "
                                            . "WHERE sys_user.record_status ='N' AND sys_user.is_active='1' "
                                            . "ORDER BY mas_department.sort, mas_position.sort ASC ";
                                        foreach ($this->db->query($sql)->result() as $row) {
                                            echo '<option value="' . $row->id . '">' . $row->first_name . ' ' . $row->last_name . ' (' . $row->department_name . ' : ' . $row->position_name . ')</option>';
                                        }
                                        ?>
                            </select>
                        </div>
                    </div>
                    <div class="form-group">
                        <label class="control-label"><?php echo $lang_module->lbl_step; ?><span
                                class="text-danger">*</span></label>
                        <div class="controls">
                            <select class="validated" name="step" style="width: 100%" required
                                data-validation-required-message="<?php echo $lang_sys->msg_require; ?>">
                                <option value=""></option>
                                <option value="1">Check</option>
                                <option value="2">Re-Check</option>
                                <option value="3">Verify</option>
                            </select>
                        </div>
                    </div>
                    <div class="form-group">
                        <label class="control-label"><?php echo $lang_module->lbl_require; ?><span
                                class="text-danger">*</span></label>
                        <div class="controls">
                            <select class="validated" name="is_must" style="width: 100%" required
                                data-validation-required-message="<?php echo $lang_sys->msg_require; ?>">
                                <option value="1"><?php echo $lang_module->lbl_yes; ?></option>
                                <option value="0"><?php echo $lang_module->lbl_no; ?></option>
                            </select>
                        </div>
                    </div>
                    <div class="form-group">
                        <label class="control-label"><?php echo $lang_module->lbl_order; ?><span
                                class="text-danger">*</span></label>
                        <div class="controls">
                            <input type="number" min="0" max="99" class="form-control validated" name="sort" required
                                data-validation-required-message="<?php echo $lang_sys->msg_require; ?>"
                                data-validation-number-message="<?php echo $lang_sys->msg_number_only; ?>"
                                data-validation-max-message="<?php echo $lang_sys->msg_max_99; ?>">
                        </div>
                    </div>
                    <div class="form-group">
                        <label class="control-label"><?php echo $lang_module->lbl_description; ?><span
                                class="text-danger">*</span></label>
                        <div class="controls">
                            <textarea name="description" class="form-control validated" rows="3" placeholder=""
                                required></textarea>
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
// "ajax": {
//                     "url": "<?php echo base_url('sys/user/ajax_list'); ?>",
//                     "data": function(d) {
//                         d.department_id = localStorage.getItem("savy-department_id"); //$('#department_id').val();
//                         d.position_id = localStorage.getItem("savy-position_id"); //$('#position_id').val();
//                         d.is_active = localStorage.getItem("savy-is_active"); //$('#is_active').val();
//                         d.gender_id = localStorage.getItem("savy-gender_id"); //$('#gender_id').val();
//                     }
//                 },
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
        "ajax": {
            "url": "<?php echo base_url('mas/department/check_ajax_list/' . $this->uri->segment(4)); ?>",
            "data": function(d) {
                d.is_must = $('#is_must').is(':checked') ? 1 : 0;
            }
        },
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
                    "step": "step"
                },
                render: function(data, type) {
                    return (data.step == 1 ? 'Check' : (data.step == 2 ? 'Re-Check' : 'Verify'));
                }
            },
            {
                "data": {
                    "id": "id"
                },
                render: function(data, type) {
                    return data.prefix_name + data.first_name + ' ' + data.last_name + ' (' + data
                        .emp_code + ') | ' + data.department_name + ', ' + data.position_name;
                }
            },
            {
                "data": "description"
            },
            {
                "data": {
                    "is_must": "is_must"
                },
                render: function(data, type) {
                    return '<div class="text-center">' + (data.is_must == 1 ?
                        '<i class="fas fa-check text-success"></i>' :
                        '<i class="fas fa-times text-warning"></i>') + '</div>';
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
            <?php if ($this->efs_lib->is_can($permission, "edit")) { ?> {
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
                        return '<button type="button" class="btn btn-danger btn-xs confirmDelete" data-href="<?php echo base_url('mas/department/check_delete/'); ?>' +
                            data.id + '"><i class="fas fa-trash-alt"></i></button>';
                    }
                }
            },
            <?php } ?>
        ],
        "columnDefs": [{
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
                $(location).attr('href',
                    '<?php echo base_url() ?>mas/department/check_change_active/' + $(this)
                    .data('id') + '.' + $(this).data('active'));
            });
        },
    });

    function reloadTable() {
        setTimeout(provTable.ajax.reload, 100);
    }




}(window, document, jQuery);
$(document).ready(function() {
    /* Set Class .select2 */
    $('#add_user').select2({
        dropdownParent: $("#addModal"),
        placeholder: "",
        allowClear: true
    });

    // input checkbox click then reloadtable
    $('#is_must').click(function() {
        // alert('test');
        provTable.ajax.reload();
    });



    $('#edit_user').select2({
        dropdownParent: $("#editModal"),
        placeholder: "",
        allowClear: true
    });
    <?php if ($this->efs_lib->is_can($permission, "add")) { ?>
    $('#addBtn').click(function() {
        $('#addForm').trigger("reset");
        var $form = $('#addForm');
        $form.find('.error,.valid').css('border-color', '').removeClass('error').removeClass('valid');
        $form.find('.form-error').remove();
        $form.find('.help-block').html('');
        $form.find('[name="user_id"]').val('').trigger('change');
        $form.find('[name="step"]').val('');
        $form.find('[name="sort"]').val(provTable.data().count() + 1);
    });
    <?php } ?>

    <?php if ($this->efs_lib->is_can($permission, "edit")) { ?>
    $('#mTable tbody').on('click', 'tr td #editBtn', function() {
        var $form = $('#editForm');
        $form.find('.error,.valid').css('border-color', '').removeClass('error').removeClass('valid');
        $form.find('.form-error').remove();
        $form.find('.help-block').html('');
        $.post("<?php echo base_url('mas/department/check_ajax_data'); ?>", {
                id: $(this).data('id')
            },
            function(data) {
                $form.find('[name="id"]').val(data['id']);
                $form.find('[name="user_id"]').val(data['user_id']).trigger('change');
                $form.find('[name="description"]').val(data['description']);
                $form.find('[name="sort"]').val(data['sort']);
                $form.find('[name="step"]').val(data['step']);
                $form.find('[name="is_must"]').val(data['is_must']).trigger('change');
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