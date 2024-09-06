<?php

defined('BASEPATH') or exit('No direct script access allowed');

class Emp_all extends CI_Controller
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
        $this->data['lang_sys'] = $this->efs_lib->language_system();
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
        $this->data['breadcrumb'] = $this->load->view('main/layout/breadcrumb', $breadcrumbs, true);
        $this->data['module'] = 'main/sys/emp_all';

        $this->db =  $this->load->database('HQMS_IPS', true);
        $this->db->select('deptID, name');
        $this->db->from('Dept');
        $this->db->order_by('deptID', 'ASC');
        $this->data['dept'] = $this->db->get()->result();
        $this->db =  $this->load->database('EFINS', true);

        $this->load->view("main/layout/index", $this->data);
    }

    public function print_r($data)
    {
        echo '<pre>';
        print_r($data);
        echo '</pre>';
        die();
    }

    // updateDept
    public function updateDept()
    {
        $this->db =  $this->load->database('EFINS', true);
        $this->db->like('name', $this->input->post('personName'));
        $this->db->update('Person', ['deptID' => $this->input->post('deptID'), 'deptName' => $this->input->post('deptName')]);

        $this->db =  $this->load->database('HQMS_IPS', true);
        $this->db->like('name', $this->input->post('personName'));
        $this->db->update('Person', ['deptID' => $this->input->post('deptID'), 'deptName' => $this->input->post('deptName')]);
        $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'success', 'message' => $this->db->last_query())));
    }

    public function getUserfromHQMS()
    {
        ini_set('max_execution_time', 0); 
        $efinsDb = $this->load->database('EFINS', true);
        $hqmsIpsDb = $this->load->database('HQMS_IPS', true);
        $shift_type = $this->input->post('shift_type');

        try {
            $sql = "
        WITH FilteredPerson AS (
            SELECT cardNumber, name, deptName, deptID, disableDate 
            FROM Person
            WHERE LEN(cardNumber) <= 8 AND cardNumber <> '' AND deptID LIKE '" . $this->input->post('deptID') . "%' " . ($shift_type == '' ? '' : "AND shiftMent = '$shift_type'") . "
        )
        SELECT 
            fp.cardNumber, 
            fp.name, 
            fp.deptName, 
            fp.deptID,
            fp.disableDate,
            ISNULL((SELECT TOP 1 cardNumber
            FROM Person
            WHERE personID LIKE CONCAT(fp.cardNumber, '%') AND LEN(cardNumber) > 8 AND cardNumber <> fp.cardNumber), '') AS cardNumber2
        FROM FilteredPerson fp
         GROUP BY fp.cardNumber, fp.name, fp.deptName, fp.deptID, fp.disableDate";
            // die($sql);
            $result = $efinsDb->query($sql)->result();

            // ดึงข้อมูล Fingerprint และ Active Users
            $fpUsers = $hqmsIpsDb->select('CardNumber')->from('Person_FP')->get()->result_array();
            $activeUsers = $hqmsIpsDb->select('personID')->from('Person')->get()->result_array();

            $countAuthDoor = $hqmsIpsDb->select('personID')->from('PubDoorAuth')->get()->result_array();

            // ดึงข้อมูล Shift
            $shiftData = $efinsDb->select('shiftMent, cardNumber')->from('Person')->get()->result_array();

            // สร้าง lookup arrays
            $fpUsersLookup = array_column($fpUsers, 'CardNumber');
            $activeUsersLookup = array_column($activeUsers, 'personID');
            $shiftDataLookup = array_column($shiftData, 'shiftMent', 'cardNumber');

            // ประมวลผลข้อมูล
            foreach ($result as &$user) {
                $user->shiftMent = isset($shiftDataLookup[$user->cardNumber]) ? $shiftDataLookup[$user->cardNumber] : '';
                $user->isFp = in_array($user->cardNumber, $fpUsersLookup) ? 1 : 0;
                $user->isActive = (in_array($user->cardNumber . '-1', $activeUsersLookup) || in_array($user->cardNumber . '-2', $activeUsersLookup)) ? 1 : 0;
                // จำนวนสิทธิ์การเข้าถึง
                $user->isAuthDoor = count(array_filter($countAuthDoor, function ($auth) use ($user) {
                    return strpos($auth['personID'], $user->cardNumber) === 0;
                }));
            }

            // กรองตาม is_active ถ้าจำเป็น
            $isActive = $this->input->post('is_active');
            if ($isActive === 'Y' || $isActive === 'N') {
                $result = array_filter($result, function ($user) use ($isActive) {
                    return ($isActive === 'Y' && $user->isActive == 1) || ($isActive === 'N' && $user->isActive == 0);
                });
            }

            $this->output
                ->set_content_type('application/json')
                ->set_output(json_encode(['data' => array_values($result)]));
        } catch (Exception $e) {
            log_message('error', 'Database query failed: ' . $e->getMessage());
            $this->output
                ->set_status_header(500)
                ->set_content_type('application/json')
                ->set_output(json_encode(['error' => 'An error occurred while fetching data']));
        }
    }

    public function countAuthDoor()
    {
        $this->db =  $this->load->database('HQMS_IPS', true);
        $this->db->select('personID');
        $this->db->from('PubDoorAuth');
        $result = $this->db->get()->result();
        return $result;
    }


    public function updateShift()
    {
        $this->db =  $this->load->database('EFINS', true);
        $this->db->like('personID', $this->input->post('personID'));
        $this->db->update('Person', ['shiftMent' => $this->input->post('shift')]);
        $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'success', 'message' => $this->db->last_query())));
    }

    // offShift
    public function offShift()
    {
        $this->db =  $this->load->database('EFINS', true);
        $shift = ($this->input->post('shift') == 'Off') ? NULL : 'A';
        $this->db->like('personID', $this->input->post('personID'));
        $this->db->update('Person', ['shiftMent' => $shift]);
        $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'success', 'message' => $this->db->last_query())));
    }

    public function checkNorow()
    {
        $this->db =  $this->load->database('HQMS_IPS', true);
        $this->db->select('*');
        $this->db->from('Person');
        $result = $this->db->get()->result();

        foreach ($result as $key => $value) {
            //  หา personID 10128931-2 ตัดข้อความตั้งแต่ - ออก
            $personID = explode('-', $value->personID);
            //  หา personID 10128931-2 และ 10128931-1 ใน $result ถ้าไม่มีให้เพิ่มเข้าไป 
            if (!in_array($personID[0] . '-1', array_column($result, 'personID'))) {
                $data = array(
                    'personID' => $personID[0] . '-1',
                    'cardNumber' => ($value->cardNumber == '' || $value->cardNumber == NULL) ? $personID[0] : $value->cardNumber,
                    'name' => $value->name,
                    'deptID' => $value->deptID,
                    'deptName' => $value->deptName,
                    'serialNumber' => $value->serialNumber,
                    'englishName' => $value->englishName,
                    'sex' => $value->sex,
                    'birthday' => $value->birthday,
                    'identityType' => $value->identityType,
                    'identityID' => $value->identityID,
                    'inID' => $value->inID,
                    'inName' => $value->inName,
                    'jobLevelID' => $value->jobLevelID,
                    'jobPositionID' => $value->jobPositionID,
                    'category' => $value->category,
                    'educational' => $value->educational,
                    'nation' => $value->nation,
                    'place' => $value->place,
                    'people' => $value->people,
                    'specialty' => $value->specialty,
                    'inaugurationDate' => $value->inaugurationDate,
                    'leaveJobDate' => $value->leaveJobDate,
                    'enableDate' => $value->enableDate,
                    'disableDate' => $value->disableDate,
                    'HealthStatus' => $value->HealthStatus,
                    'interest' => $value->interest,
                    'introducer' => $value->introducer,
                    'salaryCategory' => $value->salaryCategory,
                    'email' => $value->email,
                    'address' => $value->address,
                    'phone' => $value->phone,
                    'zip' => $value->zip,
                    'registerAddress' => $value->registerAddress,
                    'registerPhone' => $value->registerPhone,
                    'registerZip' => $value->registerZip,
                    'school' => $value->school,
                    'department' => $value->department,
                    'urgentContact' => $value->urgentContact,
                    'urgentPhone' => $value->urgentPhone,
                    'marriage' => $value->marriage,
                    'spouse' => $value->spouse,
                    'spousePhone' => $value->spousePhone,
                    'userID' => $value->userID,
                    'userLevel' => $value->userLevel,
                    'password' => $value->password,
                    'superPassword' => $value->superPassword,
                    'modifyTime' => $value->modifyTime,
                    'operator' => $value->operator,
                    'reserve1' => $value->reserve1,
                    'reserve2' => $value->reserve2,
                    'reserve3' => $value->reserve3,
                    'reserve4' => $value->reserve4,
                    'reserveChar1' => $value->reserveChar1,
                    'reserveChar2' => $value->reserveChar2,
                    'reserveChar3' => $value->reserveChar3,
                    'reserveChar4' => $value->reserveChar4,
                    'reserveChar5' => $value->reserveChar5,
                    'reserveChar6' => $value->reserveChar6,
                    'reserveChar7' => $value->reserveChar7,
                    'reserveChar8' => $value->reserveChar8,
                    'reserveInt1' => $value->reserveInt1,
                    'reserveInt2' => $value->reserveInt2,
                    'reserveInt3' => $value->reserveInt3,
                    'reserveInt4' => $value->reserveInt4,
                    'reserveInt5' => $value->reserveInt5,
                    'reserveInt6' => $value->reserveInt6,
                    'reserveInt7' => $value->reserveInt7,
                    'reserveInt8' => $value->reserveInt8,
                    'pinyin' => $value->pinyin,
                    'cardType' => $value->cardType,
                    'cardTypeDesc' => $value->cardTypeDesc,
                    'subSystem' => $value->subSystem,
                    'useCategory' => $value->useCategory,
                    'useStatus' => $value->useStatus,
                    'note' => $value->note,
                    'groupID' => $value->groupID,
                    'timeGroup' => $value->timeGroup,
                    'status' => $value->status,
                    'cardCategory' => $value->cardCategory,
                    'cardStatus' => $value->cardStatus,
                    'eatStatus' => $value->eatStatus,
                    'freeNumber' => $value->freeNumber,
                    'ATT_Free' => $value->ATT_Free,
                    'cardNumberSP1' => $value->cardNumberSP1,
                    'cardNumberSP2' => $value->cardNumberSP2,
                    'cardNumberSP3' => $value->cardNumberSP3,
                    'cardNumberSP4' => $value->cardNumberSP4,
                    'cardNumberSP5' => $value->cardNumberSP5,
                    'cardNumberSP6' => $value->cardNumberSP6,
                    'FaceUserID' => $value->FaceUserID,
                    'CREATEDATE' => $value->CREATEDATE,
                );
                $this->db =  $this->load->database('HQMS_IPS', true);
                $this->db->insert('Person', $data);
            }
            if (!in_array($personID[0] . '-2', array_column($result, 'personID'))) {
                $data = array(
                    'personID' => $personID[0] . '-2',
                    'cardNumber' => $personID[0],
                    'name' => $value->name,
                    'deptID' => $value->deptID,
                    'deptName' => $value->deptName,
                    'serialNumber' => $value->serialNumber,
                    'englishName' => $value->englishName,
                    'sex' => $value->sex,
                    'birthday' => $value->birthday,
                    'identityType' => $value->identityType,
                    'identityID' => $value->identityID,
                    'inID' => $value->inID,
                    'inName' => $value->inName,
                    'jobLevelID' => $value->jobLevelID,
                    'jobPositionID' => $value->jobPositionID,
                    'category' => $value->category,
                    'educational' => $value->educational,
                    'nation' => $value->nation,
                    'place' => $value->place,
                    'people' => $value->people,
                    'specialty' => $value->specialty,
                    'inaugurationDate' => $value->inaugurationDate,
                    'leaveJobDate' => $value->leaveJobDate,
                    'enableDate' => $value->enableDate,
                    'disableDate' => $value->disableDate,
                    'HealthStatus' => $value->HealthStatus,
                    'interest' => $value->interest,
                    'introducer' => $value->introducer,
                    'salaryCategory' => $value->salaryCategory,
                    'email' => $value->email,
                    'address' => $value->address,
                    'phone' => $value->phone,
                    'zip' => $value->zip,
                    'registerAddress' => $value->registerAddress,
                    'registerPhone' => $value->registerPhone,
                    'registerZip' => $value->registerZip,
                    'school' => $value->school,
                    'department' => $value->department,
                    'urgentContact' => $value->urgentContact,
                    'urgentPhone' => $value->urgentPhone,
                    'marriage' => $value->marriage,
                    'spouse' => $value->spouse,
                    'spousePhone' => $value->spousePhone,
                    'userID' => $value->userID,
                    'userLevel' => $value->userLevel,
                    'password' => $value->password,
                    'superPassword' => $value->superPassword,
                    'modifyTime' => $value->modifyTime,
                    'operator' => $value->operator,
                    'reserve1' => $value->reserve1,
                    'reserve2' => $value->reserve2,
                    'reserve3' => $value->reserve3,
                    'reserve4' => $value->reserve4,
                    'reserveChar1' => $value->reserveChar1,
                    'reserveChar2' => $value->reserveChar2,
                    'reserveChar3' => $value->reserveChar3,
                    'reserveChar4' => $value->reserveChar4,
                    'reserveChar5' => $value->reserveChar5,
                    'reserveChar6' => $value->reserveChar6,
                    'reserveChar7' => $value->reserveChar7,
                    'reserveChar8' => $value->reserveChar8,
                    'reserveInt1' => $value->reserveInt1,
                    'reserveInt2' => $value->reserveInt2,
                    'reserveInt3' => $value->reserveInt3,
                    'reserveInt4' => $value->reserveInt4,
                    'reserveInt5' => $value->reserveInt5,
                    'reserveInt6' => $value->reserveInt6,
                    'reserveInt7' => $value->reserveInt7,
                    'reserveInt8' => $value->reserveInt8,
                    'pinyin' => $value->pinyin,
                    'cardType' => $value->cardType,
                    'cardTypeDesc' => $value->cardTypeDesc,
                    'subSystem' => $value->subSystem,
                    'useCategory' => $value->useCategory,
                    'useStatus' => $value->useStatus,
                    'note' => $value->note,
                    'groupID' => $value->groupID,
                    'timeGroup' => $value->timeGroup,
                    'status' => $value->status,
                    'cardCategory' => $value->cardCategory,
                    'cardStatus' => $value->cardStatus,
                    'eatStatus' => $value->eatStatus,
                    'freeNumber' => $value->freeNumber,
                    'ATT_Free' => $value->ATT_Free,
                    'cardNumberSP1' => $value->cardNumberSP1,
                    'cardNumberSP2' => $value->cardNumberSP2,
                    'cardNumberSP3' => $value->cardNumberSP3,
                    'cardNumberSP4' => $value->cardNumberSP4,
                    'cardNumberSP5' => $value->cardNumberSP5,
                    'cardNumberSP6' => $value->cardNumberSP6,
                    'FaceUserID' => $value->FaceUserID,
                    'CREATEDATE' => $value->CREATEDATE,
                );
                $this->db =  $this->load->database('HQMS_IPS', true);
                $this->db->insert('Person', $data);
            }

            // ถ้า CardNumber มีค่าว่างให้เอา personID ไปใส่ใน CardNumber
            if ($value->cardNumber == '' || $value->cardNumber == NULL) {
                $this->db =  $this->load->database('HQMS_IPS', true);
                $this->db->where('personID', $value->personID);
                $this->db->update('Person', ['cardNumber' => $personID[0]]);
            }
        }
        $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'success')));
    }

    public function syncData()
    {
        //    $this->print_r($_POST); 
        $this->db =  $this->load->database('HQMS_IPS', true);
        $this->db->select('*');
        $this->db->order_by('personID', 'DESC');
        $personHQMS = $this->db->get('Person')->result();

        $this->db =  $this->load->database('EFINS', true);
        $this->db->select('*');
        $this->db->order_by('personID', 'DESC');
        $personLocal = $this->db->get('Person')->result();

        foreach ($personHQMS as $key => $value) {
            $data = array(
                'cardNumber' => $value->cardNumber,
                'serialNumber' => $value->serialNumber,
                'personID' => $value->personID,
                'name' => $value->name,
                'englishName' => $value->englishName,
                'sex' => $value->sex,
                'birthday' => $value->birthday,
                'identityType' => $value->identityType,
                'identityID' => $value->identityID,
                'deptID' => $value->deptID,
                'deptName' => $value->deptName,
                'inID' => $value->inID,
                'inName' => $value->inName,
                'jobLevelID' => $value->jobLevelID,
                'jobPositionID' => $value->jobPositionID,
                'category' => $value->category,
                'educational' => $value->educational,
                'nation' => $value->nation,
                'place' => $value->place,
                'people' => $value->people,
                'specialty' => $value->specialty,
                'inaugurationDate' => $value->inaugurationDate,
                'leaveJobDate' => $value->leaveJobDate,
                'enableDate' => $value->enableDate,
                'disableDate' => $value->disableDate,
                'HealthStatus' => $value->HealthStatus,
                'interest' => $value->interest,
                'introducer' => $value->introducer,
                'salaryCategory' => $value->salaryCategory,
                'email' => $value->email,
                'address' => $value->address,
                'phone' => $value->phone,
                'zip' => $value->zip,
                'registerAddress' => $value->registerAddress,
                'registerPhone' => $value->registerPhone,
                'registerZip' => $value->registerZip,
                'school' => $value->school,
                'department' => $value->department,
                'urgentContact' => $value->urgentContact,
                'urgentPhone' => $value->urgentPhone,
                'marriage' => $value->marriage,
                'spouse' => $value->spouse,
                'spousePhone' => $value->spousePhone,
                'userID' => $value->userID,
                'userLevel' => $value->userLevel,
                'password' => $value->password,
                'superPassword' => $value->superPassword,
                'modifyTime' => $value->modifyTime,
                'operator' => $value->operator,
                'reserve1' => $value->reserve1,
                'reserve2' => $value->reserve2,
                'reserve3' => $value->reserve3,
                'reserve4' => $value->reserve4,
                'reserveChar1' => $value->reserveChar1,
                'reserveChar2' => $value->reserveChar2,
                'reserveChar3' => $value->reserveChar3,
                'reserveChar4' => $value->reserveChar4,
                'reserveChar5' => $value->reserveChar5,
                'reserveChar6' => $value->reserveChar6,
                'reserveChar7' => $value->reserveChar7,
                'reserveChar8' => $value->reserveChar8,
                'reserveInt1' => $value->reserveInt1,
                'reserveInt2' => $value->reserveInt2,
                'reserveInt3' => $value->reserveInt3,
                'reserveInt4' => $value->reserveInt4,
                'reserveInt5' => $value->reserveInt5,
                'reserveInt6' => $value->reserveInt6,
                'reserveInt7' => $value->reserveInt7,
                'reserveInt8' => $value->reserveInt8,
                'pinyin' => $value->pinyin,
                'cardType' => $value->cardType,
                'cardTypeDesc' => $value->cardTypeDesc,
                'subSystem' => $value->subSystem,
                'useCategory' => $value->useCategory,
                'useStatus' => $value->useStatus,
                'note' => $value->note,
                'groupID' => $value->groupID,
                'timeGroup' => $value->timeGroup,
                'status' => $value->status,
                'cardCategory' => $value->cardCategory,
                'cardStatus' => $value->cardStatus,
                'eatStatus' => $value->eatStatus,
                'freeNumber' => $value->freeNumber,
                'ATT_Free' => $value->ATT_Free,
                'cardNumberSP1' => $value->cardNumberSP1,
                'cardNumberSP2' => $value->cardNumberSP2,
                'cardNumberSP3' => $value->cardNumberSP3,
                'cardNumberSP4' => $value->cardNumberSP4,
                'cardNumberSP5' => $value->cardNumberSP5,
                'cardNumberSP6' => $value->cardNumberSP6,
                'FaceUserID' => $value->FaceUserID,
                'CREATEDATE' => $value->CREATEDATE,
            );
            if (!in_array($value->personID, array_column($personLocal, 'personID'))) {
                $this->db =  $this->load->database('EFINS', true);
                $this->db->insert('Person', $data);
            } else {
                $this->db =  $this->load->database('EFINS', true);
                $this->db->where('personID', $value->personID);
                $this->db->update('Person', $data);
            }
        }
        $this->db =  $this->load->database('EFINS', true);
        $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'success')));
    }

    // add data HQMS 
    public function addDataHQMS()
    {
        $this->db = $this->load->database('HQMS_IPS', true);
        $dataCard = array(
            'personID' => $this->input->post('personIDc'),
            'cardNumber' => $this->input->post('cardNumber'),
            'name' => $this->input->post('name'),
            'deptID' => $this->input->post('deptID'),
            'deptName' => $this->input->post('deptName'),
        );
        $dataFinger = array(
            'personID' => $this->input->post('personIDfp'),
            'cardNumber' => $this->input->post('cardNo'),
            'name' => $this->input->post('name'),
            'deptID' => $this->input->post('deptID'),
            'deptName' => $this->input->post('deptName'),
        );
        $dataFinger = $this->getDataisSame($dataFinger);
        $dataCard = $this->getDataisSame($dataCard);
        // $this->print_r($dataCard);

        // สำหรับ HQMS_IPS
        $this->updateOrInsertPerson($dataCard, 'HQMS_IPS');
        $this->updateOrInsertPerson($dataFinger, 'HQMS_IPS');

        // สำหรับ EFINS
        $this->updateOrInsertPerson($dataCard, 'EFINS');
        $this->updateOrInsertPerson($dataFinger, 'EFINS');


        $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'success')));
    }

    private function updateOrInsertPerson($data, $database)
    {
        $this->db = $this->load->database($database, true);
        $this->db->select('personID');
        $this->db->where('personID', $data['personID']);
        $query = $this->db->get('Person');
        // $this->print_r($data);

        if ($query->num_rows() > 0) {
            // อัพเดทข้อมูล
            if ($database === 'EFINS') {
                $data['shiftMent'] = ($this->input->post('shift') == 'no') ? NULL : $this->input->post('shift');
            }
            // $this->print_r($this->db);
            $this->db->where('personID', $data['personID']);
            unset($data['personID']);
            if (!$this->db->update('Person', $data)) {
                $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'error')));
            }
            // ลบ personID ใน $data ออก
            // die($this->db->last_query());
        } else {
            // เพิ่มข้อมูลใหม่
            if ($database === 'EFINS') {
                $data['shiftMent'] = ($this->input->post('shift') == 'no') ? NULL : $this->input->post('shift');
            }
            if (!$this->db->insert('Person', $data)) {
                $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'error')));
            }
        }
    }

    public function getDataisSame($val)
    {
        $this->db = $this->load->database('HQMS_IPS', true);
        $this->db->select('*');
        $this->db->limit(1);
        $this->db->where_not_in('name', ['Adisak S.']);
        $result = $this->db->get('Person')->result();
        // $this->print_r($val);
        $data = [];
        foreach ($result as $key => $value) {
            $data = array(
                'personID' => $val['personID'],
                'cardNumber' => $val['cardNumber'],
                'name' => $val['name'],
                'deptID' => $val['deptID'],
                'deptName' => $val['deptName'],
                'serialNumber' => $value->serialNumber,
                'englishName' => $value->englishName,
                'sex' => $value->sex,
                'birthday' => $value->birthday,
                'identityType' => $value->identityType,
                'identityID' => $value->identityID,
                'inID' => $value->inID,
                'inName' => $value->inName,
                'jobLevelID' => $value->jobLevelID,
                'jobPositionID' => $value->jobPositionID,
                'category' => $value->category,
                'educational' => $value->educational,
                'nation' => $value->nation,
                'place' => $value->place,
                'people' => $value->people,
                'specialty' => $value->specialty,
                'inaugurationDate' => $value->inaugurationDate,
                'leaveJobDate' => $value->leaveJobDate,
                'enableDate' => $value->enableDate,
                'disableDate' => $value->disableDate,
                'HealthStatus' => $value->HealthStatus,
                'interest' => $value->interest,
                'introducer' => $value->introducer,
                'salaryCategory' => $value->salaryCategory,
                'email' => $value->email,
                'address' => $value->address,
                'phone' => $value->phone,
                'zip' => $value->zip,
                'registerAddress' => $value->registerAddress,
                'registerPhone' => $value->registerPhone,
                'registerZip' => $value->registerZip,
                'school' => $value->school,
                'department' => $value->department,
                'urgentContact' => $value->urgentContact,
                'urgentPhone' => $value->urgentPhone,
                'marriage' => $value->marriage,
                'spouse' => $value->spouse,
                'spousePhone' => $value->spousePhone,
                'userID' => $value->userID,
                'userLevel' => $value->userLevel,
                'password' => $value->password,
                'superPassword' => $value->superPassword,
                'modifyTime' => $value->modifyTime,
                'operator' => $value->operator,
                'reserve1' => $value->reserve1,
                'reserve2' => $value->reserve2,
                'reserve3' => $value->reserve3,
                'reserve4' => $value->reserve4,
                'reserveChar1' => $value->reserveChar1,
                'reserveChar2' => $value->reserveChar2,
                'reserveChar3' => $value->reserveChar3,
                'reserveChar4' => $value->reserveChar4,
                'reserveChar5' => $value->reserveChar5,
                'reserveChar6' => $value->reserveChar6,
                'reserveChar7' => $value->reserveChar7,
                'reserveChar8' => $value->reserveChar8,
                'reserveInt1' => $value->reserveInt1,
                'reserveInt2' => $value->reserveInt2,
                'reserveInt3' => $value->reserveInt3,
                'reserveInt4' => $value->reserveInt4,
                'reserveInt5' => $value->reserveInt5,
                'reserveInt6' => $value->reserveInt6,
                'reserveInt7' => $value->reserveInt7,
                'reserveInt8' => $value->reserveInt8,
                'pinyin' => $value->pinyin,
                'cardType' => $value->cardType,
                'cardTypeDesc' => $value->cardTypeDesc,
                'subSystem' => $value->subSystem,
                'useCategory' => $value->useCategory,
                'useStatus' => $value->useStatus,
                'note' => $value->note,
                'groupID' => $value->groupID,
                'timeGroup' => $value->timeGroup,
                'status' => $value->status,
                'cardCategory' => $value->cardCategory,
                'cardStatus' => $value->cardStatus,
                'eatStatus' => $value->eatStatus,
                'freeNumber' => $value->freeNumber,
                'ATT_Free' => $value->ATT_Free,
                'cardNumberSP1' => $value->cardNumberSP1,
                'cardNumberSP2' => $value->cardNumberSP2,
                'cardNumberSP3' => $value->cardNumberSP3,
                'cardNumberSP4' => $value->cardNumberSP4,
                'cardNumberSP5' => $value->cardNumberSP5,
                'cardNumberSP6' => $value->cardNumberSP6,
                'FaceUserID' => $value->FaceUserID,
                'CREATEDATE' => $value->CREATEDATE,
            );
        }
        return $data;
    }

    public function updateAccess()
    {
        // $this->print_r($this->input->post());

        $this->db = $this->load->database('HQMS_IPS', TRUE);
        $this->db->select('PubDoor.doorID, PubDoor.doorName, PubDoor.contolL1, PubWeekTime.weekTimeID');
        $this->db->from('PubDoor');
        $this->db->join('PubWeekTime', 'PubDoor.contolL1 = PubWeekTime.contolL1');
        $result = $this->db->get()->result();
        $data = [];
        if ($this->input->post('state') == 0) {
            $this->db->where('personID', $this->input->post('personId'));
            $this->db->where('doorID', $this->input->post('doorId'));
            if ($this->db->update('PubDoorAuth', array('reserve1' => 2, 'modifyTime' => date('Y-m-d H:i:s')))) {
                $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'success')));
            } else {
                $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'error')));
            }
        } else {
            $data = array(
                'personID' => $this->input->post('personId'),
                'doorID' => $this->input->post('doorId'),
                'weekTimeID' => $this->input->post('weekTimeID'),
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
                'DownLoad_Fg' => $this->input->post('dnfp'),
                'contolL1_N' => NULL,
                'Master_Card' => 0,
            );
            // $this->print_r($data);
            $this->db->select('personID, doorID');
            $this->db->from('PubDoorAuth');
            $this->db->where('personID', $this->input->post('personId'));
            $this->db->where('doorID', $this->input->post('doorId'));
            $query = $this->db->get();
            if ($query->num_rows() > 0) {
                $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'error')));
            } else {
                if ($this->db->insert('PubDoorAuth', $data)) {
                    $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'success')));
                } else {
                    $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'error')));
                }
            }
        }
    }

    public function getAccess()
    {
        ini_set('max_execution_time', 0);
        $this->db = $this->load->database('HQMS_IPS', TRUE);
        $this->db->select('PubDoor.doorID, PubDoor.doorName, PubDoor.contolL1, PubWeekTime.weekTimeID');
        $this->db->from('PubDoor');
        $this->db->join('PubWeekTime', 'PubDoor.contolL1 = PubWeekTime.contolL1');
        // $this->db->order_by('PubDoor.doorName', 'ASC');
        // die($this->db->get_compiled_select());
        $result = $this->db->get()->result();
        $data = [];
        $AllaccessCard = $this->CheckAccess($this->input->post('cardNumber') . '-1');
        $AllaccessFinger = $this->CheckAccess($this->input->post('cardNumber') . '-2');
        // $this->print_r($AllaccessCard);
        foreach ($result as $key => $value) {
            $data['data'][$key]['doorID'] = $value->doorID;
            $data['data'][$key]['contolL1'] = $value->contolL1;
            $data['data'][$key]['doorName'] = $value->doorName;
            $data['data'][$key]['weekTimeID'] = $value->weekTimeID;
            $data['data'][$key]['personID'] = $this->input->post('cardNumber');
            $data['data'][$key]['contolL1'] = $value->contolL1;
            $data['data'][$key]['accessCard'] = (in_array($value->doorID, array_column($AllaccessCard, 'doorID'))) ? 1 : 0;
            $data['data'][$key]['accessFinger'] = (in_array($value->doorID, array_column($AllaccessFinger, 'doorID'))) ? 1 : 0;
        }
        $this->output->set_content_type('application/json')->set_output(json_encode($data));
    }

    public function CheckAccess($personId)
    {
        // $this->print_r($this->input->post());
        $this->db = $this->load->database('HQMS_IPS', TRUE);
        $this->db->select('personID, doorID');
        $this->db->from('PubDoorAuth');
        $this->db->where('personID', $personId);
        // die($this->db->get_compiled_select());
        $query = $this->db->get();
        return $query->result();
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

    public function deleteDataHQMS()
    {
        $this->db =  $this->load->database('HQMS_IPS', true);
        $this->db->where('personID', $this->input->post('cardNumber') . '-1');
        // or
        $this->db->or_where('personID', $this->input->post('cardNumber') . '-2');
        if ($this->db->delete('Person')) {
            $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'success')));
        } else {
            $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'error')));
        }
    }

    public function updateStatus()
    {
        $cardNumber = $this->input->post('cardNumber');
        $isActive = $this->input->post('isActive');

        if (empty($cardNumber)) {
            $this->output->set_content_type('application/json')->set_output(json_encode([
                'status' => 'error',
                'message' => 'Card number is required'
            ]));
            return;
        }

        $efinsDb = $this->load->database('EFINS', TRUE);
        $hqmsDb = $this->load->database('HQMS_IPS', TRUE);

        if ($isActive == 1) {
            $personData = [];
            for ($i = 1; $i <= 2; $i++) {
                $efinsDb->select('*');
                $efinsDb->where('personID', $cardNumber . '-' . $i);
                $result = $efinsDb->get('Person')->row_array();
                if ($result) {
                    // ถ้า column ที่ชื่อ rowAutoID  isActive, shiftMent ให้ลบออก
                    if (array_key_exists('rowAutoID', $result)) {
                        unset($result['rowAutoID']);
                    }
                    if (array_key_exists('isActive', $result)) {
                        unset($result['isActive']);
                    }
                    if (array_key_exists('shiftMent', $result)) {
                        unset($result['shiftMent']);
                    }
                    $personData[] = $result;
                }
            }

            if (empty($personData)) {
                $this->output->set_content_type('application/json')->set_output(json_encode([
                    'status' => 'error',
                    'message' => 'Person data not found in EFINS'
                ]));
                return;
            }

            // Delete existing persons in HQMS_IPS
            $hqmsDb->where('personID', $cardNumber . '-1');
            $hqmsDb->or_where('personID', $cardNumber . '-2');
            $hqmsDb->delete('Person');



            // Insert new data
            foreach ($personData as $data) {
                $hqmsDb->insert('Person', $data);
                // die($hqmsDb->last_query());
            }
        } else {
            // Delete persons if not active
            $hqmsDb->where('personID', $cardNumber . '-1');
            $hqmsDb->or_where('personID', $cardNumber . '-2');
            $hqmsDb->delete('Person');
        }

        // Update persons
        if (!empty($personData)) {
            $hqmsDb->where('personID', $cardNumber . '-1');
            $hqmsDb->or_where('personID', $cardNumber . '-2');
            $hqmsDb->update('Person', $personData[0]);
        }

        $this->output->set_content_type('application/json')->set_output(json_encode([
            'status' => 'success',
            'message' => $hqmsDb->last_query()
        ]));
    }
}
