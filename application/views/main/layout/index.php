<!DOCTYPE html>
<html dir="ltr">
<?php
/* Get timestamp */
$timestamp = strtotime("now");
?>

<head>
    <meta charset="utf-8">
    <meta http-equiv="X-UA-Compatible" content="IE=edge">

    <!-- Clear cache Browser -->
    <meta http-equiv='cache-control' content='no-cache'>
    <meta http-equiv='expires' content='0'>
    <meta http-equiv='pragma' content='no-cache'>

    <!-- Tell the browser to be responsive to screen width -->
    <meta name="viewport" content="width=device-width, initial-scale=1">
    <meta name="description" content="">
    <meta name="author" content="">
    <!-- Favicon icon -->
    <link rel="shortcut icon" type="image/x" href="<?php echo base_url('assets/images/favicon.ico'); ?>">
    <title><?php echo $var->project; ?></title>
    <link href="<?php echo base_url(); ?>assets/libs/datatables.net-bs4/css/dataTables.bootstrap4.css" rel="stylesheet" type="text/css">
    <link href="<?php echo base_url(); ?>assets/libs/bootstrap-switch/dist/css/bootstrap3/bootstrap-switch.min.css" rel="stylesheet" type="text/css">
    <!-- Tostr CSS -->
    <link href="<?php echo base_url(); ?>assets/extra-libs/toastr/dist/build/toastr.min.css" rel="stylesheet">
    <!-- Sweet Alert CSS -->
    <link href="<?php echo base_url(); ?>assets/libs/sweetalert2/dist/sweetalert2.min.css" rel="stylesheet">
    <!-- Datatable CSS -->
    <link href="<?php echo base_url(); ?>assets/libs/datatables/media/css/rowReorder.dataTables.min.css" rel="stylesheet">
    <link href="<?php echo base_url(); ?>assets/libs/datatables/media/css/rowGroup.dataTables.min.css" rel="stylesheet">
    <script src="<?php echo base_url(); ?>assets/libs/bootstrap-material-datetimepicker/js/bootstrap-material-datetimepicker-custom.js"></script>

    <link href="<?php echo base_url(); ?>assets/libs/FontAwesome-Free-5.15.3-Web-master/FontAwesome-Free-5.15.3-Web-master/css/all.min.css" rel="stylesheet">
    <link href="<?php echo base_url(); ?>assets/libs/FontAwesome-Free-5.15.3-Web-master/FontAwesome-Free-5.15.3-Web-master/css/all.css" rel="stylesheet">

    <!-- Select2 CSS -->
    <link rel="stylesheet" type="text/css" href="<?php echo base_url(); ?>assets/libs/select2/dist/css/select2.min.css">
    <!-- Datetime CSS -->
    <link rel="stylesheet" type="text/css" href="<?php echo base_url(); ?>assets/libs/daterangepicker/daterangepicker.css">
    <!-- Custom CSS -->
    <link href="<?php echo base_url(); ?>dist/css/style.css" rel="stylesheet">
    <!-- HTML5 Shim and Respond.js IE8 support of HTML5 elements and media queries -->


    <!-- WARNING: Respond.js doesn't work if you view the page via file:// -->
    <!--[if lt IE 9]>
        <script src="https://oss.maxcdn.com/libs/html5shiv/3.7.0/html5shiv.js?stmp=<?php echo $timestamp; ?>"></script>
        <script src="https://oss.maxcdn.com/libs/respond.js/1.4.2/respond.min.js?stmp=<?php echo $timestamp; ?>"></script>
    <![endif]-->
</head>

<body>
    <!-- ============================================================== -->
    <!-- Preloader - style you can find in spinners.css -->
    <!-- ============================================================== -->
    <div class="preloader">
        <div class="lds-ripple">
            <div class="lds-pos"></div>
            <div class="lds-pos"></div>
        </div>
    </div>
    <!-- ============================================================== -->
    <!-- Main wrapper - style you can find in pages.scss -->
    <!-- ============================================================== -->
    <div id="main-wrapper">
        <!-- ============================================================== -->
        <!-- Topbar header - style you can find in pages.scss -->
        <!-- ============================================================== -->
        <?php (isset($topbar) ? $this->load->view($topbar) : ''); ?>
        <!-- ============================================================== -->
        <!-- End Topbar header -->
        <!-- ============================================================== -->
        <!-- ============================================================== -->
        <!-- Left Sidebar - style you can find in sidebar.scss  -->
        <!-- ============================================================== -->
        <?php (isset($sidebar) ? $this->load->view($sidebar) : ''); ?>
        <!-- ============================================================== -->
        <!-- End Left Sidebar - style you can find in sidebar.scss  -->
        <!-- ============================================================== -->
        <!-- ============================================================== -->
        <!-- Page wrapper  -->
        <!-- ============================================================== -->
        <div class="page-wrapper">
            <!-- ============================================================== -->
            <!-- Bread crumb and right sidebar toggle -->
            <!-- ============================================================== -->
            <?php echo (isset($breadcrumb) ? $breadcrumb : ''); ?>
            <!-- ============================================================== -->
            <!-- End Bread crumb and right sidebar toggle -->
            <!-- ============================================================== -->
            <!-- ============================================================== -->
            <!-- Container fluid  -->
            <!-- ============================================================== -->
            <div class="container-fluid">
                <!-- ============================================================== -->
                <!-- Start Page Content -->
                <!-- ============================================================== -->
                <?php (isset($module) ? $this->load->view($module) : ''); ?>
                <!-- ============================================================== -->
                <!-- End Page Content -->
                <!-- ============================================================== -->
            </div>
            <!-- ============================================================== -->
            <!-- End Container fluid  -->
            <!-- ============================================================== -->
            <!-- ============================================================== -->
            <!-- footer -->
            <!-- ============================================================== -->
            <?php (isset($footer) ? $this->load->view($footer) : ''); ?>
            <!-- ============================================================== -->

            <!-- End footer -->
            <!-- ============================================================== -->
            <!-- ============================================================== -->
            <!-- Announcement -->
            <!-- ============================================================== -->
            <?php (isset($announcement) ? $this->load->view($announcement) : ''); ?>
            <!-- ============================================================== -->
        </div>
        <!-- ============================================================== -->
        <!-- End Page wrapper  -->
        <!-- ============================================================== -->
    </div>
    <!-- ============================================================== -->
    <!-- End Wrapper -->
    <!-- ============================================================== -->
    <!-- ============================================================== -->
    <!-- All Jquery -->
    <!-- ============================================================== -->

    <script src="<?php echo base_url(); ?>assets/libs/jquery/dist/jquery.min.js?stmp=<?php echo $timestamp; ?>"></script>

    <!-- Bootstrap tether Core JavaScript -->
    <script src="<?php echo base_url(); ?>assets/libs/popper.js/dist/umd/popper.min.js?stmp=<?php echo $timestamp; ?>"></script>
    <script src="<?php echo base_url(); ?>assets/libs/bootstrap/dist/js/bootstrap.min.js?stmp=<?php echo $timestamp; ?>"></script>
    <!-- apps -->
    <script src="<?php echo base_url(); ?>dist/js/app.min.js?stmp=<?php echo $timestamp; ?>"></script>
    <script src="<?php echo base_url(); ?>dist/js/app.init.js?stmp=<?php echo $timestamp; ?>"></script>
    <!-- slimscrollbar scrollbar JavaScript -->
    <script src="<?php echo base_url(); ?>assets/libs/perfect-scrollbar/dist/perfect-scrollbar.jquery.min.js?stmp=<?php echo $timestamp; ?>"></script>
    <script src="<?php echo base_url(); ?>assets/libs/jquery-sparkline/jquery.sparkline.min.js?stmp=<?php echo $timestamp; ?>"></script>
    <!--Wave Effects -->
    <script src="<?php echo base_url(); ?>dist/js/waves.js?stmp=<?php echo $timestamp; ?>"></script>
    <!--Menu sidebar -->
    <script src="<?php echo base_url(); ?>dist/js/sidebarmenu.js?stmp=<?php echo $timestamp; ?>"></script>
    <!--Custom JavaScript -->
    <script src="<?php echo base_url(); ?>dist/js/custom.js?stmp=<?php echo $timestamp; ?>"></script>
    <!-- Datatable Page JS -->
    <script src="<?php echo base_url(); ?>assets/libs/datatables/media/js/jquery.dataTables.min.js?stmp=<?php echo $timestamp; ?>"></script>
    <script src="<?php echo base_url(); ?>dist/js/pages/datatable/custom-datatable.js?stmp=<?php echo $timestamp; ?>"></script>
    <script src="<?php echo base_url(); ?>assets/libs/datatables/media/js/dataTables.rowReorder.min.js?stmp=<?php echo $timestamp; ?>"></script>
    <script src="<?php echo base_url(); ?>assets/libs/datatables/media/js/dataTables.rowGroup.min.js?stmp=<?php echo $timestamp; ?>"></script>
    <!-- Toastr JS -->
    <script src="<?php echo base_url(); ?>assets/extra-libs/toastr/dist/build/toastr.min.js?stmp=<?php echo $timestamp; ?>"></script>
    <!-- Sweet Alert JS -->
    <script src="<?php echo base_url(); ?>assets/libs/sweetalert2/dist/sweetalert2.all.min.js?stmp=<?php echo $timestamp; ?>"></script>
    <script src="<?php echo base_url(); ?>assets/extra-libs/sweetalert2/sweet-alert.init.js?stmp=<?php echo $timestamp; ?>"></script>
    <!-- Select2 JS -->
    <script src="<?php echo base_url(); ?>assets/libs/select2/dist/js/select2.full.min.js?stmp=<?php echo $timestamp; ?>"></script>
    <script src="<?php echo base_url(); ?>assets/libs/select2/dist/js/select2.min.js?stmp=<?php echo $timestamp; ?>"></script>
    <script src="<?php echo base_url(); ?>dist/js/pages/forms/select2/select2.init.js?stmp=<?php echo $timestamp; ?>"></script>
    <!-- Minicolors JS -->
    <link rel="stylesheet" type="text/css" href="<?php echo base_url(); ?>assets/libs/@claviska/jquery-minicolors/jquery.minicolors.css">
    <script src="<?php echo base_url(); ?>assets/libs/jquery-asColor/dist/jquery-asColor.min.js?stmp=<?php echo $timestamp; ?>"></script>
    <script src="<?php echo base_url(); ?>assets/libs/jquery-asGradient/dist/jquery-asGradient.js?stmp=<?php echo $timestamp; ?>"></script>
    <script src="<?php echo base_url(); ?>assets/libs/jquery-asColorPicker/dist/jquery-asColorPicker.min.js?stmp=<?php echo $timestamp; ?>"></script>
    <script src="<?php echo base_url(); ?>assets/libs/@claviska/jquery-minicolors/jquery.minicolors.min.js?stmp=<?php echo $timestamp; ?>"></script>
    <script src="<?php echo base_url(); ?>assets/extra-libs/jqbootstrapvalidation/validation.js?stmp=<?php echo $timestamp; ?>"></script>

    <!-- Marquee JS -->
    <script src="<?php echo base_url(); ?>dist/js/marquee.js?stmp=<?php echo $timestamp; ?>"></script>

    <!-- typeahead JS -->
    <script src="<?php echo base_url(); ?>assets/libs/typeahead.js/dist/typeahead.jquery.js?stmp=<?php echo $timestamp; ?>"></script>
    <script src="<?php echo base_url(); ?>dist/js/pages/forms/typeahead/typeahead.init.js?stmp=<?php echo $timestamp; ?>"></script>

    <!-- Datetime JS -->
    <script src="<?php echo base_url(); ?>assets/libs/moment/moment.js?stmp=<?php echo $timestamp; ?>"></script>
    <script src="<?php echo base_url(); ?>assets/libs/daterangepicker/daterangepicker.js?stmp=<?php echo $timestamp; ?>"></script>

    <!-- Summernote JS -->
    <script src="<?php echo base_url(); ?>assets/libs/summernote/dist/summernote-bs4.min.js?stmp=<?php echo $timestamp; ?>"></script>

    <!-- ZXing barcode JS -->
    <script src="<?php echo base_url(); ?>assets/extra-libs/zxing-barcode/zxing.js?stmp=<?php echo $timestamp; ?>"></script>

    <!-- This Savy JS -->
    <script src="<?php echo base_url(); ?>dist/js/savy.min.js?stmp=<?php echo $timestamp; ?>"></script>



    <script type="text/javascript">
        $('.auto-save').savy('load', function() {
            console.log("All data from savy are loaded");
        });

        function savy_destroy() {
            $('.auto-save').savy('destroy', function() {
                console.log("All data from savy are Destroyed");
                window.location.reload();
            });
        }

        $("body").tooltip({
            selector: '[data-toggle=tooltip]'
        });

        /************************************/
        //default editor
        /************************************/
        $('.summernote').summernote({
            height: 150, // set editor height
            minHeight: null, // set minimum height of editor
            maxHeight: null, // set maximum height of editor
            focus: false // set focus to editable area after initializing summernote
        });


        /*******************************************/
        /*  Date & Time                            */
        /*******************************************/
        $('.datetime').daterangepicker({
            timePicker: true,
            timePicker24Hour: true,
            timePickerSeconds: false,
            locale: {
                firstDay: 1,
                format: 'YYYY-MM-DD H:mm'
            }
        });

        $('.daterange').daterangepicker({
            startDate: moment().startOf('month'),
            endDate: moment().endOf('month'),
            locale: {
                firstDay: 1,
                format: 'YYYY-MM-DD'
            }
        });

        /* 
         * Load Language For JS 
         * โหลด ภาษาในระบบ ใช้สำหรับ จาวาสคริป
         */
        var lang = null;
        //var url = window.location.origin;
        $.ajax({
            url: '<?php echo base_url("main/get_language/js"); ?>',
            async: false,
            /* this is the important line that makes the request sincronous */
            type: 'post',
            dataType: 'json',
            success: function(data) {
                lang = data;
            }
        });

        $(function() {
            $(".validated").jqBootstrapValidation();
            $('.simple-marquee-container').SimpleMarquee();
        });
        $('.colorpicker').each(function() {
            $(this).minicolors({
                control: $(this).attr('data-control') || 'hue',
                defaultValue: $(this).attr('data-defaultValue') || '',
                format: $(this).attr('data-format') || 'hex',
                keywords: $(this).attr('data-keywords') || '',
                inline: $(this).attr('data-inline') === 'true',
                letterCase: $(this).attr('data-letterCase') || 'lowercase',
                opacity: $(this).attr('data-opacity'),
                position: $(this).attr('data-position') || 'bottom left',
                swatches: $(this).attr('data-swatches') ? $(this).attr('data-swatches').split('|') : [],
                change: function(value, opacity) {
                    if (!value)
                        return;
                    if (opacity)
                        value += ', ' + opacity;
                    if (typeof console === 'object') {
                        console.log(value);
                    }
                },
                theme: 'bootstrap'
            });
        });

        /* กดปุ่ม ESC เพื่อปิดหน้าต่าง MODAL */
        $(document).keydown(function(e) {
            var code = e.keyCode || e.which;
            if (code == 27) $(".modal").modal('hide');
        });

        /* Change All fonts size */
        /*
         $("*").children().each(function () {
         var size = parseInt($(this).css("font-size"));
         size = "14px";
         $(this).css({
         'font-size': size
         });
         });
         */
    </script>
</body>

</html>

<?php

function readMoreHelper($story_desc, $chars = 50)
{
    if (getStrLenTH($story_desc) <= $chars)
        return $story_desc;
    $story_desc = getSubStrTH($story_desc, 0, $chars);
    $story_desc = $story_desc . " ...";
    return $story_desc;
}

// Get string length for Character Thai
function getStrLenTH($string)
{
    $array = getMBStrSplit($string);
    $count = 0;

    foreach ($array as $value) {
        $ascii = ord(iconv("UTF-8", "TIS-620//IGNORE", $value));

        if (!($ascii == 209 ||  ($ascii >= 212 && $ascii <= 218) || ($ascii >= 231 && $ascii <= 238))) {
            $count += 1;
        }
    }
    return $count;
}

// Convert a string to an array with multibyte string
function getMBStrSplit($string, $split_length = 1)
{
    mb_internal_encoding('UTF-8');
    mb_regex_encoding('UTF-8');

    $split_length = ($split_length <= 0) ? 1 : $split_length;
    $mb_strlen = mb_strlen($string, 'utf-8');
    $array = array();
    $i = 0;

    while ($i < $mb_strlen) {
        $array[] = mb_substr($string, $i, $split_length);
        $i = $i + $split_length;
    }

    return $array;
}

// Get part of string for Character Thai
function getSubStrTH($string, $start, $length)
{
    $length = ($length + $start) - 1;
    $array = getMBStrSplit($string);
    $count = 0;
    $return = "";

    for ($i = $start; $i < count($array); $i++) {
        $ascii = ord(iconv("UTF-8", "TIS-620//IGNORE", $array[$i]));

        if ($ascii == 209 ||  ($ascii >= 212 && $ascii <= 218) || ($ascii >= 231 && $ascii <= 238)) {
            //$start++;
            $length++;
        }

        if ($i >= $start) {
            $return .= $array[$i];
        }

        if ($i >= $length)
            break;
    }

    return $return;
}
?>