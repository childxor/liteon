<?php /* var_dump($permission); */ ?>
<?php (empty($permission) ? redirect(base_url("error404")) : ""); ?>
<?php if (!empty($this->efs_lib->is_can($permission, "view"))) { /* Can View */ ?>
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
    </style>
    <!-- This page plugin CSS -->
    <link href="<?php echo base_url(); ?>assets/libs/datatables.net-bs4/css/dataTables.bootstrap4.css" rel="stylesheet">
    <link rel="stylesheet" type="text/css" href="<?php echo base_url(); ?>assets/libs/bootstrap-switch/dist/css/bootstrap3/bootstrap-switch.min.css">
    <div class="row">
        <div class="col-12">
            <div class="card">
                <div class="card-body">
                    <div class="text-right">
                        <div class="card-body bg-light mb-3 pb-1">
                            <div class="row">
                                <div class="col-sm-12 col-lg-3">
                                    <div class="form-group row">
                                        <div class="col-sm-12"> 
                                            <?php
                                            $sql = "SELECT id, name "
                                                . "FROM mas_department "
                                                . "WHERE is_active ='1' AND record_status ='N' "
                                                . "ORDER BY sort ASC ";
                                            $departments = $this->db->query($sql)->result();
                                            ?>
                                            <select class="form-control form-control-line auto-save" id="department_id" style="width:100%">
                                                <option value=""><?php echo $lang_module->lbl_department; ?></option>
                                                <?php
                                                foreach ($departments as $department) {
                                                ?>
                                                    <option value="<?php echo $department->id; ?>"><?php echo $department->name; ?></option>
                                                <?php
                                                }
                                                ?>
                                            </select>
                                        </div>
                                    </div>
                                </div>
                                <div class="col-sm-12 col-lg-3">
                                    <div class="form-group row">
                                        <div class="col-sm-12">
                                            <?php
                                            $sql = "SELECT id, name "
                                                . "FROM mas_position "
                                                . "WHERE is_active ='1' AND record_status ='N' "
                                                . "ORDER BY sort ASC ";
                                            $positions = $this->db->query($sql)->result();
                                            ?>
                                            <select class="form-control form-control-line auto-save select2" id="position_id" style="width:100%">
                                                <option value=""><?php echo $lang_module->lbl_position; ?></option>
                                                <?php
                                                foreach ($positions as $position) {
                                                ?>
                                                    <option value="<?php echo $position->id; ?>"><?php echo $position->name; ?></option>
                                                <?php
                                                }
                                                ?>
                                            </select>

                                        </div>
                                    </div>
                                </div>
                                <div class="col-sm-12 col-lg-2">
                                    <div class="form-group row">
                                        <div class="col-sm-12">
                                            <select class="form-control form-control-line auto-save" id="is_active" style="width:100%">
                                                <option value=""><?php echo $lang_module->lbl_status; ?></option>
                                                <option value="N"><?php echo $lang_module->lbl_deactive; ?></option>
                                                <option value="Y"><?php echo $lang_module->lbl_active; ?></option>
                                            </select>
                                        </div>
                                    </div>
                                </div>
                                <div class="col-sm-12 col-lg-2">
                                    <div class="form-group row">
                                        <div class="col-sm-12">
                                            <select class="form-control form-control-line auto-save" id="gender_id" style="width:100%">
                                                <option value=""><?php echo $lang_module->lbl_gender; ?></option>
                                                <option value="1"><?php echo $lang_module->lbl_male; ?></option>
                                                <option value="2"><?php echo $lang_module->lbl_female; ?></option>
                                            </select>
                                        </div>
                                    </div>
                                </div>
                                <div class="col-sm-12 col-lg-2">
                                    <div class="form-group row">
                                        <div class="col-sm-12">
                                            <button class="btn btn-info btn-block" id="btnSearch"><i class="fas fa-search"></i> <?php echo $lang_module->lbl_search; ?></button>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="text-right">
                        <?php
                        $id = $this->session->userdata("user_profile")->id;
                        if ($id == 463) { // ต้องเป็น admin ที่สามารถเพิ่มข้อมูลได้
                        ?>
                            <a id="testbt" class="btn btn-primary mb-2" data-toggle="modal" data-backdrop="static" data-keyboard="false" data-target="#modalChangePassword" href="javascript:void(0);"><span><i class="fa fa-plus"></i> test</span></a>
                        <?php
                        }
                        if ($this->efs_lib->is_can($permission, "add")) { ?>
                            <a id="addBtn" class="btn btn-primary mb-2" data-toggle="modal" data-backdrop="static" data-keyboard="false" data-target="#addModal" href="javascript:void(0);"><span><i class="fa fa-plus"></i> <?php echo $lang_module->lbl_add; ?></span></a>
                        <?php } ?>
                    </div>
                    <div class="table-responsive m-t-40">
                        <table id="mTable" class="table table-bordered table-striped table-hover" style="width:100%">
                            <thead>
                                <tr>
                                    <th><?php echo $lang_module->lbl_order; ?></th>
                                    <th><?php echo $lang_module->lbl_fullname; ?></th>
                                    <th><?php echo $lang_module->lbl_department; ?></th>
                                    <th><?php echo $lang_module->lbl_position; ?></th>
                                    <th><?php echo $lang_module->lbl_tel; ?></th>
                                    <th class="is-role"><?php echo $lang_module->lbl_role; ?></th>
                                    <th class="is-small text-center"><i class="fa fa-eye"></i></th>
                                    <?php if ($this->efs_lib->is_can($permission, "edit")) { ?>
                                        <th class="no-sort text-center"><i class="fas fa-key"></i></th>
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
            <form id="addForm" method="post" action="<?php echo base_url('sys/user/add'); ?>" novalidate>
                <div class="modal-dialog" role="document">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h4 class="modal-title" id="addModalLabel"><?php echo $lang_module->lbl_add; ?></h4>
                            <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                        </div>
                        <div class="modal-body">
                            <div class="row">
                                <div class="col-4">
                                    <div class="form-group">
                                        <label class="control-label"><?php echo $lang_module->lbl_prefix; ?><span class="text-danger">*</span></label>
                                        <div class="controls">
                                            <select class="form-control validated" name="prefix_name" required style="width:100%" data-validation-required-message="<?php echo $lang_sys->msg_require; ?>">
                                                <option value=""></option>
                                                <option value="Mr.">Mr.</option>
                                                <option value="Mrs.">Mrs.</option>
                                                <option value="Miss.">Miss.</option>
                                                <option value="Ms.">Ms.</option>
                                                <option value="นาย">นาย</option>
                                                <option value="น.ส.">น.ส.</option>
                                                <option value="นาง">นาง</option>
                                            </select>
                                        </div>
                                    </div>
                                </div>
                                <div class="col-4">
                                    <div class="form-group">
                                        <label class="control-label"><?php echo $lang_module->lbl_firstname; ?><span class="text-danger">*</span></label>
                                        <div class="controls">
                                            <input type="text" class="form-control validated" name="first_name" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>" autocomplete="off">
                                        </div>
                                    </div>
                                </div>
                                <div class="col-4">
                                    <div class="form-group">
                                        <label class="control-label"><?php echo $lang_module->lbl_lastname; ?><span class="text-danger">*</span></label>
                                        <div class="controls">
                                            <input type="text" class="form-control validated" name="last_name" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>" autocomplete="off">
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-6">
                                    <div class="form-group">
                                        <label class="control-label"><?php echo $lang_module->lbl_department; ?><span class="text-danger">*</span></label>
                                        <div class="controls">
                                            <select class="validated" id="add_department" name="department_id" style="width: 100%" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>">
                                                <option value=""></option>
                                                <?php
                                                $sql = "SELECT id, name FROM mas_department WHERE record_status ='N' AND is_active='1' ORDER BY sort ASC ";
                                                foreach ($this->db->query($sql)->result() as $row) {
                                                    echo '<option value="' . $row->id . '">' . $row->name . '</option>';
                                                }
                                                ?>
                                            </select>
                                        </div>
                                    </div>
                                </div>
                                <div class="col-6">
                                    <div class="form-group">
                                        <label class="control-label"><?php echo $lang_module->lbl_sub_department; ?></label>
                                        <div class="controls">
                                            <select class="validated" id="add_sub_department" name="sub_department_id" style="width: 100%"></select>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-6">
                                    <div class="form-group">
                                        <label class="control-label"><?php echo $lang_module->lbl_tel; ?></label>
                                        <div class="controls">
                                            <input type="text" class="form-control" name="tel" autocomplete="off">
                                        </div>
                                    </div>
                                </div>
                                <div class="col-6">
                                    <div class="form-group">
                                        <label class="control-label"><?php echo $lang_module->lbl_email; ?></label>
                                        <div class="controls">
                                            <input type="text" class="form-control" name="email" autocomplete="off">
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_emp_code; ?><span class="text-danger">*</span></label>
                                <div class="controls">
                                    <input type="text" class="form-control validated" name="emp_code" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>" data-validation-ajax-ajax="<?php echo base_url('sys/user/ajax_exist/emp_code'); ?>" pattern="^[0-9a-zA-Z][a-zA-Z0-9-_]{1,30}$" autocomplete="off">
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_username; ?><span class="text-danger">*</span></label>
                                <div class="controls">
                                    <input type="text" class="form-control validated" name="username" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>" data-validation-ajax-ajax="<?php echo base_url('sys/user/ajax_exist/username'); ?>" pattern="^[0-9a-zA-Z][a-zA-Z0-9-_]{1,30}$" autocomplete="off">
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_position; ?><span class="text-danger">*</span></label>
                                <div class="controls">
                                    <select class="validated" name="position_id" style="width: 100%" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>">
                                        <option value=""></option>
                                        <?php
                                        $sql = "SELECT id, name FROM mas_position WHERE record_status ='N' AND is_active='1' ORDER BY sort ASC ";
                                        foreach ($this->db->query($sql)->result() as $row) {
                                            echo '<option value="' . $row->id . '">' . $row->name . '</option>';
                                        }
                                        ?>
                                    </select>
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_role; ?><span class="text-danger">*</span></label>
                                <div class="controls">
                                    <select class="validated" id="add_role" name="role_id[]" multiple style="width: 100%" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>">
                                        <?php
                                        $sql = "SELECT id, name FROM sys_role WHERE record_status ='N' ORDER BY name ASC ";
                                        foreach ($this->db->query($sql)->result() as $row) {
                                            echo '<option value="' . $row->id . '">' . $row->name . '</option>';
                                        }
                                        ?>
                                    </select>
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
        <div class="modal fade" id="resetPasswdModal" tabindex="-1" role="dialog" aria-labelledby="resetPasswdModalLabel">
            <form id="resetPasswdForm" method="post" action="<?php echo base_url('sys/user/reset_passwd'); ?>" novalidate>
                <div class="modal-dialog" role="document">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h4 class="modal-title"><?php echo $lang_sys->sys_confirm_reset_password; ?></h4>
                            <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                        </div>
                        <input type="hidden" name="id">
                        <div class="modal-body">
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_sys->sys_first_name; ?>-<?php echo $lang_sys->sys_last_name; ?> : <span id="reset_name"></span></label>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_sys->sys_username; ?> : <span id="reset_username"></span></label>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_sys->sys_department; ?> : <span id="reset_department_name"></span></label>
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="reset" class="btn btn-sm btn-default" data-dismiss="modal"><?php echo $lang_sys->sys_btn_cancel; ?></button>
                            <button type="submit" class="btn btn-sm btn-primary"><?php echo $lang_sys->sys_btn_reset_password; ?></button>
                        </div>
                    </div>
                </div>
            </form>
        </div>
        <!-- /.modal -->

        <div class="modal fade" id="editModal" tabindex="-1" role="dialog" aria-labelledby="editModalLabel">
            <form id="editForm" method="post" action="<?php echo base_url('sys/user/edit'); ?>" novalidate>
                <div class="modal-dialog" role="document">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h4 class="modal-title" id="editModalLabel"><?php echo $lang_module->lbl_edit; ?></h4>
                            <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                        </div>
                        <input type="hidden" name="id">
                        <div class="modal-body">
                            <div class="row">
                                <div class="col-4">
                                    <div class="form-group">
                                        <label class="control-label"><?php echo $lang_module->lbl_prefix; ?><span class="text-danger">*</span></label>
                                        <div class="controls">
                                            <select class="form-control validated" name="prefix_name" required style="width:100%" data-validation-required-message="<?php echo $lang_sys->msg_require; ?>">
                                                <option value=""></option>
                                                <option value="Mr.">Mr.</option>
                                                <option value="Mrs.">Mrs.</option>
                                                <option value="Miss.">Miss.</option>
                                                <option value="Ms.">Ms.</option>
                                                <option value="นาย">นาย</option>
                                                <option value="น.ส.">น.ส.</option>
                                                <option value="นาง">นาง</option>
                                            </select>
                                        </div>
                                    </div>
                                </div>
                                <div class="col-4">
                                    <div class="form-group">
                                        <label class="control-label"><?php echo $lang_module->lbl_firstname; ?><span class="text-danger">*</span></label>
                                        <div class="controls">
                                            <input type="text" class="form-control validated" name="first_name" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>" autocomplete="off">
                                        </div>
                                    </div>
                                </div>
                                <div class="col-4">
                                    <div class="form-group">
                                        <label class="control-label"><?php echo $lang_module->lbl_lastname; ?><span class="text-danger">*</span></label>
                                        <div class="controls">
                                            <input type="text" class="form-control validated" name="last_name" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>" autocomplete="off">
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-6">
                                    <div class="form-group">
                                        <label class="control-label"><?php echo $lang_module->lbl_department; ?><span class="text-danger">*</span></label>
                                        <div class="controls">
                                            <select class="validated" id="edit_department" name="department_id" style="width: 100%" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>">
                                                <option value=""></option>
                                                <?php
                                                $sql = "SELECT id, name FROM mas_department WHERE record_status ='N' ORDER BY sort ASC ";
                                                foreach ($this->db->query($sql)->result() as $row) {
                                                    echo '<option value="' . $row->id . '">' . $row->name . '</option>';
                                                }
                                                ?>
                                            </select>
                                        </div>
                                    </div>
                                </div>
                                <div class="col-6">
                                    <div class="form-group">
                                        <label class="control-label"><?php echo $lang_module->lbl_sub_department; ?></label>
                                        <div class="controls">
                                            <select class="validated" id="edit_sub_department" name="sub_department_id" style="width: 100%"></select>
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="row">
                                <div class="col-6">
                                    <div class="form-group">
                                        <label class="control-label"><?php echo $lang_module->lbl_tel; ?></label>
                                        <div class="controls">
                                            <input type="text" class="form-control" name="tel" autocomplete="off">
                                        </div>
                                    </div>
                                </div>
                                <div class="col-6">
                                    <div class="form-group">
                                        <label class="control-label"><?php echo $lang_module->lbl_email; ?></label>
                                        <div class="controls">
                                            <input type="text" class="form-control" name="email" autocomplete="off">
                                        </div>
                                    </div>
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_emp_code; ?><span class="text-danger">*</span></label>
                                <div class="controls">
                                    <input type="text" class="form-control" name="emp_code" readonly autocomplete="off">
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_username; ?><span class="text-danger">*</span></label>
                                <div class="controls">
                                    <input type="text" class="form-control" name="username" autocomplete="off">
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_position; ?><span class="text-danger">*</span></label>
                                <div class="controls">
                                    <select class="validated" name="position_id" style="width: 100%" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>">
                                        <option value=""></option>
                                        <?php
                                        $sql = "SELECT id, name FROM mas_position WHERE record_status ='N' AND is_active='1' ORDER BY sort ASC ";
                                        foreach ($this->db->query($sql)->result() as $row) {
                                            echo '<option value="' . $row->id . '">' . $row->name . '</option>';
                                        }
                                        ?>
                                    </select>
                                </div>
                            </div>
                            <div class="form-group">
                                <label class="control-label"><?php echo $lang_module->lbl_role; ?><span class="text-danger">*</span></label>
                                <div class="controls">
                                    <select class="validated" id="edit_role" name="role_id[]" multiple style="width: 100%" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>">
                                        <?php
                                        $sql = "SELECT id, name FROM sys_role WHERE record_status ='N' ORDER BY name ASC ";
                                        foreach ($this->db->query($sql)->result() as $row) {
                                            echo '<option value="' . $row->id . '">' . $row->name . '</option>';
                                        }
                                        ?>
                                    </select>
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

    <div class="modal fade" id="stepCheckModal" tabindex="-1" role="dialog">
        <div class="modal-dialog modal-step" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h4 class="modal-title" id="stepTitle">Step Check Re-check Verify</h4>
                    <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                </div>
                <div class="modal-body" id="stepTable">
                </div>
            </div>
        </div>
    </div>

    <!-- modal BUTTON TEST  -->
    <div class="modal fade" id="modalChangePassword" tabindex="-1" role="dialog">
        <div class="modal-dialog modal-lg" role="document">
            <form id="formChangePassword" class="form-horizontal" method="post" action="<?php echo base_url('user/change_password'); ?>">
                <div class="modal-content">
                    <div class="modal-header">
                        <h4 class="modal-title" id="defaultModalLabel">CHANGE PASSWORD</h4>
                        <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                    </div>
                    <div class="modal-body form-horizontal">
                        <div class="form-group">
                            <label class="control-label col-md-3"><?php echo $lang_module->lbl_username; ?><span class="text-danger">*</span></label>
                            <div class="col-md-9">
                                <input type="text" class="form-control" name="username" readonly autocomplete="off">
                            </div>
                        </div>
                        <div class="form-group row">
                            <label class="control-label col-md-3"><?php echo "PASSWORD"; ?><span class="text-danger">*</span></label>
                            <div class="col-md-9">
                                <input type="password" class="form-control" name="password" autocomplete="off">
                            </div>
                        </div>
                        <div class="form-group row">
                            <label class="control-label col-md-3"><?php echo "CONFIRM PASSWORD"; ?><span class="text-danger">*</span></label>
                            <div class="col-md-9">
                                <input type="password" class="form-control" name="confirm_password" autocomplete="off">
                            </div>
                        </div>
                    </div>
                    <div class="modal-footer">
                        <button type="reset" class="btn btn-sm btn-default" data-dismiss="modal"><?php echo $lang_module->lbl_cancel; ?></button>
                        <button type="submit" class="btn btn-sm btn-primary"><?php echo $lang_module->lbl_save; ?></button>
                    </div>
                </div>
            </form>
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

            /* บังคับให้ไม่แสดง Error บนหน้าจอ */
            $.fn.dataTable.ext.errMode = 'none';

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
                    "url": "<?php echo base_url('sys/user/ajax_list'); ?>",
                    "data": function(d) {
                        d.department_id = localStorage.getItem("savy-department_id"); //$('#department_id').val();
                        d.position_id = localStorage.getItem("savy-position_id"); //$('#position_id').val();
                        d.is_active = localStorage.getItem("savy-is_active"); //$('#is_active').val();
                        d.gender_id = localStorage.getItem("savy-gender_id"); //$('#gender_id').val();
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
                            "id": "id",
                            "user_id": "user_id",
                            "step": "step",
                            "prefix_name": "prefix_name",
                            "first_name": "first_name",
                            "last_name": "last_name",
                            "emp_code": "emp_code",
                            "username": "username"
                        },
                        render: function(data) {
                            return (data.step == 'N' ? '<a href="javascript:void(0)" class="stepCheckBtn" data-id="' + data.id + '" data-toggle="modal" data-backdrop="static" data-keyboard="false" data-target="#stepCheckModal"><i class="fa fa-info-circle text-xs"></i></a> ' : '') + data.prefix_name + data.first_name + ' ' + data.last_name + ' (' + data.emp_code + ' : ' + data.username + ') (' + data.user_id + ')';
                        }
                    },
                    {
                        "data": "department_name"
                    },
                    {
                        "data": "position_name"
                    },
                    {
                        "data": "tel"
                    },
                    {
                        "data": "role"
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
                    <?php if ($this->efs_lib->is_can($permission, "edit")) { ?> {
                            "data": {
                                "id": "id"
                            },
                            render: function(data, type) {
                                return '<button type="button" class="btn btn-outline-warning btn-xs" id="resetPasswdBtn" data-id="' + data.id + '" data-toggle="modal" data-backdrop="static" data-keyboard="false" data-target="#resetPasswdModal"><i class="fas fa-key"></i></button>';
                            }
                        },
                        {
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
                                    return '<button type="button" class="btn btn-danger btn-xs confirmDelete" data-href="<?php echo base_url('sys/user/delete/'); ?>' + data.id + '"><i class="fas fa-trash-alt"></i></button>';
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
                        "width": "150px",
                        "targets": 'is-role'
                    },
                    {
                        "width": "5px",
                        "targets": 'no-sort'
                    }
                ],
                "fnDrawCallback": function() {
                    $(".switch[type='checkbox']").bootstrapSwitch();
                    $('.switch[type="checkbox"]').on('switchChange.bootstrapSwitch', function(event, state) {
                        $(location).attr('href', '<?php echo base_url() ?>sys/user/change_active/' + $(this).data('id') + '.' + $(this).data('active'));
                    });
                },
            });

            /* ตรวจสอบ error ของตาราง (หาก SESSION หมดอายุให้ออกจากระบบ) */
            provTable.on('error.dt', function(e, settings, techNote, message) {
                console.log('An error has been reported by DataTables: ', message);
                window.location.replace("<?php echo base_url(); ?>");
            });

            function reloadTable() {
                setTimeout(provTable.ajax.reload, 100);
            }
            $('#btnSearch').on('click', function() {
                reloadTable();
            });
        }(window, document, jQuery);
        $(document).ready(function() {

            /* Set Class .select2 */
            $('#add_department, #add_sub_department').select2({
                dropdownParent: $("#addModal"),
                placeholder: "",
                allowClear: true
            });
            $('#edit_department, #edit_sub_department').select2({
                dropdownParent: $("#editModal"),
                placeholder: "",
                allowClear: true
            });

            //    name position_id
            // $('name="position_id"').select2({
            //     // dropdownParent: $("#editModal"),
            //     placeholder: "",
            //     allowClear: true
            // });

            $('#add_role').select2({
                dropdownParent: $("#addModal"),
                placeholder: "",
            });
            $('#edit_role').select2({
                dropdownParent: $("#editModal"),
                placeholder: "",
            });
            <?php if ($this->efs_lib->is_can($permission, "add")) { ?>
                $('#add_department').change(function() {
                    $.post("<?php echo base_url('sys/user/ajax_sub_department_list'); ?>", {
                            id: $(this).val()
                        },
                        function(data) {
                            $('#add_sub_department').html(data);
                        }, "json");
                });
                $('#addBtn').click(function() {
                    $('#addForm').trigger("reset");
                    var $form = $('#addForm');
                    $form.find('.error,.valid').css('border-color', '').removeClass('error').removeClass('valid');
                    $form.find('.form-error').remove();
                    $form.find('.help-block').html('');
                    var newOption = new Option(data.text, data.id, false, false);
                    $form.find('[name="department_id"]').append(newOption).trigger('change');
                    $form.find('[name="role_id[]"]').append(newOption).trigger('change');
                });
            <?php } ?>

            <?php if ($this->efs_lib->is_can($permission, "edit")) { ?>

                $('#edit_department').on('select2:select', function(e) {
                    var data = e.paraMs.data;
                    $.post("<?php echo base_url('sys/user/ajax_sub_department_list'); ?>", {
                            id: data.id
                        },
                        function(data) {
                            $('#edit_sub_department').html(data);
                        }, "json");
                });
                $('#mTable tbody').on('click', 'tr td #resetPasswdBtn', function() {
                    var $form = $('#resetPasswdForm');
                    $.post("<?php echo base_url('sys/user/ajax_data'); ?>", {
                            id: $(this).data('id')
                        },
                        function(data) {
                            $form.find('[name="id"]').val(data['item']['id']);
                            $form.find('[id="reset_department_name"]').html(data['item']['department_name']);
                            $form.find('[id="reset_name"]').html(data['item']['first_name'] + ' ' + data['item']['last_name']);
                            $form.find('[id="reset_username"]').html(data['item']['username']);
                        }, "json");
                });
                $('#mTable tbody').on('click', 'tr td #editBtn', function() {
                    var $form = $('#editForm');
                    $form.find('.error,.valid').css('border-color', '').removeClass('error').removeClass('valid');
                    $form.find('.form-error').remove();
                    $form.find('.help-block').html('');
                    $.post("<?php echo base_url('sys/user/ajax_data'); ?>", {
                            id: $(this).data('id')
                        },
                        function(data) {
                            console.log(data);
                            $form.find('[name="id"]').val(data['item']['id']);
                            $form.find('[name="prefix_name"]').val(data['item']['prefix_name']).trigger('change');
                            $form.find('[name="first_name"]').val(data['item']['first_name']);
                            $form.find('[name="last_name"]').val(data['item']['last_name']);
                            $form.find('[name="username"]').val(data['item']['username']);
                            $form.find('[name="emp_code"]').val(data['item']['emp_code']);
                            $form.find('[name="tel"]').val(data['item']['tel']);
                            $form.find('[name="email"]').val(data['item']['email']);
                            $form.find('[name="position_id"]').val(data['item']['position_id']);
                            $form.find('[name="department_id"]').val(data['item']['department_id']).trigger('change');
                            var sub = data['item']['sub_department_id'];
                            $.post("<?php echo base_url('sys/user/ajax_sub_department_list'); ?>", {
                                    id: data['item']['department_id'],
                                    sub: sub
                                },
                                function(data) {
                                    $('#edit_sub_department').html(data);
                                }, "json");
                            if (data['role_ids']) {
                                var role_ids = data['role_ids'].split(",");
                                $form.find('[name="role_id[]"]').select2('val', [role_ids]);
                            } else {
                                $form.find('[name="role_id[]"]').val(null).trigger("change");
                            }
                        }, "json");
                });
            <?php } ?>

            /* stepCheck Form */
            $('#mTable tbody').on('click', '.stepCheckBtn', function() {
                // console.log($(this).data('id'));
                $.post("<?php echo base_url('sys/user/table_department_check_pr'); ?>", {
                        user_id: $(this).data('id')
                    },
                    function(data) {
                        $('#stepTitle').html(data['title']);
                        $('#stepTable').html(data['table']);
                    }, "json");
            });

        });
        $(function() {
            <?php if ($this->session->flashdata('msg')) { ?>
                toastr.<?php echo $this->session->flashdata('type') ?>('<?php echo $this->session->flashdata('type') ?>', '<?php echo $this->session->flashdata('msg') ?>', "top-right", "#ff6849", "5000");
            <?php } ?>

        });
    </script>
<?php
} /* Can View */ ?>