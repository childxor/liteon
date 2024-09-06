<?php /* var_dump($permission); */ ?>
<?php // var_dump($_SESSION);  
?>
<?php (empty($permission) ? redirect(base_url("error404")) : ""); ?>
<?php if (!empty($this->efs_lib->is_can($permission, "view"))) { /* Can View */ ?>
    <!-- ============================================================== -->
    <!-- Start Page Content -->
    <!-- ============================================================== -->
    <div class="row">
        <div class="col-12">
            <?php if (!empty($this->efs_lib->is_can($permission, "edit"))) { /* Edit View */ ?>
                <form id="editForm" method="post" action="<?php echo base_url('sys/role/permission_change'); ?>" novalidate>
                    <input type="hidden" name="id" value="<?php echo $this->efs_lib->encrypt_segment($role->id); ?>">
                <?php } ?>
                <div class="row">
                    <div class="col-md-4">
                        <div class="card card-body">
                            <h4 class="box-title m-b-0"><?php echo $lang_module->lbl_role_data; ?></h4>
                            <p class="text-muted m-b-30 font-13"></p>
                            <div class="row">
                                <div class="col-sm-12 col-xs-12">
                                    <div class="form-group">
                                        <label class="control-label"><?php echo $lang_module->lbl_role; ?></label>
                                        <div class="controls">
                                            <h4 class="card-title mt-2"><?php echo ucfirst($role->name); ?></h4>
                                        </div>
                                    </div>
                                    <div class="form-group">
                                        <label class="control-label"><?php echo $lang_module->lbl_detail; ?></label>
                                        <div class="controls">
                                            <h4 class="card-title mt-2"><?php echo $role->description; ?></h4>
                                        </div>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                    <div class="col-md-8">
                        <div class="card card-body">
                            <h4 class="box-title m-b-0"><?php echo $lang_module->lbl_use_module; ?></h4>
                            <table class="table table-bordered table-hover">
                                <thead>
                                    <tr>
                                        <th class="col-md-9"></th>
                                        <?php

                                        $sql = "SELECT description_" . $this->session->userdata("user_profile")->cng_lang . " AS description "
                                            . "FROM   sys_permission "
                                            . "WHERE (is_active = 1) "
                                            . "AND (record_status = 'N') "
                                            . "AND id NOT IN('1')"
                                            . "ORDER BY sort";
                                        $query = $this->db->query($sql);
                                        $result = $query->result();
                                        $nrows_permission = $query->num_rows();
                                        foreach ($result as $item) {
                                        ?>
                                            <th class="font-14"><?php echo $item->description; ?></th>
                                        <?php } ?>
                                    </tr>
                                </thead>
                                <tbody>
                                    <?php
                                    $sql_module = "SELECT id, sort, icon, name_th, name_en, name_jp, module "
                                        . "FROM   sys_module "
                                        . "WHERE  (parent_module_id = 0) "
                                        . "AND (is_active = 1) "
                                        . "AND (record_status = 'N') "
                                        . "ORDER BY sort";
                                    $query_module = $this->db->query($sql_module);

                                    $nrows_module = $query_module->num_rows();
                                    if ($nrows_module > 0) {
                                        foreach ($query_module->result() as $item) {

                                            /* IF No Child Module Do continue */
                                            $this->db->where('parent_module_id', $item->id);
                                            $this->db->where('is_active', '1');
                                            $this->db->where('record_status', 'N');
                                            $this->db->from('sys_module');
                                            $child = $this->db->count_all_results();
                                            if ($child == 0)
                                                continue;

                                            /* IF No Permission Child Module Do continue */
                                            $sql_child = "SELECT id "
                                                . "FROM vw_permission_module_role "
                                                . "WHERE (module_parent = '" . $item->id . "') "
                                                . "AND (module_active = '1') "
                                                . "AND (role_id = '" . $role->id . "') ";
                                            $nrows_child = $this->db->query($sql_child)->num_rows();
                                            if ($nrows_child == 0)
                                                continue;
                                    ?>
                                            <tr class="table-info text-dark">
                                                <td colspan="<?php echo $nrows_permission + 1; ?>" class="font-14">
                                                    <i class="ti <?php echo $item->icon; ?>"></i> <?php echo $item->{"name_" . $this->session->userdata("user_profile")->cng_lang}; ?>
                                                </td>
                                            </tr>
                                            <?php if ($child > 0) { ?>
                                                <?php
                                                $sql_first_level = "SELECT id, sort, icon, name_th, name_en, name_jp, module, permission "
                                                    . "FROM sys_module "
                                                    . "WHERE (parent_module_id = " . $item->id . ") "
                                                    . "AND (is_active = 1) "
                                                    . "AND (record_status = 'N') "
                                                    . "ORDER BY sort";
                                                $query_first_level = $this->db->query($sql_first_level);
                                                $nrows_first_level = $query_first_level->num_rows();
                                                if ($nrows_first_level > 0) {
                                                    foreach ($query_first_level->result() as $first_level) {

                                                        /* ตรวจสอบว่าในโมดูลมีสิทธิอะไรบ้าง 17/11/20 */
                                                        $permis = explode("|", $first_level->permission);
                                                        if (@$this->efs_lib->is_can($this->efs_lib->get_vw_permission_module_role($first_level->module, $role->id), "view") == FALSE)
                                                            continue;
                                                ?>
                                                        <tr>
                                                            <td style="padding-left:28px !important; width: 200px;" class="font-14"><?php echo $first_level->{"name_" . $this->session->userdata("user_profile")->cng_lang}; ?></td>
                                                            <?php
                                                            $sql = "SELECT id, name, description_" . $this->session->userdata("user_profile")->cng_lang . " AS description "
                                                                . "FROM   sys_permission "
                                                                . "WHERE (is_active = 1) "
                                                                . "AND (record_status = 'N') "
                                                                . "AND id NOT IN('1')"
                                                                . "ORDER BY sort";
                                                            foreach ($this->db->query($sql)->result() as $item_permis) {
                                                            ?>
                                                                <td style="text-align: center;">
                                                                    <label class="custom-control custom-checkbox">
                                                                        <?php /* ตรวจสอบว่าในโมดูลมีสิทธิอะไรบ้าง 17/11/20 */
                                                                        if (in_array($item_permis->id, $permis)) {
                                                                        ?>
                                                                            <input class="custom-control-input" <?php echo (empty($this->efs_lib->is_can($permission, "edit")) ? " disabled" : ""); ?> type="checkbox" name="<?php echo strtolower($item_permis->name); ?>[]" value="<?php echo $first_level->id; ?>" <?php echo (@${strtolower($item_permis->name) . '_' . $first_level->id} == 'T' ? 'checked' : '') ?>>
                                                                        <?php } else { ?>
                                                                            <input class="custom-control-input" disabled type="checkbox">
                                                                        <?php } ?>
                                                                        <span class="custom-control-label"></span>
                                                                    </label>
                                                                </td>
                                                            <?php
                                                            }
                                                            ?>
                                                        </tr>
                                                    <?php } ?>
                                                <?php } ?>
                                            <?php } ?>
                                        <?php } ?>
                                    <?php } ?>
                                </tbody>
                            </table>
                            <?php if (!empty($this->efs_lib->is_can($permission, "edit"))) { /* Edit View */ ?>
                                <button type="submit" class="btn btn-sm btn-primary"><?php echo $lang_module->lbl_save; ?></button>
                            <?php } ?>
                        </div>
                    </div>
                </div>
                <?php if (!empty($this->efs_lib->is_can($permission, "edit"))) { /* Edit View */ ?>
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

    <script type="text/javascript">
        ! function(window, document, $) {
            "use strict";
        }(window, document, jQuery);

        $(function() {
            <?php if ($this->session->flashdata('msg')) { ?>
                toastr.<?php echo $this->session->flashdata('type') ?>('<?php echo $this->session->flashdata('type') ?>', '<?php echo $this->session->flashdata('msg') ?>', "top-right", "#ff6849", "5000");
            <?php } ?>

        });
    </script>
<?php } ?>