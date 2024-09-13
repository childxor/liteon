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
                        <label for="assetName" class="form-label">
                            <i class="fas fa-desktop"></i> <?php echo $lang_module->lbl_asset_name; ?>
                        </label>
                        <input type="text" class="form-control" id="assetName" name="assetName">
                    </div>
                    <div class="col-md-4 col-lg-3 mb-3">
                        <label for="assetType" class="form-label">
                            <i class="fas fa-tags"></i> <?php echo $lang_module->lbl_asset_type; ?>
                        </label>
                        <select class="form-control" id="assetType" name="assetType">
                            <option value=""><?php echo $lang_module->lbl_all_types; ?></option>
                            <option value="hardware">Hardware</option>
                            <option value="software">Software</option>
                            <option value="network">Network</option>
                        </select>
                    </div>
                    <div class="col-md-4 col-lg-3 mb-3">
                        <label for="is_active" class="form-label">
                            <i class="fas fa-toggle-on"></i> <?php echo $lang_module->lbl_status; ?>
                        </label>
                        <select class="form-control" id="is_active" name="is_active">
                            <option value=""><?php echo $lang_module->lbl_all_statuses; ?></option>
                            <option value="N"><?php echo $lang_module->lbl_active; ?></option>
                            <option value="D"><?php echo $lang_module->lbl_deleted; ?></option>
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
                            <button type="button" id="addBtn" class="btn btn-success btn-block" data-toggle="modal" data-target="#itAssetsModal">
                                <i class="fas fa-plus"></i> <?php echo $lang_module->lbl_add; ?>
                            </button>
                        </div>
                    <?php endif; ?>
                </div>
            </form>

            <div class="table-responsive">
                <table id="itAssetsTable" class="table table-bordered table-striped table-hover" style="width:100%"></table>
            </div>
        </div>
    </div>

    <?php if ($this->efs_lib->is_can($permission, "edit")) { ?>
        <!-- IT Assets Modal -->
        <div class="modal fade" id="itAssetsModal" tabindex="-1" role="dialog" aria-labelledby="itAssetsModalLabel" aria-hidden="true">
            <div class="modal-dialog modal-xl" role="document">
                <div class="modal-content">
                    <div class="modal-header bg-primary text-white">
                        <h5 class="modal-title" id="itAssetsModalLabel"><i class="fas fa-desktop mr-2"></i><span id="modalTitle">เพิ่ม/แก้ไขทรัพย์สิน IT</span></h5>
                        <button type="button" class="close text-white" data-dismiss="modal" aria-label="ปิด">
                            <span aria-hidden="true">&times;</span>
                        </button>
                    </div>
                    <div class="modal-body">
                        <form id="itAssetsForm">
                            <input type="hidden" id="id" name="id">
                            <div class="form-row">
                                <div class="form-group col-md-4">
                                    <label for="assetTag"><i class="fas fa-tag mr-2"></i>รหัสทรัพย์สิน</label>
                                    <input type="text" class="form-control" id="assetTag" name="assetTag" required>
                                </div>
                                <div class="form-group col-md-4">
                                    <label for="deviceTypeId"><i class="fas fa-laptop mr-2"></i>ประเภทอุปกรณ์</label>
                                    <select class="form-control" id="deviceTypeId" name="deviceTypeId" required>
                                        <?php foreach ($device_types as $type): ?>
                                            <option value="<?php echo $type->id; ?>"><?php echo $type->name; ?></option>
                                        <?php endforeach; ?>
                                    </select>
                                </div>
                                <div class="form-group col-md-4">
                                    <label for="brandId"><i class="fas fa-industry mr-2"></i>ยี่ห้อ</label>
                                    <select class="form-control" id="brandId" name="brandId">
                                        <?php foreach ($brands as $brand): ?>
                                            <option value="<?php echo $brand->id; ?>"><?php echo $brand->name; ?></option>
                                        <?php endforeach; ?>
                                    </select>
                                </div>
                            </div>
                            <div class="form-row">
                                <div class="form-group col-md-4">
                                    <label for="modelId"><i class="fas fa-barcode mr-2"></i>โมเดล</label>
                                    <select class="form-control" id="modelId" name="modelId">
                                        <?php foreach ($models as $model): ?>
                                            <option value="<?php echo $model->id; ?>"><?php echo $model->name; ?></option>
                                        <?php endforeach; ?>
                                    </select>
                                </div>
                                <div class="form-group col-md-4">
                                    <label for="serialNumber"><i class="fas fa-fingerprint mr-2"></i>หมายเลขซีเรียล</label>
                                    <input type="text" class="form-control" id="serialNumber" name="serialNumber">
                                </div>
                                <div class="form-group col-md-4">
                                    <label for="purchaseDate"><i class="fas fa-calendar-alt mr-2"></i>วันที่ซื้อ</label>
                                    <input type="date" class="form-control" id="purchaseDate" name="purchaseDate" value="<?php echo date('Y-m-d'); ?>">
                                </div>
                            </div>
                            <div class="form-row">
                                <div class="form-group col-md-4">
                                    <label for="purchasePrice"><i class="fas fa-dollar-sign mr-2"></i>ราคาซื้อ</label>
                                    <input type="number" step="0.01" class="form-control" id="purchasePrice" name="purchasePrice">
                                </div>
                                <div class="form-group col-md-4">
                                    <label for="vendorId"><i class="fas fa-store mr-2"></i>ผู้ขาย</label>
                                    <select class="form-control" id="vendorId" name="vendorId">
                                        <?php foreach ($vendors as $vendor): ?>
                                            <option value="<?php echo $vendor->id; ?>"><?php echo $vendor->name; ?></option>
                                        <?php endforeach; ?>
                                    </select>
                                </div>
                                <div class="form-group col-md-4">
                                    <label for="warrantyPeriod"><i class="fas fa-shield-alt mr-2"></i>ระยะเวลาการรับประกัน (เดือน)</label>
                                    <input type="number" class="form-control" id="warrantyPeriod" name="warrantyPeriod">
                                </div>
                            </div>
                            <div class="form-row">
                                <div class="form-group col-md-4">
                                    <label for="warrantyExpirationDate"><i class="fas fa-calendar-times mr-2"></i>วันหมดอายุการรับประกัน</label>
                                    <input type="date" class="form-control" id="warrantyExpirationDate" name="warrantyExpirationDate" value="<?php echo date('Y-m-d'); ?>">
                                </div>
                                <div class="form-group col-md-4">
                                    <label for="locationId"><i class="fas fa-map-marker-alt mr-2"></i>สถานที่</label>
                                    <select class="form-control" id="locationId" name="locationId">
                                        <?php foreach ($locations as $location): ?>
                                            <option value="<?php echo $location->id; ?>"><?php echo $location->name . ' (' . $location->description . ')'; ?></option>
                                        <?php endforeach; ?>
                                    </select>
                                </div>
                                <div class="form-group col-md-4">
                                    <label for="departmentId"><i class="fas fa-building mr-2"></i>แผนก</label>
                                    <select class="form-control" id="departmentId" name="departmentId">
                                        <?php foreach ($departments as $department): ?>
                                            <option value="<?php echo $department->id; ?>"><?php echo $department->name; ?></option>
                                        <?php endforeach; ?>
                                    </select>
                                </div>
                            </div>
                            <div class="form-row">
                                <div class="form-group col-md-4">
                                    <label for="currentUserId"><i class="fas fa-user mr-2"></i>ผู้ใช้ปัจจุบัน</label>
                                    <select class="form-control" id="currentUserId" name="currentUserId">
                                        <?php foreach ($users as $user): ?>
                                            <option value="<?php echo $user->id; ?>"><?php echo $user->name; ?></option>
                                        <?php endforeach; ?>
                                    </select>
                                </div>
                                <div class="form-group col-md-4">
                                    <label for="statusId"><i class="fas fa-info-circle mr-2"></i>สถานะ</label>
                                    <select class="form-control" id="statusId" name="statusId">
                                        <?php foreach ($asset_statuses as $status): ?>
                                            <option value="<?php echo $status->id; ?>"><?php echo $status->name . ' (' . $status->description . ')'; ?></option>
                                        <?php endforeach; ?>
                                    </select>
                                </div>
                                <div class="form-group col-md-4">
                                    <label for="operatingSystemId"><i class="fas fa-desktop mr-2"></i>ระบบปฏิบัติการ</label>
                                    <select class="form-control" id="operatingSystemId" name="operatingSystemId">
                                        <?php foreach ($operating_systems as $os): ?>
                                            <option value="<?php echo $os->id; ?>"><?php echo $os->name; ?></option>
                                        <?php endforeach; ?>
                                    </select>
                                </div>
                            </div>
                            <div class="form-row">
                                <div class="form-group col-md-4">
                                    <label for="ipAddress"><i class="fas fa-network-wired mr-2"></i>ที่อยู่ IP</label>
                                    <input type="text" class="form-control" id="ipAddress" name="ipAddress">
                                </div>
                                <div class="form-group col-md-4">
                                    <label for="macAddress"><i class="fas fa-ethernet mr-2"></i>ที่อยู่ MAC</label>
                                    <input type="text" class="form-control" id="macAddress" name="macAddress">
                                </div>
                                <div class="form-group col-md-4">
                                    <label for="lastAuditDate"><i class="fas fa-clipboard-check mr-2"></i>วันที่ตรวจสอบล่าสุด</label>
                                    <input type="date" class="form-control" id="lastAuditDate" name="lastAuditDate">
                                </div>
                            </div>
                            <div class="form-row">
                                <div class="form-group col-md-4">
                                    <label for="currentValue"><i class="fas fa-money-bill-wave mr-2"></i>มูลค่าปัจจุบัน</label>
                                    <input type="number" step="0.01" class="form-control" id="currentValue" name="currentValue">
                                </div>
                                <div class="form-group col-md-4">
                                    <label for="expectedReplacementDate"><i class="fas fa-calendar-plus mr-2"></i>วันที่คาดว่าจะเปลี่ยน</label>
                                    <input type="date" class="form-control" id="expectedReplacementDate" name="expectedReplacementDate">
                                </div>
                                <div class="form-group col-md-4">
                                    <label for="leaseEndDate"><i class="fas fa-file-contract mr-2"></i>วันหมดอายุสัญญาเช่า</label>
                                    <input type="date" class="form-control" id="leaseEndDate" name="leaseEndDate">
                                </div>
                            </div>
                            <div class="form-group">
                                <label for="notes"><i class="fas fa-sticky-note mr-2"></i>บันทึก</label>
                                <textarea class="form-control" id="notes" name="notes" rows="3"></textarea>
                            </div>
                            <div class="form-group">
                                <label for="imageUrl"><i class="fas fa-image mr-2"></i>URL รูปภาพ</label>
                                <input type="text" class="form-control" id="imageUrl" name="imageUrl">
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn btn-secondary" data-dismiss="modal"><i class="fas fa-times mr-2"></i>ยกเลิก</button>
                                <button type="submit" class="btn btn-primary"><i class="fas fa-save mr-2"></i>บันทึก</button>
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
        var itAssetsTable;

        ! function(window, document, $) {
            "use strict";
            $.fn.dataTable.ext.errMode = 'none';
            itAssetsTable = $('#itAssetsTable').DataTable({
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
                    "url": "<?php echo base_url('it/It_assets/getitassets'); ?>",
                    "type": "POST",
                    "data": function(data) {
                        data.assetName = $('#assetName').val();
                        data.assetType = $('#assetType').val();
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
                        "data": "assetName",
                        "className": "text-left",
                        "title": "<?php echo $lang_module->lbl_asset_name; ?>"
                    },
                    {
                        "data": "assetType",
                        "className": "text-center",
                        "title": "Asset Type"
                    },
                    {
                        "data": "manufacturer",
                        "className": "text-left",
                        "title": "Manufacturer"
                    },
                    {
                        "data": "model",
                        "className": "text-left",
                        "title": "Model"
                    },
                    {
                        "data": "serialNumber",
                        "className": "text-center",
                        "title": "Serial Number"
                    },
                    {
                        "data": "purchaseDate",
                        "className": "text-center",
                        "title": "Purchase Date"
                    },
                    {
                        "data": "status",
                        "className": "text-center",
                        "title": "Status",
                        "render": function(data, type, row) {
                            var statusClass = data === 'active' ? 'badge-success' : (data === 'inactive' ? 'badge-danger' : 'badge-warning');
                            return '<span class="badge ' + statusClass + '">' + data.charAt(0).toUpperCase() + data.slice(1) + '</span>';
                        }
                    },
                    <?php if ($this->efs_lib->is_can($permission, "edit")) { ?> {
                            "data": "id",
                            "className": "text-center",
                            "orderable": false,
                            "title": "<i class='mdi mdi-settings'></i>",
                            "width": "100px",
                            "render": function(data, type, row, meta) {
                                var editBtn = '<button class="editItAsset btn btn-sm btn-warning mr-1" data-id="' + row.id + '"><i class="fas fa-edit"></i></button>';
                                var deleteBtn = '<button class="deleteItAsset btn btn-sm btn-danger" data-id="' + row.id + '"><i class="fas fa-trash"></i></button>';
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
                itAssetsTable.ajax.reload();
            });

            $('#itAssetsForm').submit(function(e) {
                e.preventDefault();
                var formData = $(this).serialize();
                $.ajax({
                    url: '<?php echo base_url('it/It_assets/saveitasset'); ?>',
                    type: 'POST',
                    data: formData,
                    success: function(response) {
                        $('#itAssetsModal').modal('hide');
                        itAssetsTable.ajax.reload();
                        swal.fire('Success', 'IT asset saved successfully', 'success');
                    },
                    error: function() {
                        swal.fire('Error', 'An error occurred while saving the IT asset', 'error');
                    }
                });
            });
        });
    </script>
<?php } else { /* Cannot View */ ?>
    <div class="card">
        <div class="card-body">
            <div class="alert alert-danger" role="alert">
                <h4 class="alert-heading"><i class="fas fa-exclamation-triangle"></i> Access Denied</h4>
                <p>You do not have permission to view this page.</p>
            </div>
        </div>
    </div>
<?php } ?>