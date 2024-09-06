<?php /* var_dump($permission); */ ?>
<?php (empty($permission) ? redirect(base_url("error404")) : ""); ?>
<?php if (!empty($this->efs_lib->is_can($permission, "view"))) { /* Can View */ ?>
    <!-- ============================================================== -->
    <!-- Start Page Content -->
    <!-- ============================================================== -->
    <style>
        #mTable {
            max-width: 100%;
            white-space: nowrap;
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
                        <?php if ($this->efs_lib->is_can($permission, "add")) { ?>
                            <a id="addBtn" class="btn btn-primary mb-2" href="<?php echo base_url('sys/role/add_role'); ?>"><span><i class="fa fa-plus"></i> <?php echo $lang_module->lbl_add; ?></span></a>
                        <?php } ?>
                    </div>
                    <div class="table-responsive m-t-40">
                        <table id="mTable" class="table table-bordered table-striped table-hover datatable" style="width:100%">
                            <thead>
                                <tr>
                                    <th><?php echo $lang_module->lbl_order; ?></th>
                                    <th><?php echo $lang_module->lbl_role; ?></th>
                                    <th><?php echo $lang_module->lbl_detail; ?></th>
                                    <th class="no-sort text-center"><i class="fas fa-bars"></i></th>
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

    <!-- ============================================================== -->
    <!-- End Page Content -->
    <!-- ============================================================== -->
    <script src="<?php echo base_url(); ?>assets/libs/jquery/dist/jquery.min.js"></script>

    <!-- ============================================================== -->
    <!-- This page plugins -->
    <!-- ============================================================== -->
    <!-- This Page JS -->
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
                "stateSave": true,
                "rowReorder": true,
                "processing": true,
                "bSort": false,
                "language": {
                    "url": "<?php echo base_url() ?>dist/js/pages/datatable/<?php echo $this->session->userdata('user_profile')->cng_lang; ?>.json",
                    "headers": {
                        "Access-Control-Allow-Origin": "*"
                    },
                    searchPlaceholder: "<?php echo $lang_sys->lbl_search; ?>",
                },
                "ajax": "<?php echo base_url('sys/role/ajax_list'); ?>",
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
                            "name": "name"
                        },
                        render: function(data, type, row, meta) {
                            return '<span style="text-transform:capitalize;">' + data.name + '</span>';
                        }
                    },
                    {
                        "data": "description"
                    },
                    {
                        "data": {
                            "id": "id"
                        },
                        render: function(data, type) {
                            return '<a href="<?php echo base_url('sys/role/permission/'); ?>' + data.id + '" class="btn btn-info btn-xs"><i class="fas fa-align-justify"></i></a>';
                        }
                    },
                    <?php if ($this->efs_lib->is_can($permission, "edit")) { ?> {
                            "data": {
                                "id": "id"
                            },
                            render: function(data, type) {
                                return '<a href="<?php echo base_url('sys/role/edit_role/'); ?>' + data.id + '" class="btn btn-warning btn-xs"><i class="fas fa-pencil-alt"></i></a>';
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
                                    return '<button type="button" class="btn btn-danger btn-xs confirmDelete" data-href="<?php echo base_url('sys/role/delete/'); ?>' + data.id + '"><i class="fas fa-trash-alt"></i></button>';
                                }
                            }
                        },
                    <?php } ?>
                ],
                "columnDefs": [{
                        orderable: true,
                        className: 'reorder',
                        targets: 0
                    },
                    {
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
            });
            
            provTable.on('row-reorder', function(e, diff, edit) {
                for (var i = 0, ien = diff.length; i < ien; i++) {
                    var rowData = provTable.row(diff[i].node).data();
                    $.post('<?php echo base_url('sys/role/sort'); ?>', {
                        newPos: diff[i].newPosition + 1,
                        id: rowData.id
                    }, function(data) {});
                }
                reloadTable();
            });

            function reloadTable() {
                setTimeout(provTable.ajax.reload, 100);
            }
        }(window, document, jQuery);
        $(function() {
            <?php if ($this->session->flashdata('msg')) { ?>
                toastr.<?php echo $this->session->flashdata('type') ?>('<?php echo $this->session->flashdata('type') ?>', '<?php echo $this->session->flashdata('msg') ?>', "top-right", "#ff6849", "5000");
            <?php } ?>

        });
    </script>
<?php
} /* Can View */ ?>