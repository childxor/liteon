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
    <link rel="stylesheet" type="text/css" href="<?php echo base_url(); ?>assets/libs/bootstrap-switch/dist/css/bootstrap3/bootstrap-switch.min.css">
    <div class="row">
        <div class="col-12">
            <div class="card">
                <div class="card-body">
                    <div class="text-right"></div>
                    <div class="table-responsive m-t-40">
                        <table id="mTable" class="table table-bordered table-striped table-hover datatable" style="width:100%">
                            <thead>
                                <tr>
                                    <th><?php echo $lang_module->lbl_datetime; ?></th>
                                    <th><?php echo $lang_module->lbl_username; ?></th>
                                    <th><?php echo $lang_module->lbl_ip; ?></th>
                                    <th><?php echo $lang_module->lbl_module; ?></th>
                                    <th class="no-sort text-center"><i class="fas fa-search"></i></th>
                                </tr>
                            </thead>
                            <tbody></tbody>
                        </table>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div class="modal fade" id="viewModal" tabindex="-1" role="dialog">
        <div class="modal-dialog modal-lg" role="document">
            <div class="modal-content">
                <div class="modal-header">
                    <h4 class="modal-title" id="addModalLabel"><?php echo $lang_module->lbl_detail; ?></h4>
                    <button type="button" class="close" data-dismiss="modal" aria-label="Close"><span aria-hidden="true">&times;</span></button>
                </div>
                <div class="modal-body">
                    <input type="hidden" name="id">
                    <div class="modal-body">
                        <div class="form-group">
                            <label for="datetime" class="control-label"><?php echo $lang_module->lbl_datetime; ?></label>
                            <div class="controls">
                                <input type="text" class="form-control" name="datetime" readonly autocomplete="off">
                            </div>
                        </div>
                        <div class="form-group">
                            <label for="username" class="control-label"><?php echo $lang_module->lbl_username; ?></label>
                            <div class="controls">
                                <input type="text" class="form-control" name="username" readonly autocomplete="off">
                            </div>
                        </div>
                        <div class="form-group">
                            <label for="ip" class="control-label"><?php echo $lang_module->lbl_ip; ?></label>
                            <div class="controls">
                                <input type="text" class="form-control" name="ip" readonly autocomplete="off">
                            </div>
                        </div>
                        <div class="form-group">
                            <label for="os" class="control-label"><?php echo $lang_module->lbl_os; ?></label>
                            <div class="controls">
                                <input type="text" class="form-control" name="os" readonly autocomplete="off">
                            </div>
                        </div>
                        <div class="form-group">
                            <label for="browser" class="control-label"><?php echo $lang_module->lbl_browser; ?></label>
                            <div class="controls">
                                <textarea name="browser" readonly class="form-control"></textarea>
                            </div>
                        </div>
                        <div class="form-group">
                            <label for="module" class="control-label"><?php echo $lang_module->lbl_module; ?></label>
                            <div class="controls">
                                <input type="text" class="form-control" name="page" readonly autocomplete="off">
                            </div>
                        </div>
                        <div class="form-group">
                            <label for="query" class="control-label"><?php echo $lang_module->lbl_query; ?></label>
                            <div class="controls">
                                <textarea name="query" readonly class="form-control"></textarea>
                            </div>
                        </div>
                        <div class="form-group">
                            <label for="remark" class="control-label"><?php echo $lang_module->lbl_remark; ?></label>
                            <div class="controls">
                                <textarea name="remark" readonly class="form-control"></textarea>
                            </div>
                        </div>
                    </div>
                </div>
                <div class="modal-footer">
                    <button type="reset" class="btn btn-default" data-dismiss="modal"><?php echo $lang_module->lbl_close; ?></button>
                </div>
            </div>
        </div>
    </div>
    <!-- /.modal -->


    <!-- ============================================================== -->
    <!-- End Page Content -->
    <!-- ============================================================== -->
    <script src="<?php echo base_url(); ?>assets/libs/jquery/dist/jquery.min.js"></script>

    <!-- ============================================================== -->
    <!-- This page plugins -->
    <!-- ============================================================== -->

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
                "serverSide": true,
                "stateSave": false,
                "processing": true,
                "pageLength": <?php echo $this->session->userdata('user_profile')->cng_per_page; ?>,
                "bSort": false,
                "language": {
                    "url": "<?php echo base_url() ?>dist/js/pages/datatable/<?php echo $this->session->userdata('user_profile')->cng_lang; ?>.json",
                    "headers": {
                        "Access-Control-Allow-Origin": "*"
                    },
                    searchPlaceholder: "<?php echo $lang_sys->lbl_search; ?>",
                },
                "ajax": "<?php echo base_url('sys/log/ajax_list'); ?>",
                "deferRender": true,
                "aLengthMenu": [
                    [10, 25, 50, 100],
                    [10, 25, 50, 100]
                ],
                "columns": [{
                        "data": "datetime"
                    },
                    {
                        "data": {
                            "username": "username",
                            "emp_code": "emp_code",
                            "department_name": "department_name",
                            "first_name": "first_name",
                            "last_name": "last_name"
                        },
                        render: function(data, type) {

                            return (data.emp_code != null ? data.prefix_name + data.first_name + ' ' + data.last_name + ' (' + data.department_name + ':' + data.username + ')' : data.username);
                        }
                    },
                    {
                        "data": "ip"
                    },
                    {
                        "data": "page"
                    },
                    {
                        "data": {
                            "id": "id"
                        },
                        render: function(data, type) {
                            return '<button type="button" class="btn btn-info btn-xs" id="viewBtn" data-id="' + data.id + '" data-toggle="modal" data-backdrop="static" data-keyboard="false" data-target="#viewModal"><i class="fas fa-search"></i></button>';
                        }
                    },
                ],
                "columnDefs": [{
                        orderable: false,
                        targets: '_all'
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
            });

        }(window, document, jQuery);
        $(document).ready(function() {
            $('#mTable tbody').on('click', 'tr td #viewBtn', function() {
                $.post("<?php echo base_url('sys/log/ajax_data'); ?>", {
                        id: $(this).data('id')
                    },
                    function(data) {
                        $('#viewModal').find('[name="datetime"]').val(data['datetime']);
                        $('#viewModal').find('[name="username"]').val(data['username']);
                        $('#viewModal').find('[name="ip"]').val(data['ip']);
                        $('#viewModal').find('[name="os"]').val(data['os']);
                        $('#viewModal').find('[name="browser"]').val(data['browser']);
                        $('#viewModal').find('[name="page"]').val(data['page']);
                        $('#viewModal').find('[name="query"]').val(JSON.parse(unescapeHtml(data['query'])));
                        $('#viewModal').find('[name="remark"]').val(data['remark']);
                    }, "json");
            });

        });

        function unescapeHtml(safe) {
            return safe.replace(/&amp;/g, '&')
                .replace(/&lt;/g, '<')
                .replace(/&gt;/g, '>')
                .replace(/&quot;/g, '"')
                .replace(/&#039;/g, "'");
        }
        $(function() {
            <?php if ($this->session->flashdata('msg')) { ?>
                toastr.<?php echo $this->session->flashdata('type') ?>('<?php echo $this->session->flashdata('type') ?>', '<?php echo $this->session->flashdata('msg') ?>', "top-right", "#ff6849", "5000");
            <?php } ?>
        });
    </script>
<?php
} /* Can View */ ?>