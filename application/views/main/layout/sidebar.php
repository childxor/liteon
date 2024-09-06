<style>
    /* .sidebar-link :active {
        background-color: #009EFB;
    } */

    .sidebar-item :hover {
        background-color: #c5e8fc;
    }

    .sidebar-item :active {
        background-color: #009EFB;
    }

    .sidebar-footer .new-link {
        position: relative;
    }

    .sidebar-footer .new {
        display: inline-block;
        padding: 2px 4px;
        font-size: 10px;
        /* font-weight: bold; */
        color: #fff;
        background-color: #dc3545;
        border-radius: 3px;
        position: absolute;
        top: -5px;
        right: 10px;
    }
</style>
<aside class="left-sidebar">
    <!-- Sidebar scroll-->
    <div class="scroll-sidebar">
        <!-- Sidebar navigation-->
        <nav class="sidebar-nav">
            <ul id="sidebarnav">
                <!-- User profile -->
                <div class="user-profile text-center position-relative mt-1">
                    <!-- User profile text-->
                    <!-- <div class="profile-text py-1">
                        <br>
                    </div> -->
                </div>
                <!-- End User profile text-->
                <?php
                $id = $this->session->userdata("user_profile")->id;
                $menu = array(); /* (1) Variable for get identity Class ID */
                $sql_module = "SELECT id, sort, icon, name_th, name_en, name_jp, name_cn, module
                                FROM   sys_module
                                WHERE  (parent_module_id = 0) AND (is_active = 1) AND (record_status = 'N')
                                ORDER BY sort";
                // echo $sql_module;
                // die();
                $query_module = $this->db->query($sql_module);

                // echo $department_id;
                // die();
                // echo $sub->sub_department_id; die();
                // echo $count_work;

                $nrows_module = $query_module->num_rows();
                if ($nrows_module > 0) {
                    foreach ($query_module->result() as $item) {
                        $this->db->where('parent_module_id', $item->id);
                        $this->db->where('is_active', '1');
                        $this->db->where('record_status', 'N');
                        $this->db->from('sys_module');
                        $child = $this->db->count_all_results();
                        if ($child == 0) {
                            continue;
                        }

                ?>
                        <li class="sidebar-item" data-id="<?php echo $item->id; ?>">
                            <a class="sidebar-link <?php echo ($child > 0 ? 'has-arrow waves-effect waves-dark' : '') ?>" href="<?php echo ($child > 0 ? base_url($item->module) : 'javascript:void(0)') ?>" aria-expanded="false"><i class="<?php echo $item->icon; ?>"></i> <span class="hide-menu"><?php echo $item->{"name_" . (empty($this->session->userdata("user_profile")->cng_lang) ? 'th' : $this->session->userdata("user_profile")->cng_lang)}; ?></span></a>
                            <?php if ($child > 0) { ?>
                                <ul aria-expanded="false" class="collapse first-level menu<?php echo $item->id; /* use identity Class (menu.$id) for Remove parent  */ ?>">
                                    <?php
                                    $menu[] = $item->id; /* (2) Variable for get identity Class ID */
                                    $sql_first_level = "SELECT id, sort, icon, name_th, name_en, name_jp, name_cn, module
                                FROM   sys_module
                                WHERE     (parent_module_id = " . $item->id . ") AND (is_active = 1) AND (record_status = 'N')
                                ORDER BY sort";
                                    $query_first_level = $this->db->query($sql_first_level);
                                    $nrows_first_level = $query_first_level->num_rows();
                                    if ($nrows_first_level > 0) {
                                        foreach ($query_first_level->result() as $first_level) {
                                            if (@$this->efs_lib->is_can($this->efs_lib->get_permission($first_level->module), "view") == false) {
                                                continue;
                                            }

                                    ?>
                                            <li class="sidebar-item" data-id="<?php echo $first_level->id; ?>">
                                                <a class="sidebar-link" title="<?php echo $first_level->{"name_" . $this->session->userdata("user_profile")->cng_lang}; ?>" href="<?php echo base_url($first_level->module); ?>">
                                                    <i class="<?php echo $first_level->icon; ?>"></i>
                                                    <?php echo readMoreHelper($first_level->{"name_" . $this->session->userdata("user_profile")->cng_lang}, 22) ?>
                                                </a>
                                            </li>
                                <?php
                                        }
                                    }
                                }
                                ?>
                                </ul>
                        </li>
                <?php
                    }
                }
                ?>
            </ul>
        </nav>
        <!-- End Sidebar navigation -->
    </div>
    <!-- Bottom points-->
    <div class="sidebar-footer">
        <!-- item-->
        <a href="<?php echo base_url("main/profile/setting"); ?>" class="link new-link" data-toggle="tooltip" title="<?php echo @$lang_sys->sys_config_profile; ?>">
            <i id="kk" class="ti-settings"></i>
            <?php if (empty($this->session->userdata("user_profile")->email)) { ?>
                <span class="new bg-danger"></span>
                <span class="new bg-danger"><?php echo @$lang_sys->sys_alert_mail; ?></span>
            <?php } ?>
        </a>
        <!-- item-->
        <a class="link" data-toggle="tooltip" title="<?php echo @$lang_sys->sys_change_password; ?>" href="javascript:void(0);"><span data-toggle="modal" data-backdrop="static" data-keyboard="false" data-target="#changePasswordModal"><i class="ti-key"></i></span></a>
        <!-- item-->
        <a href="javascript:void(0)" data-href="<?php echo base_url('authen/logout'); ?>" class="link logout" data-toggle="tooltip" title="<?php echo @$lang_sys->sys_logout; ?>"><i class="mdi mdi-power" style="color: #dc3545;"></i></a>
    </div>
    <style>
        .sidebar-footer a:hover {
            background-color: #c5e8fc;
        }
    </style>
    <?php if (empty($this->session->userdata("user_profile")->email)) { ?>
        <style>
            .new-link {
                position: relative;
                animation: moveing 1s infinite;
            }

            /* a.new-link { */
            i#kk {
                animation: rotate 2s infinite;
            }

            @keyframes moveing {
                50% {
                    opacity: 0;
                }

                100% {
                    opacity: 1.8;
                }
            }

            @keyframes rotate {
                0% {
                    transform: rotate(0deg);
                }

                100% {
                    transform: rotate(360deg);
                }
            }
        </style>
    <?php } ?>
    <!-- End Bottom points-->
</aside>
<script>
    <?php foreach ($menu as $i) { ?>
        /* (3) Variable for get identity Class ID Remove parent li */
        if ($('.menu<?php echo $i; ?>').children().length == 0) {
            $('.menu<?php echo $i; ?>').parent().remove();
        }
    <?php } ?>
    $(document).ready(function() {
      
    });
</script>