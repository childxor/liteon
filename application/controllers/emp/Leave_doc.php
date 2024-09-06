<?php

defined('BASEPATH') or exit('No direct script access allowed');

class Leave_doc extends CI_Controller
{

    function __construct()
    {
        /* Call the Model constructor */
        parent::__construct();
        /* Get System Variable */
        $this->data['var'] = $this->efs_lib->get_var_sys();
        /* System Language */
        $this->lang = $this->efs_lib->language_system();

        /* Check Authen & Permission User */
        if (empty($this->session->userdata("user_profile")->id)) {
            redirect('authen', 'refresh');
            exit();
        } else {
            /* Get Permission Module */
            $this->data['permission'] = @$this->efs_lib->get_permission($this->uri->uri_string());
        }
    }

    public function index()
    {
        /* $this->log_lib->write_log('Class: ' . $this->router->fetch_class() . ' => ' . 'ดูข้อมูล'); */
        $this->data['topbar'] = 'main/layout/topbar';
        $this->data['sidebar'] = 'main/layout/sidebar';
        $this->data['footer'] = 'main/layout/footer';
        /* Get Current Module */
        $module = $this->efs_lib->get_current_module($this->uri->uri_string());
        /* System Language */
        $this->data['lang_sys'] = $this->lang;
        /* Module Language */
        $this->data['lang_module'] = $this->efs_lib->language_module($module->id);

        $breadcrumbs['breadcrumb'] = array(
            array(1 => array(
                "name" => '<i class="mdi mdi-home font-20" style="line-height: 20px;"></i>',
                "module" => base_url(),
                "class" => ''
            )),
            array(2 => array(
                "name" => $module->{"name_" . $this->session->userdata("user_profile")->cng_lang},
                "module" => '',
                "class" => 'active'
            ))
        );
        $this->db = $this->load->database('HQMS_IPS', TRUE);
        $this->db->select('deptID, name');
        $this->db->from('Dept');

        $this->data['dept'] = $this->db->get()->result();
        $this->db = $this->load->database('EFINS', TRUE);
        // $this->print_r($this->data['dept']);
        $this->data['breadcrumb'] = $this->load->view('main/layout/breadcrumb', $breadcrumbs, true);
        $this->data['module'] = 'main/emp/leave_doc';
        $this->load->view("main/layout/index", $this->data);
    }

    public function print_r($data)
    {
        echo '<pre>';
        print_r($data);
        echo '</pre>';
        die();
    }

    public function getUserfromHQMS()
    {
        ini_set('max_execution_time', 0);
        // $this->db = $this->load->database('HQMS_IPS', true);
        $this->db = $this->load->database('EFINS', true);

        $sql = "
        WITH FilteredPerson AS (
            SELECT rowAutoID, cardNumber, name, deptName, deptID,disableDate 
            FROM Person
            WHERE LEN(cardNumber) <= 8 AND cardNumber <> '' AND deptID LIKE '" . $this->input->post('deptID') . "%' 
        )
        SELECT 
            fp.rowAutoID, 
            fp.cardNumber, 
            fp.name, 
            fp.deptName, 
            fp.deptID,
            fp.disableDate,
            ISNULL((SELECT TOP 1 cardNumber
            FROM Person
            WHERE personID LIKE CONCAT(fp.cardNumber, '%') AND LEN(cardNumber) > 8 AND cardNumber <> fp.cardNumber), '') AS cardNumber2
        FROM FilteredPerson fp";

        try {
            $result = $this->db->query($sql)->result();
            $this->db =  $this->load->database('EFINS', true);
            $this->db->select('shiftMent, cardNumber, personID');
            $this->db->from('Person');
            $result2 = $this->db->get()->result();
            foreach ($result as $key => $value) {
                // ถ้า result2 cardNumber เหมือนกันให้ add shiftMent ไปใน result
                if (in_array($value->cardNumber, array_column($result2, 'cardNumber'))) {
                    $result[$key]->shiftMent = $result2[array_search($value->cardNumber, array_column($result2, 'cardNumber'))]->shiftMent;
                } else {
                    $result[$key]->shiftMent = '';
                }
            }
            $this->db =  $this->load->database('HQMS_IPS', true);
            $this->db->select('personID');
            $this->db->from('Person');
            $result3 = $this->db->get()->result();
            foreach ($result as $key => $value) {
                // ถ้า result personID มี แต่ใน $result3 ไม่มีให้ add isActive = 0
                if (!in_array($value->cardNumber . '-1', array_column($result3, 'personID')) && !in_array($value->cardNumber . '-2', array_column($result3, 'personID'))) {
                    $result[$key]->isActive = 0;
                } else {
                    $result[$key]->isActive = 1;
                }
            }

            $this->output
                ->set_content_type('application/json')
                ->set_output(json_encode(['data' => $result]));
        } catch (Exception $e) {
            log_message('error', 'Database query failed: ' . $e->getMessage());
            $this->output
                ->set_status_header(500)
                ->set_content_type('application/json')
                ->set_output(json_encode(['error' => 'An error occurred while fetching data']));
        }
    } 

    public function leaveRequest()
    {
        $this->load->library('upload');
        $this->db = $this->load->database('EFINS', TRUE);

        $uploadedFiles = [];
        $uploadPath = 'uploads/leave_doc/';

        // ตรวจสอบและสร้างโฟลเดอร์ถ้ายังไม่มี
        if (!is_dir($uploadPath)) {
            mkdir($uploadPath, 0777, true);
        }
        if (!empty($_FILES['attachments']['name'])) {
            $filesCount = count($_FILES['attachments']['name']);

            for ($i = 0; $i < $filesCount; $i++) {
                $_FILES['userfile']['name'] = $_FILES['attachments']['name'][$i];
                $_FILES['userfile']['type'] = $_FILES['attachments']['type'][$i];
                $_FILES['userfile']['tmp_name'] = $_FILES['attachments']['tmp_name'][$i];
                $_FILES['userfile']['error'] = $_FILES['attachments']['error'][$i];
                $_FILES['userfile']['size'] = $_FILES['attachments']['size'][$i];

                $config = [
                    'upload_path' => $uploadPath,
                    'allowed_types' => 'pdf|jpg|jpeg|png|doc|docx',
                    'max_size' => 8192, // 8MB
                    'encrypt_name' => TRUE
                ];

                $this->upload->initialize($config);

                if ($this->upload->do_upload('userfile')) {
                    $fileData = $this->upload->data();
                    $uploadedFiles[] = $uploadPath . $fileData['file_name'];
                } else {
                    // จัดการข้อผิดพลาดในการอัพโหลด
                    $error = $this->upload->display_errors();
                    return $this->output->set_content_type('application/json')->set_output(json_encode(['status' => 'error', 'message' => $error]));
                }
            }
        }
        $data = [
            'personid' => $this->input->post('personID'),
            'cardnumber' => $this->input->post('cardNumber'),
            'deptname' => $this->input->post('deptName'),
            'leavedate' => $this->input->post('leaveDate'),
            'leaveduration' => $this->input->post('leaveDuration'),
            'leaveunit' => $this->input->post('leaveUnit'),
            'leavetype' => $this->input->post('leaveType'),
            'leavepath' => ($uploadedFiles) ? implode(',', $uploadedFiles) : NULL,
            'leavereason' => $this->input->post('leaveReason'),
            'approver' => NULL,
            'created_at' => date('Y-m-d H:i:s'),
            'created_username' => $this->session->userdata("user_profile")->username,
        ];

        if ($this->db->insert('emp_leave', $data)) {
            $response = ['status' => 'success', 'message' => 'Leave request submitted successfully'];
        } else {
            $response = ['status' => 'error', 'message' => 'Failed to submit leave request'];
        }
        return $this->output->set_content_type('application/json')->set_output(json_encode($response));
    }

    public function getDataHQMS_IPS()
    {
        $startDate = ($this->input->post('startDate') == '') ? date('Y-m-d') : $this->input->post('startDate');
        $endDate = ($this->input->post('endDate') == '') ? date('Y-m-d') : $this->input->post('endDate');
        $doorName = ($this->input->post('doorID') == '') ? '' : $this->input->post('doorID');

        $timeCondition = '';
        $startTime = '';
        $endTime = '';
        if ($this->input->post('selectInOut') == 'in') {
            $timeCondition = 'AND CONVERT(TIME, A.EVENTTIME) BETWEEN @startTime AND @endTime';
            $startTime = '06:00:00';
            $endTime = '12:00:00';
        } else if ($this->input->post('selectInOut') == 'out') {
            $timeCondition = 'AND CONVERT(TIME, A.EVENTTIME) BETWEEN @startTime AND @endTime';
            $startTime = '12:00:01';
            $endTime = '20:00:00';
        }

        if (!empty($this->input->post('can_add'))) {
            $check = "";
        } else {
            $check = $this->session->userdata("user_profile")->emp_code;
        }

        $query = "DECLARE @EVENTTIME3 datetime = '" . $startDate . " 00:00:00';
        DECLARE @BETWEEN4 datetime = '" . $endDate . " 23:59:59';
        DECLARE @emp_cname nvarchar(100) = N'" . $check . "';
        DECLARE @doorNames nvarchar(100) = N'" . $doorName . "';
        DECLARE @startTime time = '" . $startTime . "';
        DECLARE @endTime time = '" . $endTime . "';
    
        EXEC sp_executesql N'
        WITH EventData AS ( 
        SELECT 
        A.ROWAUTOID,
        A.PERSONID AS EMP_ID,
        A.PERSONNAME AS EMP_CNAME,
        A.EVENTCARD AS CARD_ID,
        A.EVENTCODE AS EVENTCODE_ID,
        CASE 
            WHEN RIGHT(A.DEVICEID, 10) IN (''01'', ''02'', ''03'', ''04'', ''0'') THEN N''進''
            WHEN RIGHT(A.DEVICEID, 10) = ''1'' THEN N''出''
            ELSE ''''
        END AS DEVICENAMEIO,
        CASE 
            WHEN RIGHT(A.DEVICEID, 10) IN (''01'', ''02'', ''03'', ''04'', ''0'') THEN N''進''
            WHEN RIGHT(A.DEVICEID, 10) = ''1'' THEN N''出''
            ELSE N''未知''
        END AS DEPLOYDEVICE_ID,
        A.EVENTTIME,
        ISNULL(A.DOORNAME, A.DEVICENAME) AS DEVICENAME,
        A.DOORNAME, 
        A.DEVICENAME AS QDEVICENAME,
        A.EVENTNAME AS EVENTCODE_NAME_OLD,
        A.DEVICEL1ID,
        A.DEVICEL1NAME,
        A.DEVICEL2ID,
        A.DEVICEL3ID,
        CONVERT(VARCHAR(10), A.EVENTTIME, 120) AS EVENT_DATE,
        CONVERT(VARCHAR(8), A.EVENTTIME, 108) AS EVENT_TIME,
        B.JOBPOSITIONID,
        A.DEVICEID AS DOORID,
        Y.DEPTID AS DEP_ID,
        Y.NAME AS DEP_NAME,
        A.EVENTTYPE AS EVENTCODE_LEVEL,
        Y.CODE,
        A.RESERVE3 AS PRODUCT_TYPE,
        A.NEWEVENTCODE_ID,
        A.NEWEVENTCODE_NAME AS EVENTCODE_NAME,
        A.NEWEVENTCODE_TYPE
              FROM PUBEVENT A
        LEFT JOIN PERSON B ON A.PERSONID = B.PERSONID
        LEFT JOIN DEPT Y ON A.DEPTID = Y.DEPTID
        WHERE A.PERSONID LIKE @emp_cname + ''%'' 
        " . $timeCondition . "
            )
            SELECT 
            ROWAUTOID, EMP_ID, EMP_CNAME, CARD_ID, DEP_ID, DEP_NAME,
            EVENTTIME, EVENT_DATE, EVENT_TIME, DEVICENAME, DEVICENAMEIO,
            EVENTCODE_NAME, DEVICEL1ID, DEVICEL1NAME, DOORID, DEVICEL2ID, PRODUCT_TYPE
        FROM EventData
        WHERE NEWEVENTCODE_TYPE IS NOT NULL
        AND EVENTTIME BETWEEN @EVENTTIME3 AND @BETWEEN4 AND DOORNAME LIKE ''%'' + @doorNames + ''%''
        ORDER BY EMP_ID, EVENTTIME',  -- เพิ่มการเรียงลำดับตาม EMP_ID และ EVENTTIME
        N'@EVENTTIME3 datetime, @BETWEEN4 datetime, @emp_cname nvarchar(100), @doorNames nvarchar(100), @startTime time, @endTime time',
        @EVENTTIME3, @BETWEEN4, @emp_cname, @doorNames, @startTime, @endTime";
        // die($query);
        $this->db = $this->load->database('HQMS_IPS', TRUE);
        $result['data'] = [];
        $query_result = $this->db->query($query)->result();

        $current_emp_id = null;
        $last_event_time = null;
        $in_time = null;

        foreach ($query_result as $value) {
            $event_time = new DateTime($value->EVENTTIME);

            if ($current_emp_id !== $value->EMP_ID) {
                // เริ่มต้นพนักงานใหม่
                if ($in_time !== null) {
                    // บันทึกข้อมูลพนักงานคนก่อนหน้า
                    $result['data'][] = $this->createEmployeeRecord($current_emp_id, $in_time, $last_event_time, $last_value);
                }
                $current_emp_id = $value->EMP_ID;
                $in_time = $event_time;
                $last_event_time = $event_time;
            } else {
                $time_diff = $event_time->diff($last_event_time);
                $hours_diff = $time_diff->h + ($time_diff->days * 24);

                if ($hours_diff >= 1 && $hours_diff <= 2) {
                    // บันทึกข้อมูลเข้า-ออก
                    $result['data'][] = $this->createEmployeeRecord($current_emp_id, $in_time, $last_event_time, $last_value);
                    $in_time = $event_time;
                }
                $last_event_time = $event_time;
            }
            $last_value = $value;
        }

        // บันทึกข้อมูลสำหรับพนักงานคนสุดท้าย
        if ($in_time !== null) {
            $result['data'][] = $this->createEmployeeRecord($current_emp_id, $in_time, $last_event_time, $last_value);
        }

        return $this->output->set_content_type('application/json')->set_output(json_encode($result));
    }

    public function insertDistruption()
    {
        $rowAutoId = $this->input->post('id');
        // $this->print_r($_POST);
        $this->db = $this->load->database('HQMS_IPS', TRUE);
        $this->db->select('*');
        $this->db->from('PubEvent');
        $this->db->where('rowAutoID', $rowAutoId);
        $result = $this->db->get()->result();
        $result = $result[0];
        $eventTime = date('Y-m-d', strtotime($result->eventTime)) . ' 18:00:00';

        $data = array(
            'eventType' => $result->eventType,
            'eventTime' => $eventTime,
            'eventName' => $result->eventName,
            'eventCode' => $result->eventCode,
            'eventCard' => $result->eventCard,
            'personID' => $result->personID,
            'personName' => $result->personName,
            'deptID' => $result->deptID,
            'deptName' => $result->deptName,
            'deptCode' => $result->deptCode,
            'deviceID' => $result->deviceID,
            'deviceName' => $result->deviceName,
            'deviceType' => $result->deviceType,
            'doorName' => $result->doorName,
            'deviceL1ID' => $result->deviceL1ID,
            'deviceL1Name' => $result->deviceL1Name,
            'deviceL1Type' => $result->deviceL1Type,
            'deviceL2ID' => $result->deviceL2ID,
            'deviceL2Name' => $result->deviceL2Name,
            'deviceL2Type' => $result->deviceL2Type,
            'deviceL3ID' => $result->deviceL3ID,
            'deviceL3Name' => $result->deviceL3Name,
            'tag' => $result->tag,
            'reserve1' => $result->reserve1,
            'reserve2' => $result->reserve2,
            'reserve3' => $result->reserve3,
            'reserve4' => $result->reserve4,
            'systemType' => $result->systemType,
            'sourcePK' => $result->sourcePK,
            'systemName' => $result->systemName,
            'InOut' => $fresult->InOut,
            'EmailAlarmSend' => $result->EmailAlarmSend,
            'cctvUpdate' => $result->cctvUpdate,
            'extend1' => $result->extend1,
            'NewEventCode_Id' => $result->NewEventCode_Id,
            'NewEventCode_Name' => $result->NewEventCode_Name,
            'NewEventCode_Type' => $result->NewEventCode_Type,
            'Temperature' => $result->Temperature,
            'DeductAmount' => $result->DeductAmount,
            'PreviousBalance' => $result->PreviousBalance,
            'NowBalance' => $result->NowBalance,
            'DeductType' => $result->DeductType,
        );
        if ($this->db->insert('PubEvent', $data)) {
            $dd['status'] = 'success';
        } else {
            $dd['status'] = 'error';
        }
        return $this->output->set_content_type('application/json')->set_output(json_encode($dd));
    }
}
