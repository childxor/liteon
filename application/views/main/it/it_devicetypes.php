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
            width: 90px !important;
        }
    </style>

    <div class="card">
        <div class="card-body">
            <form id="searchForm">
                <div class="row mb-4">
                    <div class="col-md-4 col-lg-3 mb-3">
                        <label for="name" class="form-label">
                            <i class="fas fa-desktop"></i> <?php echo $lang_module->lbl_device_type_name; ?>
                        </label>
                        <input type="text" class="form-control" id="name" name="name">
                    </div>
                    <div class="col-md-4 col-lg-3 mb-3">
                        <label for="is_active" class="form-label">
                            <i class="fas fa-toggle-on"></i> <?php echo $lang_module->lbl_status; ?>
                        </label>
                        <select class="form-control" id="is_active" name="is_active">
                            <option value=""><?php echo $lang_module->lbl_all_statuses; ?></option>
                            <option value="1"><?php echo $lang_module->lbl_active; ?></option>
                            <option value="0"><?php echo $lang_module->lbl_deleted; ?></option>
                        </select>
                    </div>
                </div>
                <div class="row mb-4">
                    <div class="col-md-4 col-lg-3 mb-3">
                        <button type="submit" class="btn btn-primary btn-block">
                            <i class="fas fa-search"></i> <?php echo $lang_module->lbl_search; ?>
                        </button>
                    </div>
                    <?php if ($this->efs_lib->is_can($permission, "add")) : ?>
                        <div class="col-md-4 col-lg-3 mb-3">
                            <button type="button" id="addBtn" class="btn btn-success btn-block" data-toggle="modal" data-target="#deviceTypeModal">
                                <i class="fas fa-plus"></i> <?php echo $lang_module->lbl_add; ?>
                            </button>
                        </div>
                    <?php endif; ?>
                </div>
            </form>

            <div class="table-responsive">
                <table id="deviceTypeTable" class="table table-bordered table-striped table-hover" style="width:100%"></table>
            </div>
        </div>
    </div>

    <?php if ($this->efs_lib->is_can($permission, "edit")) { ?>
        <!-- Device Type Modal -->
        <div class="modal fade" id="deviceTypeModal" tabindex="-1" role="dialog" aria-labelledby="deviceTypeModalLabel" aria-hidden="true">
            <div class="modal-dialog" role="document">
                <div class="modal-content">
                    <div class="modal-header bg-primary text-white">
                        <h5 class="modal-title" id="deviceTypeModalLabel"><i class="fas fa-desktop mr-2"></i><span id="modalTitle">Add Device Type</span></h5>
                        <button type="button" class="close text-white" data-dismiss="modal" aria-label="Close">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body">
                        <form id="deviceTypeForm">
                            <input type="hidden" id="id" name="id">
                            <div class="form-group">
                                <label for="deviceTypeName"><i class="fas fa-tag mr-2"></i>Name</label>
                                <input type="text" class="form-control" id="deviceTypeName" name="name" required>
                            </div>
                            <div class="form-group">
                                <label for="deviceTypeDescription"><i class="fas fa-info-circle mr-2"></i>Description</label>
                                <textarea class="form-control" id="deviceTypeDescription" name="description" rows="3"></textarea>
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-dismiss="modal"><i class="fas fa-times mr-2"></i>Cancel</button>
                                <button type="submit" class="btn btn-primary"><i class="fas fa-save mr-2"></i>Save</button>
                            </div>
                        </form>
                    </div>
                </div>
            </div>
        </div>
    <?php } ?>

    <!-- ============================================================== -->
    <!-- End Page Content -->
    <!-- ============================================================== -->
    <script src="<?php echo base_url(); ?>assets/libs/jquery/dist/jquery.min.js"></script>
    <!-- This page plugins -->
    <script src="<?php echo base_url(); ?>assets/extra-libs/jqbootstrapvalidation/validation.js"></script>
    <script src="<?php echo base_url(); ?>assets/libs/bootstrap-switch/dist/js/bootstrap-switch.min.js"></script>

    <!-- Datatable Page JS -->
    <script src="<?php echo base_url(); ?>assets/libs/datatables/media/js/jquery.dataTables.min.js"></script>
    <script src="<?php echo base_url(); ?>dist/js/pages/datatable/custom-datatable.js"></script>

    <script type="text/javascript">
        var deviceTypeTable;

        ! function(window, document, $) {
            "use strict";
            $.fn.dataTable.ext.errMode = 'none';
            deviceTypeTable = $('#deviceTypeTable').DataTable({
                "autoWidth": false,
                "pageLength": <?php echo $this->session->userdata('user_profile')->cng_per_page; ?>,
                "processing": true,
                "serverSide": false,
                "bSort": true,
                "stateSave": false,
                "language": {
                    "url": "<?php echo base_url() ?>dist/js/pages/datatable/<?php echo $this->session->userdata('user_profile')->cng_lang; ?>.json",
                    "headers": {
                        "Access-Control-Allow-Origin": "*"
                    },
                    searchPlaceholder: "<?php echo $lang_sys->lbl_search; ?>",
                },
                "ajax": {
                    "url": "<?php echo base_url('it/it_devicetypes/getDeviceTypes'); ?>",
                    "type": "POST",
                    "data": function(data) {
                        data.name = $('#name').val();
                        data.is_active = $('#is_active').val();
                    }
                },
                "columns": [{
                        "data": null,
                        "className": "text-center",
                        "width": "20px",
                        "orderable": false,
                        "title": "<?php echo $lang_module->lbl_order; ?>",
                        "render": function(data, type, row, meta) {
                            return meta.row + 1;
                        }
                    },
                    {
                        "data": "name",
                        "className": "text-left",
                        "title": "<?php echo $lang_module->lbl_device_type_name; ?>"
                    },
                    {
                        "data": "description",
                        "className": "text-left",
                        "title": "<?php echo $lang_module->lbl_description; ?>"
                    },
                    {
                        "data": "is_active",
                        "className": "text-center",
                        "title": "<?php echo $lang_module->lbl_status; ?>",
                        "render": function(data, type, row, meta) {
                            var isActive = row.is_active;
                            // ถ้า is_active เป็น 1 ให้เลือก
                            var html = '<div class="custom-control custom-switch">' +
                                '<input type="checkbox" class="isActive custom-control-input" ' + (isActive ? 'checked' : '') + ' id="' + row.cardNumber + 'Sh" data-personID="' + row.cardNumber + '-3">' +
                                '<label class="custom-control-label" for="' + row.cardNumber + 'Sh"></label>' +
                                '</div>';
                            return html;
                        }
                    },
                    <?php if ($this->efs_lib->is_can($permission, "edit")) { ?> {
                            "data": "id",
                            "className": "text-center",
                            "orderable": false,
                            "title": "<i class='mdi mdi-settings'></i>",
                            "width": "100px",
                            "render": function(data, type, row, meta) {
                                var editBtn = '<button class="editDeviceType btn btn-sm btn-warning mr-1" data-id="' + row.id + '"><i class="fas fa-edit"></i></button>';
                                var deleteBtn = '<button class="deleteDeviceType btn btn-sm btn-danger" data-id="' + row.id + '"><i class="fas fa-trash"></i></button>';
                                return editBtn + deleteBtn;
                            }
                        },
                    <?php } ?>
                ],
                "order": [
                    [0, "asc"]
                ]
            });
        }(window, document, jQuery);

        $(document).ready(function() {
            $('#searchForm').submit(function(e) {
                e.preventDefault();
                deviceTypeTable.ajax.reload();
            });

            $('#is_active').change(function() {
                deviceTypeTable.column(3).search($(this).val()).draw();
            });

            $('#deviceTypeForm').submit(function(e) {
                e.preventDefault();
                var formData = $(this).serialize();
                $.ajax({
                    url: '<?php echo base_url('it/it_devicetypes/saveDeviceType'); ?>',
                    type: 'POST',
                    data: formData,
                    success: function(response) {
                        $('#deviceTypeModal').modal('hide');
                        deviceTypeTable.ajax.reload();
                        swal.fire('Success', response.success, 'success');
                    },
                    error: function() {
                        swal.fire('Error', 'An error occurred while saving the device type', 'error');
                    }
                });
            });

            $(document).on('click', '.editDeviceType', function() {
                var id = $(this).data('id');
                var row = deviceTypeTable.row($(this).closest('tr')).data();
                $('#id').val(row.id);
                $('#deviceTypeName').val(row.name);
                $('#deviceTypeDescription').val(row.description);
                $('#modalTitle').text('Edit Device Type');
                $('#deviceTypeModal').modal('show');
            });

            $(document).on('click', '.deleteDeviceType', function() {
                var id = $(this).data('id');
                swal.fire({
                    title: 'Are you sure?',
                    text: "You won't be able to revert this!",
                    icon: 'warning',
                    showCancelButton: true,
                    confirmButtonColor: '#3085d6',
                    cancelButtonColor: '#d33',
                    confirmButtonText: 'Yes, delete it!'
                }).then((result) => {
                    if (result.isConfirmed) {
                        $.ajax({
                            url: '<?php echo base_url('it/it_devicetypes/deleteDeviceType/'); ?>' + id,
                            type: 'POST',
                            success: function(response) {
                                deviceTypeTable.ajax.reload();
                                swal.fire('Deleted!', 'Device type has been deleted.', 'success');
                            }
                        });
                    }
                });
            });

            $('#addBtn').click(function() {
                $('#deviceTypeForm')[0].reset();
                $('#id').val('');
                $('#modalTitle').text('Add Device Type');
            });
        });
    </script>
<?php
} /* Can View */ ?>