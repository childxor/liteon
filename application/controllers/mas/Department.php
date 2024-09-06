<?php

defined('BASEPATH') or exit('No direct script access allowed');

class Department extends CI_Controller
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
        $this->data['breadcrumb'] = $this->load->view('main/layout/breadcrumb', $breadcrumbs, true);
        $this->data['module'] = 'main/mas/department';

        $this->load->view("main/layout/index", $this->data);
    }

    /* Get Data to List Table */

    public function ajax_list()
    {
        header('Access-Control-Allow-Origin: *');
        ini_set('max_execution_time', 300);
        $sql = "SELECT id, color, sort, name, description, is_active, created_username "
            . "FROM mas_department "
            . "WHERE record_status='N' "
            . "ORDER BY sort ASC";
        $data['data'] = $this->db->query($sql)->result();
        /* Init $items NULL */
        $items['data'] = array();
        foreach ($data['data'] as $key => $item) {
            $items['data'][$key]['id'] = $this->efs_lib->encrypt_segment($item->id);
            $items['data'][$key]['color'] = $item->color;
            $items['data'][$key]['sort'] = $item->sort;
            $items['data'][$key]['name'] = $item->name;
            $items['data'][$key]['description'] = $item->description;
            $items['data'][$key]['is_active'] = $item->is_active;
            $items['data'][$key]['created_username'] = $item->created_username;
        }
        echo json_encode($items);
    }

    /* Get Data to Item */

    function ajax_data()
    {
        header('Access-Control-Allow-Origin: *');
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $sql = "SELECT * "
            . "FROM mas_department "
            . "WHERE record_status='N' AND id ='" . $id . "'";
        $data = $this->db->query($sql)->row();
        $data->id = $this->efs_lib->encrypt_segment($data->id);
        echo json_encode($data);
    }

    /* Sort Data List Table */

    function sort()
    {
        $data = array(
            'sort' => $this->input->post('newPos')
        );
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $this->db->where('id', $id);
        $this->db->update('mas_department', $data);
        $this->log_lib->write_log('Department => ' . 'จัดลำดับข้อมูลเสร็จสิ้น', json_encode($this->db->last_query()));
    }

    /* Add Data */

    function add()
    {
        $data = array(
            'sort' => $this->input->post('sort'),
            'color' => $this->input->post('color'),
            'name' => $this->input->post('name'),
            'description' => $this->input->post('description'),
            'created_username' => $this->session->userdata("user_profile")->username,
            'created_at' => date('Y-m-d H:i:s'),
            'record_status' => 'N'
        );
        if (!$this->db->insert('mas_department', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_add);
            /* Create Log Data  */
            $this->log_lib->write_log('error-department-add=> ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Department => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Edit Data */

    function edit()
    {
        $data = array(
            'color' => $this->input->post('color'),
            'name' => $this->input->post('name'),
            'description' => $this->input->post('description'),
            'updated_username' => $this->session->userdata("user_profile")->username,
            'updated_at' => date('Y-m-d H:i:s')
        );
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $this->db->where('id', $id);
        if (!$this->db->update('mas_department', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-department-update=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Department => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
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
        if (!$this->db->update('mas_department', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_delete);
            /* Create Log Data  */
            $this->log_lib->write_log('error-department-delete=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_delete_success);
            $this->log_lib->write_log('Department => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
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
        if (!$this->db->update('mas_department', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-department-active=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Department => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Sub Department Index */

    public function sub()
    {
        $department_id = $this->efs_lib->decrypt_segment($this->uri->segment(4));
        if (empty($department_id))
            redirect(base_url("error404")); /* IF Return Null Redirect to default URL */

        $this->data['topbar'] = 'main/layout/topbar';
        $this->data['sidebar'] = 'main/layout/sidebar';
        $this->data['footer'] = 'main/layout/footer';
        /* Get Current Module */
        $parent_module = $this->efs_lib->get_parent_module($this->uri->uri_string());

        /* Get Parent Module For Department */
        $sql = "SELECT * FROM mas_department WHERE record_status = 'N' ";
        $sql .= "AND id = '" . $department_id . "'";
        $department = $this->db->query($sql)->row();

        /* System Language */
        $this->data['lang_sys'] = $this->efs_lib->language_system();
        /* Module Language */
        $this->data['lang_module'] = $this->efs_lib->language_module(11);

        $breadcrumbs['breadcrumb'] = array(
            array(1 => array(
                "name" => '<i class="mdi mdi-home font-20" style="line-height: 20px;"></i>',
                "module" => base_url(),
                "class" => ''
            )),
            array(2 => array(
                "name" => $parent_module->{"name_" . $this->session->userdata("user_profile")->cng_lang},
                "module" => base_url($parent_module->module),
                "class" => ''
            )),
            array(3 => array(
                "name" => $department->name,
                "module" => '',
                "class" => 'active'
            ))
        );
        $this->data['breadcrumb'] = $this->load->view('main/layout/breadcrumb', $breadcrumbs, true);
        $this->data['module'] = 'main/mas/sub_department';

        $this->load->view("main/layout/index", $this->data);
    }

    /* Get Data to List Table */

    public function sub_ajax_list()
    {
        header('Access-Control-Allow-Origin: *');
        ini_set('max_execution_time', 300);
        $sql = "SELECT id, department_id, sort, name, description, is_active, created_username "
            . "FROM mas_sub_department "
            . "WHERE record_status='N' "
            . "AND department_id ='" . $this->efs_lib->decrypt_segment($this->uri->segment(4)) . "'";
        $sql .= "ORDER BY sort ASC";
        $data['data'] = $this->db->query($sql)->result();
        /* Init $items NULL */
        $items['data'] = array();
        foreach ($data['data'] as $key => $item) {
            $items['data'][$key]['id'] = $this->efs_lib->encrypt_segment($item->id);
            $items['data'][$key]['department_id'] = $item->department_id;
            $items['data'][$key]['sort'] = $item->sort;
            $items['data'][$key]['name'] = $item->name;
            $items['data'][$key]['description'] = $item->description;
            $items['data'][$key]['is_active'] = $item->is_active;
            $items['data'][$key]['created_username'] = $item->created_username;
        }
        echo json_encode($items);
    }

    /* Get Data to Item */

    function sub_ajax_data()
    {
        header('Access-Control-Allow-Origin: *');
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $sql = "SELECT * "
            . "FROM mas_sub_department "
            . "WHERE record_status='N' AND id ='" . $id . "'";
        $data = $this->db->query($sql)->row();
        $data->id = $this->efs_lib->encrypt_segment($data->id);
        echo json_encode($data);
    }

    /* Sort Data List Table */

    function sub_sort()
    {
        $data = array(
            'sort' => $this->input->post('newPos')
        );
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $this->db->where('id', $id);
        $this->db->update('mas_sub_department', $data);
        $this->log_lib->write_log('Department Sub => ' . 'จัดลำดับข้อมูลเสร็จสิ้น', json_encode($this->db->last_query()));
    }

    /* Add Data */

    function sub_add()
    {
        $data = array(
            'department_id' => $this->efs_lib->decrypt_segment($this->input->post('department_id')),
            'sort' => $this->input->post('sort'),
            'name' => $this->input->post('name'),
            'description' => $this->input->post('description'),
            'created_username' => $this->session->userdata("user_profile")->username,
            'created_at' => date('Y-m-d H:i:s'),
            'record_status' => 'N'
        );
        if (!$this->db->insert('mas_sub_department', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_add);
            /* Create Log Data  */
            $this->log_lib->write_log('error-department-sub-add=> ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Department Sub => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Edit Data */

    function sub_edit()
    {
        $data = array(
            'name' => $this->input->post('name'),
            'description' => $this->input->post('description'),
            'updated_username' => $this->session->userdata("user_profile")->username,
            'updated_at' => date('Y-m-d H:i:s')
        );
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $this->db->where('id', $id);
        if (!$this->db->update('mas_sub_department', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-department-sub-update=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Department Sub => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Delete Data */

    function sub_delete()
    {
        $id = $this->efs_lib->decrypt_segment($this->uri->segment(4));
        $data = array(
            'record_status' => 'D',
            'updated_username' => $this->session->userdata("user_profile")->username,
            'updated_at' => date('Y-m-d H:i:s'),
        );
        $this->db->where('id', $id);
        if (!$this->db->update('mas_sub_department', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_delete);
            /* Create Log Data  */
            $this->log_lib->write_log('error-department-sub-delete=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_delete_success);
            $this->log_lib->write_log('Department Sub => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Change Active Data */

    function sub_change_active()
    {
        $status = explode('.', $this->uri->segment(4));
        $data['is_active'] = ($status[1] == 1 ? 0 : 1);
        $id = $this->efs_lib->decrypt_segment($status[0]);
        $this->db->where('id', $id);
        if (!$this->db->update('mas_sub_department', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-department-sub-active=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Department Sub => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Check Department Index */

    public function check()
    {
        $department_id = $this->efs_lib->decrypt_segment($this->uri->segment(4));
        if (empty($department_id))
            redirect(base_url("error404")); /* IF Return Null Redirect to default URL */

        $this->data['topbar'] = 'main/layout/topbar';
        $this->data['sidebar'] = 'main/layout/sidebar';
        $this->data['footer'] = 'main/layout/footer';
        /* Get Current Module */
        $parent_module = $this->efs_lib->get_parent_module($this->uri->uri_string());

        /* Get Parent Module For Department */
        $sql = "SELECT * "
            . "FROM mas_department "
            . "WHERE record_status = 'N' "
            . "AND id = '" . $department_id . "'";
        $department = $this->db->query($sql)->row();

        /* System Language */
        $this->data['lang_sys'] = $this->efs_lib->language_system();
        /* Module Language */
        $this->data['lang_module'] = $this->efs_lib->language_module(11);

        $breadcrumbs['breadcrumb'] = array(
            array(1 => array(
                "name" => '<i class="mdi mdi-home font-20" style="line-height: 20px;"></i>',
                "module" => base_url(),
                "class" => ''
            )),
            array(2 => array(
                "name" => $parent_module->{"name_" . $this->session->userdata("user_profile")->cng_lang},
                "module" => base_url($parent_module->module),
                "class" => ''
            )),
            array(3 => array(
                "name" => $department->name,
                "module" => '',
                "class" => 'active'
            ))
        );
        $this->data['breadcrumb'] = $this->load->view('main/layout/breadcrumb', $breadcrumbs, true);
        $this->data['module'] = 'main/mas/check_department';

        $this->load->view("main/layout/index", $this->data);
    }

    /* Get Data to List Table */

    public function check_ajax_list()
    {
        header('Access-Control-Allow-Origin: *');
        ini_set('max_execution_time', 300);
        // $is_active = $_GET['is_active'];

        $is_must = $_GET['is_must'];

        $sql = "SELECT mas_department_check_pr.id, mas_department_check_pr.department_id, mas_department_check_pr.step, mas_department_check_pr.is_must, mas_department_check_pr.sort, mas_department_check_pr.user_id, mas_department_check_pr.description, mas_department_check_pr.is_active, mas_department_check_pr.created_username "
            . ", sys_user.emp_code, sys_user.prefix_name, sys_user.first_name, sys_user.last_name, mas_department.name AS department_name, mas_position.name AS position_name "
            . "FROM mas_department_check_pr INNER JOIN sys_user ON mas_department_check_pr.user_id = sys_user.id "
            . "INNER JOIN mas_department ON mas_department.id = sys_user.department_id "
            . "INNER JOIN mas_position ON mas_position.id = sys_user.position_id "
            . "WHERE mas_department_check_pr.record_status='N' ";
        $sql .= ($is_must == 1 ? " AND mas_department_check_pr.is_active = 1 " : "");
        $sql .= " AND mas_department_check_pr.department_id ='" . $this->efs_lib->decrypt_segment($this->uri->segment(4)) . "' ";
        $sql .= "ORDER BY mas_department_check_pr.step, mas_department_check_pr.sort ASC";
        $data['data'] = $this->db->query($sql)->result();

        /* Init $items NULL */
        $items['data'] = array();
        foreach ($data['data'] as $key => $item) {
            $items['data'][$key]['id'] = $this->efs_lib->encrypt_segment($item->id);
            $items['data'][$key]['department_id'] = $item->department_id;
            $items['data'][$key]['step'] = $item->step;
            $items['data'][$key]['is_must'] = $item->is_must;
            $items['data'][$key]['sort'] = $item->sort;
            $items['data'][$key]['emp_code'] = $item->emp_code;
            $items['data'][$key]['prefix_name'] = $item->prefix_name;
            $items['data'][$key]['first_name'] = $item->first_name;
            $items['data'][$key]['last_name'] = $item->last_name;
            $items['data'][$key]['department_name'] = $item->department_name;
            $items['data'][$key]['position_name'] = $item->position_name;
            $items['data'][$key]['description'] = $item->description;
            $items['data'][$key]['is_active'] = $item->is_active;
            $items['data'][$key]['created_username'] = $item->created_username;
        }
        echo json_encode($items);
    }

    /* Get Data to Item */

    function check_ajax_data()
    {
        header('Access-Control-Allow-Origin: *');
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $sql = "SELECT * "
            . "FROM mas_department_check_pr "
            . "WHERE record_status='N' AND id ='" . $id . "'";
        $data = $this->db->query($sql)->row();
        $data->id = $this->efs_lib->encrypt_segment($data->id);
        echo json_encode($data);
    }

    /* Sort Data List Table */

    function check_sort()
    {
        $data = array(
            'sort' => $this->input->post('newPos')
        );
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $this->db->where('id', $id);
        $this->db->update('mas_department_check_pr', $data);
        $this->log_lib->write_log('Department Check PR => ' . 'จัดลำดับข้อมูลเสร็จสิ้น', json_encode($this->db->last_query()));
    }

    /* Add Data */

    function check_add()
    {
        $data = array(
            'department_id' => $this->efs_lib->decrypt_segment($this->input->post('department_id')),
            'sort' => $this->input->post('sort'),
            'step' => $this->input->post('step'),
            'is_must' => $this->input->post('is_must'),
            'user_id' => $this->input->post('user_id'),
            'description' => $this->input->post('description'),
            'created_username' => $this->session->userdata("user_profile")->username,
            'created_at' => date('Y-m-d H:i:s'),
            'record_status' => 'N'
        );
        if (!$this->db->insert('mas_department_check_pr', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_add);
            /* Create Log Data  */
            $this->log_lib->write_log('error-department-check-add=> ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Department Check PR => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Edit Data */

    function check_edit()
    {
        /* var_dump($this->input->post());die(); */
        $data = array(
            'user_id' => $this->input->post('user_id'),
            'sort' => $this->input->post('sort'),
            'step' => $this->input->post('step'),
            'is_must' => $this->input->post('is_must'),
            'description' => $this->input->post('description'),
            'updated_username' => $this->session->userdata("user_profile")->username,
            'updated_at' => date('Y-m-d H:i:s')
        );
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $this->db->where('id', $id);
        if (!$this->db->update('mas_department_check_pr', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-department-check-update=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Department Check PR => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Delete Data */

    function check_delete()
    {
        $id = $this->efs_lib->decrypt_segment($this->uri->segment(4));
        $data = array(
            'record_status' => 'D',
            'updated_username' => $this->session->userdata("user_profile")->username,
            'updated_at' => date('Y-m-d H:i:s'),
        );
        $this->db->where('id', $id);
        if (!$this->db->update('mas_department_check_pr', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_delete);
            /* Create Log Data  */
            $this->log_lib->write_log('error-department-check-delete=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_delete_success);
            $this->log_lib->write_log('Department Check PR => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Change Active Data */

    function check_change_active()
    {
        $status = explode('.', $this->uri->segment(4));
        $data['is_active'] = ($status[1] == 1 ? 0 : 1);
        $id = $this->efs_lib->decrypt_segment($status[0]);
        $this->db->where('id', $id);
        if (!$this->db->update('mas_department_check_pr', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-department-check-active=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Department Check PR => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* <!-- ============================================================== -->
      <!-- Function -->
      <!-- ============================================================== --> */
}
