<?php /* var_dump($permission); */ ?>
<style></style>
<div class="row">
    <!-- Column -->
    <div class="col-lg-4 col-xlg-3 col-md-5">
        <div class="card">
            <div class="card-body">
                <center class="mt-4">
                    <div class="avatar-modal-circle">
                        <?php echo $this->session->userdata('user_profile')->department_description; ?>
                    </div>
                    <h4 class="card-title mt-2"> <?php echo $this->session->userdata('user_profile')->first_name . '  ' . $this->session->userdata('user_profile')->last_name; ?></h4>
                    <h6 class="card-subtitle"><?php echo $lang_sys->sys_department; ?> : <?php echo $this->session->userdata('user_profile')->department_name; ?></h6>
                    <h6 class="card-subtitle"><?php echo $lang_sys->sys_sub_department; ?> : <?php echo $this->session->userdata('user_profile')->sub_department_name; ?></h6>
                    <h6 class="card-subtitle"><?php echo $lang_sys->sys_username; ?> : <?php echo $this->session->userdata('user_profile')->username; ?></h6>
                    <style>
                        .card-subtitle:hover {
                            background-color: #f5f5f5;
                            font-size: <?php echo $this->session->userdata('user_profile')->cng_font_size; ?>px;
                        }
                    </style>
                </center>
            </div>
            <div>
                <hr>
            </div>
            <div class="card-body">
                <small class="text-muted"><?php echo $lang_sys->sys_email; ?></small>
                <h6><?php echo $this->session->userdata('user_profile')->email; ?></h6>
                <small class="text-muted pt-4 db"><?php echo $lang_sys->sys_tel; ?></small>
                <h6><?php echo $this->session->userdata('user_profile')->tel; ?></h6>
                <small class="text-muted pt-4 db"><?php echo $lang_sys->sys_roles; ?></small>
                <h6><?php echo $role; ?></h6>
            </div>
        </div>
    </div>
    <!-- Column -->
    <!-- Column -->
    <style>
        .nav-item:hover {
            background-color: #f5f5f5;
        }
    </style>
    <div class="col-lg-8 col-xlg-9 col-md-7">
        <div class="card">
            <!-- Tabs -->

            <ul class="nav nav-pills custom-pills" id="pills-tab" role="tablist">
                <li class="nav-item">
                    <a class="nav-link " id="pills-timeline-tab" data-toggle="pill" href="#timeline" role="tab" aria-controls="pills-timeline" aria-selected="true"><?php echo $lang_sys->sys_timeline; ?></a>
                </li>
                <li class="nav-item">
                    <a class="nav-link active" id="pills-setting-tab" data-toggle="pill" href="#setting" role="tab" aria-controls="pills-setting" aria-selected="false"><?php echo $lang_sys->sys_config_profile; ?></a>
                </li>
                <li class="nav-item">
                    <a class="nav-link" id="pills-setting-tab" data-toggle="pill" href="#settingText" role="tab" aria-controls="pills-setting" aria-selected="false"><?php echo $lang_sys->sys_config_text; ?></a>
                </li>
            </ul>
            <!-- Tabs -->
            <div class="tab-content" id="pills-tabContent">
                <div class="tab-pane fade show " id="timeline" role="tabpanel" aria-labelledby="pills-timeline-tab">
                    <div class="card-body scroll-sidebar" style="height: 500px; overflow-y: scroll;">
                        <ul class="timeline">
                            <?php
                            $sql_log = "SELECT TOP(50) * FROM sys_log WHERE username ='" . $this->session->userdata('user_profile')->username . "' ORDER BY datetime DESC ";
                            foreach ($this->db->query($sql_log)->result() as $key => $log) {
                                $inverted = ($key % 2 ? 'timeline-inverted' : '');
                                $type_log = $this->log_lib->get_type_log($log->remark);
                            ?>
                                <li class="<?php echo $inverted; ?> timeline-item">
                                    <div class="timeline-badge <?php echo $type_log['color']; ?>"><span class="font-14"><i class="ti <?php echo $type_log['icon']; ?>"></i></span></div>
                                    <div class="timeline-panel">
                                        <div class="timeline-heading">
                                            <h6><small class="text-muted"><i class="ti ti-agenda"></i> <?php echo $log->remark; ?></small> </h6>
                                            <h6><small class="text-muted"><i class="ti ti-time"></i> <?php echo $log->datetime; ?></small> </h6>
                                            <h6><small class="text-muted"><i class="ti ti-<?php echo (strtolower($log->device) !== 'computer' ? 'mobile' : 'desktop'); ?>"></i> <?php echo $log->os; ?> (IP:<?php echo $log->ip; ?>) </small></h6>
                                            <h6><small class="text-muted"><i class="ti ti-cloud"></i> <?php echo $log->browser; ?></small> </h6>
                                        </div>
                                    </div>
                                </li>
                            <?php } ?>
                        </ul>
                    </div>
                </div>
                <div class="tab-pane fade show active" id="setting" role="tabpanel" aria-labelledby="pills-setting-tab">
                    <div class="card-body">
                        <form class="form-horizontal form-material" method="post" action="<?php echo base_url('main/edit_profile'); ?>">
                            <h4 class="card-title"><?php echo $lang_sys->sys_personal_info; ?></h4>
                            <div class="row" style="vertical-align: middle;">
                                <!-- <div style="width: 100%;margin: 10px auto;"> -->
                                <div class="form-group col-2">
                                    <label class="col-md-12 mt-2"><?php echo $lang_sys->sys_prefix_name; ?></label>
                                    <div class="col-md-12">
                                        <select class="form-control validated" name="prefix_name" required style="width:100%" data-validation-required-message="<?php echo $lang_sys->msg_require; ?>">
                                            <option value=""></option>
                                            <option value="Mr." <?php echo ($this->session->userdata('user_profile')->prefix_name == 'Mr.' ? 'selected' : ''); ?>>Mr.</option>
                                            <option value="Mrs." <?php echo ($this->session->userdata('user_profile')->prefix_name == 'Mrs.' ? 'selected' : ''); ?>>Mrs.</option>
                                            <option value="Ms." <?php echo ($this->session->userdata('user_profile')->prefix_name == 'Ms.' ? 'selected' : ''); ?>>Ms.</option>
                                            <option value="นาย" <?php echo ($this->session->userdata('user_profile')->prefix_name == 'นาย' ? 'selected' : ''); ?>>นาย</option>
                                            <option value="นาย" <?php echo ($this->session->userdata('user_profile')->prefix_name == 'นาย' ? 'selected' : ''); ?>>นาย</option>
                                            <option value="น.ส." <?php echo ($this->session->userdata('user_profile')->prefix_name == 'น.ส.' ? 'selected' : ''); ?>>น.ส.</option>
                                            <option value="นาง" <?php echo ($this->session->userdata('user_profile')->prefix_name == 'นาง' ? 'selected' : ''); ?>>นาง</option>
                                        </select>
                                    </div>
                                </div>
                                <div class="form-group col-5">
                                    <label class="col-md-12 mt-2"><?php echo $lang_sys->sys_first_name; ?></label>
                                    <div class="col-md-12">
                                        <input type="text" value="<?php echo $this->session->userdata('user_profile')->first_name; ?>" placeholder="<?php echo $this->session->userdata('user_profile')->first_name; ?>" class="form-control form-control-line" name="first_name" required autocomplete="off">
                                    </div>
                                </div>
                                <div class="form-group col-5">
                                    <label class="col-md-12 mt-2"><?php echo $lang_sys->sys_last_name; ?></label>
                                    <div class="col-md-12">
                                        <input type="text" value="<?php echo $this->session->userdata('user_profile')->last_name; ?>" placeholder="<?php echo $this->session->userdata('user_profile')->last_name; ?>" class="form-control form-control-line" name="last_name" required autocomplete="off">
                                    </div>
                                </div>
                                <!-- </div> -->
                            </div>
                            <div class="form-group">
                                <label class="col-md-12 mt-2"><?php echo $lang_sys->sys_tel; ?> <i class="fa fa-info-circle" data-toggle="tooltip" data-placement="top" title="<?php echo $lang_sys->sys_setting_tel_info; ?>"></i></label>
                                <div class="col-md-6">
                                    <input type="text" value="<?php echo $this->session->userdata('user_profile')->tel; ?>" placeholder="<?php echo $this->session->userdata('user_profile')->tel; ?>" class="form-control form-control-line" name="tel" autocomplete="off">
                                </div>
                            </div>
                            <div class="form-group">
                                <label for="example-email" class="col-md-12 mt-2"><?php echo $lang_sys->sys_email; ?> <i class="fa fa-info-circle" data-toggle="tooltip" data-placement="top" title="<?php echo $lang_sys->sys_setting_mail_info; ?>"></i></label>
                                <div class="col-md-6">
                                    <input type="email" value="<?php echo $this->session->userdata('user_profile')->email; ?>" placeholder="<?php echo $this->session->userdata('user_profile')->email; ?>" class="form-control form-control-line" name="email" autocomplete="off">
                                </div>
                            </div>
                            <!-- </form> -->
                    </div>
                </div>
                <div class="tab-pane fade" id="settingText" role="tabpanel" aria-labelledby="pills-setting-tab">
                    <div class="form-group card-body">
                        <h4 class="card-title"><?php echo $lang_sys->sys_personal_setting; ?></h4>
                        <div class="row">
                            <div class="col-md-6">
                                <div class="form-group">
                                    <label class="col-md-12"><?php echo $lang_sys->sys_home_page; ?></label>
                                    <div class="col-md-12">
                                        <select class="form-control form-control-line" id="module_id" name="default_module_id" style="width:100%">
                                            <option value="" selected></option>
                                            <?php
                                            $sql = "SELECT * FROM sys_module WHERE record_status='N' AND parent_module_id ='0' ORDER BY sort ASC";
                                            $parents = $this->db->query($sql)->result();
                                            ?>
                                            <?php foreach ($parents as $parent) { ?>
                                                <?php
                                                $sql = "SELECT DISTINCT module_id, module_parent, name_th, name_en, name_jp "
                                                    . "FROM vw_permission "
                                                    . "WHERE module_parent='" . $parent->id . "' AND (user_id = '" . $this->session->userdata('user_profile')->id . "') ";
                                                $nrows = $this->db->query($sql)->num_rows();
                                                if ($nrows > 0) {
                                                    $subs = $this->db->query($sql)->result();
                                                ?>
                                                    <optgroup label="<i class='ti <?php echo $parent->icon; ?>'></i> <?php echo $parent->{"name_" . $this->session->userdata("user_profile")->cng_lang}; ?>">
                                                        <?php foreach ($subs as $sub) { ?>
                                                            <option id="opt_<?php echo $sub->module_id; ?>" value="<?php echo $sub->module_id; ?>" <?php echo ($this->session->userdata("user_profile")->default_module_id == $sub->module_id ? 'selected' : ''); ?>><?php echo $sub->{"name_" . $this->session->userdata("user_profile")->cng_lang}; ?></option>
                                                        <?php } ?>
                                                    <?php } ?>
                                                    </optgroup>
                                                <?php } ?>
                                        </select>
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="col-md-12"><?php echo $lang_sys->sys_font_size; ?></label>
                                    <div class="col-md-12">
                                        <select class="form-control form-control-line" name="cng_font_size" required style="width:100%">
                                            <option value="6" <?php echo ($this->session->userdata('user_profile')->cng_font_size == '6' ? 'selected' : ''); ?>>6px</option>
                                            <option value="8" <?php echo ($this->session->userdata('user_profile')->cng_font_size == '8' ? 'selected' : ''); ?>>8px</option>
                                            <option value="10" <?php echo ($this->session->userdata('user_profile')->cng_font_size == '10' ? 'selected' : ''); ?>>10px</option>
                                            <option value="12" <?php echo ($this->session->userdata('user_profile')->cng_font_size == '12' ? 'selected' : ''); ?>>12px</option>
                                            <option value="13" <?php echo ($this->session->userdata('user_profile')->cng_font_size == '13' ? 'selected' : ''); ?>>13px</option>
                                            <option value="14" <?php echo ($this->session->userdata('user_profile')->cng_font_size == '14' ? 'selected' : ''); ?>>14px</option>
                                            <option value="16" <?php echo ($this->session->userdata('user_profile')->cng_font_size == '16' ? 'selected' : ''); ?>>16px</option>
                                            <option value="18" <?php echo ($this->session->userdata('user_profile')->cng_font_size == '18' ? 'selected' : ''); ?>>18px</option>
                                        </select>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div class="form-group">
                                    <label class="col-md-12"><?php echo $lang_sys->sys_table_font_size; ?></label>
                                    <div class="col-md-12">
                                        <select class="form-control form-control-line" name="cng_table_font_size" required style="width:100%">
                                            <option value="6" <?php echo ($this->session->userdata('user_profile')->cng_table_font_size == '6' ? 'selected' : ''); ?>>6px</option>
                                            <option value="8" <?php echo ($this->session->userdata('user_profile')->cng_table_font_size == '8' ? 'selected' : ''); ?>>8px</option>
                                            <option value="10" <?php echo ($this->session->userdata('user_profile')->cng_table_font_size == '10' ? 'selected' : ''); ?>>10px</option>
                                            <option value="12" <?php echo ($this->session->userdata('user_profile')->cng_table_font_size == '12' ? 'selected' : ''); ?>>12px</option>
                                            <option value="13" <?php echo ($this->session->userdata('user_profile')->cng_table_font_size == '13' ? 'selected' : ''); ?>>13px</option>
                                            <option value="14" <?php echo ($this->session->userdata('user_profile')->cng_table_font_size == '14' ? 'selected' : ''); ?>>14px</option>
                                            <option value="16" <?php echo ($this->session->userdata('user_profile')->cng_table_font_size == '16' ? 'selected' : ''); ?>>16px</option>
                                            <option value="18" <?php echo ($this->session->userdata('user_profile')->cng_table_font_size == '18' ? 'selected' : ''); ?>>18px</option>
                                        </select>
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="col-md-12"><?php echo $lang_sys->sys_view_per_page; ?></label>
                                    <div class="col-md-12">
                                        <select class="form-control form-control-line" name="cng_per_page" required style="width:100%">
                                            <option value="10" <?php echo ($this->session->userdata('user_profile')->cng_per_page == '10' ? 'selected' : ''); ?>>10</option>
                                            <option value="25" <?php echo ($this->session->userdata('user_profile')->cng_per_page == '25' ? 'selected' : ''); ?>>25</option>
                                            <option value="50" <?php echo ($this->session->userdata('user_profile')->cng_per_page == '50' ? 'selected' : ''); ?>>50</option>
                                            <option value="100" <?php echo ($this->session->userdata('user_profile')->cng_per_page == '100' ? 'selected' : ''); ?>>100</option>
                                        </select>
                                    </div>
                                </div>
                            </div>
                            <div class="col-md-6">
                                <div class="form-group">
                                    <label class="col-md-12"><?php echo $lang_sys->sys_language; ?></label>
                                    <div class="col-md-12">
                                        <select class="form-control form-control-line" name="cng_lang" required style="width:100%">
                                            <option value="th" <?php echo ($this->session->userdata('user_profile')->cng_lang == 'th' ? 'selected' : ''); ?>>ไทย</option>
                                            <option value="en" <?php echo ($this->session->userdata('user_profile')->cng_lang == 'en' ? 'selected' : ''); ?>>English</option>
                                            <option value="jp" <?php echo ($this->session->userdata('user_profile')->cng_lang == 'jp' ? 'selected' : ''); ?>>Japanese</option>
                                        </select>
                                    </div>
                                </div>
                                <div class="form-group">
                                    <label class="col-md-12"><?php echo $lang_sys->sys_notification; ?></label>
                                    <div class="col-md-12">
                                        <select class="form-control form-control-line" name="cng_alert_time" required style="width:100%">
                                            <option value="1" <?php echo ($this->session->userdata('user_profile')->cng_alert_time == '1' ? 'selected' : ''); ?>><?php echo $lang_sys->sys_realtime; ?></option>
                                            <option value="15" <?php echo ($this->session->userdata('user_profile')->cng_alert_time == '15' ? 'selected' : ''); ?>>15 <?php echo $lang_sys->sys_sec; ?></option>
                                            <option value="30" <?php echo ($this->session->userdata('user_profile')->cng_alert_time == '30' ? 'selected' : ''); ?>>30 <?php echo $lang_sys->sys_sec; ?></option>
                                            <option value="60" <?php echo ($this->session->userdata('user_profile')->cng_alert_time == '60' ? 'selected' : ''); ?>>1 <?php echo $lang_sys->sys_minute; ?></option>
                                            <option value="120" <?php echo ($this->session->userdata('user_profile')->cng_alert_time == '120' ? 'selected' : ''); ?>>2 <?php echo $lang_sys->sys_minute; ?></option>
                                            <option value="300" <?php echo ($this->session->userdata('user_profile')->cng_alert_time == '300' ? 'selected' : ''); ?>>5 <?php echo $lang_sys->sys_minute; ?></option>
                                            <option value="28800" <?php echo ($this->session->userdata('user_profile')->cng_alert_time == '28800' ? 'selected' : ''); ?>><?php echo $lang_sys->sys_not_notify; ?></option>
                                        </select>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="form-group text-left">
                    <div class="col-sm-12">
                        <button class="btn btn-success" type="submit"><i class="fa fa-save"></i> <?php echo $lang_sys->sys_btn_save; ?></button>
                        <!-- <button class="btn btn-secondary" type="button" onclick="resetForm()"><i class="fa fa-refresh fa-spin" style="font-size:24px"></i> <?php echo $lang_sys->sys_btn_reset; ?></button> -->
                        <button class="btn btn-secondary" type="button" onclick="resetForm()"><i class="fa fa-trash"></i> <?php echo $lang_sys->sys_btn_reset; ?></button>
                    </div>
                </div>
                </form> <!-- ฟอร์มบันทึก ตั้งใจทำให้ครอบคลุมทุก input -->
            </div>
        </div>
    </div>
    <!-- Column -->
</div>
<!-- Row -->

<script src="<?php echo base_url(); ?>assets/libs/jquery/dist/jquery.min.js"></script>

<!-- ============================================================== -->
<!-- This page plugins -->
<!-- ============================================================== -->
<!--Custom JavaScript -->
<script type="text/javascript">
    var create_icon = $('#module_id');

    ! function(window, document, $) {
        "use strict";
    }(window, document, jQuery);

    $(document).ready(function() {
        $('.tree ul').fadeIn();
        $('#setting select, #settingText select').select2({
            escapeMarkup: function(m) {
                return m;
            },
            placeholder: "",
            // allowClear: true
        });
        create_icon = $('#module_id').select2({
            escapeMarkup: function(m) {
                return m;
            },
            placeholder: "",
            // allowClear: true
        });
    });
    ! function(window, document, $) {
        "use strict";
    }(window, document, jQuery);

    function resetForm() {
        $('select[name="prefix_name"]').val(<?php echo "'" . $this->session->userdata('user_profile')->prefix_name . "'"; ?>).trigger('change');
        $('input[name="first_name"]').val(<?php echo "'" . $this->session->userdata('user_profile')->first_name . "'"; ?>);
        $('input[name="last_name"]').val(<?php echo "'" . $this->session->userdata('user_profile')->last_name . "'"; ?>);
        $('input[name="tel"]').val(<?php echo "'" . $this->session->userdata('user_profile')->tel . "'"; ?>);
        $('input[name="email"]').val(<?php echo "'" . $this->session->userdata('user_profile')->email . "'"; ?>);
        $('select[name="default_module_id"]').val(<?php echo "'" . $this->session->userdata('user_profile')->default_module_id . "'"; ?>).trigger('change');
        $('select[name="cng_font_size"]').val(<?php echo "'" . $this->session->userdata('user_profile')->cng_font_size . "'"; ?>).trigger('change');
        $('select[name="cng_table_font_size"]').val(<?php echo "'" . $this->session->userdata('user_profile')->cng_table_font_size . "'"; ?>).trigger('change');
        $('select[name="cng_per_page"]').val(<?php echo "'" . $this->session->userdata('user_profile')->cng_per_page . "'"; ?>).trigger('change');
        $('select[name="cng_lang"]').val(<?php echo "'" . $this->session->userdata('user_profile')->cng_lang . "'"; ?>).trigger('change');
        $('select[name="cng_alert_time"]').val(<?php echo "'" . $this->session->userdata('user_profile')->cng_alert_time . "'"; ?>).trigger('change');
        toastr.options = {
            "closeButton": true,
            "debug": false,
            "newestOnTop": true,
            "progressBar": true,
            "positionClass": "toast-top-right",
            "preventDuplicates": false,
            "onclick": null,
            "showDuration": "300",
            "hideDuration": "1000",
            "timeOut": "1500",
            "extendedTimeOut": "1000",
            "showEasing": "swing",
            "hideEasing": "linear",
            "showMethod": "fadeIn",
            "hideMethod": "fadeOut"
        };
        toastr.success('Reset form success.', 'Success', "top-right", "#ff6849", "5000");
    }
    $(function() {
        <?php
        /* Active tab setting */
        if ($this->uri->segment(3) == 'setting') {
        ?>
            $('#pills-setting-tab').click();
        <?php
        }
        ?>
        <?php if ($this->session->flashdata('msg')) { ?>
            toastr.<?php echo $this->session->flashdata('type') ?>('<?php echo $this->session->flashdata('type') ?>', '<?php echo $this->session->flashdata('msg') ?>', "top-right", "#ff6849", "5000");
        <?php } ?>

    });
</script>