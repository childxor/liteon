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
                            <i class="fas fa-desktop"></i> <?php echo $lang_module->lbl_software_name; ?>
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
                            <button type="button" id="addBtn" class="btn btn-success btn-block" data-toggle="modal" data-target="#softwarelicensesModal">
                                <i class="fas fa-plus"></i> <?php echo $lang_module->lbl_add; ?>
                            </button>
                        </div>
                    <?php endif; ?>
                </div>
            </form>

            <div class="table-responsive">
                <table id="softwarelicensesTable" class="table table-bordered table-striped table-hover" style="width:100%"></table>
            </div>
        </div>
    </div>

    <?php if ($this->efs_lib->is_can($permission, "edit")) { ?>
        <!-- Software Licenses Modal -->
        <div class="modal fade" id="softwarelicensesModal" tabindex="-1" role="dialog" aria-labelledby="softwarelicensesModalLabel" aria-hidden="true">
            <div class="modal-dialog modal-lg" role="document">
                <div class="modal-content">
                    <div class="modal-header bg-primary text-white">
                        <h5 class="modal-title" id="softwarelicensesModalLabel"><i class="fas fa-desktop mr-2"></i><span id="modalTitle">Add Software License</span></h5>
                        <button type="button" class="close text-white" data-dismiss="modal" aria-label="Close">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body">
                        <form id="softwarelicensesForm">
                            <input type="hidden" id="id" name="id">
                            <div class="form-row">
                                <div class="form-group col-md-6">
                                    <label for="softwareName"><i class="fas fa-tag mr-2"></i>Name</label>
                                    <input type="text" class="form-control" id="softwareName" name="name" required>
                                </div>
                                <div class="form-group col-md-6">
                                    <label for="softwarePublisher"><i class="fas fa-building mr-2"></i>Publisher</label>
                                    <input type="text" class="form-control" id="softwarePublisher" name="publisher">
                                </div>
                            </div>
                            <div class="form-row">
                                <div class="form-group col-md-6">
                                    <label for="purchaseDate"><i class="fas fa-calendar-alt mr-2"></i>Purchase Date</label>
                                    <input type="date" class="form-control" id="purchaseDate" name="purchasedate">
                                </div>
                                <div class="form-group col-md-6">
                                    <label for="expirationDate"><i class="fas fa-calendar-times mr-2"></i>Expiration Date</label>
                                    <input type="date" class="form-control" id="expirationDate" name="expirationdate">
                                </div>
                            </div>
                            <div class="form-group">
                                <label for="licenseKey"><i class="fas fa-key mr-2"></i>License Key</label>
                                <input type="text" class="form-control" id="licenseKey" name="licensekey">
                            </div>
                            <div class="form-row">
                                <div class="form-group col-md-6">
                                    <label for="numberOfLicenses"><i class="fas fa-list-ol mr-2"></i>Number of Licenses</label>
                                    <input type="number" class="form-control" id="numberOfLicenses" name="numberoflicenses">
                                </div>
                                <div class="form-group col-md-6">
                                    <label for="cost"><i class="fas fa-dollar-sign mr-2"></i>Cost</label>
                                    <input type="number" step="0.01" class="form-control" id="cost" name="cost">
                                </div>
                            </div>
                            <div class="form-group">
                                <label for="notes"><i class="fas fa-sticky-note mr-2"></i>Notes</label>
                                <textarea class="form-control" id="notes" name="notes" rows="3"></textarea>
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
        var softwarelicensesTable;

        !function(window, document, $) {
            "use strict";
            $.fn.dataTable.ext.errMode = 'none';
            softwarelicensesTable = $('#softwarelicensesTable').DataTable({
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
                    "url": "<?php echo base_url('it/It_softwarelicenses/getsoftwarelicenses'); ?>",
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
                        "title": "<?php echo $lang_module->lbl_software_name; ?>"
                    },
                    {
                        "data": "publisher",
                        "className": "text-left",
                        "title": "Publisher"
                    },
                    {
                        "data": "purchasedate",
                        "className": "text-center",
                        "title": "Purchase Date"
                    },
                    {
                        "data": "expirationdate",
                        "className": "text-center",
                        "title": "Expiration Date"
                    },
                    {
                        "data": "licensekey",
                        "className": "text-left",
                        "title": "License Key"
                    },
                    {
                        "data": "numberoflicenses",
                        "className": "text-center",
                        "title": "Number of Licenses"
                    },
                    {
                        "data": "cost",
                        "className": "text-right",
                        "title": "Cost",
                        "render": function(data, type, row) {
                            return parseFloat(data).toFixed(2);
                        }
                    },
                    {
                        "data": "record_status",
                        "className": "text-center",
                        "title": "Status",
                        "render": function(data, type, row) {
                            return data === 'N' ? '<span class="badge badge-success">Active</span>' : '<span class="badge badge-danger">Deleted</span>';
                        }
                    },
                    <?php if ($this->efs_lib->is_can($permission, "edit")) { ?>
                    {
                        "data": "id",
                        "className": "text-center",
                        "orderable": false,
                        "title": "<i class='mdi mdi-settings'></i>",
                        "width": "100px",
                        "render": function(data, type, row, meta) {
                            var editBtn = '<button class="editsoftwarelicenses btn btn-sm btn-warning mr-1" data-id="' + row.id + '"><i class="fas fa-edit"></i></button>';
                            var deleteBtn = '<button class="deletesoftwarelicenses btn btn-sm btn-danger" data-id="' + row.id + '"><i class="fas fa-trash"></i></button>';
                            return editBtn + deleteBtn;
                        }
                    },
                    <?php } ?>
                ],
                "order": [
                    [1, "asc"]
                ]
            });
        }(window, document, jQuery);

        $(document).ready(function() {
            $('#searchForm').submit(function(e) {
                e.preventDefault();
                softwarelicensesTable.ajax.reload();
            });

            $('#softwarelicensesForm').submit(function(e) {
                e.preventDefault();
                var formData = $(this).serialize();
                $.ajax({
                    url: '<?php echo base_url('it/It_softwarelicenses/savesoftwarelicenses'); ?>',
                    type: 'POST',
                    data: formData,
                    success: function(response) {
                        $('#softwarelicensesModal').modal('hide');
                        softwarelicensesTable.ajax.reload();
                        swal.fire('Success', 'Software license saved successfully', 'success');
                    },
                    error: function() {
                        swal.fire('Error', 'An error occurred while saving the software license', 'error');
                    }
                });
            });

            $(document).on('click', '.editsoftwarelicenses', function() {
                var id = $(this).data('id');
                $.ajax({
                    url: '<?php echo base_url('it/It_softwarelicenses/getsoftwarelicense/'); ?>' + id,
                    type: 'GET',
                    success: function(response) {
                        var data = JSON.parse(response);
                        $('#id').val(data.id);
                        $('#softwareName').val(data.name);
                        $('#softwarePublisher').val(data.publisher);
                        $('#purchaseDate').val(data.purchasedate);
                        $('#expirationDate').val(data.expirationdate);
                        $('#licenseKey').val(data.licensekey);
                        $('#numberOfLicenses').val(data.numberoflicenses);
                        $('#cost').val(data.cost);
                        $('#notes').val(data.notes);
                        $('#modalTitle').text('Edit Software License');
                        $('#softwarelicensesModal').modal('show');
                    }
                });
            });

            $(document).on('click', '.deletesoftwarelicenses', function() {
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
                            url: '<?php echo base_url('it/It_softwarelicenses/deletesoftwarelicense/'); ?>' + id,
                            type: 'POST',
                            success: function(response) {
                                softwarelicensesTable.ajax.reload();
                                swal.fire('Deleted!', 'Software license has been deleted.', 'success');
                            },
                            error: function() {
                                swal.fire('Error', 'An error occurred while deleting the software license', 'error');
                            }
                        });
                    }
                });
            });

            $('#softwarelicensesModal').on('hidden.bs.modal', function() {
                $('#softwarelicensesForm').trigger('reset');
                $('#id').val('');
                $('#modalTitle').text('Add Software License');
            });

        });
    </script>
<?php } else { /* Cannot View */ ?> }
    <script>
        swal.fire('Error', 'You do not have permission to view this page', 'error');
    </script>
<?php } ?>