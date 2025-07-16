var SetProcessApp = {
    // กำหนดค่าเริ่มต้น
    config: {
        baseUrl: typeof appBasePath !== 'undefined' ? appBasePath + 'OracleStations' : '/OracleStations',
        serialDelimiters: ['\n', '\r\n', ',', ';'],
        maxSerialNumbers: 1000
    }, 

    // เริ่มต้นแอพ
    init: function() { 
        console.log('SetProcessApp initialized');
        this.bindEvents();
        this.loadDropdownData();
        this.updateSerialCount(); 
    },

    // ผูกเหตุการณ์ต่างๆ
    bindEvents: function() {
        // การเปลี่ยนแปลงใน serial input
        $('#serialInput').on('input', () => {
            this.updateSerialCount();
        });

        // ปุ่มล้าง serials
        $('#clearSerials').click(() => {
            this.clearSerials();
        });

        // ปุ่มตรวจสอบ serials
        $('#validateSerials').click(() => {
            this.validateSerials();
        });

        // ปุ่มตรวจสอบก่อนบันทึก
        $('#previewBtn').click(() => {
            this.showPreview();
        });

        // ปุ่มยืนยันบันทึก
        $('#confirmSubmit').click(() => {
            this.submitForm();
        });

        // ปุ่มดำเนินการต่อหลังตรวจสอบ
        $('#proceedAfterValidate').click(() => {
            $('#validateModal').modal('hide');
            this.showPreview();
        });

        // การส่งฟอร์ม
        $('#setProcessForm').submit((e) => {
            e.preventDefault();
            this.showPreview();
        });

        // Cascading dropdown events
        $('#factorySelect').change(() => {
            this.onFactoryChange();
        });

        $('#stageSelect').change(() => {
            this.onStageChange();
        });

        $('#processSelect').change(() => {
            this.onProcessChange();
        });

        $('#pdlineSelect').change(() => {
            this.onPdlineChange();
        });

        // Modal events
        $('#validateModal').on('hidden.bs.modal', () => {
            this.cleanupValidateModal();
        });
    },

    // โหลดข้อมูลสำหรับ dropdown
    loadDropdownData: function() {
        // โหลด factories
        $.get(this.config.baseUrl + '/GetFactories') 
            .done((data) => {
                this.populateDropdown('#factorySelect', data);
            })
            .fail((error) => {
                console.error('Error loading factories:', error);
                this.showError('ไม่สามารถโหลดข้อมูลโรงงานได้');
            });

        // โหลด stages (ทั้งหมด)
        $.get(this.config.baseUrl + '/GetStages')
            .done((data) => {
                this.populateDropdown('#stageSelect', data);
            })
            .fail((error) => {
                console.error('Error loading stages:', error);
                this.showError('ไม่สามารถโหลดข้อมูลขั้นตอนได้');
            });

        // โหลด processes (ทั้งหมด)
        $.get(this.config.baseUrl + '/GetProcesses')
            .done((data) => {
                this.populateDropdown('#processSelect', data);
            })
            .fail((error) => {
                console.error('Error loading processes:', error);
                this.showError('ไม่สามารถโหลดข้อมูลกระบวนการได้');
            });

        // โหลด pdlines (ทั้งหมด)
        $.get(this.config.baseUrl + '/GetPdlines')
            .done((data) => {
                this.populateDropdown('#pdlineSelect', data);
            })
            .fail((error) => {
                console.error('Error loading pdlines:', error);
                this.showError('ไม่สามารถโหลดข้อมูลสายการผลิตได้');
            });

        // โหลด terminals (ทั้งหมด)
        $.get(this.config.baseUrl + '/GetTerminals')
            .done((data) => {
                this.populateDropdown('#terminalSelect', data);
            })
            .fail((error) => {
                console.error('Error loading terminals:', error);
                this.showError('ไม่สามารถโหลดข้อมูลสถานีได้');
            });

        // โหลด routes (ทั้งหมด)
        $.get(this.config.baseUrl + '/GetRoutes')
            .done((data) => {
                this.populateDropdown('#routeSelect', data);
            })
            .fail((error) => {
                console.error('Error loading routes:', error);
                this.showError('ไม่สามารถโหลดข้อมูลเส้นทางได้');
            });
    },

    // เติมข้อมูลใน dropdown
    populateDropdown: function(selector, data) {
        const $select = $(selector);
        const defaultOption = $select.find('option').first();
        
        $select.empty().append(defaultOption);

        if (data && data.length > 0) {
            data.forEach(item => {
                const option = $('<option>')
                    .val(item.Id)
                    .text(item.Name + (item.Description ? ' - ' + item.Description : ''));
                $select.append(option);
            });

            // แสดงจำนวนตัวเลือก
            this.updateDropdownCount(selector, data.length);
            
            // ถ้ามีตัวเลือกเพียงตัวเดียว ให้เลือกตัวเลือกนั้นทันที
            if (data.length === 1) {
                $select.val(data[0].Id);
                
                // เพิ่ม visual feedback
                $select.addClass('auto-selected');
                setTimeout(() => {
                    $select.removeClass('auto-selected');
                }, 2000);
                
                // Trigger change event เพื่อให้ cascading dropdown ทำงาน
                $select.trigger('change');
                
                // แสดงข้อความแจ้งเตือน
                const optionText = data[0].Name + (data[0].Description ? ' - ' + data[0].Description : '');
                this.showAutoSelectMessage(selector, optionText);
            }
        }
    },

    // แสดงข้อความแจ้งเตือนเมื่อเลือกอัตโนมัติ
    showAutoSelectMessage: function(selector, optionText) {
        const fieldName = this.getFieldName(selector);
        
        // สร้าง toast notification
        const toastHtml = `
            <div class="toast align-items-center text-white bg-info border-0" role="alert" aria-live="assertive" aria-atomic="true">
                <div class="d-flex">
                    <div class="toast-body">
                        <i class="fas fa-magic me-2"></i>
                        <strong>${fieldName}</strong> เลือกอัตโนมัติ: <strong>${optionText}</strong>
                    </div>
                    <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button>
                </div>
            </div>
        `;

        // เพิ่ม toast container ถ้ายังไม่มี
        if ($('#toastContainer').length === 0) {
            $('body').append('<div id="toastContainer" class="toast-container position-fixed top-0 end-0 p-3" style="z-index: 9999;"></div>');
        }

        // เพิ่ม toast และแสดง
        $('#toastContainer').append(toastHtml);
        const toastElement = $('#toastContainer .toast').last();
        const toast = new bootstrap.Toast(toastElement);
        toast.show();

        // ลบ toast หลังจาก 3 วินาที
        setTimeout(() => {
            toastElement.remove();
        }, 3000);
    },

    // รับชื่อฟิลด์จาก selector
    getFieldName: function(selector) {
        const fieldNames = {
            '#factorySelect': 'โรงงาน',
            '#stageSelect': 'ขั้นตอน', 
            '#processSelect': 'กระบวนการ',
            '#pdlineSelect': 'สายการผลิต',
            '#terminalSelect': 'สถานี',
            '#routeSelect': 'เส้นทาง'
        };
        return fieldNames[selector] || 'ฟิลด์';
    },

    // อัพเดทจำนวนตัวเลือกใน dropdown
    updateDropdownCount: function(selector, count) {
        const $select = $(selector);
        const $label = $select.closest('.mb-3').find('.form-label');
        
        // ลบ badge เดิม
        $label.find('.badge').remove();
        
        // เพิ่ม badge ใหม่
        const badgeClass = count === 0 ? 'bg-secondary' : count === 1 ? 'bg-success' : 'bg-primary';
        const badgeText = count === 0 ? 'ไม่มีตัวเลือก' : count === 1 ? '1 ตัวเลือก' : `${count} ตัวเลือก`;
        
        $label.append(` <span class="badge ${badgeClass} ms-2">${badgeText}</span>`);
    },

    // อัพเดทจำนวน serial numbers
    updateSerialCount: function() {
        const serialNumbers = this.getSerialNumbers();
        $('#countNumber').text(serialNumbers.length);
        
        if (serialNumbers.length > this.config.maxSerialNumbers) {
            $('#serialCount').html(`
                <span class="badge bg-danger">
                    <i class="fas fa-exclamation-triangle me-1"></i>
                    เกินจำนวนสูงสุด: ${serialNumbers.length} รายการ (สูงสุด ${this.config.maxSerialNumbers})
                </span>
            `);
        } else if (serialNumbers.length > 0) {
            $('#serialCount').html(`
                <span class="badge bg-success">
                    <i class="fas fa-check me-1"></i>
                    จำนวน: ${serialNumbers.length} รายการ
                </span>
            `);
        } else {
            $('#serialCount').html(`
                <span class="badge bg-light text-dark">
                    <i class="fas fa-list-ol me-1"></i>
                    จำนวน: 0 รายการ
                </span>
            `);
        }
    },

    // ดึง serial numbers จาก input
    getSerialNumbers: function() {
        const input = $('#serialInput').val().trim();
        if (!input) return [];

        // แยกด้วย delimiters ต่างๆ
        let serialNumbers = [];
        this.config.serialDelimiters.forEach(delimiter => {
            if (input.includes(delimiter)) {
                serialNumbers = input.split(delimiter);
                return false; // หยุดลูป
            }
        });
 
        if (serialNumbers.length === 0) {
            serialNumbers = [input];
        }

        // ลบช่องว่างและกรองค่าว่าง
        return serialNumbers
            .map(s => s.trim())
            .filter(s => s.length > 0)
            .filter((value, index, self) => self.indexOf(value) === index); // ลบข้อมูลซ้ำ
    },

    // ล้าง serials
    clearSerials: function() {
        Swal.fire({
            title: 'ยืนยันการลบ',
            text: 'คุณต้องการลบ Serial Numbers ทั้งหมดหรือไม่?',
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: 'ลบ',
            cancelButtonText: 'ยกเลิก',
            confirmButtonColor: '#dc3545'
        }).then((result) => {
            if (result.isConfirmed) {
                $('#serialInput').val('');
                this.updateSerialCount();
                this.showSuccess('ลบ Serial Numbers เรียบร้อย');
            }
        });
    },

    // ตรวจสอบ serials
    validateSerials: function() {
        const serialNumbers = this.getSerialNumbers();
        
        if (serialNumbers.length === 0) {
            this.showWarning('กรุณาใส่ Serial Number');
            return;
        }

        if (serialNumbers.length > this.config.maxSerialNumbers) {
            this.showError(`จำนวน Serial Numbers เกินจำนวนสูงสุด (${this.config.maxSerialNumbers})`);
            return;
        }

        // แสดง loading
        this.showLoading('กำลังตรวจสอบ Serial Numbers...');

        // เรียก API ตรวจสอบ
        $.ajax({
            url: this.config.baseUrl + '/ValidateSerialNumbers',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(serialNumbers),
            success: (response) => {
                Swal.close();
                if (response.success) {
                    this.showValidateResults(response.data);
                } else {
                    this.showError(response.message || 'เกิดข้อผิดพลาดในการตรวจสอบ');
                }
            },
            error: (xhr, status, error) => {
                Swal.close();
                console.error('Error validating serial numbers:', error);
                this.showError('เกิดข้อผิดพลาดในการตรวจสอบ Serial Numbers');
            }
        });
    },

    // แสดงผลการตรวจสอบด้วย DataTable
    showValidateResults: function(data) {
        // Clean up modal ก่อน
        this.cleanupValidateModal();

        // เติมข้อมูลใหม่
        data.forEach((item, index) => {
            const rowClass = item.Found ? '' : 'table-warning';
            const statusBadge = item.Found ? 
                '<span class="badge bg-success"><i class="fas fa-check me-1"></i>พบ</span>' : 
                '<span class="badge bg-danger"><i class="fas fa-times me-1"></i>ไม่พบ</span>';

            const row = `
                <tr class="${rowClass}">
                    <td>${index + 1}</td>
                    <td><code>${item.SerialNumber}</code></td>
                    <td>${item.WorkOrder}</td>
                    <td>${item.PartId}</td>
                    <td>${statusBadge}<br/><small>${item.CurrentStatus}</small></td>
                    <td><small>${item.WorkFlag}</small></td>
                    <td><small>${item.CurrentProcess}</small></td>
                    <td><small>${item.CurrentTerminal}</small></td>
                    <td><small>${item.CurrentStage}</small></td>
                    <td><small>${item.CurrentPdline}</small></td>
                    <td><small>${item.InProcessTime}</small></td>
                    <td><small>${item.OutProcessTime}</small></td>
                </tr>
            `;
            $('#validateTable tbody').append(row);
        });

        // สร้าง DataTable ใหม่
        const table = $('#validateTable').DataTable({
            responsive: true,
            pageLength: 25,
            autoWidth: true,
            lengthMenu: [10, 25, 50, 100],
            order: [[0, 'asc']],
            language: {
                url: 'https://cdn.datatables.net/plug-ins/1.13.7/i18n/th.json'
            },
            dom: 'Bfrtip',
            buttons: [
                {
                    extend: 'excel',
                    text: '<i class="fas fa-file-excel me-1"></i>Export Excel',
                    className: 'btn btn-success btn-sm',
                    title: 'Serial Numbers Status - ' + new Date().toLocaleDateString('th-TH')
                },
                {
                    extend: 'print',
                    text: '<i class="fas fa-print me-1"></i>Print',
                    className: 'btn btn-info btn-sm',
                    title: 'Serial Numbers Status'
                },
                {
                    text: '<i class="fas fa-sync me-1"></i>Refresh',
                    className: 'btn btn-secondary btn-sm',
                    action: () => {
                        this.validateSerials();
                    }
                }
            ],
            columnDefs: [
                { targets: [0], orderable: true },
                { targets: [1, 2, 3], orderable: true },
                { targets: '_all', orderable: false }
            ],
            initComplete: function() {
                // เพิ่มข้อมูลสรุป
                const foundCount = data.filter(item => item.Found).length;
                const notFoundCount = data.length - foundCount;
                
                const summaryHtml = `
                    <div class="alert alert-info mt-3">
                        <h6><i class="fas fa-chart-pie me-2"></i>สรุปผลการตรวจสอบ</h6>
                        <div class="row">
                            <div class="col-md-3">
                                <span class="badge bg-primary fs-6">ทั้งหมด: ${data.length}</span>
                            </div>
                            <div class="col-md-3">
                                <span class="badge bg-success fs-6">พบในระบบ: ${foundCount}</span>
                            </div>
                            <div class="col-md-3">
                                <span class="badge bg-danger fs-6">ไม่พบ: ${notFoundCount}</span>
                            </div>
                            <div class="col-md-3">
                                <span class="badge bg-info fs-6">อัตราพบ: ${((foundCount/data.length)*100).toFixed(1)}%</span>
                            </div>
                        </div>
                    </div>
                `;
                
                $('#validateContent').append(summaryHtml);
            }
        });

        // แสดง modal
        $('#validateModal').modal('show');
    },

    // แสดงตัวอย่างก่อนบันทึก
    showPreview: function() {
        const serialNumbers = this.getSerialNumbers();
        const processId = $('#processSelect').val();
        const terminalId = $('#terminalSelect').val();
        const routeId = $('#routeSelect').val();
        const stageId = $('#stageSelect').val();
        const pdlineId = $('#pdlineSelect').val();
        const remarks = $('#remarksInput').val();

        // ตรวจสอบข้อมูล
        if (serialNumbers.length === 0) {
            this.showWarning('กรุณาใส่ Serial Number');
            return;
        }

        if (!processId) {
            this.showWarning('กรุณาเลือกกระบวนการ');
            return;
        }

        if (serialNumbers.length > this.config.maxSerialNumbers) {
            this.showError(`จำนวน Serial Numbers เกินจำนวนสูงสุด (${this.config.maxSerialNumbers})`);
            return;
        }

        // สร้าง preview content
        let html = '<div class="container-fluid">';
        
        // ข้อมูลการตั้งค่า
        html += '<div class="row">';
        html += '<div class="col-md-6">';
        html += '<h6><i class="fas fa-cogs me-2"></i>การตั้งค่า</h6>';
        html += '<table class="table table-sm">';
        
        const factoryId = $('#factorySelect').val();
        if (factoryId) {
            html += `<tr><td>โรงงาน:</td><td>${$('#factorySelect option:selected').text()}</td></tr>`;
        }
        
        if (stageId) {
            html += `<tr><td>ขั้นตอน:</td><td>${$('#stageSelect option:selected').text()}</td></tr>`;
        }
        
        html += `<tr><td>กระบวนการ:</td><td><strong>${$('#processSelect option:selected').text()}</strong></td></tr>`;
        
        if (pdlineId) {
            html += `<tr><td>สายการผลิต:</td><td>${$('#pdlineSelect option:selected').text()}</td></tr>`;
        }
        
        if (terminalId) {
            html += `<tr><td>สถานี:</td><td>${$('#terminalSelect option:selected').text()}</td></tr>`;
        }
        
        if (routeId) {
            html += `<tr><td>เส้นทาง:</td><td>${$('#routeSelect option:selected').text()}</td></tr>`;
        }
        
        if (remarks) {
            html += `<tr><td>หมายเหตุ:</td><td>${remarks}</td></tr>`;
        }
        
        html += '</table>';
        html += '</div>';

        // Serial Numbers
        html += '<div class="col-md-6">';
        html += '<h6><i class="fas fa-barcode me-2"></i>Serial Numbers</h6>';
        html += `<div class="alert alert-info">จำนวนทั้งหมด: <strong>${serialNumbers.length}</strong> รายการ</div>`;
        html += '<div style="max-height: 200px; overflow-y: auto;">';
        html += '<ul class="list-unstyled">';
        
        serialNumbers.forEach((serial, index) => {
            html += `<li><span class="badge bg-light text-dark me-2">${index + 1}</span>${serial}</li>`;
        });
        
        html += '</ul>';
        html += '</div>';
        html += '</div>';
        html += '</div>';
        html += '</div>';

        $('#previewContent').html(html);
        $('#previewModal').modal('show');
    },

    // ส่งฟอร์ม
    submitForm: function() {
        const serialNumbers = this.getSerialNumbers();
        const processId = $('#processSelect').val();
        const terminalId = $('#terminalSelect').val() || null;
        const routeId = $('#routeSelect').val() || null;
        const stageId = $('#stageSelect').val() || null;
        const pdlineId = $('#pdlineSelect').val() || null;
        const remarks = $('#remarksInput').val() || null;

        const data = {
            serialNumbers: serialNumbers,
            processId: parseInt(processId),
            terminalId: terminalId ? parseInt(terminalId) : null,
            routeId: routeId ? parseInt(routeId) : null,
            stageId: stageId ? parseInt(stageId) : null,
            pdlineId: pdlineId ? parseInt(pdlineId) : null,
            remarks: remarks
        };

        // ปิด preview modal
        $('#previewModal').modal('hide');

        // แสดง loading
        this.showLoading('กำลังดำเนินการ...');

        // ส่งข้อมูล
        $.ajax({
            url: this.config.baseUrl + '/SetProcess',
            type: 'POST',
            contentType: 'application/json',
            data: JSON.stringify(data),
            success: (response) => {
                Swal.close();
                this.showResults(response);
            },
            error: (xhr, status, error) => {
                Swal.close();
                console.error('Error:', error);
                this.showError('เกิดข้อผิดพลาดในการส่งข้อมูล');
            }
        });
    },

    // แสดงผลลัพธ์
    showResults: function(response) {
        let html = '<div class="container-fluid">';
        
        // สรุปผล
        html += '<div class="row mb-3">';
        html += '<div class="col-12">';
        
        if (response.success) {
            html += '<div class="alert alert-success">';
            html += '<i class="fas fa-check-circle me-2"></i>';
            html += `<strong>สำเร็จ!</strong> ${response.message}`;
            html += '</div>';
        } else {
            html += '<div class="alert alert-warning">';
            html += '<i class="fas fa-exclamation-triangle me-2"></i>';
            html += `<strong>มีข้อผิดพลาด!</strong> ${response.message}`;
            html += '</div>';
        }
        
        html += '</div>';
        html += '</div>';

        // รายละเอียดผลลัพธ์
        if (response.results && response.results.length > 0) {
            html += '<div class="row">';
            html += '<div class="col-12">';
            html += '<h6><i class="fas fa-list me-2"></i>รายละเอียดผลลัพธ์</h6>';
            html += '<div class="table-responsive">';
            html += '<table class="table table-sm table-striped">';
            html += '<thead>';
            html += '<tr>';
            html += '<th>ลำดับ</th>';
            html += '<th>Serial Number</th>';
            html += '<th>สถานะ</th>';
            html += '<th>ข้อความ</th>';
            html += '</tr>';
            html += '</thead>';
            html += '<tbody>';

            response.results.forEach((result, index) => {
                html += '<tr>';
                html += `<td>${index + 1}</td>`;
                html += `<td>${result.serialNumber}</td>`;
                html += `<td>`;
                
                if (result.success) {
                    html += '<span class="badge bg-success"><i class="fas fa-check me-1"></i>สำเร็จ</span>';
                } else {
                    html += '<span class="badge bg-danger"><i class="fas fa-times me-1"></i>ล้มเหลว</span>';
                }
                
                html += `</td>`;
                html += `<td>${result.message}</td>`;
                html += '</tr>';
            });

            html += '</tbody>';
            html += '</table>';
            html += '</div>';
            html += '</div>';
            html += '</div>';
        }

        html += '</div>';

        $('#resultsContent').html(html);
        $('#resultsModal').modal('show');

        // ล้างฟอร์มหากสำเร็จทั้งหมด
        if (response.success) {
            this.clearForm();
        }
    },

    // ล้างฟอร์ม
    clearForm: function() {
        $('#serialInput').val('');
        $('#factorySelect').val('');
        $('#stageSelect').val('');
        $('#processSelect').val('');
        $('#pdlineSelect').val('');
        $('#terminalSelect').val('');
        $('#routeSelect').val('');
        $('#remarksInput').val('');
        this.updateSerialCount();
    },

    // Cascading dropdown functions
    onFactoryChange: function() {
        const factoryId = $('#factorySelect').val();
        
        // ล้างและโหลด stages ที่เกี่ยวข้อง
        this.clearDropdown('#stageSelect');
        this.clearDropdown('#processSelect');
        this.clearDropdown('#pdlineSelect');
        this.clearDropdown('#terminalSelect');
        this.clearDropdown('#routeSelect');

        if (factoryId) {
            // โหลด stages ของ factory นี้
            $.get(this.config.baseUrl + '/GetStages', { factoryId: factoryId })
                .done((data) => {
                    this.clearAndPopulateDropdown('#stageSelect', data);
                })
                .fail((error) => {
                    console.error('Error loading stages:', error);
                });

            // โหลด pdlines ของ factory นี้
            $.get(this.config.baseUrl + '/GetPdlines', { factoryId: factoryId })
                .done((data) => {
                    this.clearAndPopulateDropdown('#pdlineSelect', data);
                })
                .fail((error) => {
                    console.error('Error loading pdlines:', error);
                });

            // โหลด routes ของ factory นี้
            $.get(this.config.baseUrl + '/GetRoutes', { factoryId: factoryId })
                .done((data) => {
                    this.clearAndPopulateDropdown('#routeSelect', data);
                })
                .fail((error) => {
                    console.error('Error loading routes:', error);
                });
        }
    },

    onStageChange: function() {
        const stageId = $('#stageSelect').val();
        const factoryId = $('#factorySelect').val();
        
        // ล้างและโหลด processes ที่เกี่ยวข้อง
        this.clearDropdown('#processSelect');
        this.clearDropdown('#terminalSelect');

        if (stageId) {
            // โหลด processes ของ stage นี้
            $.get(this.config.baseUrl + '/GetProcesses', { stageId: stageId })
                .done((data) => {
                    this.clearAndPopulateDropdown('#processSelect', data);
                })
                .fail((error) => {
                    console.error('Error loading processes:', error);
                });

            // โหลด terminals ของ stage นี้
            $.get(this.config.baseUrl + '/GetTerminals', { stageId: stageId })
                .done((data) => {
                    this.clearAndPopulateDropdown('#terminalSelect', data);
                })
                .fail((error) => {
                    console.error('Error loading terminals:', error);
                });

            // โหลด pdlines ของ factory (ไม่ filter ด้วย stage)
            if (factoryId) {
                $.get(this.config.baseUrl + '/GetPdlines', { factoryId: factoryId })
                    .done((data) => {
                        this.clearAndPopulateDropdown('#pdlineSelect', data);
                    })
                    .fail((error) => {
                        console.error('Error loading pdlines:', error);
                    });
            }
        }
    },

    onProcessChange: function() {
        const processId = $('#processSelect').val();
        const stageId = $('#stageSelect').val();
        const pdlineId = $('#pdlineSelect').val();
        
        // ล้างและโหลด terminals ที่เกี่ยวข้อง
        this.clearDropdown('#terminalSelect');

        if (processId) {
            // โหลด terminals ของ process นี้
            const params = { processId: processId };
            
            if (stageId) params.stageId = stageId;
            if (pdlineId) params.pdlineId = pdlineId;

            $.get(this.config.baseUrl + '/GetTerminals', params)
                .done((data) => {
                    this.clearAndPopulateDropdown('#terminalSelect', data);
                })
                .fail((error) => {
                    console.error('Error loading terminals:', error);
                });
        }
    },

    onPdlineChange: function() {
        const pdlineId = $('#pdlineSelect').val();
        const processId = $('#processSelect').val();
        const stageId = $('#stageSelect').val();
        
        // ล้างและโหลด terminals ที่เกี่ยวข้อง
        this.clearDropdown('#terminalSelect');

        if (pdlineId) {
            // โหลด terminals ของ pdline นี้
            const params = { pdlineId: pdlineId };
            
            if (processId) params.processId = processId;
            if (stageId) params.stageId = stageId;

            $.get(this.config.baseUrl + '/GetTerminals', params)
                .done((data) => {
                    this.clearAndPopulateDropdown('#terminalSelect', data);
                })
                .fail((error) => {
                    console.error('Error loading terminals:', error);
                });
        }
    },

    // ล้าง dropdown
    clearDropdown: function(selector) {
        const $select = $(selector);
        const defaultOption = $select.find('option').first();
        $select.empty().append(defaultOption);
        
        // ล้าง badge จำนวนตัวเลือก
        this.updateDropdownCount(selector, 0);
    },

    // ล้าง dropdown และเลือกอัตโนมัติถ้ามีตัวเลือกเดียว
    clearAndPopulateDropdown: function(selector, data) {
        this.clearDropdown(selector);
        this.populateDropdown(selector, data);
    },

    // Clean up validate modal
    cleanupValidateModal: function() {
        // ทำลาย DataTable ถ้ามี
        if ($.fn.DataTable.isDataTable('#validateTable')) {
            $('#validateTable').DataTable().destroy();
        }
        
        // เคลียร์ข้อมูล
        $('#validateTable tbody').empty();
        $('#validateContent .alert').remove();
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
    },

    showLoading: function(message) {
        Swal.fire({
            title: message,
            allowOutsideClick: false,
            didOpen: () => {
                Swal.showLoading();
            }
        });
    }
}; 