// ตัวแปรสำหรับเก็บ DataTable instance
let computersDataTable;

// ฟังก์ชันแปลงรหัสสถานที่เป็นชื่อที่อ่านง่าย
function getLocationDisplayName(locationCode) {
    const locationMap = {
        'PD': 'พื้นที่ไลน์ผลิต',
        'RMW': 'Raw material WH',
        'EW': 'Electronic WH',
        'RSFPW': 'Raw materials & semi Finished WH',
        'DQE': 'DQE LAB',
        'IQC': 'IQC room',
        'FA': 'พื้นที่รับของสโตร์',
        'JIG': 'ห้องเก็บอะไหล่',
        'DCC': 'ห้องเก็บของเล็กข้างห้องเซิฟเวอร์',
        'POR': 'ออฟฟิศ',
        'MR': 'ห้องบอส',
        'BTR': 'ห้องประชุม',
        'TC': 'ห้องเทรนงานชั้น 2',
        'CF': 'ห้องประชุมชั้น 2',
        'OFPD': 'ออฟฟิศไลน์ผลิต',
        'PR': 'ห้องปลิ้นติ้ง',
        'OM': 'ห้องผสมสี',
        'SC': 'ห้องแผ่นสกีนรูม',
        'PTR': 'ห้องยาม',
        'SLSC': 'ที่พักพนักงาน',
        'MICR': 'ห้องเซิฟเวอร์',
        'CR': 'ห้องเปลี่ยนรองเท้า'
    };
    return locationMap[locationCode] || locationCode || 'ไม่ระบุ';
}

// ฟังก์ชันแสดง modal คอมพิวเตอร์ของพนักงาน
function showComputers(personID) {
    // เก็บ personID ไว้ใช้ใน event listener
    $('#employeeComputersModal').data('personID', personID);
    
    // ตั้งค่าข้อมูลพนักงาน
    $('#computerPersonName').text('พนักงานรหัส ' + personID);
    $('#computerPersonIdDisplay').text(personID);

    // ทำลาย DataTable เก่า (ถ้ามี)
    if (computersDataTable) {
        computersDataTable.destroy();
        computersDataTable = null;
    }

    // เปิด modal
    const modal = new bootstrap.Modal(document.getElementById('employeeComputersModal'));
    modal.show();
}

// ฟังก์ชันสำหรับสร้าง DataTable
function createComputersDataTable(personID) {
    // สร้าง DataTable ใหม่
    computersDataTable = $('#computersTable').DataTable({
        processing: true,
        serverSide: false,
        responsive: false,
        scrollX: true,
        scrollY: '50vh',
        scrollCollapse: true,
        autoWidth: false,
        language: {
            "decimal": "",
            "emptyTable": "ไม่มีข้อมูลในตาราง",
            "info": "แสดง _START_ ถึง _END_ จาก _TOTAL_ รายการ",
            "infoEmpty": "แสดง 0 ถึง 0 จาก 0 รายการ",
            "infoFiltered": "(กรองข้อมูลจาก _MAX_ รายการทั้งหมด)",
            "infoPostFix": "",
            "thousands": ",",
            "lengthMenu": "แสดง _MENU_ รายการ",
            "loadingRecords": "กำลังโหลด...",
            "processing": "กำลังดำเนินการ...",
            "search": "ค้นหา:",
            "zeroRecords": "ไม่พบข้อมูลที่ค้นหา",
            "paginate": {
                "first": "หน้าแรก",
                "last": "หน้าสุดท้าย",
                "next": "ถัดไป",
                "previous": "ก่อนหน้า"
            },
            "aria": {
                "sortAscending": ": เปิดใช้งานการเรียงลำดับขั้นสูง",
                "sortDescending": ": เปิดใช้งานการเรียงลำดับลดลง"
            }
        },
        ajax: {
            url: appBasePath + 'Asset/GetByOwner?owner=' + personID,
            dataSrc: function(json) {
                return json || [];
            },
            error: function(xhr, error, thrown) {
                console.error('Error loading data:', error);
                alert('เกิดข้อผิดพลาดในการโหลดข้อมูล');
            }
        },
        columns: [
            {
                data: 'AssetNo',
                title: '<i class="fas fa-tag me-1"></i>รหัสทรัพย์สิน',
                width: '120px',
                render: function(data) {
                    return '<span class="text-nowrap">' + (data || '-') + '</span>';
                }
            },
            {
                data: 'ProductSerial',
                title: '<i class="fas fa-barcode me-1"></i>Serial Number',
                width: '140px',
                render: function(data) {
                    return '<span class="text-nowrap">' + (data || '-') + '</span>';
                }
            },
            {
                data: null,
                title: '<i class="fas fa-laptop me-1"></i>ชื่อ/รุ่น',
                width: '160px',
                render: function(data) {
                    return '<div class="text-nowrap"><strong>' + (data.Name || '-') + '</strong><br><small class="text-muted">' + (data.Model || '-') + '</small></div>';
                }
            },
            {
                data: 'Type',
                title: '<i class="fas fa-box me-1"></i>ประเภท',
                width: '100px',
                className: 'text-center',
                render: function(data) {
                    if (data === 'Computer') {
                        return '<span class="badge bg-primary">คอมพิวเตอร์</span>';
                    } else if (data === 'Laptop') {
                        return '<span class="badge bg-success">โน้ตบุ๊ค</span>';
                    } else {
                        return '<span class="badge bg-secondary">' + (data || '-') + '</span>';
                    }
                }
            },
            {
                data: 'Status',
                title: '<i class="fas fa-signal me-1"></i>สถานะ',
                width: '90px',
                className: 'text-center',
                render: function(data) {
                    switch (data) {
                        case '1':
                            return '<span class="badge bg-success">ใช้งาน</span>';
                        case '2':
                            return '<span class="badge bg-warning">ซ่อม</span>';
                        case '3':
                            return '<span class="badge bg-danger">ชำรุด</span>';
                        case '4':
                            return '<span class="badge bg-dark">ทำลาย</span>';
                        case '5':
                            return '<span class="badge bg-secondary">ไม่ใช้</span>';
                        case '6':
                            return '<span class="badge bg-info">สำรอง</span>';
                        default:
                            return '<span class="badge bg-secondary">-</span>';
                    }
                }
            },
            {
                data: 'MacAddressCount',
                title: '<i class="fas fa-network-wired me-1"></i>Mac Address',
                width: '100px',
                className: 'text-center',
                render: function(data) {
                    const macCount = data || 0;
                    if (macCount > 0) {
                        return '<span class="badge bg-success"><i class="fas fa-check me-1"></i>' + macCount + '</span>';
                    } else {
                        return '<span class="badge bg-secondary"><i class="fas fa-times me-1"></i>0</span>';
                    }
                }
            },
            {
                data: null,
                title: '<i class="fas fa-map-marker-alt me-1"></i>สถานที่',
                width: '200px',
                render: function(data) {
                    let locationText = '';
                    if (data.Location && data.LocationDetail) {
                        locationText = getLocationDisplayName(data.Location) + ' - ' + data.LocationDetail;
                    } else if (data.Location) {
                        locationText = getLocationDisplayName(data.Location);
                    } else if (data.LocationDetail) {
                        locationText = data.LocationDetail;
                    } else {
                        locationText = '-';
                    }
                    return '<div class="text-wrap" style="max-width: 200px;">' + locationText + '</div>';
                }
            },
            {
                data: 'Manufacturer',
                title: '<i class="fas fa-building me-1"></i>ผู้ผลิต',
                width: '120px',
                render: function(data) {
                    return '<span class="text-nowrap">' + (data || '-') + '</span>';
                }
            },
            {
                data: 'WindowsVersion',
                title: '<i class="fab fa-windows me-1"></i>Windows',
                width: '100px',
                render: function(data) {
                    return '<span class="text-nowrap">' + (data || '-') + '</span>';
                }
            },
            {
                data: 'InstallationDate',
                title: '<i class="fas fa-calendar-alt me-1"></i>วันติดตั้ง',
                width: '120px',
                render: function(data) {
                    if (data) {
                        return '<span class="text-nowrap">' + new Date(data).toLocaleDateString('th-TH') + '</span>';
                    } else {
                        return '<span class="text-nowrap">-</span>';
                    }
                }
            }
        ],
        dom: '<"row"<"col-sm-12 col-md-6"l><"col-sm-12 col-md-6"f>>' +
             '<"row"<"col-sm-12"tr>>' +
             '<"row"<"col-sm-12 col-md-5"i><"col-sm-12 col-md-7"p>>',
        pageLength: 100,
        lengthMenu: [[5, 10, 25, 50, 100, -1], [5, 10, 25, 50, 100, "ทั้งหมด"]],
        order: [[0, 'desc']],
        columnDefs: [
            { targets: [3, 4, 5], orderable: false },
            { targets: [0, 1, 2, 7, 8, 9], className: 'align-middle' },
            { targets: [6], className: 'align-middle text-start' }
        ],
        fixedColumns: {
            leftColumns: 1
        },
        drawCallback: function(settings) {
            // เพิ่มการจัดการสไตล์สำหรับตารางใน modal
            $('#computersTable').addClass('table-sm');
            
            // ปรับสไตล์ให้ตารางแสดงผลดีขึ้น
            $('#computersTable td').css({
                'vertical-align': 'middle',
                'padding': '8px'
            });
            
            // ปรับสไตล์ header
            $('#computersTable thead th').css({
                'vertical-align': 'middle',
                'padding': '10px 8px',
                'font-weight': 'bold'
            });
        }
    });
}

// สร้าง DataTable เมื่อ modal เปิดแล้ว
$(document).on('shown.bs.modal', '#employeeComputersModal', function() {
    const personID = $(this).data('personID');
    if (personID && !computersDataTable) {
        // สร้าง DataTable หลังจากที่ modal เปิดแล้วเพื่อให้คำนวณขนาดได้ถูกต้อง
        createComputersDataTable(personID);
    }
});

// ทำลาย DataTable เมื่อปิด modal
$(document).on('hidden.bs.modal', '#employeeComputersModal', function() {
    if (computersDataTable) {
        computersDataTable.destroy();
        computersDataTable = null;
    }
}); 