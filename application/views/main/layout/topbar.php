<link href="<?php echo base_url('dist/css/marquee.css'); ?>" rel="stylesheet">
<style>
    .tooltip-inner {
        max-width: 800px;
        /* the minimum width */
    }

    /* Avatar User */
    .avatar-circle {
        width: 40px;
        height: 40px;
        border-radius: 50%;
        font-size: 18px !important;
        color: #fff;
        line-height: 40px;
        text-align: center;
        background: <?php echo $this->session->userdata('user_profile')->color; ?>
    }

    .avatar-modal-circle {
        width: 80px;
        height: 80px;
        border-radius: 50%;
        font-size: 50px !important;
        color: #fff;
        line-height: 80px;
        text-align: center;
        background: <?php echo $this->session->userdata('user_profile')->color; ?>;
        margin-left: auto;
        margin-right: auto;
    }

    /* Profile Setting */
    .dataTables_wrapper {
        font-size: <?php echo $this->session->userdata('user_profile')->cng_table_font_size; ?>px !important;
    }

    .dataTables_wrapper th {
        font-weight: bold !important;
    }

    .page-breadcrumb .breadcrumb {
        font-size: <?php echo $this->session->userdata('user_profile')->cng_font_size; ?>px !important;
    }

    .sidebar-link,
    .marquee-sibling {
        font-size: <?php echo $this->session->userdata('user_profile')->cng_font_size; ?>px !important;
    }

    p,
    input {
        font-size: <?php echo $this->session->userdata('user_profile')->cng_font_size; ?>px !important;
    }

    .dropdown-menu {
        padding: 0.5rem 0.5rem !important;
        font-size: <?php echo $this->session->userdata('user_profile')->cng_font_size; ?>px !important;
    }

    .table td {
        padding: 0.3rem !important;
    }

    .btn {
        padding: 0.2rem 0.6rem !important;
        border-radius: 4px !important;
    }

    /* END Profile Setting */

    /* Define the hover highlight color for the table row */
    .table-hover tbody>tr:hover {
        background-color: #9fa7ff52 !important;
    }
</style>
<header class="topbar">
    <nav class="navbar top-navbar navbar-expand-md navbar-dark">
        <div class="navbar-header">
            <!-- This is for the sidebar toggle which is visible on mobile only -->
            <a class="nav-toggler waves-effect waves-light d-block d-md-none" href="javascript:void(0)"><i class="ti-menu ti-close"></i></a>
            <!-- ============================================================== -->
            <!-- Logo -->
            <!-- ============================================================== -->
            <a class="navbar-brand" href="<?php echo base_url(); ?>">
                <!-- Logo icon -->
                <b class="logo-icon">
                    <!--You can put here icon as well // <i class="wi wi-sunset"></i> //-->
                    <!-- Dark Logo icon -->
                    <img src="<?php echo base_url($var->logo_icon); ?>" alt="<?php echo $var->project; ?>" class="logo d-none" />
                </b>
                <!--End Logo icon -->
                <!-- Logo text -->
                <span class="logo-text">
                    <!-- dark Logo text -->
                    <img src="<?php echo base_url('assets/images/logo-text.png'); ?>" alt="<?php echo $var->project; ?>" class="logo" style="width: 180px" />
                </span>
            </a>
            <!-- ============================================================== -->
            <!-- End Logo -->
            <!-- ============================================================== -->
            <!-- ============================================================== -->
            <!-- Toggle which is visible on mobile only -->
            <!-- ============================================================== -->
            <a class="topbartoggler d-block d-md-none waves-effect waves-light" href="javascript:void(0)" data-toggle="collapse" data-target="#navbarSupportedContent" aria-controls="navbarSupportedContent" aria-expanded="false" aria-label="Toggle navigation"><i class="ti-more"></i></a>
        </div>
        <!-- ============================================================== -->
        <!-- End Logo -->
        <!-- ============================================================== -->
        <div class="navbar-collapse collapse" id="navbarSupportedContent">
            <!-- ============================================================== -->
            <!-- toggle and nav items -->
            <!-- ============================================================== -->
            <ul class="navbar-nav float-left mr-auto">
                <li class="nav-item d-none d-md-block"><a class="nav-link sidebartoggler waves-effect waves-light" href="javascript:void(0)" data-sidebartype="mini-sidebar"><i class="icon-arrow-left-circle"></i></a></li>
                <!-- ============================================================== -->
                <!-- Messages -->
                <!-- ============================================================== -->
                <?php $notify = $this->efs_lib->get_notification(); ?>
                <li class="nav-item dropdown d-none">
                    <a class="nav-link dropdown-toggle waves-effect waves-dark" href="" id="2" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false"> <i class="mdi mdi-email"></i>
                        <?php echo ($notify->nrow > 0 ? "<div class='notify'> <span class='heartbit'></span> <span class='point'></span> </div>" : ""); ?>

                    </a>
                    <div class="dropdown-menu mailbox animated bounceInDown" aria-labelledby="2">
                        <ul class="list-style-none">
                            <li>
                                <div class="font-weight-medium border-bottom rounded-top py-3 px-4">
                                    ข่าวประกาศ
                                </div>
                            </li>
                            <li>
                                <div class="message-center message-body position-relative">
                                    <!-- Message -->
                                    <?php
                                    if ($notify->nrow > 0) {
                                        $txt = "";
                                        $sql_announcement = "SELECT TOP(5) * FROM mas_announcement "
                                            . "WHERE (record_status = 'N') AND is_active = '1' ORDER BY show DESC";
                                        $announcement_rows = $this->db->query($sql_announcement)->num_rows();
                                        if ($announcement_rows > 0) {
                                            echo '<pre>';
                                            print_r($this->db->query($sql_announcement)->result());
                                            echo '</pre>';

                                            foreach ($this->db->query($sql_announcement)->result() as $announce) {
                                    ?>
                                                <a href="javascript:void(0)" class="message-item d-flex align-items-center border-bottom px-3 py-2">
                                                    <span class="btn btn-info rounded-circle btn-circle"><i class="fa fa-newspaper"></i></span>
                                                    <div class="w-75 d-inline-block v-middle pl-2">
                                                        <h5 class="message-title mb-0 mt-1"><?php echo $announce->title; ?></h5> <span class="font-12 text-nowrap d-block text-muted"><?php echo $this->efs_lib->datetime_to_th($announce->show, false); ?></span>
                                                    </div>
                                                </a>
                                    <?php
                                            }
                                        }
                                        echo $txt;
                                    } else {
                                        echo '<span class="font-12 text-center d-block text-muted">--- ไม่มีข้อความใหม่ ---</span>';
                                    }
                                    ?>
                                </div>
                            </li>
                            <li>
                                <a class="nav-link border-top text-center text-dark pt-3" href="javascript:void(0);"> <b>ดูประกาศทั้งหมด</b> <i class="fa fa-angle-right"></i> </a>
                            </li>
                        </ul>
                    </div>
                </li>
                <!-- ============================================================== -->
                <!-- End Messages -->
                <!-- ============================================================== -->
            </ul>
            <!-- ============================================================== -->
            <!-- Announcement -->
            <!-- ============================================================== -->
            <!-- <div class="simple-marquee-container d-none d-md-block d-md-block"> -->

            <div class="marquee-sibling">
                <i class="ti-announcement"></i>
            </div>
            <div class="simple-marquee-container" style="margin-right: 25px;">
                <div class="marquee">
                    <?php
                    $txt = "";
                    $sql_announcement = "SELECT TOP(10) title, txt FROM mas_announcement "
                        . "WHERE (record_status = 'N') AND is_active = 1 "
                        . "AND ({ fn NOW() } BETWEEN show AND hide) ORDER BY show DESC";
                    $announcement_rows = $this->db->query($sql_announcement)->num_rows();
                    if ($announcement_rows > 0) {
                        foreach ($this->db->query($sql_announcement)->result() as $announce) {

                            $txt .= "  #" . $announce->title . "    " . strip_tags($announce->txt) . "                                        ";
                        }
                    }
                    echo $txt;
                    ?>
                </div>
            </div>


            <!-- ============================================================== -->
            <!-- End Announcement -->
            <!-- ============================================================== -->
            <!-- ============================================================== -->
            <!-- Right side toggle and nav items -->
            <!-- ============================================================== -->
            <ul class="navbar-nav float-right">
                <li class="nav-item dropdown">
                    <a class="dropdown-toggle text-muted waves-effect waves-dark pro-pic" href="" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                        <div class="d-flex no-block align-items-center p-2 border-bottom">
                            <div class="">
                                <div class=" avatar-circle">
                                    <?php echo $this->session->userdata('user_profile')->department_description; ?>
                                </div>
                            </div>
                            <div class="ml-2">
                                <h5 class="mb-1"><?php echo $this->session->userdata('user_profile')->first_name; ?> <?php echo $this->session->userdata('user_profile')->last_name; ?></h5>
                                <h6 class="text-white"><?php echo $this->session->userdata('user_profile')->sub_department_name; ?> <?php echo (!empty($this->session->userdata('user_profile')->position_name) ? '(' . $this->session->userdata('user_profile')->position_name . ')' : ''); ?> </h6>
                            </div>
                        </div>
                    </a>
                    <div class="dropdown-menu dropdown-menu-right user-dd animated flipInY">
                        <div class="d-flex no-block align-items-center p-3 mb-2 border-bottom">
                            <a href="<?php echo base_url("main/profile"); ?>" class="btn btn-rounded btn-info btn-sm"><?php echo @$lang_sys->sys_view_profile; ?></a>
                        </div>
                        <a class="dropdown-item" href="<?php echo base_url("main/profile/setting"); ?>"><i class="ti-settings mr-1 ml-1"></i> <?php echo @$lang_sys->sys_config_profile; ?></a>
                        <a id="changePasswordBtn" class="dropdown-item" data-toggle="modal" data-backdrop="static" data-keyboard="false" data-target="#changePasswordModal" href="javascript:void(0);"><i class="ti-key mr-1 ml-1"></i> <?php echo @$lang_sys->sys_change_password; ?></a>
                        <div class="dropdown-divider"></div>
                        <a class="dropdown-item logout" href="javascript:void(0)" data-href="<?php echo base_url("authen/logout"); ?>"><i class="fa fa-power-off mr-1 ml-1"></i> <?php echo @$lang_sys->sys_logout; ?></a>
                    </div>
                </li>
                <!-- ============================================================== -->
                <!-- User profile -->
                <!-- ============================================================== -->
                <!-- ============================================================== -->
                <!-- Multiple Language Soon -->
                <!-- ============================================================== -->
                <li class="nav-item dropdown">
                    <a class="nav-link dropdown-toggle" href="#" id="navbarDropdown2" role="button" data-toggle="dropdown" aria-haspopup="true" aria-expanded="false">
                        <i class="flag-icon flag-icon-<?php echo ($this->session->userdata('user_profile')->cng_lang == 'en' ? 'gb' : $this->session->userdata('user_profile')->cng_lang); ?>"></i>
                    </a>
                    <div class="dropdown-menu dropdown-menu-right  animated bounceInDown" aria-labelledby="navbarDropdown2">
                        <a class="dropdown-item switch_language <?php echo ($this->session->userdata('user_profile')->cng_lang == 'th' ? 'active' : ''); ?>" href="<?php echo base_url("main/change_language/th"); ?>" data-id="th"><i class="flag-icon flag-icon-th"></i> ไทย</a>
                        <a class="dropdown-item switch_language <?php echo ($this->session->userdata('user_profile')->cng_lang == 'en' ? 'active' : ''); ?>" href="<?php echo base_url("main/change_language/en"); ?>" data-id="en"><i class="flag-icon flag-icon-gb"></i> English</a>
                        <a class="dropdown-item switch_language <?php echo ($this->session->userdata('user_profile')->cng_lang == 'cn' ? 'active' : ''); ?>" href="<?php echo base_url("main/change_language/cn"); ?>" data-id="cn"><i class="flag-icon flag-icon-cn"></i> Chinese</a>
                    </div>
                </li>
            </ul>
        </div>
    </nav>
</header>
<?php if ($this->session->userdata('user_profile')->id) { ?>
    <div class="modal fade" id="changePasswordModal" tabindex="-1" role="dialog" aria-labelledby="changePasswordModalLabel">
        <form id="changePasswordForm" class="form-horizontal form-material" method="post" action="<?php echo base_url('main/changePassword'); ?>" novalidate>
            <div class="modal-dialog" role="document">
                <div class="modal-content">
                    <div class="modal-body">
                        <button id="btnClose" type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                        <div class="logo text-center">
                            <div class="db">
                                <div class="avatar-modal-circle mb-1">
                                    <?php echo $this->session->userdata('user_profile')->department_description; ?>
                                </div>
                            </div>
                            <h5 class="font-weight-medium mb-3"><?php echo $this->session->userdata('user_profile')->first_name . '  ' . $this->session->userdata('user_profile')->last_name; ?></h5>
                            <h6 id="txtExpired" class="font-weight-medium mb-3 text-muted"><?php echo $lang_sys->sys_password_expired; ?> : <?php echo date('Y-m-d H:i:s', strtotime($this->session->userdata('expired_passwd'))); ?></h6>
                        </div>
                        <div class="form-group">
                            <div class="controls">
                                <input type="password" placeholder="<?php echo $lang_sys->sys_current_password; ?>" class="form-control form-control-line validated" id="old_password" name="old_password" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>" data-validation-ajax-ajax="<?php echo base_url('main/check_current_password'); ?>" autocomplete="off">
                            </div>
                        </div>
                        <div class="form-group">
                            <div class="controls">
                                <input type="password" minlength="4" placeholder="<?php echo $lang_sys->sys_new_password; ?>" class="form-control form-control-line validated" name="new_password" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>" data-validation-minlength-message="<?php echo $lang_sys->msg_min_char_4; ?>" autocomplete="off">
                            </div>
                        </div>
                        <div class="form-group">
                            <div class="controls">
                                <input type="password" minlength="4" placeholder="<?php echo $lang_sys->sys_re_new_password; ?>" class="form-control form-control-line validated" name="re_new_password" required data-validation-required-message="<?php echo $lang_sys->msg_require; ?>" data-validation-match-match="new_password" data-validation-match-message="<?php echo $lang_sys->msg_re_new_password; ?>" data-validation-minlength-message="<?php echo $lang_sys->msg_min_char_4; ?>" autocomplete="off">
                            </div>
                        </div>
                        <div class="form-group text-center">
                            <div class="col-xs-12 pb-3">
                                <button class="btn btn-block btn-info" type="submit"><?php echo $lang_sys->sys_change_password; ?></button>
                            </div>
                            <div class="col-xs-12 pb-3">
                                <a id="btnLogout" class="dropdown-item logout d-none" href="javascript:void(0)" data-href="<?php echo base_url("authen/logout"); ?>"><i class="fa fa-power-off mr-1 ml-1"></i> <?php echo @$lang_sys->sys_logout; ?></a>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </form>
    </div>
    <!-- /.modal -->
<?php } ?>
<script src="<?php echo base_url(); ?>assets/libs/jquery/dist/jquery.min.js"></script>

<script type="text/javascript">
    $(function() {
        $("#changePasswordModal").on('show.bs.modal', function() {
            $('#changePasswordForm').trigger("reset");
            var $form = $('#changePasswordForm');
            <?php if ($this->efs_lib->check_expired_date($this->session->userdata('expired_passwd')) === TRUE) { ?>
                $('#btnClose').addClass('d-none');
                $('#btnLogout').removeClass('d-none');
            <?php } else { ?>
                $('#btnClose').removeClass('d-none');
                $('#btnLogout').addClass('d-none');
            <?php } ?>
            $form.find('.error,.valid').css('border-color', '').removeClass('error').removeClass('valid');
            $form.find('.form-error').remove();
            $form.find('.help-block').html('');
            setTimeout(function() {
                $('#old_password').focus();
                $('#txtExpired').removeClass('text-danger');
                $('#txtExpired').addClass('text-mute');
            }, 500);
        });

        <?php if ($this->efs_lib->check_expired_date($this->session->userdata('expired_passwd')) === TRUE) { ?>
            $('#changePasswordBtn').click();
            $('#txtExpired').addClass('text-danger');
            $('#txtExpired').removeClass('text-muted');
            setTimeout(function() {
                $('#old_password').focus();
            }, 500);
        <?php } ?>
    });
</script>