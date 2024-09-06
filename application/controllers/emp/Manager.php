<?php

defined('BASEPATH') or exit('No direct script access allowed');

class Manager extends CI_Controller
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
        // $this->print_r($this->session->userdata("user_profile"));
        $this->db = $this->load->database('HQMS_IPS', TRUE);
        $this->db->select('doorID, doorName');
        $this->db->from('PubDoor');
        $this->data['Door'] = $this->db->get()->result();
        $this->data['department'] = $this->db->query("SELECT * FROM Dept")->result();
        // $this->print_r($this->data['department']);



        $this->db = $this->load->database('EFINS', TRUE);
        // $this->print_r($this->data['doorname']);
        $this->data['breadcrumb'] = $this->load->view('main/layout/breadcrumb', $breadcrumbs, true);
        $this->data['module'] = 'main/emp/manager';
        $this->load->view("main/layout/index", $this->data);
    }

    public function print_r($data)
    {
        echo '<pre>';
        print_r($data);
        echo '</pre>';
        die();
    }

    public function giveAccess()
    {
        $this->db = $this->load->database('HQMS_IPS', TRUE);
        $this->db->select('doorID, doorName, contolL1');
        $this->db->from('PubDoor');
        $this->db->where('doorName', $this->input->post('doorName'));
        $result = $this->db->get()->result();
        $result = $result[0];
        $dowloadfg = $this->input->post('dnFp');

        $this->db = $this->load->database('HQMS_IPS', TRUE);
        $this->db->select('weekTimeID');
        $this->db->from('PubWeekTime');
        $this->db->where('contolL1', $result->contolL1);
        $result2 = $this->db->get()->result();
        $result2 = $result2[0];


        $data = array(
            'personID' => $this->input->post('id'),
            'doorID' => $result->doorID,
            'weekTimeID' => $result2->weekTimeID,
            'holidayGroupID' => NULL,
            'tag' => 'HTA860PMF',
            'reserve1' => 0,
            'reserve2' => NULL,
            'reserve3' => NULL,
            'reserve4' => NULL,
            'operator' => 'SUPERVISOR',
            'UserGroupID' => NULL,
            'modifyTime' => date('Y-m-d H:i:s'),
            'DoubleCheck' => 0,
            'BlackList' => 0,
            'Check_TimeZone' => 1,
            'Check_Holiday' => 1,
            'DownLoad_Fg' => $dowloadfg,
            'contolL1_N' => NULL,
            'Master_Card' => 0,
        );
        // ค้นหาว่ามีข้อมูลนี้แล้วหรือยัง 
        $this->db->select('personID, doorID');
        $this->db->from('PubDoorAuth');
        $this->db->where('personID', $this->input->post('id'));
        $this->db->where('doorID', $result->doorID);
        // die($this->db->get_compiled_select());
        $query = $this->db->get();
        if ($query->num_rows() > 0) {
            $dd['status'] = 'error';
            $dd['message'] = 'ข้อมูลนี้มีอยู่แล้ว';
        } else {
            if ($this->db->insert('PubDoorAuth', $data)) {
                $dd['status'] = 'success';
            } else {
                $dd['status'] = 'error';
            }
        }
        return $this->output->set_content_type('application/json')->set_output(json_encode($dd));
    }

    public function getDoorName()
    {
        $this->db = $this->load->database('HQMS_IPS', TRUE);
        $this->db->select('doorID, doorName');
        $this->db->from('PubDoor');
        $result['data'] = $this->db->get()->result();
        // $this->print_r($result);
        return $result;
    }

    // update ให้ตรงเวลา morning
    public function setTimeKhem()
    {
        $time = $this->input->post('firstTime');
        // $startDate = $this->input->post('startDate');
        $startDate = date('Y-m-d', strtotime($time));

        // สร้างเวลาใหม่แบบสุ่มระหว่าง 07:10:00 ถึง 07:59:59
        $newTime = sprintf(
            "%s 07:%02d:%02d",
            $startDate,
            rand(10, 59),
            rand(0, 59)
        );
        $newEventTime = date('Y-m-d H:i:s', strtotime($newTime));

        $this->db = $this->load->database('HQMS_IPS', TRUE);

        $query = $this->db->query("SELECT rowAutoID FROM PubEvent WHERE DATEDIFF(SECOND, eventTime, CONVERT(DATETIME2(7), ?, 121)) = 0", array($time));

        if ($query === FALSE) {
            $result = array(
                'status' => 'error',
                'message' => 'เกิดข้อผิดพลาดในการค้นหาข้อมูล',
                'query' => $this->db->last_query(),
                'error' => $this->db->error()
            );
        } elseif ($query->num_rows() > 0) {
            $row = $query->row();
            $rowAutoId = $row->rowAutoID;

            // อัพเดทเวลาใหม่
            $updateResult = $this->db->query("UPDATE PubEvent SET eventTime = ? WHERE rowAutoID = ?", array($newEventTime, $rowAutoId));

            if ($updateResult) {
                $result = array(
                    'status' => 'success',
                    'message' => 'อัพเดทเวลาสำเร็จ',
                    'newTime' => $newEventTime,
                    'query' => $this->db->last_query()
                );
            } else {
                $result = array(
                    'status' => 'error',
                    'message' => 'ไม่สามารถอัพเดทเวลาได้',
                    'query' => $this->db->last_query(),
                    'error' => $this->db->error()
                );
            }
        } else {
            $result = array(
                'status' => 'error',
                'message' => 'ไม่พบข้อมูลที่ต้องการอัพเดท',
                'query' => $this->db->last_query()
            );
        }

        return $this->output
            ->set_content_type('application/json')
            ->set_output(json_encode($result));
    }
    // update ให้ตรงเวลา evening
    public function editDataevning()
    {
        $time = $this->input->post('firstTime');
        // $startDate = $this->input->post('startDate');
        $startDate = date('Y-m-d', strtotime($time));

        // สร้างเวลาใหม่แบบสุ่มระหว่าง 07:10:00 ถึง 07:59:59
        $newTime = sprintf(
            "%s 19:%02d:%02d",
            $startDate,
            rand(10, 59),
            rand(0, 59)
        );
        $newEventTime = date('Y-m-d H:i:s', strtotime($newTime));

        $this->db = $this->load->database('HQMS_IPS', TRUE);

        $query = $this->db->query("SELECT rowAutoID FROM PubEvent WHERE DATEDIFF(SECOND, eventTime, CONVERT(DATETIME2(7), ?, 121)) = 0", array($time));

        if ($query === FALSE) {
            $result = array(
                'status' => 'error',
                'message' => 'เกิดข้อผิดพลาดในการค้นหาข้อมูล',
                'query' => $this->db->last_query(),
                'error' => $this->db->error()
            );
        } elseif ($query->num_rows() > 0) {
            $row = $query->row();
            $rowAutoId = $row->rowAutoID;

            // อัพเดทเวลาใหม่
            $updateResult = $this->db->query("UPDATE PubEvent SET eventTime = ? WHERE rowAutoID = ?", array($newEventTime, $rowAutoId));

            if ($updateResult) {
                $result = array(
                    'status' => 'success',
                    'message' => 'อัพเดทเวลาสำเร็จ',
                    'newTime' => $newEventTime,
                    'query' => $this->db->last_query()
                );
            } else {
                $result = array(
                    'status' => 'error',
                    'message' => 'ไม่สามารถอัพเดทเวลาได้',
                    'query' => $this->db->last_query(),
                    'error' => $this->db->error()
                );
            }
        } else {
            $result = array(
                'status' => 'error',
                'message' => 'ไม่พบข้อมูลที่ต้องการอัพเดท',
                'query' => $this->db->last_query()
            );
        }

        return $this->output
            ->set_content_type('application/json')
            ->set_output(json_encode($result));
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
        $eventTime = ($this->input->post('startDate') == '') ? date('Y-m-d') : $this->input->post('startDate') . ' 07:' . rand(10, 59) . ':' . rand(10, 59);

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
            'InOut' => $result->InOut,
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
        die($this->db->last_query());
        return $this->output->set_content_type('application/json')->set_output(json_encode($dd));
    }

    public function getDataHQMS_IPS()
    {
        $this->load->model('emp/Manager_Model');

        $start_date = $this->input->post('selectDate') . ' 00:00:00';
        $end_date = $this->input->post('selectDate') . ' 23:59:59';
        $door_id = $this->input->post('doorID') ?: 'all';
        $in_out = $this->input->post('selectInOut');
        $shift_type = $this->input->post('selectShift');
        $dept = $this->input->post('selectDistrup');

        // Validate and sanitize inputs
        $start_date = $this->security->xss_clean($start_date);
        $end_date = $this->security->xss_clean($end_date);
        $door_id = $this->security->xss_clean($door_id);
        $in_out = $this->security->xss_clean($in_out);
        $shift_type = $this->security->xss_clean($shift_type);

        try {
            $event_data = $this->Manager_Model->get_event_data($start_date, $end_date, $door_id, $in_out, $shift_type, $dept);
            $person_shift_data = $this->Manager_Model->get_person_shift_data();

            $result = $this->process_data($event_data, $person_shift_data);

            $this->output
                ->set_content_type('application/json')
                ->set_output(json_encode(['status' => 'success', 'data' => $result]));
        } catch (Exception $e) {
            log_message('error', 'Error in getDataHQMS_IPS: ' . $e->getMessage());
            $this->output
                ->set_content_type('application/json')
                ->set_output(json_encode(['status' => 'error', 'message' => 'An error occurred while fetching data.']));
        }
    }

    private function process_data($event_data, $person_shift_data)
    {
        $result = [];
        foreach ($event_data as $event) {
            $base_person_id = $event->basePersonID;
            if (strpos($event->deviceName, 'HR Attendance') !== false) {
                // สำหรับ HR Attendance ใช้ลอจิกเดิม
                if (!isset($result[$base_person_id])) {
                    $result[$base_person_id] = [
                        'personID' => $base_person_id,
                        'personName' => $event->personName,
                        'deptCode' => $event->deptCode,
                        'deptName' => $event->deptName,
                        'deptID' => $event->deptID,
                        'FirstTime' => $event->eventTime,
                        'LastTime' => $event->eventTime,
                        'deviceName' => $event->deviceName,
                        'rDate' => $event->rDate,
                        'shift' => $this->get_shift($event->personName, $person_shift_data),
                        'fingerprintUsed' => strpos($event->personID, '-2') !== false,
                        'cardUsed' => strpos($event->personID, '-1') !== false
                    ];
                } else {
                    if ($event->eventTime < $result[$base_person_id]['FirstTime']) {
                        $result[$base_person_id]['FirstTime'] = $event->eventTime;
                    }
                    if ($event->eventTime > $result[$base_person_id]['LastTime']) {
                        $result[$base_person_id]['LastTime'] = $event->eventTime;
                    }
                    $result[$base_person_id]['fingerprintUsed'] |= strpos($event->personID, '-2') !== false;
                    $result[$base_person_id]['cardUsed'] |= strpos($event->personID, '-1') !== false;
                }
            } else {
                // สำหรับประตูอื่นๆ ให้แสดงทุกรายการ
                $result[] = [
                    'personID' => $base_person_id,
                    'personName' => $event->personName,
                    'deptCode' => $event->deptCode,
                    'deptName' => $event->deptName,
                    'deptID' => $event->deptID,
                    'FirstTime' => $event->eventTime,  // ใช้ eventTime เป็นทั้ง FirstTime และ LastTime
                    'LastTime' => $event->eventTime,
                    'deviceName' => $event->deviceName,
                    'rDate' => $event->rDate,
                    'shift' => $this->get_shift($event->personName, $person_shift_data),
                    'fingerprintUsed' => strpos($event->personID, '-2') !== false,
                    'cardUsed' => strpos($event->personID, '-1') !== false
                ];
            }
        }
        return array_values($result);
    }

    private function get_shift($person_name, $person_shift_data)
    {
        foreach ($person_shift_data as $person) {
            if ($person->name == $person_name) {
                return $person->shiftMent;
            }
        }
        return null;
    }


    public function prepareForYear($year = 2024)
    {
        $shifts = [];
        $start_date = new DateTime($year . '-01-01');
        $end_date = new DateTime($year . '-12-31');

        // กำหนดวันเริ่มต้นของกะ A
        $shift_start_date = new DateTime($year . '-08-05');

        // ตัวนับวันสำหรับการสลับกะทุก 14 วัน
        $day_counter = 0;

        while ($start_date <= $end_date) {
            $current_date = $start_date->format('Y-m-d');
            $current_week = $start_date->format('W');

            // ตรวจสอบว่าถึงวันเริ่มต้นกะหรือยัง
            if ($start_date >= $shift_start_date) {
                $is_a_shift = (floor($day_counter / 14) % 2 == 0);
                $day_counter++;
            } else {
                // ก่อนวันเริ่มต้น ให้เป็นกะ B
                $is_a_shift = false;
            }

            $day_shift_start = clone $start_date;
            $day_shift_start->setTime(8, 0);
            $day_shift_end = clone $day_shift_start;
            $day_shift_end->setTime(17, 0);

            $day_ot_start = clone $day_shift_end;
            $day_ot_start->setTime(17, 30);
            $day_ot_end = clone $day_ot_start;
            $day_ot_end->setTime(19, 30);

            $night_shift_start = clone $start_date;
            $night_shift_start->setTime(20, 0);
            $night_shift_end = clone $night_shift_start;
            $night_shift_end->modify('+1 day')->setTime(5, 0);

            $night_ot_start = clone $night_shift_end;
            $night_ot_start->setTime(5, 30);
            $night_ot_end = clone $night_ot_start;
            $night_ot_end->setTime(7, 30);

            $shifts[] = [
                'date' => $current_date,
                'week' => $current_week,
                'year' => $year,
                'day_shift' => $is_a_shift ? 'A' : 'B',
                'night_shift' => $is_a_shift ? 'B' : 'A',
                'day_shift_start' => $day_shift_start->format('Y-m-d H:i:s'),
                'day_shift_end' => $day_shift_end->format('Y-m-d H:i:s'),
                'day_shift_ot_start' => $day_ot_start->format('Y-m-d H:i:s'),
                'day_shift_ot_end' => $day_ot_end->format('Y-m-d H:i:s'),
                'night_shift_start' => $night_shift_start->format('Y-m-d H:i:s'),
                'night_shift_end' => $night_shift_end->format('Y-m-d H:i:s'),
                'night_shift_ot_start' => $night_ot_start->format('Y-m-d H:i:s'),
                'night_shift_ot_end' => $night_ot_end->format('Y-m-d H:i:s')
            ];

            $start_date->modify('+1 day');
        }
        return $shifts;
    }
}
