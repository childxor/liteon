<?php /* var_dump($permission); */ ?>
<?php (empty($permission) ? redirect(base_url("error404")) : ""); ?>
<?php if (!empty($this->efs_lib->is_can($permission, "view"))) { /* Can View */ ?>
    <!-- ============================================================== -->
    <!-- Start Page Content -->
    <!-- ============================================================== -->
    <div class="row">
        <div class="col-12">
            <?php if (!empty($this->efs_lib->is_can($permission, "edit"))) { // Edit View 
            ?>
                <form id="editForm" method="post" action="<?php echo base_url('sys/role/edit'); ?>" novalidate>
                    <input type="hidden" name="id" value="<?php echo $this->efs_lib->encrypt_segment($role->id); ?>">
                <?php } ?>
                <div class="row">
                    <div class="col-md-6">
                        <div class="card card-body">
                            <h3 class="box-title m-b-0"><?php echo $lang_module->lbl_role_data; ?></h3>
                            <p class="text-muted m-b-30 font-13"></p>
                            <div class="row">
                                <div class="col-sm-12 col-xs-12">
                                    <div class="form-group">
                                        <label class="control-label"><?php echo $lang_module->lbl_role_name; ?><span class="text-danger">*</span></label>
                                        <div class="controls">
                                            <input type="text" class="form-control validated" id="name" value="<?php echo ucfirst($role->name); ?>" readonly autocomplete="off">
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <label class="control-label"><?php echo $lang_module->lbl_detail; ?></label>
                                        <textarea class="form-control" id="description" name="description"><?php echo $role->description; ?></textarea>
                                    </div>
                                    <div class="form-group">
                                        <label class="control-label"><?php echo $lang_module->lbl_default_module; ?><span class="text-danger">* <?php echo $lang_module->lbl_default_require; ?></span></label>
                                        <div class="controls">
                                            <select class="select2 col-12 validated" id="module_id" name="default_module_id" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>" style="width:100%">
                                                <option value="" selected></option>
                                                <?php
                                                $sql = "SELECT * FROM sys_module WHERE record_status='N' AND parent_module_id ='0' ORDER BY sort ASC";
                                                $parents = $this->db->query($sql)->result();
                                                ?>
                                                <?php foreach ($parents as $parent) { ?>
                                                    <?php
                                                    $sql = "SELECT * FROM sys_module WHERE record_status='N' AND parent_module_id ='" . $parent->id . "' AND is_active ='1' ORDER BY sort ASC";
                                                    $nrows = $this->db->query($sql)->num_rows();
                                                    if ($nrows > 0) {
                                                        $subs = $this->db->query($sql)->result();
                                                    ?>
                                                        <optgroup label="<i class='ti <?php echo $parent->icon; ?>'></i> <?php echo $parent->{"name_" . $this->session->userdata("user_profile")->cng_lang}; ?>">
                                                            <?php foreach ($subs as $sub) { ?>
                                                                <option id="opt_<?php echo $sub->id; ?>" value="<?php echo $sub->id; ?>" <?php echo ($role->default_module_id == $sub->id ? 'selected' : ''); ?>><?php echo $sub->{"name_" . $this->session->userdata("user_profile")->cng_lang}; ?></option>
                                                            <?php } ?>
                                                        <?php } ?>
                                                        </optgroup>
                                                    <?php } ?>
                                            </select>
                                        </div>
                                    </div>
                                    <?php if (!empty($this->efs_lib->is_can($permission, "edit"))) { // Edit View 
                                    ?>
                                        <div class="form-group">
                                            <button type="submit" class="btn btn-sm btn-primary"><?php echo $lang_module->lbl_save; ?></button>
                                        </div>
                                    <?php } ?>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-6">
                        <div class="card card-body">
                            <h3 class="box-title m-b-0"><?php echo $lang_module->lbl_use_module; ?></h3>
                            <p class="text-muted m-b-30 font-13"></p>
                            <div class="controls">
                                <input type="button" class="btn btn-default button_tree" value="<?php echo $lang_module->lbl_collepsed; ?>" data-id="Collepsed">
                                <input type="button" class="btn btn-default button_tree" value="<?php echo $lang_module->lbl_expanded; ?>" data-id="Expanded">
                                <?php if (strcmp($role->id, "1") !== 0) { // Edit View 
                                ?>
                                    <input type="button" class="btn btn-default button_tree" value="<?php echo $lang_module->lbl_check_all; ?>" data-id="Checked All">
                                    <input type="button" class="btn btn-default button_tree" value="<?php echo $lang_module->lbl_uncheck_all; ?>" data-id="Unchecked All">
                                <?php } ?>
                            </div>
                            <?php
                            $sql = "SELECT * FROM sys_module WHERE record_status='N' AND parent_module_id ='0' ORDER BY sort ASC";
                            $parents = $this->db->query($sql)->result();
                            ?>
                            <ul class="tree">
                                <?php foreach ($parents as $parent) { ?>
                                    <?php
                                    $sql = "SELECT * FROM sys_module WHERE record_status='N' AND parent_module_id ='" . $parent->id . "' AND is_active ='1' ORDER BY sort ASC";
                                    $nrows = $this->db->query($sql)->num_rows();
                                    if ($nrows > 0) {
                                        $subs = $this->db->query($sql)->result();
                                    ?>
                                        <li class="has">
                                            <label><?php echo $parent->{"name_" . $this->session->userdata("user_profile")->cng_lang}; ?> <span class="total">(<?php echo $nrows; ?>)</span></label>
                                            <ul>
                                                <?php foreach ($subs as $sub) { ?>
                                                    <li class="">
                                                        <label class="custom-control custom-checkbox">
                                                            <input class="custom-control-input" type="checkbox" name="module_ids[]" value="<?php echo $sub->id; ?>" <?php echo (strcmp($role->id, "1") !== 0 ? "" : "disabled"); ?>>
                                                            <span class="custom-control-label"><?php echo $sub->{"name_" . $this->session->userdata("user_profile")->cng_lang}; ?></span>
                                                        </label>
                                                    </li>
                                                <?php } ?>
                                            </ul>
                                        </li>
                                    <?php } ?>
                                <?php } ?>
                            </ul>
                        </div>
                    </div>
                </div>
                <?php if (!empty($this->efs_lib->is_can($permission, "edit"))) { // Edit View 
                ?>
                </form>
            <?php } ?>
        </div>
    </div>

    <!-- ============================================================== -->
    <!-- End Page Content -->
    <!-- ============================================================== -->
    <script src="<?php echo base_url(); ?>assets/libs/jquery/dist/jquery.min.js"></script>

    <!-- ============================================================== -->
    <!-- This page plugins -->
    <!-- ============================================================== -->
    <!--Custom JavaScript -->
    <!-- This Page JS -->
    <script src="<?php echo base_url(); ?>assets/extra-libs/jqbootstrapvalidation/validation.js"></script>
    <script src="<?php echo base_url(); ?>assets/libs/bootstrap-switch/dist/js/bootstrap-switch.min.js"></script>
    <link href="<?php echo base_url(); ?>dist/css/module_tree.css" rel="stylesheet" type="text/css" />
    <script src="<?php echo base_url(); ?>dist/js/module_tree.js" type="text/javascript"></script>

    <script type="text/javascript">
        <?php
        foreach ($permission_module_id as $module_id) {
        ?>
            $('[name="module_ids[]"][value="<?php echo $module_id->module_id; ?>"]').attr('checked', true);
        <?php
        }
        ?>
        $('.tree input[type=checkbox]:not(:checked)').each(function() {
            $('#opt_' + this.value).prop('disabled', true);
        });
        var create_icon = $('#module_id');

        ! function(window, document, $) {
            "use strict";
        }(window, document, jQuery);

        $(document).ready(function() {
            $('.tree ul').fadeIn();
            create_icon = $('#module_id').select2({
                escapeMarkup: function(m) {
                    return m;
                },
                placeholder: "",
                allowClear: true
            });
        });

        $(document).on('change', '.tree input[type=checkbox]', function() {
            /* alert(this.value); */
            create_icon.val(null);
            create_icon.select2('destroy');
            create_icon.select2({
                escapeMarkup: function(m) {
                    return m;
                },
                placeholder: "",
                allowClear: true
            });
            $('.tree input[type=checkbox]:not(:checked)').each(function() {
                $('#opt_' + this.value).prop('disabled', true);
            });
            $('.tree input[type=checkbox]:checked').each(function() {
                $('#opt_' + this.value).prop('disabled', false);
            });
        });

        $(function() {
            <?php if ($this->session->flashdata('msg')) { ?>
                toastr.<?php echo $this->session->flashdata('type') ?>('<?php echo $this->session->flashdata('type') ?>', '<?php echo $this->session->flashdata('msg') ?>', "top-right", "#ff6849", "5000");
            <?php } ?>

        });
    </script>
<?php } ?>