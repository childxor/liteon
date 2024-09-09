<?php

defined('BASEPATH') or exit('No direct script access allowed');

class Authen extends CI_Controller
{

    function __construct()
    {
        /* Call the Model constructor */
        parent::__construct();
        $this->data['var'] = $this->efs_lib->get_var_sys();
        $this->load->library('cart');
        $this->hqms_db = $this->load->database('HQMS_IPS', TRUE);
    }

    public function index()
    {
        if (!empty($this->session->userdata("user_profile")->id)) {
            $this->print_r(base_url());
            redirect(base_url());
            exit();
        } else {
            /* System Language login */
            $this->data['lang'] = $this->efs_lib->language_login();
            $this->data['department'] = $this->db->query("SELECT * FROM mas_department WHERE record_status = 'N' ORDER BY name")->result();
            $this->data['position'] = $this->db->query("SELECT * FROM mas_position WHERE record_status = 'N' ORDER BY name")->result();

            $this->data['lang_choose'] = $this->session->userdata('lang');
            $this->load->view("login", $this->data);
        }
    }

    public function ch_lg_http()
    {
        $this->session->set_userdata('lang', $_POST['lang']);
        // $this->print_r($this->session->userdata('lang'));
    }

    public function login()
    {
        $lang = $this->efs_lib->language_login();
        $this->session->unset_userdata(['user_profile', 'user_permission', 'expired_passwd', 'token']);
        $this->db = $this->load->database('EFINS', TRUE);

        $username = $this->input->post('username', TRUE);
        $passwd = $this->input->post('passwd', TRUE);

        if (empty($username) || empty($passwd)) {
            $this->session->set_flashdata('type', 'warning');
            $this->session->set_flashdata('msg', $lang->sys_login_empty_fields);
            redirect(base_url("authen"));
            return;
        }

        $this->db->select('su.id, su.position_id, mp.name AS position_name, su.emp_code, su.prefix_name, su.username, 
                           su.first_name, su.last_name, su.tel, su.email, su.is_active, su.cng_per_page, 
                           su.cng_font_size, su.cng_table_font_size, su.cng_lang, su.cng_alert_time, su.default_module_id,
                           md.id AS department_id, md.name AS department_name, msd.name AS sub_department_name, 
                           md.description AS department_description, md.color')
            ->from('sys_user su')
            ->join('mas_department md', 'su.department_id = md.id', 'inner')
            ->join('mas_sub_department msd', 'su.sub_department_id = msd.id', 'left')
            ->join('mas_position mp', 'su.position_id = mp.id', 'inner')
            ->where('su.username', $username)
            ->where('su.record_status', 'N');

        $user_profile = $this->db->get()->row();

        if (!$user_profile || $user_profile->is_active != 1) {
            $this->session->set_flashdata('type', 'warning');
            $this->session->set_flashdata('msg', $lang->sys_login_username . ' <u>' . $username . '</u> ' .
                ($user_profile ? $lang->sys_login_user_disabled : $lang->sys_login_without_username));
            redirect(base_url("authen"));
            return;
        }

        $passwd_query = $this->db->where('user_id', $user_profile->id)
            ->where('record_status', 'N')
            ->get('sys_user_passwd');

        $role_query = $this->db->where('user_id', $user_profile->id)
            ->where('record_status', 'N')
            ->get('sys_role_user');

        if ($passwd_query->num_rows() > 0 && $role_query->num_rows() > 0) {
            $item_passwd = $passwd_query->row();
            if ($this->efs_lib->paswd_decrypt($passwd, $item_passwd->hash)) {
                $user_profile->name_sys = 'efs';
                $user_profile->role = $this->db->select('role_id')->where('user_id', $user_profile->id)->get('sys_role_user')->result();

                $default_module = $user_profile->default_module_id
                    ? $this->db->select('module')->where('id', $user_profile->default_module_id)->get('sys_module')->row()->module
                    : $this->db->select('DISTINCT sm.module')
                    ->from('sys_role_user sru')
                    ->join('sys_role sr', 'sru.role_id = sr.id')
                    ->join('sys_module sm', 'sr.default_module_id = sm.id')
                    ->where('sru.record_status', 'N')
                    ->where('sru.user_id', $user_profile->id)
                    ->get()->row()->module;

                $this->session->set_userdata([
                    'expired_passwd' => $item_passwd->expired,
                    'default_module' => $default_module,
                    'user_profile' => $user_profile,
                    'token' => time()
                ]);

                $this->log_lib->write_log('Authen => ' . $lang->sys_login_submit);
                $this->Checkday($user_profile->id);
                redirect(base_url());
                return;
            }
        }

        $this->session->set_flashdata('type', 'warning');
        $this->session->set_flashdata('msg', $lang->sys_login_username . ' <u>' . $username . '</u> ' . $lang->sys_login_incorrect);
        $this->log_lib->write_log('error-authen-incorrect=> ' . $this->session->flashdata('msg'));
        redirect(base_url("authen"));
    }

    private function get_default_module($user_profile)
    {
        $sql = $user_profile->default_module_id
            ? "SELECT module AS default_module FROM sys_module WHERE id = ?"
            : "SELECT DISTINCT sm.module AS default_module 
               FROM sys_role_user sru 
               INNER JOIN sys_role sr ON sru.role_id = sr.id 
               INNER JOIN sys_module sm ON sr.default_module_id = sm.id 
               WHERE sru.record_status = 'N' AND sru.user_id = ?";

        $query = $this->db->query($sql, array($user_profile->default_module_id ?: $user_profile->id));
        return $query->row()->default_module;
    }

    private function prepare_user_profile($user_profile)
    {
        $user_profile->name_sys = 'efs';
        $user_profile->role = $this->get_role($user_profile->id);
        return $user_profile;
    }

    private function handle_login_error($username, $lang, $error_type)
    {
        $messages = [
            'incorrect' => $lang->sys_login_incorrect,
            'no_password' => $lang->sys_login_without_password,
            'no_group' => $lang->sys_login_without_group,
            'disabled' => $lang->sys_login_user_disabled,
            'no_user' => $lang->sys_login_without_username
        ];

        $this->session->set_flashdata('type', 'warning');
        $this->session->set_flashdata('msg', $lang->sys_login_username . ' <u>' . $username . '</u> ' . $messages[$error_type]);
        $this->log_lib->write_log('error-authen-' . $error_type . '=> ' . $this->session->flashdata('msg'));
    }

    public function user()
    {
        $username = trim($this->input->post('username'));
        $sql = "SELECT sys_user.id, sys_user.position_id, mas_position.name AS position_name, sys_user.prefix_name, sys_user.username, sys_user.first_name, sys_user.last_name, sys_user.tel, sys_user.email, sys_user.is_active "
            . ", sys_user.cng_per_page, sys_user.cng_font_size, sys_user.cng_table_font_size, sys_user.cng_lang, sys_user.cng_alert_time, sys_user.default_module_id "
            . ", mas_department.id AS department_id, mas_department.name AS department_name, mas_sub_department.name AS sub_department_name, mas_department.description AS department_description, mas_department.color "
            . "FROM sys_user INNER JOIN mas_department ON sys_user.department_id = mas_department.id "
            . "LEFT JOIN mas_sub_department ON sys_user.sub_department_id = mas_sub_department.id "
            . "INNER JOIN mas_position ON sys_user.position_id = mas_position.id "
            . "WHERE sys_user.username = '" . $username . "' "
            . "AND sys_user.record_status = 'N'";
        $query = $this->db->query($sql)->row();
        $sess = array(
            'firstname' => $query->first_name,
            'lastname' => $query->last_name
        );
        $this->session->set($sess);
        // $data['user'] = 'sto/requisition_staff_list';
        // $this->load->view('main/sto/requisition_staff_list', $data);
        $this->load->view('main/sto/requisition_staff_list', $sess);

        // $data['title'] = "My Real Title";
        // $data['heading'] = "My Real Heading";

        // $this->load->view('main/sto/requisition_staff_list', $data);
        // return view('layout', $data);
    }

    public function print_r($data)
    {
        echo '<pre>';
        print_r($data);
        echo '</pre>';
        die();
    }

    public function reportIssue()
    {
        // จัดการการอัพโหลดไฟล์
        $uploadedFilePath = null;
        if (!empty($_FILES['attachFile']['name'])) {
            $config['upload_path'] = FCPATH . 'uploads/reportIssue/';
            $config['allowed_types'] = 'gif|jpg|png|pdf|doc|docx|xls|xlsx|ppt|pptx';
            $config['max_size'] = 5000; // 5MB
            $config['file_name'] = 'reportIssue_' . date('YmdHis');

            if ($this->load->library('upload', $config) === false) {
                $this->output->set_content_type('application/json')->set_output(json_encode(['status' => 'error', 'message' => 'ไม่สามารถอัพโหลดไฟล์ได้']));
                return;
            }


            if (!$this->upload->do_upload('attachFile')) {
                $error = $this->upload->display_errors();
                $this->output->set_content_type('application/json')->set_output(json_encode(['status' => 'error', 'message' => $error]));
                return;
            } else {
                $uploadData = $this->upload->data();
                $uploadedFilePath = $config['upload_path'] . $uploadData['file_name'];
            }
        }

        // เตรียมข้อมูลสำหรับ stored procedure
        $queueNumber = $this->generateQueueNumber();
        $priority = $this->getPriorityFromUrgency($this->input->post('urgencyLevel'));

        $params = array(
            'queue_number' => $queueNumber,
            'priority' => $priority,
            'issueType' => $this->input->post('issueType'),
            'urgencyLevel' => $this->input->post('urgencyLevel'),
            'affectedSystem' => $this->input->post('affectedSystem'),
            'issueDescription' => $this->input->post('issueDescription'),
            'expectedResult' => $this->input->post('expectedResult'),
            'contactName' => $this->input->post('contactName'),
            'contactEmail' => $this->input->post('contactEmail'),
            'contactPhone' => $this->input->post('contactPhone'),
            'attachFile' => $uploadedFilePath
        );

        // เรียกใช้ stored procedure
        $result = $this->db->query("EXEC sp_insert_report_issue ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?", $params);

        if ($result) {
            $insertedId = $this->db->insert_id();
            // ส่งอีเมลแจ้งเตือนทีมสนับสนุน (ถ้าต้องการ)
            // $this->sendEmailToSupportTeam($queueNumber);

            $this->output->set_content_type('application/json')->set_output(json_encode([
                'status' => 'success',
                'message' => 'รายงานปัญหาสำเร็จ',
                'queueNumber' => $queueNumber
            ]));
        } else {
            $this->output->set_content_type('application/json')->set_output(json_encode([
                'status' => 'error',
                'message' => 'เกิดข้อผิดพลาดในการรายงานปัญหา'
            ]));
        }
    }

    private function generateQueueNumber()
    {
        $prefix = date('Ymd');
        $result = $this->db->query("EXEC sp_get_next_queue_number ?", array($prefix))->row();
        return $result->next_queue_number;
    }

    private function getPriorityFromUrgency($urgencyLevel)
    {
        switch ($urgencyLevel) {
            case 'critical':
                return 1;
            case 'high':
                return 2;
            case 'medium':
                return 3;
            case 'low':
            default:
                return 4;
        }
    }

    public function get_role($id)
    {
        $this->db->select('sys_role_user.id, sys_role_user.role_id, sys_role_user.user_id, sys_role_user.created_username, sys_role_user.created_at, sys_role_user.updated_username, sys_role_user.updated_at,
        sys_role_user.record_status, sys_role.name');
        $this->db->from('sys_role_user');
        $this->db->join('sys_role', 'sys_role_user.role_id = sys_role.id');
        $this->db->where('sys_role_user.user_id', $id);
        $this->db->where('sys_role_user.record_status', 'N');
        $query = $this->db->get()->result();
        return $query;
    }

    public function logout()
    {
        $this->cart->destroy();
        /* System Language login */
        $lang = $this->efs_lib->language_login();
        $this->log_lib->write_log('Authen => ' . $lang->sys_login_logout);
        $this->session->unset_userdata(array('user_profile', 'user_permission', 'expired_passwd', 'token'));
        redirect(base_url());
    }

    public function Checkday($idUser)
    {
        //    $idUser = $this->session->userdata("user_profile")->id;
        $sql = "SELECT * FROM sys_role_user WHERE (user_id = '" . $idUser . "') AND (record_status = 'N') AND (role_id = '8')";
        if ($numrow = $this->db->query($sql)->num_rows() > 0) {
            $sql = "SELECT count(id) as cnt FROM sto_csm WHERE   (DAY(csm_date) = DAY({ fn NOW() })) AND (MONTH(csm_date) = MONTH({ fn NOW() })) AND (YEAR(csm_date) = YEAR({ fn NOW() })) AND (record_status = 'N')";
            $result = $this->db->query($sql)->row()->cnt;
            // echo $result; die();
            if ($result == "0") {
                $date = date('Y-m-d');
                $day = date('D', strtotime($date));
                if ($day == 'Mon' || $day == 'Wed' || $day == 'Fri') {
                    $this->random_material_check();
                }
            }
        }
    }

    public function random_material_check()
    {
        $date = date("Y-m-01");
        $last = date("Y-m-t");
        $sql = "SELECT   TOP (15) material_id FROM (SELECT DISTINCT sto_issue_detail.material_id
        FROM sto_issue INNER JOIN sto_issue_detail ON sto_issue.id = sto_issue_detail.issue_id INNER JOIN
        sto_material ON sto_issue_detail.material_id = sto_material.id
        WHERE (sto_material.is_active = '1') AND (sto_issue.issue_date BETWEEN '" . $date . "' AND '" . $last . "') AND (sto_issue_detail.material_id <> '0') AND (sto_material.record_status = 'N')) AS derivedtbl_1
        ORDER BY NEWID()";
        $num = $this->db->query($sql)->num_rows();

        if ($num < 5) {
            $date = date("Y-m-01", strtotime("-1 months"));
            $last = date("Y-m-t", strtotime("-1 months"));
            $sql = "SELECT   TOP (15) material_id FROM (SELECT DISTINCT sto_issue_detail.material_id
            FROM sto_issue INNER JOIN sto_issue_detail ON sto_issue.id = sto_issue_detail.issue_id INNER JOIN
            sto_material ON sto_issue_detail.material_id = sto_material.id
            WHERE (sto_material.is_active = '1') AND (sto_issue.issue_date BETWEEN '" . $date . "' AND '" . $last . "') AND (sto_issue_detail.material_id <> '0') AND (sto_material.record_status = 'N')) AS derivedtbl_1
            ORDER BY NEWID()";
            $data = $this->db->query($sql)->result();
        } else {
            $data = $this->db->query($sql)->result();
        }
        // echo $sql;
        $sql = "SELECT csm_no FROM sto_csm WHERE (csm_no LIKE 'CSM" . date('Ym') . "%')";
        $no = $this->db->query($sql)->num_rows(); /* Count $issue_no of ISSUE */
        $csm_no = "CSM" . date('Ym') . str_pad($no + 1, 3, '0', STR_PAD_LEFT);
        $insert = array(
            'csm_no' => $csm_no,
            'csm_by' => $this->session->userdata("user_profile")->id,
            'csm_date' => date("Y-m-d H:m:s"),
            'csm_status' => "CSM",
            'created_username' => $this->session->userdata("user_profile")->username,
            'created_at' => date('Y-m-d H:i:s'),
        );
        if (!$this->db->insert('sto_csm', $insert)) {
            // error
        } else {
            $csm_id = $this->db->insert_id();
            foreach ($data as $o) {
                $datain = array(
                    'csm_id' => $csm_id,
                    'material_id' => $o->material_id,
                    'qty_sys' => $this->check_qty_material($o->material_id),
                    'created_username' => $this->session->userdata("user_profile")->username,
                    'created_at' => date('Y-m-d H:i:s'),
                );
                if (!$this->db->insert('sto_csm_detail', $datain)) {
                    // error
                }
            }
        }
        // redirect(base_url("sto/material/check_stock"));
    }

    public function check_qty_material($material_id)
    {
        $sql = "SELECT vw_material.material_code AS material_type, vw_material.material_name AS name_th, SUM(vw_qty_material.qty) AS qty, vw_material_1.id AS material_id, avg(vw_qty_material.price_per_unit) as price_per_unit 
        FROM vw_qty_material INNER JOIN
        vw_material ON vw_qty_material.material_id = vw_material.id FULL OUTER JOIN
        vw_material AS vw_material_1 ON vw_material.material_code = vw_material_1.material_code
        WHERE material_id = '" . $material_id . "' 
        GROUP BY vw_material.material_code, vw_material.material_name, vw_material.id, vw_material_1.id
        ORDER BY qty desc";
        // die($sql);
        $total_qty = 0;
        foreach ($qty = $this->db->query($sql)->result() as $qtys) {
            $total_qty += $qtys->qty;
        }
        return ($total_qty);
    }

    public function register()
    {
        // $this->print_r($this->input->post());
        // เช็คว่ามี username นี้ในระบบหรือไม่
        $this->db->where('username', $this->input->post('username'));
        $query = $this->db->get('sys_user');
        if ($query->num_rows() > 0) {
            $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'error', 'message' => 'Username already exists')));
            return;
        }
        // เช็คว่ามี email นี้ในระบบหรือไม่
        $this->db->where('email', $this->input->post('email'));
        $query = $this->db->get('sys_user');
        if ($query->num_rows() > 0) {
            $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'error', 'message' => 'Email already exists')));
            return;
        }
        // เช็คว่ามี card นี้ในระบบหรือไม่
        $this->db->where('card_number', $this->input->post('card_number'));
        $query = $this->db->get('sys_user');
        if ($query->num_rows() > 0) {
            $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'error', 'message' => 'Card already exists')));
            return;
        }
        // เช็คว่ามี employee นี้ในระบบหรือไม่
        $this->db->where('emp_code', $this->input->post('emp_code'));
        $query = $this->db->get('sys_user');
        if ($query->num_rows() > 0) {
            $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'error', 'message' => 'Employee already exists')));
            return;
        }
        // ถ้าไม่มีให้ insert ข้อมูล

        $data = array(
            'username' => $this->input->post('username'),
            'prefix_name' => $this->input->post('prefix_name'),
            'first_name' => $this->input->post('first_name'),
            'last_name' => $this->input->post('last_name'),
            'emp_code' => $this->input->post('emp_code'),
            'card_number' => $this->input->post('card_number'),
            'department_id' => $this->input->post('department_id'),
            'email' => $this->input->post('email'),
            'created_at' => date('Y-m-d H:i:s'),
            'updated_at' => date('Y-m-d H:i:s'),
            'is_active' => 0,
            'record_status' => 'N'
        );
        if (!$this->db->insert('sys_user', $data)) {
            $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'error', 'message' => 'Register failed')));
            return;
        } else {
            $hash = $this->efs_lib->paswd_encrypt($this->input->post('emp_code'));
            $data2 = array(
                'user_id' => $this->db->insert_id(),
                'default_passwd' => $this->input->post('emp_code'),
                'hash' => $this->efs_lib->paswd_encrypt($this->input->post('emp_code')),
                'expired' => date('Y-m-d', strtotime('+90 days')),
                'created_at' => date('Y-m-d H:i:s'),
                'updated_at' => date('Y-m-d H:i:s'),
                'record_status' => 'N'
            );
            if (!$this->db->insert('sys_user_passwd', $data2)) {
                $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'error', 'message' => 'Register failed')));
                return;
            } else {
                $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'success', 'message' => 'Register success')));
                return;
            }
        }
    }

    public function checkField()
    {
        $field = $this->input->post('field');
        $value = $this->input->post('value');
        // SELECT DISTINCT cardNumber, name
        // FROM            Person
        // WHERE        (LEN(cardNumber) < 10)
        // die('test');
        // $asdf = $this->hqms_db->query("SELECT DISTINCT cardNumber, name FROM Person WHERE (cardNumber = '" . $value . "')")->result();
        // $this->print_r($asdf);


        switch ($field) {
            case 'username':
                $this->db->where('username', $value);
                $query = $this->db->get('sys_user');
                if ($query->num_rows() > 0) {
                    $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'error', 'message' => 'Username already exists')));
                    return;
                }
                break;

            case 'email':
                $this->db->where('email', $value);
                $query = $this->db->get('sys_user');
                if ($query->num_rows() > 0) {
                    $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'error', 'message' => 'Email already exists')));
                    return;
                }
                break;

            case 'card_number':
                $this->db->where('card_number', $value);
                $query = $this->db->get('sys_user');
                if ($query->num_rows() > 0) {
                    $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'error', 'message' => 'Card already exists')));
                    return;
                }
                break;

            case 'emp_code':
                $this->db->where('emp_code', $value);
                $query = $this->db->get('sys_user');
                if ($query->num_rows() > 0) {
                    $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'error', 'message' => 'Employee already exists')));
                    return;
                }
                break;

            default:
                $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'success', 'message' => 'Field is available')));
                return;
        }

        // ถ้าไม่มีข้อผิดพลาด
        $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'success', 'message' => 'Field is available')));
    }
}
