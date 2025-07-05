// Pallet Management JavaScript
var PalletManagementApp = {
    // กำหนดค่าเริ่มต้น
    config: {
        baseUrl: '/OracleStations',
        defaultEmpId: 70000049 // ตัวอย่าง Employee ID
    },

    // เริ่มต้นแอพ
    init: function() {
        console.log('PalletManagementApp initialized');
        this.bindEvents();
        this.loadPallets();
    },

    // ผูกเหตุการณ์ต่างๆ
    bindEvents: function() {
        // โหลดใหม่เมื่อเปลี่ยนเดือน
        $('#filterMonth').on('change', () => {
            this.loadPallets();
        });

        // ปุ่มค้นหา
        $('#searchPallet').click(() => {
            this.searchPallet();
        });

        // ปุ่มล้างการค้นหา
        $('#clearSearch').click(() => {
            this.clearSearch();
        });

        // ปุ่มสร้างพาเลท
        $('#createPallet').click(() => {
            this.createPallet();
        });

        // Event handlers สำหรับตาราง
        $('#palletTable').on('click', '.btn-view', (e) => {
            const palletId = $(e.currentTarget).data('pallet');
            this.viewPalletDetails(palletId);
        });

        $('#palletTable').on('click', '.btn-close', (e) => {
            const palletId = $(e.currentTarget).data('pallet');
            this.closePallet(palletId);
        });

        $('#palletTable').on('click', '.btn-open', (e) => {
            const palletId = $(e.currentTarget).data('pallet');
            this.openPallet(palletId);
        });

        $('#palletTable').on('click', '.btn-delete', (e) => {
            const palletId = $(e.currentTarget).data('pallet');
            this.deletePallet(palletId);
        });

        // Event handlers สำหรับกล่องใน modal
        $('#palletDetailContent').on('click', '.btn-close-carton', (e) => {
            const cartonId = $(e.currentTarget).data('carton');
            this.closeCarton(cartonId);
        });

        $('#palletDetailContent').on('click', '.btn-open-carton', (e) => {
            const cartonId = $(e.currentTarget).data('carton');
            this.openCarton(cartonId);
        });

        $('#palletDetailContent').on('click', '.btn-delete-carton', (e) => {
            const cartonId = $(e.currentTarget).data('carton');
            this.deleteCarton(cartonId);
        });
    },

    // โหลดข้อมูลพาเลท
    loadPallets: function() {
        const yyyymm = $('#filterMonth').val();
        
        $.get(this.config.baseUrl + '/GetPallets', { yyyymm: yyyymm })
            .done((data) => {
                if (data && data.length > 0) {
                    this.updatePalletTable(data);
                } else {
                    $('#palletTable tbody').html('<tr><td colspan="9" class="text-center">ไม่พบข้อมูลพาเลท</td></tr>');
                }
            })
            .fail((error) => {
                console.error('Error loading pallets:', error);
                this.showError('ไม่สามารถโหลดข้อมูลพาเลทได้');
            });
    },

    // อัปเดตตารางพาเลท
    updatePalletTable: function(data) {
        const table = $('#palletTable').DataTable();
        if (table) {
            table.destroy();
        }

        $('#palletTable').DataTable({
            data: data,
            columns: [
                { 
                    data: null,
                    render: function(data, type, row, meta) {
                        return meta.row + 1;
                    }
                },
                { data: 'PalletId' },
                { data: 'WorkOrder' },
                { data: 'PartId' },
                { 
                    data: 'CloseFlag',
                    render: function(data) {
                        if (data === 'Y') {
                            return '<span class="badge bg-success">ปิดแล้ว</span>';
                        } else {
                            return '<span class="badge bg-warning">เปิดอยู่</span>';
                        }
                    }
                },
                { data: 'TerminalName' },
                { 
                    data: 'SerialCount',
                    render: function(data) {
                        return `<span class="badge bg-info">${data}</span>`;
                    }
                },
                { 
                    data: 'CreateTime',
                    render: function(data) {
                        return new Date(data).toLocaleDateString('th-TH');
                    }
                },
                {
                    data: null,
                    render: function(data, type, row) {
                        let buttons = `
                            <div class="btn-group btn-group-sm" role="group">
                                <button type="button" class="btn btn-info btn-view" data-pallet="${row.PalletId}">
                                    <i class="fas fa-eye"></i>
                                </button>`;
                        
                        if (row.CloseFlag === 'N') {
                            buttons += `
                                <button type="button" class="btn btn-warning btn-close" data-pallet="${row.PalletId}">
                                    <i class="fas fa-lock"></i>
                                </button>`;
                        } else {
                            buttons += `
                                <button type="button" class="btn btn-success btn-open" data-pallet="${row.PalletId}">
                                    <i class="fas fa-unlock"></i>
                                </button>`;
                        }
                        
                        buttons += `
                                <button type="button" class="btn btn-danger btn-delete" data-pallet="${row.PalletId}">
                                    <i class="fas fa-trash"></i>
                                </button>
                            </div>`;
                        
                        return buttons;
                    }
                }
            ],
            responsive: true,
            pageLength: 10,
            lengthMenu: [5, 10, 25, 50],
            order: [[6, 'desc']], // เรียงตามวันที่สร้าง
            language: {
                url: '//cdn.datatables.net/plug-ins/1.13.7/i18n/th.json'
            }
        });
    },

    // เปิดพาเลท
    openPallet: function(palletId) {
        Swal.fire({
            title: 'ยืนยันการเปิดพาเลท',
            text: `คุณต้องการเปิดพาเลท ${palletId} หรือไม่?`,
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'เปิดพาเลท',
            cancelButtonText: 'ยกเลิก',
            confirmButtonColor: '#198754'
        }).then((result) => {
            if (result.isConfirmed) {
                const data = {
                    palletNo: palletId,
                    openEmpId: this.config.defaultEmpId
                };

                $.ajax({
                    url: this.config.baseUrl + '/OpenPallet',
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(data),
                    success: (response) => {
                        if (response.success) {
                            this.showSuccess(response.message);
                            this.loadPallets();
                        } else {
                            this.showError(response.message);
                        }
                    },
                    error: () => {
                        this.showError('เกิดข้อผิดพลาดในการเปิดพาเลท');
                    }
                });
            }
        });
    },

    // ปิดพาเลท
    closePallet: function(palletId) {
        Swal.fire({
            title: 'ยืนยันการปิดพาเลท',
            text: `คุณต้องการปิดพาเลท ${palletId} หรือไม่?`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'ปิดพาเลท',
            cancelButtonText: 'ยกเลิก',
            confirmButtonColor: '#ffc107'
        }).then((result) => {
            if (result.isConfirmed) {
                const data = {
                    palletNo: palletId,
                    closeEmpId: this.config.defaultEmpId,
                    fullFlag: 'Y'
                };

                $.ajax({
                    url: this.config.baseUrl + '/ClosePallet',
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(data),
                    success: (response) => {
                        if (response.success) {
                            this.showSuccess(response.message);
                            this.loadPallets();
                        } else {
                            this.showError(response.message);
                        }
                    },
                    error: () => {
                        this.showError('เกิดข้อผิดพลาดในการปิดพาเลท');
                    }
                });
            }
        });
    },

    // เปิดกล่อง
    openCarton: function(cartonId) {
        const palletId = $('#palletDetailModal').data('pallet-id');
        
        Swal.fire({
            title: 'ยืนยันการเปิดกล่อง',
            text: `คุณต้องการเปิดกล่อง ${cartonId} หรือไม่?`,
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'เปิดกล่อง',
            cancelButtonText: 'ยกเลิก',
            confirmButtonColor: '#198754'
        }).then((result) => {
            if (result.isConfirmed) {
                const data = {
                    cartonNo: cartonId,
                    openEmpId: this.config.defaultEmpId
                };

                $.ajax({
                    url: this.config.baseUrl + '/OpenCarton',
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(data),
                    success: (response) => {
                        if (response.success) {
                            this.showSuccess(response.message);
                            // รีเฟรช modal
                            $('#palletDetailModal').modal('hide');
                            // เปิด modal ใหม่เพื่อแสดงข้อมูลที่อัปเดต
                            setTimeout(() => {
                                $(`[data-pallet="${palletId}"]`).click();
                            }, 500);
                        } else {
                            this.showError(response.message);
                        }
                    },
                    error: () => {
                        this.showError('เกิดข้อผิดพลาดในการเปิดกล่อง');
                    }
                });
            }
        });
    },

    // ปิดกล่อง
    closeCarton: function(cartonId) {
        const palletId = $('#palletDetailModal').data('pallet-id');
        
        Swal.fire({
            title: 'ยืนยันการปิดกล่อง',
            text: `คุณต้องการปิดกล่อง ${cartonId} หรือไม่?`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'ปิดกล่อง',
            cancelButtonText: 'ยกเลิก',
            confirmButtonColor: '#ffc107'
        }).then((result) => {
            if (result.isConfirmed) {
                const data = {
                    cartonNo: cartonId,
                    closeEmpId: this.config.defaultEmpId
                };

                $.ajax({
                    url: this.config.baseUrl + '/CloseCarton',
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(data),
                    success: (response) => {
                        if (response.success) {
                            this.showSuccess(response.message);
                            // รีเฟรช modal
                            $('#palletDetailModal').modal('hide');
                            // เปิด modal ใหม่เพื่อแสดงข้อมูลที่อัปเดต
                            setTimeout(() => {
                                $(`[data-pallet="${palletId}"]`).click();
                            }, 500);
                        } else {
                            this.showError(response.message);
                        }
                    },
                    error: () => {
                        this.showError('เกิดข้อผิดพลาดในการปิดกล่อง');
                    }
                });
            }
        });
    },

    // ดูรายละเอียดพาเลท
    viewPalletDetails: function(palletId) {
        // เก็บ palletId ไว้ใน modal
        $('#palletDetailModal').data('pallet-id', palletId);
        
        // แสดง loading
        Swal.fire({
            title: 'กำลังโหลดข้อมูล...',
            allowOutsideClick: false,
            didOpen: () => {
                Swal.showLoading();
            }
        });

        $.get(this.config.baseUrl + '/GetPalletDetails', { palletNo: palletId })
            .done((response) => {
                Swal.close();
                if (response.success) {
                    this.showPalletDetails(response);
                } else {
                    this.showError(response.message);
                }
            })
            .fail(() => {
                Swal.close();
                this.showError('ไม่สามารถโหลดข้อมูลรายละเอียดได้');
            });
    },

    // แสดงรายละเอียดพาเลทใน modal
    showPalletDetails: function(response) {
        const pallet = response.pallet;
        const serials = response.serials;
        const cartons = response.cartons;

        let detailHtml = `
            <div class="row">
                <div class="col-md-6">
                    <h6><i class="fas fa-info-circle me-2"></i>ข้อมูลพาเลท</h6>
                    <table class="table table-sm">
                        <tr><td>Pallet ID:</td><td><strong>${pallet.PalletId}</strong></td></tr>
                        <tr><td>Work Order:</td><td>${pallet.WorkOrder}</td></tr>
                        <tr><td>Part ID:</td><td>${pallet.PartId}</td></tr>
                        <tr><td>สถานะ:</td><td>${pallet.CloseFlag === 'Y' ? '<span class="badge bg-success">ปิดแล้ว</span>' : '<span class="badge bg-warning">เปิดอยู่</span>'}</td></tr>
                        <tr><td>Terminal:</td><td>${pallet.TerminalName || 'ไม่ระบุ'}</td></tr>
                        <tr><td>QC Flag:</td><td>${pallet.QcFlag === 'Y' ? 'ต้องตรวจสอบ' : 'ไม่ต้องตรวจสอบ'}</td></tr>
                        <tr><td>Mix Flag:</td><td>${pallet.MixFlag === 'Y' ? 'ผสม Work Order' : 'ไม่ผสม'}</td></tr>
                        <tr><td>วันที่สร้าง:</td><td>${new Date(pallet.CreateTime).toLocaleString('th-TH')}</td></tr>
                        ${pallet.CloseTime ? `<tr><td>วันที่ปิด:</td><td>${new Date(pallet.CloseTime).toLocaleString('th-TH')}</td></tr>` : ''}
                    </table>
                </div>
                <div class="col-md-6">
                    <h6><i class="fas fa-chart-pie me-2"></i>สรุปข้อมูล</h6>
                    <div class="row">
                        <div class="col-6">
                            <div class="card bg-primary text-white">
                                <div class="card-body text-center">
                                    <h4>${serials.length}</h4>
                                    <small>Serial Numbers</small>
                                </div>
                            </div>
                        </div>
                        <div class="col-6">
                            <div class="card bg-success text-white">
                                <div class="card-body text-center">
                                    <h4>${cartons.length}</h4>
                                    <small>กล่อง</small>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>`;

        if (serials.length > 0) {
            detailHtml += `
                <div class="row mt-3">
                    <div class="col-12">
                        <h6><i class="fas fa-list me-2"></i>Serial Numbers (${serials.length} รายการ)</h6>
                        <div class="table-responsive" style="max-height: 200px;">
                            <table class="table table-sm table-striped">
                                <thead class="table-dark">
                                    <tr>
                                        <th>Serial Number</th>
                                        <th>Work Order</th>
                                        <th>Carton No</th>
                                        <th>สถานะ</th>
                                        <th>เข้าระบบ</th>
                                    </tr>
                                </thead>
                                <tbody>`;
            
            serials.forEach(serial => {
                detailHtml += `
                    <tr>
                        <td><code>${serial.SerialNumber}</code></td>
                        <td>${serial.WorkOrder}</td>
                        <td>${serial.CartonNo || 'ไม่ระบุ'}</td>
                        <td>${serial.CurrentStatus}</td>
                        <td>${serial.InProcessTime ? new Date(serial.InProcessTime).toLocaleString('th-TH') : 'ไม่ระบุ'}</td>
                    </tr>`;
            });
            
            detailHtml += `
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>`;
        }

        if (cartons.length > 0) {
            detailHtml += `
                <div class="row mt-3">
                    <div class="col-12">
                        <h6><i class="fas fa-boxes me-2"></i>กล่องในพาเลท (${cartons.length} รายการ)</h6>
                        <div class="table-responsive">
                            <table class="table table-sm table-striped">
                                <thead class="table-dark">
                                    <tr>
                                        <th>Carton No</th>
                                        <th>Work Order</th>
                                        <th>สถานะ</th>
                                        <th>จำนวน Serial</th>
                                        <th>วันที่สร้าง</th>
                                        <th>การดำเนินการ</th>
                                    </tr>
                                </thead>
                                <tbody>`;
            
            cartons.forEach(carton => {
                detailHtml += `
                    <tr>
                        <td><code>${carton.CartonId}</code></td>
                        <td>${carton.WorkOrder}</td>
                        <td>${carton.CloseFlag === 'Y' ? '<span class="badge bg-success">ปิดแล้ว</span>' : '<span class="badge bg-warning">เปิดอยู่</span>'}</td>
                        <td><span class="badge bg-info">${carton.SerialCount}</span></td>
                        <td>${new Date(carton.CreateTime).toLocaleDateString('th-TH')}</td>
                        <td>
                            ${carton.CloseFlag === 'N' ? 
                                `<button class="btn btn-warning btn-sm btn-close-carton" data-carton="${carton.CartonId}">
                                    <i class="fas fa-lock"></i> ปิด
                                </button>` :
                                `<button class="btn btn-success btn-sm btn-open-carton" data-carton="${carton.CartonId}">
                                    <i class="fas fa-unlock"></i> เปิด
                                </button>`
                            }
                            <button class="btn btn-danger btn-sm btn-delete-carton" data-carton="${carton.CartonId}">
                                <i class="fas fa-trash"></i> ลบ
                            </button>
                        </td>
                    </tr>`;
            });
            
            detailHtml += `
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>`;
        }

        $('#palletDetailContent').html(detailHtml);
        $('#palletDetailModal').modal('show');
    },

    // ค้นหาพาเลท
    searchPallet: function() {
        const searchTerm = $('#palletSearch').val();
        if (searchTerm) {
            const table = $('#palletTable').DataTable();
            table.search(searchTerm).draw();
        } else {
            this.showWarning('กรุณากรอกคำค้นหา');
        }
    },

    // ล้างการค้นหา
    clearSearch: function() {
        $('#palletSearch').val('');
        const table = $('#palletTable').DataTable();
        table.search('').draw();
    },

    // สร้างพาเลทใหม่
    createPallet: function() {
        const palletId = $('#newPalletId').val();
        const workOrder = $('#workOrder').val();
        const partId = $('#partId').val();
        const terminalId = $('#terminalId').val();
        const qcFlag = $('#qcFlag').val();
        const mixFlag = $('#mixFlag').val();
        
        if (!palletId || !workOrder || !partId || !terminalId) {
            this.showWarning('กรุณากรอกข้อมูลที่จำเป็นให้ครบถ้วน');
            return;
        }

        const data = {
            palletNo: palletId,
            workOrder: workOrder,
            partId: parseInt(partId),
            terminalId: parseInt(terminalId),
            createEmpId: this.config.defaultEmpId,
            qcFlag: qcFlag,
            mixFlag: mixFlag
        };

        Swal.fire({
            title: 'ยืนยันการสร้าง',
            text: `คุณต้องการสร้างพาเลท ${palletId} หรือไม่?`,
            icon: 'question',
            showCancelButton: true,
            confirmButtonText: 'สร้าง',
            cancelButtonText: 'ยกเลิก'
        }).then((result) => {
            if (result.isConfirmed) {
                $.ajax({
                    url: this.config.baseUrl + '/CreatePallet',
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(data),
                    success: (response) => {
                        if (response.success) {
                            this.showSuccess(response.message);
                            // ล้างฟอร์ม
                            $('#newPalletId').val('');
                            $('#workOrder').val('');
                            $('#partId').val('');
                            $('#terminalId').val('');
                            // โหลดข้อมูลใหม่
                            this.loadPallets();
                        } else {
                            this.showError(response.message);
                        }
                    },
                    error: () => {
                        this.showError('เกิดข้อผิดพลาดในการสร้างพาเลท');
                    }
                });
            }
        });
    },

    // ลบพาเลท
    deletePallet: function(palletId) {
        Swal.fire({
            title: 'ยืนยันการลบ',
            text: `คุณต้องการลบพาเลท ${palletId} หรือไม่?`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'ลบ',
            cancelButtonText: 'ยกเลิก',
            confirmButtonColor: '#dc3545'
        }).then((result) => {
            if (result.isConfirmed) {
                const data = {
                    palletNo: palletId
                };

                $.ajax({
                    url: this.config.baseUrl + '/DeletePallet',
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(data),
                    success: (response) => {
                        if (response.success) {
                            this.showSuccess(response.message);
                            this.loadPallets();
                        } else {
                            this.showError(response.message);
                        }
                    },
                    error: () => {
                        this.showError('เกิดข้อผิดพลาดในการลบพาเลท');
                    }
                });
            }
        });
    },

    // ลบกล่อง
    deleteCarton: function(cartonId) {
        const palletId = $('#palletDetailModal').data('pallet-id');
        
        Swal.fire({
            title: 'ยืนยันการลบ',
            text: `คุณต้องการลบกล่อง ${cartonId} หรือไม่?`,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'ลบ',
            cancelButtonText: 'ยกเลิก',
            confirmButtonColor: '#dc3545'
        }).then((result) => {
            if (result.isConfirmed) {
                const data = {
                    cartonNo: cartonId
                };

                $.ajax({
                    url: this.config.baseUrl + '/DeleteCarton',
                    type: 'POST',
                    contentType: 'application/json',
                    data: JSON.stringify(data),
                    success: (response) => {
                        if (response.success) {
                            this.showSuccess(response.message);
                            // รีเฟรช modal
                            $('#palletDetailModal').modal('hide');
                            // เปิด modal ใหม่เพื่อแสดงข้อมูลที่อัปเดต
                            setTimeout(() => {
                                $(`[data-pallet="${palletId}"]`).click();
                            }, 500);
                        } else {
                            this.showError(response.message);
                        }
                    },
                    error: () => {
                        this.showError('เกิดข้อผิดพลาดในการลบกล่อง');
                    }
                });
            }
        });
    },

    // แสดงข้อความแจ้งเตือน
    showSuccess: function(message) {
        Swal.fire({
            title: 'สำเร็จ',
            text: message,
            icon: 'success',
            timer: 2000,
            showConfirmButton: false
        });
    },

    showError: function(message) {
        Swal.fire({
            title: 'ข้อผิดพลาด',
            text: message,
            icon: 'error',
            confirmButtonText: 'ตกลง'
        });
    },

    showWarning: function(message) {
        Swal.fire({
            title: 'คำเตือน',
            text: message,
            icon: 'warning',
            confirmButtonText: 'ตกลง'
        });
    }
}; 