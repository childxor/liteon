<?php

defined('BASEPATH') or exit('No direct script access allowed');

class User extends CI_Controller
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
        $this->data['module'] = 'main/sys/user';

        $this->load->view("main/layout/index", $this->data);
    }

    /* Get Data to List Table */

    public function ajax_list()
    {
        header('Access-Control-Allow-Origin: *');
        ini_set('max_execution_time', 300);
        $is_active = $_GET['is_active'];
        $department_id = $_GET['department_id'];
        $position_id = $_GET['position_id'];
        $gender_id = $_GET['gender_id'];
        $sql = "SELECT DISTINCT "
            . "sys_user.id, sys_user.department_id, sys_user.sub_department_id, sys_user.emp_code, sys_user.username, sys_user.gender_id, sys_user.prefix_name, sys_user.first_name, "
            . "sys_user.last_name, sys_user.tel, sys_user.email, sys_user.position_id, sys_user.is_active, sys_user.default_module_id, sys_user.cng_per_page, sys_user.cng_font_size, "
            . "sys_user.cng_table_font_size, sys_user.cng_lang, sys_user.cng_alert_time, sys_user.created_username, sys_user.created_at, sys_user.updated_username, sys_user.updated_at,"
            . "sys_user.record_status, mas_position.name AS position_name, mas_department.name AS department_name, mas_department.description AS department_description, mas_department.color, "
            . "mas_sub_department.name AS sub_department_name, mas_department_check_pr.record_status AS step "
            . "FROM sys_user INNER JOIN "
            . "mas_department ON sys_user.department_id = mas_department.id INNER JOIN "
            . "mas_position ON sys_user.position_id = mas_position.id LEFT OUTER JOIN "
            . "mas_department_check_pr ON sys_user.id = mas_department_check_pr.user_id AND mas_department_check_pr.record_status = 'N' LEFT OUTER JOIN "
            . "mas_sub_department ON sys_user.sub_department_id = mas_sub_department.id "
            . "WHERE (sys_user.record_status = 'N') ";
        $sql .= (!empty($is_active) ? "AND sys_user.is_active = '" . ($is_active == 'N' ? 0 : 1) . "' " : "");
        $sql .= (!empty($department_id) ? "AND sys_user.department_id = '" . $department_id . "' " : "");
        $sql .= (!empty($position_id) ? "AND sys_user.position_id = '" . $position_id . "' " : "");
        $sql .= (!empty($gender_id) ? "AND sys_user.gender_id = '" . $gender_id . "' " : "");
        $sql .= "ORDER BY sys_user.is_active DESC ";
        $data['data'] = $this->db->query($sql)->result();

        /* Init $items NULL */
        $items['data'] = array();
        foreach ($data['data'] as $key => $item) {
            $items['data'][$key]['id'] = $this->efs_lib->encrypt_segment($item->id);
            $items['data'][$key]['user_id'] = $item->id;
            $items['data'][$key]['step'] = $item->step;
            $items['data'][$key]['prefix_name'] = $item->prefix_name;
            $items['data'][$key]['first_name'] = $item->first_name;
            $items['data'][$key]['last_name'] = $item->last_name;
            $items['data'][$key]['emp_code'] = $item->emp_code;
            $items['data'][$key]['username'] = $item->username;
            $items['data'][$key]['position_name'] = $item->position_name;
            $items['data'][$key]['tel'] = $item->tel;
            $items['data'][$key]['position_id'] = $item->position_id;
            $items['data'][$key]['department_name'] = $item->department_name . (!empty($item->sub_department_name) ? ' (' . $item->sub_department_name . ')' : '');
            $items['data'][$key]['color'] = $item->color;
            $items['data'][$key]['role'] = $this->get_role_user($item->id);
            $items['data'][$key]['department_description'] = $item->department_description;
            $items['data'][$key]['is_active'] = $item->is_active;
            $items['data'][$key]['created_username'] = $item->created_username;
        }
        echo json_encode($items);
    }

    /* Get Data ti Item */

    function ajax_data()
    {
        header('Access-Control-Allow-Origin: *');
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $sql = "SELECT sys_user.id, sys_user.position_id, sys_user.prefix_name, sys_user.emp_code, sys_user.username, sys_user.first_name, sys_user.last_name, sys_user.tel, sys_user.email, sys_user.department_id , sys_user.sub_department_id "
            . ",mas_department.name AS department_name, mas_department.description AS department_description, mas_department.color "
            . "FROM sys_user INNER JOIN mas_department ON sys_user.department_id = mas_department.id "
            . "WHERE sys_user.id = '" . $id . "' "
            . "AND sys_user.record_status = 'N'";
        $data['item'] = $this->db->query($sql)->row();
        $data['item']->id = $this->efs_lib->encrypt_segment($data['item']->id);

        $data['role_ids'] = '';
        $sql = "SELECT sys_role.id "
            . "FROM sys_role_user INNER JOIN sys_role ON sys_role_user.role_id = sys_role.id "
            . "WHERE sys_role_user.user_id = '" . $id . "' "
            . "AND sys_role_user.record_status = 'N'";
        $result = $this->db->query($sql)->result();
        foreach ($result as $row) {
            $data['role_ids'] .= $row->id . ',';
        }
        $data['role_ids'] = substr($data['role_ids'], 0, -1);
        // $this->print_r($data);

        echo json_encode($data); 
    }

    public function print_r($data)
    {
        echo '<pre>';
        print_r($data);
        echo '</pre>';
    }

    /* Add Data */

    function add()
    {
        $role_ids = $this->input->post('role_id');
        $data = array(
            'department_id' => $this->input->post('department_id'),
            'sub_department_id' => $this->input->post('sub_department_id'),
            'emp_code' => $this->input->post('emp_code'),
            'username' => $this->input->post('username'),
            'gender_id' => ($this->input->post('prefix_name') == 'นาย' || strtoupper($this->input->post('prefix_name')) == 'MR.' ? '1' : '2'),
            'prefix_name' => $this->input->post('prefix_name'),
            'first_name' => $this->input->post('first_name'),
            'last_name' => $this->input->post('last_name'),
            'tel' => $this->input->post('tel'),
            'email' => $this->input->post('email'),
            'position_id' => $this->input->post('position_id'),
            'created_username' => $this->session->userdata("user_profile")->username,
            'created_at' => date('Y-m-d H:i:s'),
            'record_status' => 'N'
        );
        if (!$this->db->insert('sys_user', $data)) {

            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_add);
            /* Create Log Data  */
            $this->log_lib->write_log('error-user-add=> ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        } else {
            /* Get Last ID in Data */
            $user_id = $this->db->insert_id();
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('User => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
            /* SET Default password This User by username */
            $hash = $this->efs_lib->paswd_encrypt($this->input->post('username'));
            $data = array(
                'user_id' => $user_id,
                'default_passwd' => $this->input->post('username'),
                'hash' => $hash,
                'expired' => $this->efs_lib->increment_date(),
                'created_username' => $this->session->userdata("user_profile")->username,
                'created_at' => date('Y-m-d H:i:s'),
                'record_status' => 'N'
            );
            if (!$this->db->insert('sys_user_passwd', $data)) {
                $this->session->set_flashdata('type', 'error');
                $this->session->set_flashdata('msg', $this->lang->sys_msg_error_gen_password);
                /* Create Log Data  */
                $this->log_lib->write_log('error-user-gen-password=> ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
            }
            /* SET Roles for This User */
            foreach ($role_ids as $role_id) {
                $data = array(
                    'role_id' => $role_id,
                    'user_id' => $user_id,
                    'created_username' => $this->session->userdata("user_profile")->username,
                    'created_at' => date('Y-m-d H:i:s'),
                    'record_status' => 'N'
                );
                if (!$this->db->insert('sys_role_user', $data)) {
                    $this->session->set_flashdata('type', 'error');
                    $this->session->set_flashdata('msg', $this->lang->sys_msg_error_add_permission);
                    /* Create Log Data  */
                    $this->log_lib->write_log('error-user-add-permission=> ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
                }
            }
        }
        redirect($this->agent->referrer());
    }

    /* Edit Data */

    function edit()
    {
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $role_ids = $this->input->post('role_id');
        $data = array(
            'department_id' => $this->input->post('department_id'),
            'sub_department_id' => $this->input->post('sub_department_id'),
            'gender_id' => ($this->input->post('prefix_name') == 'นาย' || strtoupper($this->input->post('prefix_name')) == 'MR.' ? '1' : '2'),
            'prefix_name' => $this->input->post('prefix_name'),
            'first_name' => $this->input->post('first_name'),
            'last_name' => $this->input->post('last_name'),
            'username' => $this->input->post('username'),
            'tel' => $this->input->post('tel'),
            'email' => $this->input->post('email'),
            'position_id' => $this->input->post('position_id'),
            'updated_username' => $this->session->userdata("user_profile")->username,
            'updated_at' => date('Y-m-d H:i:s')
        );
        $this->db->where('id', $id);
        if (!$this->db->update('sys_user', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-user-update=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('User => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));

            /* SET Roles for This User */
            $data = array(
                'record_status' => 'D'
            );
            $this->db->where('user_id', $id);
            $this->db->update('sys_role_user', $data);

            foreach ($role_ids as $role_id) {
                $data = array(
                    'role_id' => $role_id,
                    'user_id' => $id,
                    'created_username' => $this->session->userdata("user_profile")->username,
                    'created_at' => date('Y-m-d H:i:s'),
                    'record_status' => 'N'
                );
                if (!$this->db->insert('sys_role_user', $data)) {
                    $this->session->set_flashdata('type', 'error');
                    $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
                    /* Create Log Data  */
                    $this->log_lib->write_log('error-user-role-update=> ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
                }
            }
        }
        redirect($this->agent->referrer());
    }

    /* Delete Data */

    function delete()
    {
        $id = $this->efs_lib->decrypt_segment($this->uri->segment(4));
        $data = array(
            'record_status' => 'D',
            'updated_username' => $this->session->userdata("user_profile")->username,
            'updated_at' => date('Y-m-d H:i:s'),
        );
        $this->db->where('id', $id);
        if (!$this->db->update('sys_user', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_delete);
            /* Create Log Data  */
            $this->log_lib->write_log('error-user-delete=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_delete_success);
            $this->log_lib->write_log('User => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
            $data = array(
                'record_status' => 'D',
                'updated_username' => $this->session->userdata("user_profile")->username,
                'updated_at' => date('Y-m-d H:i:s'),
            );
            /* DELETE user from Table Ref. */
            $this->db->where('user_id', $id);
            $this->db->update('sys_role_user', $data);
            $this->log_lib->write_log('User => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
            $this->db->where('user_id', $id);
            $this->db->update('sys_user_passwd', $data);
            $this->log_lib->write_log('User => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Change Active Data */

    function change_active()
    {
        $status = explode('.', $this->uri->segment(4));
        $data['is_active'] = ($status[1] == 1 ? 0 : 1);
        $id = $this->efs_lib->decrypt_segment($status[0]);

        $this->db->where('id', $id);
        if (!$this->db->update('sys_user', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-user-active=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('User => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Reset Password User */

    public function reset_passwd()
    {
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $sql = "SELECT default_passwd FROM sys_user_passwd WHERE record_status='N' AND user_id ='" . $id . "'";
        $row = $this->db->query($sql)->row();
        $passwd = $row->default_passwd;
        $hash = $this->efs_lib->paswd_encrypt($passwd);
        $expired = $this->efs_lib->increment_date();
        $data = array(
            'hash' => $hash,
            'expired' => $expired,
            'updated_username' => $this->session->userdata("user_profile")->username,
            'updated_at' => date('Y-m-d H:i:s')
        );
        $this->db->where('record_status', 'N');
        $this->db->where('user_id', $id);
        if (!$this->db->update('sys_user_passwd', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-user-reset-password=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $data['expired_passwd'] = $expired;
            $this->session->set_userdata($data);
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_main_success_change_password);
            $this->log_lib->write_log('Main Change Password Profile => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* <!-- ============================================================== -->
      <!-- Function -->
      <!-- ============================================================== --> */

    function ajax_exist()
    {
        /* Check Exist username */
        header('Access-Control-Allow-Origin: *');
        ini_set('max_execution_time', 300);
        $feild = $this->uri->segment(4);
        $value = $this->input->get_post("value");
        $sql = "SELECT id FROM sys_user WHERE record_status='N' AND " . $feild . " = '" . trim($value) . "'";
        $nrows = $this->db->query($sql)->num_rows();
        if ($nrows > 0) {
            echo json_encode(
                array(
                    "value" => $value,
                    "valid" => 0,
                    "message" => "<span class='text-danger'>" . $value . "</span> " . $this->lang->sys_msg_already_data
                )
            );
        } else {
            echo json_encode(
                array(
                    "value" => $value,
                    "valid" => 1,
                    "message" => ""
                )
            );
        }
    }

    function ajax_sub_department_list()
    {
        /* Get Data to select option */
        $option = '<option value=""></option>';
        $sql = "SELECT id, name "
            . "FROM mas_sub_department "
            . "WHERE record_status ='N' AND department_id ='" . $this->input->post('id') . "' "
            . "ORDER BY sort ASC ";
        foreach ($this->db->query($sql)->result() as $row) {
            $option .= '<option value="' . $row->id . '" ' . ($this->input->post('sub') == $row->id ? 'selected' : '') . '>' . $row->name . '</option>';
        }
        echo json_encode($option);
    }

    function get_role_user($id = null)
    {
        /* Get Role for User to table */
        header('Access-Control-Allow-Origin: *');
        $role = '';
        $sql = "SELECT sys_role.name "
            . "FROM sys_role_user INNER JOIN sys_role ON sys_role_user.role_id = sys_role.id "
            . "WHERE sys_role_user.user_id = '" . $id . "' "
            . "AND sys_role_user.record_status = 'N'";
        $result = $this->db->query($sql)->result();
        foreach ($result as $row) {
            $role .= '<span class="badge badge-info mr-1 mb-1">' . $row->name . '</span>';
        }
        return $role;
    }


    function table_department_check_pr()
    {
        $user_id = $this->efs_lib->decrypt_segment($this->input->post('user_id'));
        $data['table'] = "";
        $sql = " SELECT DISTINCT ";
        $sql .=  (!empty($user_id) ? "sys_user.prefix_name, sys_user.first_name, sys_user.last_name, mas_department.id, mas_department.name " : "mas_department.id, mas_department.name ");
        $sql .= "FROM mas_department INNER JOIN
                      mas_department_check_pr ON mas_department.id = mas_department_check_pr.department_id INNER JOIN
                      sys_user ON mas_department_check_pr.user_id = sys_user.id
            WHERE  mas_department_check_pr.is_active ='1' AND mas_department_check_pr.record_status ='N' ";

        $sql .= (!empty($user_id) ? "AND mas_department_check_pr.user_id ='" . $user_id . "' " : "");

        $result = $this->db->query($sql)->result();
        foreach ($result as $row) {
            $head =  '<h4>หน่วยงาน ' . $row->name . '</h4>';
            $table_mas_department_check_pr = '<table class="table table-bordered table-striped table-hover" style="font-size:12px !important; width:100%;">';
            $table_mas_department_check_pr .= '<tr>' .
                '<th class="text-center">ขั้นตอน</th>' .
                '<th class="text-center">ลำดับ</th>' .
                '<th class="text-center">จำเป็น</th>' .
                '<th>ผู้ตรวจสอบ</th>' .
                '</tr>';
            $sql = "SELECT mas_department_check_pr.department_id, mas_department_check_pr.step, mas_department_check_pr.is_must, mas_department_check_pr.user_id, 
        mas_department_check_pr.sort, sys_user.prefix_name, sys_user.first_name, sys_user.last_name, mas_position.name AS position_name
        FROM mas_department_check_pr INNER JOIN
            sys_user ON mas_department_check_pr.user_id = sys_user.id INNER JOIN
            mas_position ON sys_user.position_id = mas_position.id
        WHERE (mas_department_check_pr.is_active ='1') AND (mas_department_check_pr.record_status = 'N') 
        AND (mas_department_check_pr.department_id = '" . $row->id . "')
        ORDER BY mas_department_check_pr.step, mas_department_check_pr.sort";
            // echo $sql;die();
            $nrows = $this->db->query($sql)->num_rows();

            if ($nrows > 0) {
                $result = $this->db->query($sql)->result();
                foreach ($result as $item) {
                    $table_mas_department_check_pr .= '<tr class="" style="background:' . ($item->step == 1 ? '#ffcf77' : ($item->step == 2 ? '#9fa7ff' : '#e6c5ff')) . '">' .
                        '<td class="text-center">' . ($item->step == 1 ? 'Check' : ($item->step == 2 ? 'Re-Check' : 'Verify')) . '</td>' .
                        '<td class="text-center">' . $item->sort . '</td>' .
                        '<td class="text-center">' . ($item->is_must == 1 ? '<i class="fas fa-check text-success"></i>' : '<i class="fas fa-times text-warning"></i>')  . '</td>' .
                        '<td>' . $item->prefix_name . $item->first_name . ' ' . $item->last_name . ' (' . $item->position_name . ')</td>' .
                        '</tr>';
                }
            } else {
                $table_mas_department_check_pr .= '<tr><td class="text-center" col-span="6">-</td></tr>';
            }
            $table_mas_department_check_pr .= '</table></div>';
            $data['table'] .= $head . $table_mas_department_check_pr;
        }
        $data['title'] = '';
        echo (!empty($user_id) ? json_encode($data) : $data['table']);
    }

    public function getUserfromHQMS()
    {
        ini_set('max_execution_time', 0);
        $this->db =  $this->load->database('HQMS_IPS', true);
        $this->db->select('*');
        $this->db->from('Person');
        $this->db->limit(100);
        $result = $this->db->get()->result();
        $this->output->set_content_type('application/json')->set_output(json_encode($result));
    }
}
