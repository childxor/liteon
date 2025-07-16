// Oracle Test Application - ใช้ Controller สำหรับ HTML Generation
const OracleTestApp = {
    baseUrl: typeof appBasePath !== 'undefined' ? appBasePath + 'OracleTest' : '/OracleTest',
    
    init() {
        console.log('OracleTestApp initializing...');
        this.initEventHandlers();
        this.loadDashboardStats();
        
        // เพิ่มฟังก์ชันตรวจสอบสถานะระบบ
        this.addDebugHelpers();
        console.log('OracleTestApp initialized successfully');
    },

    // เพิ่มฟังก์ชันช่วยในการ debug
    addDebugHelpers() {
        // เพิ่มฟังก์ชันตรวจสอบสถานะใน window object สำหรับ debugging
        window.OracleDebug = {
            getState: () => ({
                currentTableName: this.currentTableName,
                currentTableData: this.currentTableData ? this.currentTableData.length : 0,
                currentTableColumns: this.currentTableColumns ? this.currentTableColumns.length : 0,
                currentFilteredData: this.currentFilteredData ? this.currentFilteredData.length : 0,
                currentFilteredColumns: this.currentFilteredColumns ? this.currentFilteredColumns.length : 0,
                hasCurrentDataTable: !!this.currentDataTable,
                hasHistoryDataTable: !!this.historyDataTable,
                dataTableVisible: $('#dataTableContainer').is(':visible'),
                isDataTable: $.fn.DataTable.isDataTable('#oracleDataTable'), 
                dataTablesWrapperCount: $('.dataTables_wrapper').length,
                tableElementExists: $('#oracleDataTable').length > 0,
                columnsInfo: {
                    original: this.currentTableColumns,
                    filtered: this.currentFilteredColumns,
                    hiddenCount: this.currentTableColumns && this.currentFilteredColumns ? 
                        this.currentTableColumns.length - this.currentFilteredColumns.length : 0
                }
            }),
            clearAll: async () => {
                await this.clearPreviousResults();
                console.log('All data cleared');
            },
            testQuery: (query) => {
                $('#sqlQuery').val(query || 'SELECT * FROM DUAL');
                this.executeQuery();
            },
            forceDestroyDataTable: async () => {
                console.log('Force destroying DataTable...');
                try {
                    await this.forceCleanupDataTable();
                    await this.ensureCleanTableStructure();
                    
                    console.log('DataTable force destroyed and recreated');
                    return true;
                } catch (error) {
                    console.error('Force destroy failed:', error);
                    return false;
                }
            },
            forceCompleteReset: async () => {
                console.log('Force complete reset...');
                try {
                    await this.forceCompleteReset();
                    console.log('Complete reset successful');
                    return true;
                } catch (error) {
                    console.error('Complete reset failed:', error);
                    return false;
                }
            },
            simulateTableChange: async (newTableName) => {
                console.log(`Simulating table change to: ${newTableName}`);
                const oldTable = this.currentTableName;
                this.currentTableName = newTableName;
                
                try {
                    await this.forceCompleteReset();
                    console.log(`Table changed from ${oldTable} to ${newTableName}`);
                    return true;
                } catch (error) {
                    console.error('Table change simulation failed:', error);
                    this.currentTableName = oldTable; // Revert
                    return false;
                }
            },
            recreateTable: async () => {
                try {
                    await this.ensureCleanTableStructure();
                    console.log('Table element recreated');
                    return true;
                } catch (error) {
                    console.error('Table recreation failed:', error);
                    return false;
                }
            }
        };
    },

    initEventHandlers() {
        // Tab navigation
        $('#query-tab').on('shown.bs.tab', () => this.onQueryTabShown());
        $('#history-tab').on('shown.bs.tab', () => this.onHistoryTabShown());
        $('#connection-tab').on('shown.bs.tab', () => this.onConnectionTabShown());
        $('#btnRefreshHistory').on('click', () => this.loadQueryHistory());
        
        // Query execution
        $('#btnExecuteQuery').on('click', () => this.executeQuery());
        $('#btnClearQuery').on('click', () => this.clearQuery());
        
        // Example query buttons
        $(document).on('click', '.example-query', (e) => this.loadExampleQuery(e));
        
        // Connection test
        $('#btnTestConnection').on('click', () => this.testConnection());
        $('#btnShowConnectionTest').on('click', () => this.quickConnectionTest());
        $('#btnGetData').on('click', () => this.getData());
        
        // Event delegation for dynamic buttons
        $(document).on('click', '.view-group-detail', (e) => this.viewGroupDetail(e));
        $(document).on('click', '.view-detail', (e) => this.viewDetail(e));
        $(document).on('click', '.reuse-query', (e) => this.reuseQuery(e));
        $(document).on('click', '.show-column-info', (e) => this.showColumnInfo(e));
        $(document).on('click', '.export-history', (e) => this.exportHistory(e));
        $(document).on('click', '.copy-sql', (e) => this.copySqlToClipboard(e));
        
        // Child row toggle for DataTables
        $(document).on('click', '.child-row-toggle', (e) => this.toggleChildRow(e));
        
        // Data table action buttons
        $(document).on('click', '.edit-row', (e) => this.editRow(e));
        $(document).on('click', '.delete-row', (e) => this.deleteRow(e));
        $(document).on('click', '.select-for-where', (e) => this.selectForWhere(e));
        
        // Insert record buttons
        $('#btnInsertToCurrentTable').on('click', () => this.insertToCurrentTable());
        $('#btnInsertToTable').on('click', () => this.insertToTable());
        
        // Enter key support for query execution
        $('#sqlQuery').on('keydown', (e) => {
            if (e.ctrlKey && e.key === 'Enter') {
                e.preventDefault();
                this.executeQuery();
            }
        });
    },

    // Tab event handlers
    onQueryTabShown() {
        console.log('Query tab shown');
        // ไม่ต้องทำอะไรพิเศษ เพียงแค่ log
    },

    onHistoryTabShown() {
        console.log('History tab shown');
        this.loadQueryHistory();
    },

    onConnectionTabShown() {
        console.log('Connection tab shown');
        // ไม่ต้องทำอะไรพิเศษ เพียงแค่ log
    },

    async loadQueryHistory() {
        try {
            $('#historyContainer').html('<div class="text-center py-4"><div class="spinner-border"></div><p class="mt-2">กำลังโหลดประวัติ...</p></div>');
            
            const response = await $.get(`${this.baseUrl}/GetQueryHistory`);
            if (response.success) {
                if (response.data && response.data.length > 0) {
                    this.renderHistoryTable(response.data);
                } else {
                    $('#historyContainer').html(`
                        <div class="alert alert-info text-center">
                            <i class="fas fa-info-circle fa-2x mb-2"></i>
                            <h5>ยังไม่มีประวัติการใช้งาน</h5>
                            <p class="mb-2">${response.message || 'เมื่อคุณใช้งาน Query Builder ระบบจะบันทึกประวัติไว้ให้ดูที่นี่'}</p>
                            <small class="text-muted">กรุณาลองทดสอบการเชื่อมต่อ Oracle หรือรัน Query ใดๆ เพื่อสร้างประวัติ</small>
                        </div>
                    `);
                }
            } else {
                if (response.message && response.message.includes('ตาราง OracleActivityHistory ยังไม่ได้ถูกสร้าง')) {
                    $('#historyContainer').html(`
                        <div class="alert alert-warning">
                            <i class="fas fa-exclamation-triangle fa-2x mb-3"></i>
                            <h5>ต้องการสร้างตารางประวัติ</h5>
                            <p class="mb-3">${response.message}</p>
                            <div class="text-start">
                                <h6><i class="fas fa-magic me-2"></i>วิธีแก้ไขอัตโนมัติ (แนะนำ):</h6>
                                <div class="mb-3">
                                    <button id="btnAutoCreateTables" class="btn btn-success">
                                        <i class="fas fa-magic me-1"></i>สร้างตารางอัตโนมัติ
                                    </button>
                                    <small class="d-block text-muted mt-1">คลิกเพื่อสร้างตารางและข้อมูลทดสอบอัตโนมัติ</small>
                                </div>
                                
                                <h6><i class="fas fa-tools me-2"></i>หรือวิธีการแก้ไขด้วยตนเอง:</h6>
                                <ol class="mb-3">
                                    <li>เปิด SQL Server Management Studio</li>
                                    <li>เชื่อมต่อกับฐานข้อมูล <strong>HRM_IPS</strong></li>
                                    <li>เปิดไฟล์ <code>SQL/CreateOracleHistoryTables.sql</code></li>
                                    <li>รันคำสั่ง SQL ทั้งหมด</li>
                                    <li>รีเฟรชหน้านี้</li>
                                </ol>
                                <button class="btn btn-outline-warning btn-sm" onclick="location.reload()">
                                    <i class="fas fa-sync-alt me-1"></i>รีเฟรชหน้า
                                </button>
                                <button class="btn btn-outline-info btn-sm ms-2" onclick="window.open((typeof appBasePath !== 'undefined' ? appBasePath : '/') + 'SQL/CreateOracleHistoryTables.sql', '_blank')">
                                    <i class="fas fa-download me-1"></i>ดาวน์โหลด SQL Script
                                </button>
                            </div>
                        </div>
                    `);
                    
                    // เพิ่ม event handler สำหรับปุ่มสร้างตารางอัตโนมัติ
                    $('#btnAutoCreateTables').on('click', () => this.initializeHistoryTables());
                } else {
                    $('#historyContainer').html(`<div class="alert alert-danger">${response.message || 'ไม่สามารถโหลดประวัติได้'}</div>`);
                }
            }
        } catch (error) {
            $('#historyContainer').html('<div class="alert alert-danger">เกิดข้อผิดพลาดในการโหลดประวัติ</div>');
        }
    },

    async executeQuery() {
        const sqlQuery = $('#sqlQuery').val().trim();
        
        if (!sqlQuery) {
            this.showAlert('warning', 'กรุณาใส่ SQL Query');
            $('#sqlQuery').focus();
            return;
        }

        try {
            const $btn = $('#btnExecuteQuery');
            $btn.html('<i class="fas fa-spinner fa-spin me-2"></i>กำลังดำเนินการ...').prop('disabled', true);
            
            // ล้างข้อมูลเก่าทั้งหมด (แต่ไม่ล้างชื่อตารางเพื่อให้ตรวจสอบการเปลี่ยนแปลงได้)
            await this.clearPreviousResults();
            
            console.log('Executing SQL:', sqlQuery);
            
            const response = await $.post(`${this.baseUrl}/ExecuteQuery`, {
                sqlQuery: sqlQuery,
                parameters: '' // ส่งเป็น empty string
            });

            console.log('Query response:', response);

            if (response.success) {
                await this.displayQueryResult(response);
                this.showAlert('success', response.message, 3000);
                
                // รีเฟรชประวัติหลังจากรัน query สำเร็จ
                if ($('#history-tab').hasClass('active')) {
                    setTimeout(() => this.loadQueryHistory(), 500);
                }
            } else {
                this.showAlert('danger', response.message || 'เกิดข้อผิดพลาดในการรัน Query');
                $('#queryResult').html(`
                    <div class="alert alert-danger">
                        <i class="fas fa-exclamation-triangle me-2"></i>
                        <strong>ข้อผิดพลาด:</strong> ${response.message || 'ไม่สามารถรัน Query ได้'}
                    </div>
                `);
            }
        } catch (error) {
            console.error('Execute query error:', error);
            const errorMessage = error.responseJSON?.message || error.message || 'เกิดข้อผิดพลาดในการเรียกใช้ Query';
            this.showAlert('danger', errorMessage);
            $('#queryResult').html(`
                <div class="alert alert-danger">
                    <i class="fas fa-exclamation-triangle me-2"></i>
                    <strong>ข้อผิดพลาดการเชื่อมต่อ:</strong> ${errorMessage}
                </div>
            `);
        } finally {
            $('#btnExecuteQuery').html('<i class="fas fa-play me-2"></i>เรียกใช้ Query').prop('disabled', false);
        }
    },

    // ฟังก์ชันใหม่สำหรับล้างผลลัพธ์เก่า
    async clearPreviousResults() {
        try {
            console.log('Clearing previous results...');
            
            // ล้าง result container
            $('#queryResult').html('');
            $('#recordCount').text('');
            
            // ใช้ helper function สำหรับ cleanup DataTable
            await this.forceCleanupDataTable();
            
            // สร้างโครงสร้างตารางใหม่
            await this.ensureCleanTableStructure();
            
            // ซ่อน container
            $('#dataTableContainer').hide();
            
            // รีเซ็ตตัวแปรสถานะ (ยกเว้นชื่อตารางเพื่อให้ตรวจสอบการเปลี่ยนแปลงได้)
            this.currentDataTable = null;
            // ไม่ล้าง this.currentTableName เพื่อให้ตรวจสอบการเปลี่ยนตารางได้
            this.currentTableData = null;
            this.currentTableColumns = null;
            this.currentFilteredData = null;
            this.currentFilteredColumns = null;
            
            console.log('Previous results cleared successfully. Current table preserved:', this.currentTableName);
        } catch (error) {
            console.error('Critical error clearing previous results:', error);
            // Emergency cleanup
            try {
                const tableWrapper = $('.table-responsive');
                if (tableWrapper.length) {
                    tableWrapper.html(`
                        <table id="oracleDataTable" class="table table-striped table-hover" style="width:100%">
                            <thead class="table-dark"></thead>
                            <tbody></tbody>
                        </table>
                    `);
                }
                $('#dataTableContainer').hide();
                this.currentDataTable = null;
                // ไม่ล้าง this.currentTableName เพื่อให้ตรวจสอบการเปลี่ยนตารางได้
                this.currentTableData = null;
                this.currentTableColumns = null;
                this.currentFilteredData = null;
                this.currentFilteredColumns = null;
            } catch (emergencyError) {
                console.error('Emergency cleanup failed:', emergencyError);
            }
        }
    },

    async displayQueryResult(response) {
        try {
            console.log('Displaying query result:', response);
            
            // แสดงข้อความสำเร็จพร้อมข้อมูลสถิติ
            const statsHtml = response.data && response.data.length > 0 ? 
                `<small class="d-block mt-1">พบข้อมูล ${response.data.length} รายการ, ${response.columns ? response.columns.length : 0} คอลัมน์</small>` : '';
                
            $('#queryResult').html(`
                <div class="alert alert-success">
                    <i class="fas fa-check-circle me-2"></i>
                    ${response.message}
                    ${statsHtml}
                </div>
            `);
            
            if (response.data && response.data.length > 0 && response.columns && response.columns.length > 0) {
                // ดึงชื่อตารางจาก SQL
                const tableName = this.extractTableNameFromSQL(response.sql || '');
                
                // ตรวจสอบว่าเป็นตารางใหม่หรือไม่
                const isDifferentTable = this.currentTableName && this.currentTableName !== tableName;
                
                console.log('Table comparison:', {
                    previousTable: this.currentTableName,
                    newTable: tableName,
                    isDifferentTable: isDifferentTable
                });
                
                // ถ้าเป็นตารางใหม่ ให้ force cleanup ก่อน
                if (isDifferentTable) {
                    console.log('Different table detected, forcing complete cleanup...');
                    this.showAlert('info', `เปลี่ยนจากตาราง ${this.currentTableName} ไปยัง ${tableName} - กำลังเตรียมตารางใหม่...`, 3000);
                    await this.forceCompleteReset();
                }
                
                // เก็บข้อมูลปัจจุบัน (ข้อมูลต้นฉบับ)
                this.currentTableName = tableName;
                this.currentTableData = [...response.data]; // เก็บข้อมูลต้นฉบับ
                this.currentTableColumns = [...response.columns]; // เก็บคอลัมน์ต้นฉบับ
                
                console.log('Setting current table data:', {
                    tableName: this.currentTableName,
                    dataRows: this.currentTableData.length,
                    columns: this.currentTableColumns.length,
                    isDifferentTable: isDifferentTable
                });
                
                // สร้างตารางใหม่ (renderDataTable จะจัดการ cleanup เอง)
                const renderResult = await this.renderDataTable(response.columns, response.data, tableName);
                
                // เก็บข้อมูลที่กรองแล้วจากผลลัพธ์ของ renderDataTable
                if (renderResult && renderResult.success) {
                    this.currentFilteredData = renderResult.filteredData || response.data;
                    this.currentFilteredColumns = renderResult.filteredColumns || response.columns;
                }
                
                // อัปเดต record count พร้อมข้อมูลคอลัมน์
                if (renderResult && renderResult.hiddenColumns && renderResult.hiddenColumns.length > 0) {
                    $('#recordCount').text(`${response.data.length} รายการ | ${renderResult.visibleColumns}/${response.columns.length} คอลัมน์`);
                } else {
                    $('#recordCount').text(`${response.data.length} รายการ`);
                }
                $('#dataTableContainer').show();
                
                // เลื่อนไปยังตาราง
                setTimeout(() => {
                    const tableContainer = $('#dataTableContainer');
                    if (tableContainer.length && tableContainer.is(':visible')) {
                        $('html, body').animate({
                            scrollTop: tableContainer.offset().top - 100
                        }, 500);
                    }
                }, 500);
                
            } else if (response.data && response.data.length === 0) {
                $('#queryResult').append(`
                    <div class="alert alert-info mt-2">
                        <i class="fas fa-info-circle me-2"></i>
                        Query รันสำเร็จ แต่ไม่พบข้อมูล
                    </div>
                `);
                $('#dataTableContainer').hide();
                
            } else {
                // กรณีไม่มีข้อมูลหรือเป็น Query ที่ไม่ return ข้อมูล
                if (response.message && response.message.includes('สำเร็จ')) {
                    $('#queryResult').append(`
                        <div class="alert alert-success mt-2">
                            <i class="fas fa-check-circle me-2"></i>
                            Query ดำเนินการสำเร็จ
                        </div>
                    `);
                }
                $('#dataTableContainer').hide();
            }
            
        } catch (error) {
            console.error('Error displaying query result:', error);
            this.showAlert('danger', 'เกิดข้อผิดพลาดในการแสดงผลลัพธ์');
        }
    },

    async forceCleanupDataTable() {
        console.log('Force cleanup DataTable...');
        
        try {
            // Remove any existing DataTable instances
            if ($.fn.DataTable.isDataTable('#oracleDataTable')) {
                console.log('Destroying existing DataTable instance...');
                const table = $('#oracleDataTable').DataTable();
                table.off(); // Remove all event listeners
                table.clear();
                table.destroy(true); // Remove from DOM
                
                // Wait for destroy to complete
                await new Promise(resolve => setTimeout(resolve, 150));
            }
            
            // Force remove any leftover DataTable data and wrappers
            $('#oracleDataTable').removeData('DataTable');
            $('.dataTables_wrapper').remove();
            $('.dataTables_scrollHead').remove();
            $('.dataTables_scrollBody').remove();
            $('.dataTables_scrollFoot').remove();
            
            // Clear any leftover DataTable settings
            if (window.DataTable && window.DataTable.settings) {
                window.DataTable.settings.splice(0);
            }
            
            console.log('DataTable cleanup completed');
            
        } catch (error) {
            console.warn('Error during DataTable cleanup:', error);
            // Force DOM cleanup even if DataTable cleanup fails
            $('.dataTables_wrapper').remove();
            $('#oracleDataTable').removeData('DataTable');
        }
    },

    async ensureCleanTableStructure() {
        console.log('Ensuring clean table structure...');
        
        try {
            // Get the table container
            const tableContainer = $('.table-responsive');
            
            if (!tableContainer.length) {
                console.error('Table container (.table-responsive) not found!');
                throw new Error('Table container not found');
            }
            
            // Completely replace the table element to ensure clean state
            tableContainer.html(`
                <table id="oracleDataTable" class="table table-striped table-hover" style="width:100%">
                    <thead class="table-dark"></thead>
                    <tbody></tbody>
                </table>
            `);
            
            // Wait for DOM to settle
            await new Promise(resolve => setTimeout(resolve, 100));
            
            // Verify the table element was created
            const tableElement = $('#oracleDataTable');
            if (!tableElement.length) {
                throw new Error('Failed to create table element');
            }
            
            console.log('Clean table structure created successfully');
            
        } catch (error) {
            console.error('Error ensuring clean table structure:', error);
            throw error;
        }
    },

    filterEmptyColumns(columns, data) {
        console.log('Filtering empty columns...');
        
        try {
            const filteredColumns = [];
            const hiddenColumns = [];
            
            // Check each column for actual data
            columns.forEach(column => {
                let hasData = false;
                
                // Check if any row has non-null, non-empty data for this column
                for (let row of data) {
                    const value = row[column];
                    
                    if (value !== null && 
                        value !== undefined && 
                        value !== '' && 
                        String(value).trim() !== '' &&
                        String(value).toLowerCase() !== 'null') {
                        hasData = true;
                        break;
                    }
                }
                
                if (hasData) {
                    filteredColumns.push(column);
                } else {
                    hiddenColumns.push(column);
                }
            });
            
            // Create filtered data with only visible columns
            const filteredData = data.map(row => {
                const filteredRow = {};
                filteredColumns.forEach(column => {
                    filteredRow[column] = row[column];
                });
                return filteredRow;
            });
            
            console.log(`Column filtering completed: ${filteredColumns.length} visible, ${hiddenColumns.length} hidden`);
            
            return {
                filteredColumns,
                filteredData,
                hiddenColumns
            };
            
        } catch (error) {
            console.error('Error filtering columns:', error);
            // Return original data if filtering fails
            return {
                filteredColumns: columns,
                filteredData: data,
                hiddenColumns: []
            };
        }
    },

    async forceCompleteReset() {
        console.log('Force complete reset for different table...');
        
        try {
            // Step 1: Force cleanup all DataTable instances and data
            await this.forceCleanupDataTable();
            
            // Step 2: Wait longer for cleanup to complete
            await new Promise(resolve => setTimeout(resolve, 200));
            
            // Step 3: Clear any cached jQuery data
            $('#oracleDataTable').removeData();
            $('#dataTableContainer').removeData();
            
            // Step 4: Remove all DataTable related classes and attributes
            $('.dataTables_wrapper').remove();
            $('.dt-buttons').remove();
            $('.dataTables_info').remove();
            $('.dataTables_paginate').remove();
            $('.dataTables_length').remove();
            $('.dataTables_filter').remove();
            
            // Step 5: Clear DataTable settings globally
            if (window.DataTable && window.DataTable.settings) {
                // Clear all settings
                window.DataTable.settings.length = 0;
            }
            
            // Step 6: Reset all internal variables
            this.currentDataTable = null;
            this.currentTableName = null;
            this.currentTableData = null;
            this.currentTableColumns = null;
            this.currentFilteredData = null;
            this.currentFilteredColumns = null;
            
            // Step 7: Hide container temporarily
            $('#dataTableContainer').hide();
            
            // Step 8: Recreate clean table structure
            await this.ensureCleanTableStructure();
            
            // Step 9: Final wait for everything to settle
            await new Promise(resolve => setTimeout(resolve, 150));
            
            console.log('Force complete reset completed successfully');
            
        } catch (error) {
            console.error('Error in force complete reset:', error);
            
            // Emergency fallback - recreate entire container
            try {
                const containerParent = $('#dataTableContainer').parent();
                $('#dataTableContainer').remove();
                
                const newContainer = $(`
                    <div id="dataTableContainer" class="mt-4" style="display: none;">
                        <div class="card">
                            <div class="card-header bg-dark text-white d-flex justify-content-between align-items-center">
                                <h5 class="mb-0"><i class="fas fa-table me-2"></i>ผลลัพธ์</h5>
                                <span id="recordCount" class="badge bg-light text-dark"></span>
                            </div>
                            <div class="card-body">
                                <div class="table-responsive">
                                    <table id="oracleDataTable" class="table table-striped table-hover" style="width:100%">
                                        <thead class="table-dark"></thead>
                                        <tbody></tbody>
                                    </table>
                                </div>
                            </div>
                        </div>
                    </div>
                `);
                
                containerParent.append(newContainer);
                console.log('Emergency container recreation completed');
                
            } catch (emergencyError) {
                console.error('Emergency container recreation failed:', emergencyError);
                throw error;
            }
        }
    },

    async renderDataTable(columns, data, tableName = 'Unknown') {
        try {
            console.log(`Rendering DataTable for ${tableName} with ${data.length} rows and ${columns.length} columns`);
            
            // Step 1: Extra validation for new table
            if (!columns || !Array.isArray(columns) || columns.length === 0) {
                throw new Error('Invalid columns data');
            }
            
            if (!data || !Array.isArray(data)) {
                throw new Error('Invalid data array');
            }
            
            // Step 2: Check if we need additional cleanup for different table
            const needsExtraCleanup = this.currentTableName && this.currentTableName !== tableName;
            
            if (needsExtraCleanup) {
                console.log(`Table changed from ${this.currentTableName} to ${tableName}, performing extra cleanup...`);
                await this.forceCompleteReset();
            } else {
                // Normal cleanup for same table or first time
                await this.forceCleanupDataTable();
                await this.ensureCleanTableStructure();
            }
            
            console.log('Data validation and cleanup completed, proceeding with DataTable creation...');
            
            // Step 3: Filter out empty columns
            const { filteredColumns, filteredData, hiddenColumns } = this.filterEmptyColumns(columns, data);
            
            if (hiddenColumns.length > 0) {
                console.log(`Hidden ${hiddenColumns.length} empty columns:`, hiddenColumns);
                this.showAlert('info', `ซ่อนคอลัมน์ที่ไม่มีข้อมูล ${hiddenColumns.length} คอลัมน์: ${hiddenColumns.join(', ')}`, 4000);
            }
            
            // Use filtered data for DataTable
            const columnsToUse = filteredColumns;
            const dataToUse = filteredData;
            
            console.log(`Displaying ${columnsToUse.length} columns (hidden ${hiddenColumns.length})`);
            
            // สร้างโครงสร้างตารางใหม่
            const tableHtml = `
                <thead class="table-dark">
                    <tr>
                        ${columnsToUse.map(col => `<th>${col}</th>`).join('')}
                        <th width="120">การกระทำ</th>
                    </tr>
                </thead>
                <tbody></tbody>
            `;
            $('#oracleDataTable').html(tableHtml);
            
            // สร้างคอลัมน์ใหม่พร้อม Action column
            const tableColumns = columnsToUse.map(col => ({
                title: col,
                data: col,
                render: (data, type, row) => {
                    if (type === 'display') {
                        const value = data || '';
                        const displayValue = value.toString();
                        return displayValue.length > 50 ? 
                            `<span title="${this.escapeHtml(displayValue)}">${this.escapeHtml(displayValue.substring(0, 50))}...</span>` : 
                            this.escapeHtml(displayValue);
                    }
                    return data || '';
                }
            }));

            // เพิ่ม Action column
            tableColumns.push({
                title: 'การกระทำ',
                data: null,
                orderable: false,
                searchable: false,
                width: '120px',
                className: 'text-center',
                render: (data, type, row, meta) => {
                    const rowIndex = meta.row;
                    return `
                        <div class="btn-group btn-group-sm" role="group">
                            <button class="btn btn-outline-primary btn-sm edit-row" data-row-index="${rowIndex}" title="แก้ไข">
                                <i class="fas fa-edit"></i>
                            </button>
                            <button class="btn btn-outline-danger btn-sm delete-row" data-row-index="${rowIndex}" title="ลบ">
                                <i class="fas fa-trash"></i>
                            </button>
                            <button class="btn btn-outline-info btn-sm select-for-where" data-row-index="${rowIndex}" title="ใช้เป็นเงื่อนไข WHERE">
                                <i class="fas fa-filter"></i>
                            </button>
                        </div>
                    `;
                }
            });

            // Step 4: Create DataTable with robust configuration
            console.log('Creating new DataTable instance...');
            
            // Verify table element exists and is ready
            const $table = $('#oracleDataTable');
            if (!$table.length) {
                throw new Error('Table element not found before DataTable initialization');
            }
            
            // Prepare DataTable configuration
            const dataTableConfig = {
                data: dataToUse,
                columns: tableColumns,
                pageLength: 25,
                dom: 'Bfrtip',
                buttons: [
                    {
                        extend: 'copy',
                        text: '<i class="fas fa-copy"></i> Copy',
                        className: 'btn-outline-secondary'
                    },
                    {
                        extend: 'excel',
                        text: '<i class="fas fa-file-excel"></i> Excel',
                        className: 'btn-outline-success'
                    },
                    {
                        extend: 'csv',
                        text: '<i class="fas fa-file-csv"></i> CSV',
                        className: 'btn-outline-info'
                    },
                    {
                        text: '<i class="fas fa-columns"></i> คอลัมน์',
                        className: 'btn-outline-warning',
                        action: () => this.showTableColumnInfo(tableName)
                    }
                ],
                scrollX: true,
                responsive: false, // Disable responsive to avoid conflicts
                autoWidth: false,
                destroy: true,
                deferRender: true, // Improve performance
                language: {
                    emptyTable: "ไม่มีข้อมูลในตาราง",
                    zeroRecords: "ไม่พบข้อมูลที่ตรงกับการค้นหา",
                    search: "ค้นหา:",
                    lengthMenu: "แสดง _MENU_ รายการ",
                    info: "แสดง _START_ ถึง _END_ จากทั้งหมด _TOTAL_ รายการ",
                    paginate: {
                        first: "หน้าแรก",
                        last: "หน้าสุดท้าย",
                        next: "ถัดไป",
                        previous: "ก่อนหน้า"
                    }
                },
                columnDefs: [
                    { targets: -1, orderable: false, searchable: false }
                ],
                drawCallback: function(settings) {
                    try {
                        // เพิ่ม tooltip สำหรับข้อมูลที่ยาว
                        $('[title]').tooltip();
                    } catch (e) {
                        console.warn('Error in drawCallback:', e);
                    }
                },
                initComplete: function(settings, json) {
                    console.log('DataTable initialization complete successfully');
                },
                error: function(settings, techNote, message) {
                    console.error('DataTable error:', message, techNote);
                    throw new Error(`DataTable error: ${message}`);
                }
            };
            
            // Initialize DataTable with error handling
            try {
                this.currentDataTable = $table.DataTable(dataTableConfig);
                console.log('DataTable created successfully');
                
                // Verify DataTable was created properly
                if (!this.currentDataTable || !$.fn.DataTable.isDataTable('#oracleDataTable')) {
                    throw new Error('DataTable initialization failed');
                }
                
            } catch (initError) {
                console.error('DataTable initialization error:', initError);
                throw new Error(`Failed to initialize DataTable: ${initError.message}`);
            }

            // อัปเดตส่วนหัวของตาราง
            const headerText = hiddenColumns.length > 0 ? 
                `<i class="fas fa-table me-2"></i>ผลลัพธ์: ${tableName} <small class="badge bg-secondary ms-2">${columnsToUse.length}/${columns.length} คอลัมน์</small>` :
                `<i class="fas fa-table me-2"></i>ผลลัพธ์: ${tableName}`;
            $('#dataTableContainer .card-header h5').html(headerText);
            
            console.log(`DataTable rendered successfully for table: ${tableName}, ${dataToUse.length} rows, ${columnsToUse.length}/${columns.length} columns`);
            
            // Return information about the rendering
            return {
                success: true,
                visibleColumns: columnsToUse.length,
                totalColumns: columns.length,
                hiddenColumns: hiddenColumns,
                dataRows: dataToUse.length,
                filteredData: dataToUse,
                filteredColumns: columnsToUse
            };
            
        } catch (error) {
            console.error('Critical error rendering DataTable:', error);
            this.showAlert('danger', `เกิดข้อผิดพลาดในการแสดงตาราง: ${error.message}`);
            
            // Emergency fallback - แสดงข้อมูลในรูปแบบง่าย
            console.log('Attempting fallback display...');
            try {
                // Force cleanup first
                await this.forceCleanupDataTable();
                
                let fallbackHtml = '<div class="alert alert-warning mb-3">';
                fallbackHtml += '<i class="fas fa-exclamation-triangle me-2"></i>';
                fallbackHtml += '<strong>ไม่สามารถสร้าง DataTable ได้</strong> แต่ข้อมูลสามารถดูได้ในรูปแบบตารางธรรมดา:';
                fallbackHtml += '</div>';
                
                fallbackHtml += '<div class="card">';
                fallbackHtml += '<div class="card-header bg-warning text-dark">';
                fallbackHtml += `<h6 class="mb-0"><i class="fas fa-table me-2"></i>ข้อมูลจาก: ${tableName}</h6>`;
                fallbackHtml += '</div>';
                fallbackHtml += '<div class="card-body p-0">';
                fallbackHtml += '<div class="table-responsive">';
                fallbackHtml += '<table class="table table-striped table-hover mb-0">';
                fallbackHtml += '<thead class="table-dark"><tr>';
                
                columnsToUse.forEach(col => {
                    fallbackHtml += `<th>${this.escapeHtml(col)}</th>`;
                });
                fallbackHtml += '</tr></thead><tbody>';
                
                const displayData = dataToUse.slice(0, 100); // แสดงแค่ 100 แถวแรก
                displayData.forEach((row, index) => {
                    fallbackHtml += '<tr>';
                    columnsToUse.forEach(col => {
                        const value = row[col] || '';
                        const displayValue = value.toString();
                        const shortValue = displayValue.length > 100 ? 
                            displayValue.substring(0, 100) + '...' : 
                            displayValue;
                        fallbackHtml += `<td title="${this.escapeHtml(displayValue)}">${this.escapeHtml(shortValue)}</td>`;
                    });
                    fallbackHtml += '</tr>';
                });
                
                fallbackHtml += '</tbody></table></div></div>';
                
                if (dataToUse.length > 100 || hiddenColumns.length > 0) {
                    fallbackHtml += '<div class="card-footer bg-light">';
                    fallbackHtml += '<small class="text-muted">';
                    fallbackHtml += `<i class="fas fa-info-circle me-1"></i>`;
                    
                    if (dataToUse.length > 100) {
                        fallbackHtml += `แสดงเฉพาะ 100 แถวแรกจากทั้งหมด ${dataToUse.length} แถว`;
                    }
                    
                    if (hiddenColumns.length > 0) {
                        if (dataToUse.length > 100) fallbackHtml += ' | ';
                        fallbackHtml += `ซ่อน ${hiddenColumns.length} คอลัมน์: ${hiddenColumns.join(', ')}`;
                    }
                    
                    fallbackHtml += '</small></div>';
                }
                
                fallbackHtml += '</div>';
                
                // Add retry button
                fallbackHtml += '<div class="mt-3 text-center">';
                fallbackHtml += '<button class="btn btn-primary me-2" onclick="window.OracleDebug.forceDestroyDataTable().then(() => location.reload())">';
                fallbackHtml += '<i class="fas fa-redo me-1"></i>ลองใหม่ด้วย DataTable';
                fallbackHtml += '</button>';
                fallbackHtml += '<button class="btn btn-secondary" onclick="window.OracleDebug.clearAll()">';
                fallbackHtml += '<i class="fas fa-eraser me-1"></i>ล้างข้อมูล';
                fallbackHtml += '</button>';
                fallbackHtml += '</div>';
                
                $('.table-responsive').parent().html(fallbackHtml);
                $('#dataTableContainer').show();
                
                // Update record count
                const recordText = hiddenColumns.length > 0 ? 
                    `${data.length} รายการ | ${columnsToUse.length}/${columns.length} คอลัมน์ (แสดงแบบตารางธรรมดา)` :
                    `${data.length} รายการ (แสดงแบบตารางธรรมดา)`;
                $('#recordCount').text(recordText);
                
                console.log('Fallback display created successfully');
                
                // Return fallback information
                return {
                    success: false,
                    fallback: true,
                    visibleColumns: columnsToUse.length,
                    totalColumns: columns.length,
                    hiddenColumns: hiddenColumns,
                    dataRows: dataToUse.length,
                    filteredData: dataToUse,
                    filteredColumns: columnsToUse
                };
                
            } catch (fallbackError) {
                console.error('Fallback display also failed:', fallbackError);
                
                // Ultimate fallback - show basic message
                const tableContainer = $('.table-responsive').parent();
                tableContainer.html(`
                    <div class="alert alert-danger">
                        <h5><i class="fas fa-exclamation-triangle me-2"></i>ไม่สามารถแสดงข้อมูลได้</h5>
                        <p class="mb-2">เกิดข้อผิดพลาดในการแสดงผลข้อมูล กรุณาลองวิธีต่อไปนี้:</p>
                        <ol class="mb-3">
                            <li>รีเฟรชหน้าเว็บ</li>
                            <li>ใช้คำสั่ง SQL ที่เรียบง่ายกว่า</li>
                            <li>ลองใช้ query ที่ return ข้อมูลน้อยกว่า</li>
                        </ol>
                        <button class="btn btn-outline-light" onclick="location.reload()">
                            <i class="fas fa-sync-alt me-1"></i>รีเฟรชหน้า
                        </button>
                    </div>
                `);
                
                this.showAlert('danger', 'ไม่สามารถแสดงข้อมูลได้ กรุณารีเฟรชหน้าเว็บ');
            }
            
            // Return error information
            return {
                success: false,
                error: true,
                message: error.message,
                filteredData: data,
                filteredColumns: columns
            };
        }
    },

    extractTableNameFromSQL(sql) {
        if (!sql) return 'Unknown';
        
        try {
            // ลบ comments และ trim
            const cleanSQL = sql.replace(/--.*$/gm, '').replace(/\/\*[\s\S]*?\*\//g, '').trim();
            
            // หา FROM clause
            const fromMatch = cleanSQL.match(/\bFROM\s+([^\s,\)]+)/i);
            if (fromMatch) {
                return fromMatch[1].replace(/["`\[\]]/g, '');
            }
            
            // หา UPDATE clause
            const updateMatch = cleanSQL.match(/\bUPDATE\s+([^\s,\)]+)/i);
            if (updateMatch) {
                return updateMatch[1].replace(/["`\[\]]/g, '');
            }
            
            // หา INSERT INTO clause
            const insertMatch = cleanSQL.match(/\bINTO\s+([^\s,\(]+)/i);
            if (insertMatch) {
                return insertMatch[1].replace(/["`\[\]]/g, '');
            }
            
            return 'Unknown';
        } catch (e) {
            return 'Unknown';
        }
    },

    async clearQuery() {
        try {
            // ล้าง SQL Query
            $('#sqlQuery').val('');
            
            // ล้างผลลัพธ์เก่าทั้งหมด
            await this.clearPreviousResults();
            
            // เซ็ต focus กลับไปที่ textarea
            $('#sqlQuery').focus();
            
            this.showAlert('info', 'ล้างข้อมูล Query แล้ว พร้อมสำหรับคำสั่งใหม่', 2000);
            
            console.log('Query cleared successfully');
        } catch (error) {
            console.error('Error clearing query:', error);
            this.showAlert('danger', 'เกิดข้อผิดพลาดในการล้างข้อมูล');
        }
    },

    async testConnection() {
        try {
            const $btn = $('#btnTestConnection');
            $btn.html('<i class="fas fa-spinner fa-spin me-2"></i>กำลังทดสอบ...').prop('disabled', true);
            
            const response = await $.post(`${this.baseUrl}/TestConnection`);
            
            const alertClass = response.success ? 'alert-success' : 'alert-danger';
            $('#connectionResult').html(`<div class="alert ${alertClass}">${response.message}</div>`);
        } catch (error) {
            $('#connectionResult').html('<div class="alert alert-danger">เกิดข้อผิดพลาดในการทดสอบการเชื่อมต่อ</div>');
        } finally {
            $('#btnTestConnection').html('<i class="fas fa-plug me-2"></i>ทดสอบการเชื่อมต่อ').prop('disabled', false);
        }
    },

    async getData() {
        const tableName = $('#tableName').val().trim();
        try {
            const response = await $.get(`${this.baseUrl}/GetOracleData`, { tableName });
            
            const alertClass = response.success ? 'alert-success' : 'alert-danger';
            const message = response.success ? 
                `ตาราง ${response.tableName} มีข้อมูล ${response.recordCount} รายการ` : 
                response.message;
            
            $('#dataResult').html(`<div class="alert ${alertClass}">${message}</div>`);
        } catch (error) {
            $('#dataResult').html('<div class="alert alert-danger">เกิดข้อผิดพลาดในการดึงข้อมูล</div>');
        }
    },

    async loadDashboardStats() {
        try {
            const response = await $.get(`${this.baseUrl}/GetDashboardStats`);
            if (response.success) {
                $('#todayActivities').text(response.data.TodayActivities || 0);
                $('#weekActivities').text(response.data.WeekActivities || 0);
                $('#avgExecutionTime').text(Math.round(response.data.AvgExecutionTime || 0) + ' ms');
                $('#totalErrors').text(response.data.TotalErrors || 0);
            } else {
                // If no stats available, set to 0
                $('#todayActivities').text('0');
                $('#weekActivities').text('0');
                $('#avgExecutionTime').text('0 ms');
                $('#totalErrors').text('0');
            }
        } catch (error) {
            // Set default values on error
            $('#todayActivities').text('-');
            $('#weekActivities').text('-');
            $('#avgExecutionTime').text('-');
            $('#totalErrors').text('-');
        }
    },

    viewGroupDetail(e) {
        const sqlHash = $(e.currentTarget).data('sql-hash');
        const activityType = $(e.currentTarget).data('activity-type');
        console.log('View group detail:', sqlHash, activityType);
    },

    async viewDetail(e) {
        const id = $(e.currentTarget).data('id');
        try {
            const response = await $.get(`${this.baseUrl}/GetQueryDetail`, { id });
            if (response.success) {
                this.showDetailModal(response.data);
            } else {
                this.showAlert('danger', response.message);
            }
        } catch (error) {
            this.showAlert('danger', 'เกิดข้อผิดพลาดในการโหลดรายละเอียด');
        }
    },

    showDetailModal(data) {
        const modalHtml = `
            <div class="modal fade" id="detailModal" tabindex="-1">
                <div class="modal-dialog modal-lg">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title">รายละเอียดประวัติ Query</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <div class="row">
                                <div class="col-md-6">
                                    <h6><i class="fas fa-info-circle me-2"></i>ข้อมูลทั่วไป</h6>
                                    <table class="table table-sm">
                                        <tr><td><strong>ประเภท:</strong></td><td><span class="badge ${this.getActivityBadgeClass(data.activityType)}">${data.activityType}</span></td></tr>
                                        <tr><td><strong>ผู้ใช้:</strong></td><td>${data.userName || '-'}</td></tr>
                                        <tr><td><strong>ตาราง:</strong></td><td>${data.tableName || '-'}</td></tr>
                                        <tr><td><strong>แถวที่กระทบ:</strong></td><td>${data.affectedRows}</td></tr>
                                        <tr><td><strong>เวลาใช้:</strong></td><td>${data.executionTimeMs} ms</td></tr>
                                        <tr><td><strong>สถานะ:</strong></td><td><span class="badge ${data.isSuccess ? 'bg-success' : 'bg-danger'}">${data.isSuccess ? 'สำเร็จ' : 'ผิดพลาด'}</span></td></tr>
                                        <tr><td><strong>วันที่:</strong></td><td>${data.createdDate}</td></tr>
                                    </table>
                                </div>
                                <div class="col-md-6">
                                    <h6><i class="fas fa-network-wired me-2"></i>ข้อมูลการเชื่อมต่อ</h6>
                                    <table class="table table-sm">
                                        <tr><td><strong>Session ID:</strong></td><td><small>${data.sessionId}</small></td></tr>
                                        <tr><td><strong>Client IP:</strong></td><td>${data.clientIP || '-'}</td></tr>
                                        <tr><td><strong>User Agent:</strong></td><td><small>${data.userAgent || '-'}</small></td></tr>
                                    </table>
                                </div>
                            </div>
                            <div class="row mt-3">
                                <div class="col-12">
                                    <h6><i class="fas fa-code me-2"></i>SQL Command</h6>
                                    <pre class="bg-light p-3 border rounded"><code>${data.sqlCommand || '-'}</code></pre>
                                </div>
                            </div>
                            ${data.parameters ? `
                            <div class="row mt-3">
                                <div class="col-12">
                                    <h6><i class="fas fa-cogs me-2"></i>Parameters</h6>
                                    <pre class="bg-light p-3 border rounded"><code>${data.parameters}</code></pre>
                                </div>
                            </div>
                            ` : ''}
                            ${data.errorMessage ? `
                            <div class="row mt-3">
                                <div class="col-12">
                                    <h6><i class="fas fa-exclamation-triangle me-2 text-danger"></i>ข้อผิดพลาด</h6>
                                    <div class="alert alert-danger">
                                        <code>${data.errorMessage}</code>
                                    </div>
                                </div>
                            </div>
                            ` : ''}
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">ปิด</button>
                            <button type="button" class="btn btn-success reuse-query" data-sql="${data.sqlCommand}" data-bs-dismiss="modal">
                                <i class="fas fa-redo me-1"></i>ใช้ Query นี้
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;
        
        // Remove existing modal if any
        $('#detailModal').remove();
        
        // Add modal to body and show
        $('body').append(modalHtml);
        $('#detailModal').modal('show');
        
        // Clean up when modal is hidden
        $('#detailModal').on('hidden.bs.modal', function() {
            $(this).remove();
        });
    },

    renderHistoryTable(data) {
        const tableHtml = `
            <div class="row mb-3">
                <div class="col-md-6">
                    <h6><i class="fas fa-table me-2"></i>ประวัติการ Query (จัดกลุ่มตาม SQL)</h6>
                    <small class="text-muted">คลิกที่ปุ่ม + เพื่อดูรายละเอียดของแต่ละการทำงาน</small>
                </div>
                <div class="col-md-6 text-end">
                    <div class="btn-group btn-group-sm">
                        <button class="btn btn-outline-success export-history" data-format="excel" title="Export เป็น Excel">
                            <i class="fas fa-file-excel me-1"></i>Excel
                        </button>
                        <button class="btn btn-outline-info export-history" data-format="csv" title="Export เป็น CSV">
                            <i class="fas fa-file-csv me-1"></i>CSV
                        </button>
                    </div>
                </div>
            </div>
            <div class="table-responsive">
                <table id="historyTable" class="table table-striped table-hover w-100">
                    <thead class="table-dark">
                        <tr>
                            <th width="30"></th>
                            <th>ประเภท</th>
                            <th>SQL Preview</th>
                            <th>ตาราง</th>
                            <th>จำนวนครั้ง</th>
                            <th>เวลาเฉลี่ย</th>
                            <th>สำเร็จ/ผิดพลาด</th>
                            <th>ล่าสุด</th>
                            <th>การกระทำ</th>
                        </tr>
                    </thead>
                    <tbody>
                        ${data.map(item => `
                            <tr data-sql-hash="${item.sqlHash}" data-activity-type="${item.activityType}">
                                <td>
                                    <button class="btn btn-sm btn-outline-secondary child-row-toggle" data-sql-hash="${item.sqlHash}" data-activity-type="${item.activityType}" title="ดูรายละเอียด">
                                        <i class="fas fa-plus"></i>
                                    </button>
                                </td>
                                <td><span class="badge ${this.getActivityBadgeClass(item.activityType)}">${item.activityType}</span></td>
                                <td>
                                    <div class="d-flex align-items-center">
                                        <span title="${item.fullSqlCommand}" class="text-truncate" style="max-width: 250px;">${item.sqlPreview}${item.sqlPreview.length >= 100 ? '...' : ''}</span>
                                        <button class="btn btn-sm btn-outline-secondary ms-1 copy-sql" data-sql-hash="${item.sqlHash}" data-activity-type="${item.activityType}" title="คัดลอก SQL">
                                            <i class="fas fa-copy"></i>
                                        </button>
                                    </div>
                                </td>
                                <td>
                                    <div class="d-flex align-items-center">
                                        <span>${item.tableName}</span>
                                        ${item.tableName !== 'Unknown' ? `<button class="btn btn-sm btn-outline-info ms-1 show-column-info" data-table="${item.tableName}" title="ดู Column Info"><i class="fas fa-columns"></i></button>` : ''}
                                    </div>
                                </td>
                                <td><span class="badge bg-primary">${item.executionCount}</span></td>
                                <td>${item.avgExecutionTime} ms</td>
                                <td>
                                    <span class="badge bg-success">${item.successCount}</span>
                                    ${item.errorCount > 0 ? `<span class="badge bg-danger ms-1">${item.errorCount}</span>` : ''}
                                </td>
                                <td><small>${item.lastExecuted}</small></td>
                                <td>
                                    <div class="btn-group btn-group-sm">
                                        <button class="btn btn-outline-success reuse-query" data-sql-hash="${item.sqlHash}" data-activity-type="${item.activityType}" title="ใช้ SQL นี้">
                                            <i class="fas fa-redo"></i>
                                        </button>
                                    </div>
                                </td>
                            </tr>
                        `).join('')}
                    </tbody>
                </table>
            </div>
        `;
        
        $('#historyContainer').html(tableHtml);
        
        // ทำลาย DataTable เก่าอย่างสมบูรณ์
        if (this.historyDataTable && $.fn.DataTable.isDataTable('#historyTable')) {
            this.historyDataTable.clear().destroy();
            this.historyDataTable = null;
        }
        
        // สร้าง DataTable ใหม่
        this.historyDataTable = $('#historyTable').DataTable({
            order: [[7, 'desc']], // เรียงตามวันที่ล่าสุด
            pageLength: 20,
            dom: 'frtip', // ไม่แสดง buttons default เพราะเรามี custom buttons
            scrollX: true,
            columnDefs: [
                { orderable: false, targets: [0, 8] }, // ปิด sorting สำหรับคอลัมน์ปุ่ม
                { searchable: false, targets: [0, 8] }
            ],
            language: {
                url: '//cdn.datatables.net/plug-ins/1.13.7/i18n/th.json',
                emptyTable: "ยังไม่มีประวัติการใช้งาน",
                zeroRecords: "ไม่พบข้อมูลที่ตรงกับการค้นหา"
            }
        });
    },

    getActivityBadgeClass(activityType) {
        switch (activityType?.toUpperCase()) {
            case 'SELECT': return 'bg-success';
            case 'INSERT': return 'bg-primary';
            case 'UPDATE': return 'bg-warning text-dark';
            case 'DELETE': return 'bg-danger';
            case 'CONNECTION_TEST': return 'bg-info';
            default: return 'bg-secondary';
        }
    },

    async initializeHistoryTables() {
        try {
            const $btn = $('#btnAutoCreateTables');
            const originalText = $btn.html();
            $btn.html('<i class="fas fa-spinner fa-spin me-1"></i>กำลังสร้างตาราง...').prop('disabled', true);
            
            const response = await $.post(`${this.baseUrl}/InitializeHistoryTables`);
            
            if (response.success) {
                this.showAlert('success', response.message);
                
                // รีโหลดประวัติหลังจากสร้างตารางเสร็จ
                setTimeout(() => {
                    this.loadQueryHistory();
                }, 1000);
            } else {
                this.showAlert('danger', response.message || 'ไม่สามารถสร้างตารางได้');
                $btn.html(originalText).prop('disabled', false);
            }
        } catch (error) {
            this.showAlert('danger', 'เกิดข้อผิดพลาดในการสร้างตาราง');
            $('#btnAutoCreateTables').html('<i class="fas fa-magic me-1"></i>สร้างตารางอัตโนมัติ').prop('disabled', false);
        }
    },

    async toggleChildRow(e) {
        e.preventDefault();
        const $btn = $(e.currentTarget);
        const $row = $btn.closest('tr');
        const sqlHash = $btn.data('sql-hash');
        const activityType = $btn.data('activity-type');
        
        if ($row.next().hasClass('child-row')) {
            // ปิด child row
            $row.next().remove();
            $btn.find('i').removeClass('fa-minus').addClass('fa-plus');
            return;
        }
        
        try {
            $btn.html('<i class="fas fa-spinner fa-spin"></i>').prop('disabled', true);
            
            const response = await $.get(`${this.baseUrl}/GetQueryGroupDetail`, {
                sqlHash: sqlHash,
                activityType: activityType
            });
            
            if (response.success && response.data.length > 0) {
                const childRowHtml = this.generateChildRowHtml(response.data);
                $row.after(`<tr class="child-row"><td colspan="9">${childRowHtml}</td></tr>`);
                $btn.find('i').removeClass('fa-plus').addClass('fa-minus');
            } else {
                this.showAlert('warning', 'ไม่พบรายละเอียดของกลุ่มนี้');
            }
        } catch (error) {
            this.showAlert('danger', 'เกิดข้อผิดพลาดในการโหลดรายละเอียด');
        } finally {
            $btn.html('<i class="fas fa-minus"></i>').prop('disabled', false);
        }
    },

    generateChildRowHtml(data) {
        return `
            <div class="p-3 bg-light">
                <h6><i class="fas fa-list me-2"></i>รายละเอียดการทำงาน (${data.length} ครั้ง)</h6>
                <div class="table-responsive">
                    <table class="table table-sm table-bordered">
                        <thead class="table-secondary">
                            <tr>
                                <th>เวลา</th>
                                <th>ผู้ใช้</th>
                                <th>แถวที่กระทบ</th>
                                <th>เวลาใช้</th>
                                <th>สถานะ</th>
                                <th>IP</th>
                                <th>การกระทำ</th>
                            </tr>
                        </thead>
                        <tbody>
                            ${data.map(item => `
                                <tr>
                                    <td><small>${item.createdDate}</small></td>
                                    <td><small>${item.userName || '-'}</small></td>
                                    <td><span class="badge bg-info">${item.affectedRows}</span></td>
                                    <td>${item.executionTime}</td>
                                    <td>${item.statusBadge}</td>
                                    <td><small>${item.clientIP || '-'}</small></td>
                                    <td>
                                        <button class="btn btn-xs btn-outline-primary view-detail" data-id="${item.id}" title="ดูรายละเอียดเต็ม">
                                            <i class="fas fa-eye"></i>
                                        </button>
                                    </td>
                                </tr>
                                ${item.errorMessage ? `
                                <tr class="table-danger">
                                    <td colspan="7"><small><strong>Error:</strong> ${item.errorMessage}</small></td>
                                </tr>` : ''}
                            `).join('')}
                        </tbody>
                    </table>
                </div>
            </div>
        `;
    },

    async showColumnInfo(e) {
        e.preventDefault();
        const tableName = $(e.currentTarget).data('table');
        
        try {
            const response = await $.get(`${this.baseUrl}/GetTableColumnInfo`, { tableName });
            
            if (response.success) {
                this.showColumnInfoModal(response.data, response.tableName);
            } else {
                this.showAlert('danger', response.message);
            }
        } catch (error) {
            this.showAlert('danger', 'เกิดข้อผิดพลาดในการโหลดข้อมูล Column');
        }
    },

    showColumnInfoModal(columns, tableName) {
        const modalHtml = `
            <div class="modal fade" id="columnInfoModal" tabindex="-1">
                <div class="modal-dialog modal-lg">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title"><i class="fas fa-columns me-2"></i>Column Information: ${tableName}</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <div class="table-responsive">
                                <table class="table table-striped table-hover">
                                    <thead class="table-dark">
                                        <tr>
                                            <th>#</th>
                                            <th>Column Name</th>
                                            <th>Data Type</th>
                                            <th>Nullable</th>
                                            <th>Default Value</th>
                                        </tr>
                                    </thead>
                                    <tbody>
                                        ${columns.map(col => `
                                            <tr>
                                                <td>${col.columnId}</td>
                                                <td><strong>${col.columnName}</strong></td>
                                                <td><code>${col.dataType}</code></td>
                                                <td><span class="badge ${col.nullable === 'YES' ? 'bg-warning' : 'bg-success'}">${col.nullable}</span></td>
                                                <td><small>${col.defaultValue}</small></td>
                                            </tr>
                                        `).join('')}
                                    </tbody>
                                </table>
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">ปิด</button>
                        </div>
                    </div>
                </div>
            </div>
        `;
        
        // Remove existing modal
        $('#columnInfoModal').remove();
        $('body').append(modalHtml);
        $('#columnInfoModal').modal('show');
        
        // Clean up when modal is hidden
        $('#columnInfoModal').on('hidden.bs.modal', function() {
            $(this).remove();
        });
    },

    async exportHistory(e) {
        e.preventDefault();
        const format = $(e.currentTarget).data('format');
        
        try {
            const $btn = $(e.currentTarget);
            const originalText = $btn.html();
            $btn.html('<i class="fas fa-spinner fa-spin me-1"></i>กำลัง Export...').prop('disabled', true);
            
            window.open(`${this.baseUrl}/ExportHistory?format=${format}&days=30`, '_blank');
            
            setTimeout(() => {
                $btn.html(originalText).prop('disabled', false);
            }, 2000);
            
            this.showAlert('success', `กำลัง Export ข้อมูลเป็น ${format.toUpperCase()}`);
        } catch (error) {
            this.showAlert('danger', 'เกิดข้อผิดพลาดในการ Export');
        }
    },

    async copySqlToClipboard(e) {
        e.preventDefault();
        const sqlHash = $(e.currentTarget).data('sql-hash');
        const activityType = $(e.currentTarget).data('activity-type');
        
        try {
            const response = await $.post(`${this.baseUrl}/GetReusableQuery`, {
                sqlHash: sqlHash,
                activityType: activityType
            });
            
            if (response.success) {
                // Copy to clipboard
                await navigator.clipboard.writeText(response.sqlCommand);
                
                const $btn = $(e.currentTarget);
                const originalHtml = $btn.html();
                $btn.html('<i class="fas fa-check text-success"></i>');
                
                setTimeout(() => {
                    $btn.html(originalHtml);
                }, 1500);
                
                this.showAlert('success', 'คัดลอก SQL ไปยัง Clipboard แล้ว');
            } else {
                this.showAlert('danger', response.message);
            }
        } catch (error) {
            this.showAlert('danger', 'เกิดข้อผิดพลาดในการคัดลอก SQL');
        }
    },

    async reuseQuery(e) {
        e.preventDefault();
        const sqlHash = $(e.currentTarget).data('sql-hash');
        const activityType = $(e.currentTarget).data('activity-type');
        
        try {
            const response = await $.post(`${this.baseUrl}/GetReusableQuery`, {
                sqlHash: sqlHash,
                activityType: activityType
            });
            
            if (response.success) {
                // ล้างผลลัพธ์เก่า
                this.clearPreviousResults();
                
                // ใส่ SQL ใหม่
                $('#sqlQuery').val(response.sqlCommand);
                
                // เปลี่ยนไปที่แท็บ Query
                $('#query-tab').tab('show');
                $('#sqlQuery').focus();
                
                this.showAlert('success', 'นำ SQL มาใช้ในแท็บ Query Builder แล้ว กดปุ่ม "เรียกใช้ Query" เพื่อรัน');
                
                console.log('Reused SQL command:', response.sqlCommand);
            } else {
                this.showAlert('danger', response.message);
            }
        } catch (error) {
            console.error('Error reusing query:', error);
            this.showAlert('danger', 'เกิดข้อผิดพลาดในการใช้ SQL ซ้ำ');
        }
    },

    // แก้ไขข้อมูลในแถว
    editRow(e) {
        e.preventDefault();
        const rowIndex = $(e.currentTarget).data('row-index');
        const rowData = this.currentDataTable.row(rowIndex).data();
        
        if (!this.currentTableName || this.currentTableName === 'Unknown') {
            this.showAlert('warning', 'ไม่สามารถแก้ไขได้เนื่องจากไม่ทราบชื่อตาราง');
            return;
        }
        
        this.showEditModal(rowData, rowIndex);
    },

    // ลบข้อมูลในแถว
    deleteRow(e) {
        e.preventDefault();
        const rowIndex = $(e.currentTarget).data('row-index');
        const rowData = this.currentDataTable.row(rowIndex).data();
        
        if (!this.currentTableName || this.currentTableName === 'Unknown') {
            this.showAlert('warning', 'ไม่สามารถลบได้เนื่องจากไม่ทราบชื่อตาราง');
            return;
        }
        
        this.showDeleteConfirmModal(rowData, rowIndex);
    },

    // เลือกข้อมูลเพื่อสร้าง WHERE clause
    selectForWhere(e) {
        e.preventDefault();
        const rowIndex = $(e.currentTarget).data('row-index');
        const rowData = this.currentDataTable.row(rowIndex).data();
        
        this.showWhereSelectionModal(rowData);
    },

    // แสดง Modal สำหรับแก้ไขข้อมูล
    showEditModal(rowData, rowIndex) {
        const columns = this.currentTableColumns.filter(col => col !== 'การกระทำ');
        const modalHtml = `
            <div class="modal fade" id="editModal" tabindex="-1" data-bs-backdrop="static">
                <div class="modal-dialog modal-lg">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title">
                                <i class="fas fa-edit me-2"></i>แก้ไขข้อมูล - ${this.currentTableName}
                            </h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <form id="editForm">
                                <div class="row">
                                    ${columns.map(col => `
                                        <div class="col-md-6 mb-3">
                                            <label for="edit_${col}" class="form-label">${col}</label>
                                            <input type="text" class="form-control" id="edit_${col}" name="${col}" 
                                                   value="${this.escapeHtml(rowData[col] || '')}"
                                                   data-original-value="${this.escapeHtml(rowData[col] || '')}">
                                        </div>
                                    `).join('')}
                                </div>
                                <div class="alert alert-info">
                                    <i class="fas fa-info-circle me-2"></i>
                                    <strong>หมายเหตุ:</strong> ระบบจะใช้ข้อมูลเดิมทั้งหมดเป็นเงื่อนไข WHERE ในการอัปเดต
                                </div>
                            </form>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">ยกเลิก</button>
                            <button type="button" class="btn btn-primary" id="saveEdit" data-row-index="${rowIndex}">
                                <i class="fas fa-save me-1"></i>บันทึกการแก้ไข
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;
        
        this.showModal(modalHtml, 'editModal', () => {
            $('#saveEdit').off('click').on('click', (e) => this.saveEdit(e));
        });
    },

    // แสดง Modal สำหรับยืนยันการลบ
    showDeleteConfirmModal(rowData, rowIndex) {
        const modalHtml = `
            <div class="modal fade" id="deleteModal" tabindex="-1">
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header bg-danger text-white">
                            <h5 class="modal-title">
                                <i class="fas fa-trash me-2"></i>ยืนยันการลบข้อมูล
                            </h5>
                            <button type="button" class="btn-close btn-close-white" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <div class="alert alert-warning">
                                <i class="fas fa-exclamation-triangle me-2"></i>
                                <strong>คำเตือน:</strong> การลบข้อมูลนี้ไม่สามารถยกเลิกได้!
                            </div>
                            <p><strong>ตาราง:</strong> ${this.currentTableName}</p>
                            <p><strong>ข้อมูลที่จะลบ:</strong></p>
                            <div class="table-responsive">
                                <table class="table table-sm table-bordered">
                                    ${Object.keys(rowData).filter(key => key !== 'การกระทำ').map(key => `
                                        <tr>
                                            <td><strong>${key}</strong></td>
                                            <td>${this.escapeHtml(rowData[key] || '')}</td>
                                        </tr>
                                    `).join('')}
                                </table>
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">ยกเลิก</button>
                            <button type="button" class="btn btn-danger" id="confirmDelete" data-row-index="${rowIndex}">
                                <i class="fas fa-trash me-1"></i>ลบข้อมูล
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;
        
        this.showModal(modalHtml, 'deleteModal', () => {
            $('#confirmDelete').off('click').on('click', (e) => this.confirmDelete(e));
        });
    },

    // แสดง Modal สำหรับเลือกคอลัมน์ WHERE
    showWhereSelectionModal(rowData) {
        const columns = this.currentTableColumns.filter(col => col !== 'การกระทำ');
        const modalHtml = `
            <div class="modal fade" id="whereModal" tabindex="-1">
                <div class="modal-dialog modal-lg">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title">
                                <i class="fas fa-filter me-2"></i>สร้างเงื่อนไข WHERE
                            </h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <p><strong>เลือกคอลัมน์ที่ต้องการใช้เป็นเงื่อนไข:</strong></p>
                            <div class="row">
                                ${columns.map(col => `
                                    <div class="col-md-6 mb-2">
                                        <div class="form-check">
                                            <input class="form-check-input where-column" type="checkbox" 
                                                   id="where_${col}" value="${col}"
                                                   data-value="${this.escapeHtml(rowData[col] || '')}">
                                            <label class="form-check-label" for="where_${col}">
                                                <strong>${col}</strong> = "${rowData[col] || ''}"
                                            </label>
                                        </div>
                                    </div>
                                `).join('')}
                            </div>
                            <div class="mt-3">
                                <label for="wherePreview" class="form-label">ตัวอย่าง WHERE clause:</label>
                                <textarea id="wherePreview" class="form-control" rows="3" readonly 
                                          placeholder="เลือกคอลัมน์เพื่อดูตัวอย่าง WHERE clause"></textarea>
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">ยกเลิก</button>
                            <button type="button" class="btn btn-primary" id="useWhere">
                                <i class="fas fa-copy me-1"></i>ใช้เงื่อนไขนี้
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;
        
        this.showModal(modalHtml, 'whereModal', () => {
            // Update WHERE preview when checkboxes change
            $('.where-column').on('change', this.updateWherePreview.bind(this));
            $('#useWhere').off('click').on('click', this.useWhereClause.bind(this));
        });
    },

    // อัปเดตตัวอย่าง WHERE clause
    updateWherePreview() {
        const selectedColumns = [];
        $('.where-column:checked').each(function() {
            const column = $(this).val();
            const value = $(this).data('value');
            
            if (value === null || value === '') {
                selectedColumns.push(`${column} IS NULL`);
            } else if (isNaN(value)) {
                selectedColumns.push(`${column} = '${value.toString().replace(/'/g, "''")}'`);
            } else {
                selectedColumns.push(`${column} = ${value}`);
            }
        });
        
        const whereClause = selectedColumns.length > 0 ? 
            `WHERE ${selectedColumns.join(' AND ')}` : 
            '';
            
        $('#wherePreview').val(whereClause);
    },

    // ใช้ WHERE clause ที่เลือก
    useWhereClause() {
        const whereClause = $('#wherePreview').val();
        if (whereClause) {
            const currentSQL = $('#sqlQuery').val();
            const newSQL = `SELECT * FROM ${this.currentTableName} ${whereClause}`;
            
            $('#sqlQuery').val(newSQL);
            $('#whereModal').modal('hide');
            $('#query-tab').tab('show');
            $('#sqlQuery').focus();
            
            this.showAlert('success', 'นำเงื่อนไข WHERE ไปใส่ใน SQL Query แล้ว');
        } else {
            this.showAlert('warning', 'กรุณาเลือกคอลัมน์อย่างน้อย 1 คอลัมน์');
        }
    },

    // บันทึกการแก้ไข
    async saveEdit(e) {
        const rowIndex = $(e.currentTarget).data('row-index');
        const originalData = this.currentDataTable.row(rowIndex).data();
        
        // รวบรวมข้อมูลใหม่
        const updatedData = {};
        const changedFields = [];
        
        $('#editForm input').each(function() {
            const fieldName = $(this).attr('name');
            const newValue = $(this).val();
            const originalValue = $(this).data('original-value') || '';
            
            updatedData[fieldName] = newValue;
            
            if (newValue !== originalValue) {
                changedFields.push(fieldName);
            }
        });
        
        if (changedFields.length === 0) {
            this.showAlert('info', 'ไม่มีการเปลี่ยนแปลงข้อมูล');
            return;
        }
        
        try {
            const $btn = $('#saveEdit');
            $btn.html('<i class="fas fa-spinner fa-spin me-1"></i>กำลังบันทึก...').prop('disabled', true);
            
            // สร้างคำสั่ง UPDATE
            const updateFields = changedFields.map(field => 
                `${field} = '${updatedData[field].replace(/'/g, "''")}'`
            ).join(', ');
            
            const whereConditions = this.currentTableColumns
                .filter(col => col !== 'การกระทำ' && originalData[col] !== null && originalData[col] !== '')
                .map(col => {
                    const value = originalData[col];
                    if (isNaN(value)) {
                        return `${col} = '${value.toString().replace(/'/g, "''")}'`;
                    } else {
                        return `${col} = ${value}`;
                    }
                }).join(' AND ');
            
            const response = await $.post(`${this.baseUrl}/UpdateRecord`, {
                tableName: this.currentTableName,
                updateFields: updateFields,
                whereClause: whereConditions,
                parameters: ''
            });
            
            if (response.success) {
                // อัปเดตข้อมูลใน DataTable
                this.currentDataTable.row(rowIndex).data(updatedData).draw();
                $('#editModal').modal('hide');
                this.showAlert('success', response.message);
                
                // รีเฟรชประวัติ
                if ($('#history-tab').hasClass('active')) {
                    setTimeout(() => this.loadQueryHistory(), 500);
                }
            } else {
                this.showAlert('danger', response.message);
            }
        } catch (error) {
            this.showAlert('danger', 'เกิดข้อผิดพลาดในการบันทึกข้อมูล');
        } finally {
            $('#saveEdit').html('<i class="fas fa-save me-1"></i>บันทึกการแก้ไข').prop('disabled', false);
        }
    },

    // ยืนยันการลบ
    async confirmDelete(e) {
        const rowIndex = $(e.currentTarget).data('row-index');
        const rowData = this.currentDataTable.row(rowIndex).data();
        
        try {
            const $btn = $('#confirmDelete');
            $btn.html('<i class="fas fa-spinner fa-spin me-1"></i>กำลังลบ...').prop('disabled', true);
            
            const whereConditions = this.currentTableColumns
                .filter(col => col !== 'การกระทำ' && rowData[col] !== null && rowData[col] !== '')
                .map(col => {
                    const value = rowData[col];
                    if (isNaN(value)) {
                        return `${col} = '${value.toString().replace(/'/g, "''")}'`;
                    } else {
                        return `${col} = ${value}`;
                    }
                }).join(' AND ');
            
            const response = await $.post(`${this.baseUrl}/DeleteRecord`, {
                tableName: this.currentTableName,
                whereClause: whereConditions,
                parameters: ''
            });
            
            if (response.success) {
                // ลบแถวจาก DataTable
                this.currentDataTable.row(rowIndex).remove().draw();
                $('#deleteModal').modal('hide');
                this.showAlert('success', response.message);
                
                // อัปเดตจำนวนรายการ
                const newCount = this.currentDataTable.rows().count();
                $('#recordCount').text(`${newCount} รายการ`);
                
                // รีเฟรชประวัติ
                if ($('#history-tab').hasClass('active')) {
                    setTimeout(() => this.loadQueryHistory(), 500);
                }
            } else {
                this.showAlert('danger', response.message);
            }
        } catch (error) {
            this.showAlert('danger', 'เกิดข้อผิดพลาดในการลบข้อมูล');
        } finally {
            $('#confirmDelete').html('<i class="fas fa-trash me-1"></i>ลบข้อมูล').prop('disabled', false);
        }
    },

    // ฟังก์ชันช่วยสำหรับแสดง Modal
    showModal(modalHtml, modalId, callback) {
        // ลบ modal เก่า
        $(`#${modalId}`).remove();
        
        // เพิ่ม modal ใหม่
        $('body').append(modalHtml);
        
        // แสดง modal
        $(`#${modalId}`).modal('show');
        
        // เรียก callback
        if (callback) {
            callback();
        }
        
        // ทำความสะอาดเมื่อ modal ถูกปิด
        $(`#${modalId}`).on('hidden.bs.modal', function() {
            $(this).remove();
        });
    },

    // ฟังก์ชันช่วยสำหรับ escape HTML
    escapeHtml(text) {
        if (text === null || text === undefined) return '';
        return text.toString()
            .replace(/&/g, "&amp;")
            .replace(/</g, "&lt;")
            .replace(/>/g, "&gt;")
            .replace(/"/g, "&quot;")
            .replace(/'/g, "&#039;");
    },

    // เพิ่มข้อมูลในตารางปัจจุบัน
    insertToCurrentTable() {
        if (!this.currentTableName || this.currentTableName === 'Unknown') {
            this.showAlert('warning', 'กรุณา Query ตารางก่อนเพื่อระบุตารางที่จะเพิ่มข้อมูล');
            return;
        }
        
        this.showInsertModal(this.currentTableName, this.currentTableColumns);
    },

    // เพิ่มข้อมูลในตารางอื่น
    insertToTable() {
        const modalHtml = `
            <div class="modal fade" id="selectTableModal" tabindex="-1">
                <div class="modal-dialog">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title">
                                <i class="fas fa-table me-2"></i>เลือกตารางสำหรับเพิ่มข้อมูล
                            </h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <div class="mb-3">
                                <label for="insertTableName" class="form-label">ชื่อตาราง:</label>
                                <input type="text" class="form-control" id="insertTableName" 
                                       placeholder="ใส่ชื่อตาราง Oracle (เช่น TEmpLeave, TUser)">
                            </div>
                            <div class="alert alert-info">
                                <i class="fas fa-info-circle me-2"></i>
                                ระบบจะดึงโครงสร้างคอลัมน์จากตารางที่ระบุ
                            </div>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">ยกเลิก</button>
                            <button type="button" class="btn btn-primary" id="confirmTableSelection">
                                <i class="fas fa-arrow-right me-1"></i>ต่อไป
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;
        
        this.showModal(modalHtml, 'selectTableModal', () => {
            $('#confirmTableSelection').off('click').on('click', () => this.confirmTableSelection());
        });
    },

    // ยืนยันการเลือกตาราง
    async confirmTableSelection() {
        const tableName = $('#insertTableName').val().trim();
        if (!tableName) {
            this.showAlert('warning', 'กรุณาระบุชื่อตาราง');
            return;
        }
        
        try {
            const $btn = $('#confirmTableSelection');
            $btn.html('<i class="fas fa-spinner fa-spin me-1"></i>กำลังดึงข้อมูล...').prop('disabled', true);
            
            const response = await $.get(`${this.baseUrl}/GetTableColumnInfo`, { tableName });
            
            if (response.success) {
                const columns = response.data.map(col => col.columnName);
                $('#selectTableModal').modal('hide');
                this.showInsertModal(tableName, columns);
            } else {
                this.showAlert('danger', response.message);
            }
        } catch (error) {
            this.showAlert('danger', 'เกิดข้อผิดพลาดในการดึงข้อมูลตาราง');
        } finally {
            $('#confirmTableSelection').html('<i class="fas fa-arrow-right me-1"></i>ต่อไป').prop('disabled', false);
        }
    },

    // แสดง Modal สำหรับเพิ่มข้อมูล
    showInsertModal(tableName, columns) {
        const filteredColumns = columns.filter(col => col !== 'การกระทำ');
        const modalHtml = `
            <div class="modal fade" id="insertModal" tabindex="-1" data-bs-backdrop="static">
                <div class="modal-dialog modal-lg">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h5 class="modal-title">
                                <i class="fas fa-plus-circle me-2"></i>เพิ่มข้อมูลใหม่ - ${tableName}
                            </h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body">
                            <form id="insertForm">
                                <div class="row">
                                    ${filteredColumns.map(col => `
                                        <div class="col-md-6 mb-3">
                                            <label for="insert_${col}" class="form-label">${col}</label>
                                            <input type="text" class="form-control" id="insert_${col}" name="${col}" 
                                                   placeholder="ระบุค่าสำหรับ ${col}">
                                        </div>
                                    `).join('')}
                                </div>
                                <div class="alert alert-info">
                                    <i class="fas fa-info-circle me-2"></i>
                                    <strong>หมายเหตุ:</strong> ให้ใส่เฉพาะฟิลด์ที่จำเป็น ฟิลด์ที่เว้นว่างจะถูกส่งเป็น NULL
                                </div>
                            </form>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">ยกเลิก</button>
                            <button type="button" class="btn btn-primary" id="saveInsert" data-table-name="${tableName}">
                                <i class="fas fa-save me-1"></i>บันทึกข้อมูลใหม่
                            </button>
                        </div>
                    </div>
                </div>
            </div>
        `;
        
        this.showModal(modalHtml, 'insertModal', () => {
            $('#saveInsert').off('click').on('click', (e) => this.saveInsert(e));
        });
    },

    // บันทึกข้อมูลใหม่
    async saveInsert(e) {
        const tableName = $(e.currentTarget).data('table-name');
        
        // รวบรวมข้อมูลจาก form
        const insertData = {};
        const fields = [];
        const values = [];
        
        $('#insertForm input').each(function() {
            const fieldName = $(this).attr('name');
            const value = $(this).val().trim();
            
            if (value) {
                fields.push(fieldName);
                values.push(`'${value.replace(/'/g, "''")}'`);
                insertData[fieldName] = value;
            }
        });
        
        if (fields.length === 0) {
            this.showAlert('warning', 'กรุณาระบุข้อมูลอย่างน้อย 1 ฟิลด์');
            return;
        }
        
        try {
            const $btn = $('#saveInsert');
            $btn.html('<i class="fas fa-spinner fa-spin me-1"></i>กำลังบันทึก...').prop('disabled', true);
            
            // สร้างคำสั่ง INSERT
            const insertSQL = `INSERT INTO ${tableName} (${fields.join(', ')}) VALUES (${values.join(', ')})`;
            
            const response = await $.post(`${this.baseUrl}/ExecuteQuery`, {
                sqlQuery: insertSQL,
                parameters: ''
            });
            
            if (response.success) {
                $('#insertModal').modal('hide');
                this.showAlert('success', 'เพิ่มข้อมูลใหม่สำเร็จ');
                
                // ถ้าเป็นตารางปัจจุบัน ให้รีเฟรชข้อมูล
                if (tableName === this.currentTableName) {
                    // รีรัน Query ปัจจุบัน
                    const currentSQL = $('#sqlQuery').val();
                    if (currentSQL.trim()) {
                        setTimeout(() => this.executeQuery(), 500);
                    }
                }
                
                // รีเฟรชประวัติ
                if ($('#history-tab').hasClass('active')) {
                    setTimeout(() => this.loadQueryHistory(), 500);
                }
            } else {
                this.showAlert('danger', response.message);
            }
        } catch (error) {
            this.showAlert('danger', 'เกิดข้อผิดพลาดในการบันทึกข้อมูล');
        } finally {
            $('#saveInsert').html('<i class="fas fa-save me-1"></i>บันทึกข้อมูลใหม่').prop('disabled', false);
        }
    },

    // แสดงข้อมูล Column Info ของตาราง (อัปเดตจากเดิม)
    async showTableColumnInfo(tableName) {
        if (!tableName || tableName === 'Unknown') {
            this.showAlert('warning', 'ไม่ทราบชื่อตารางที่จะดูข้อมูล Column');
            return;
        }
        
        try {
            const response = await $.get(`${this.baseUrl}/GetTableColumnInfo`, { tableName });
            
            if (response.success) {
                this.showColumnInfoModal(response.data, response.tableName);
            } else {
                this.showAlert('danger', response.message);
            }
        } catch (error) {
            this.showAlert('danger', 'เกิดข้อผิดพลาดในการโหลดข้อมูล Column');
        }
    },

    showAlert(type, message, timeout = 5000) {
        const icon = type === 'success' ? 'fa-check-circle' : 
                    type === 'warning' ? 'fa-exclamation-triangle' : 
                    type === 'info' ? 'fa-info-circle' : 'fa-times-circle';
        
        const alertHtml = `
            <div class="alert alert-${type} alert-dismissible fade show oracle-alert position-fixed" role="alert" 
                 style="top: 20px; right: 20px; z-index: 9999; min-width: 300px; max-width: 500px;">
                <i class="fas ${icon} me-2"></i>${message}
                <button type="button" class="btn-close" data-bs-dismiss="alert"></button>
            </div>
        `;
        
        // ลบ alert เก่าก่อนแสดงใหม่
        $('.oracle-alert').remove();
        
        $('body').prepend(alertHtml);
        
        if (timeout > 0) {
            setTimeout(() => {
                $('.oracle-alert').fadeOut(300, function() {
                    $(this).remove();
                });
            }, timeout);
        }
    },

    // ฟังก์ชันใหม่สำหรับโหลดตัวอย่าง Query
    loadExampleQuery(e) {
        try {
            const query = $(e.currentTarget).data('query');
            if (query) {
                $('#sqlQuery').val(query);
                $('#sqlQuery').focus();
                
                // ล้างผลลัพธ์เก่าหากมี
                if (this.currentDataTable || $('#dataTableContainer').is(':visible')) {
                    this.clearPreviousResults();
                }
                
                this.showAlert('info', 'โหลดตัวอย่าง Query แล้ว กดปุ่ม "เรียกใช้ Query" หรือ Ctrl+Enter เพื่อรัน', 4000);
                
                console.log('Example query loaded:', query);
            }
        } catch (error) {
            console.error('Error loading example query:', error);
            this.showAlert('danger', 'เกิดข้อผิดพลาดในการโหลดตัวอย่าง Query');
        }
    },

    // ฟังก์ชันทดสอบการเชื่อมต่อแบบเร็ว
    async quickConnectionTest() {
        try {
            $('#btnShowConnectionTest').html('<i class="fas fa-spinner fa-spin me-2"></i>กำลังทดสอบ...').prop('disabled', true);
            
            const response = await $.post(`${this.baseUrl}/TestConnection`);
            
            if (response.success) {
                this.showAlert('success', `✓ เชื่อมต่อ Oracle สำเร็จ: ${response.message}`, 5000);
            } else {
                this.showAlert('danger', `✗ ไม่สามารถเชื่อมต่อได้: ${response.message}`, 8000);
            }
        } catch (error) {
            this.showAlert('danger', '✗ เกิดข้อผิดพลาดในการทดสอบการเชื่อมต่อ', 5000);
        } finally {
            $('#btnShowConnectionTest').html('<i class="fas fa-plug me-2"></i>ทดสอบการเชื่อมต่อ').prop('disabled', false);
        }
    },
};
