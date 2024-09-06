<?php

defined('BASEPATH') or exit('No direct script access allowed');

class Position extends CI_Controller
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
        $this->data['module'] = 'main/mas/position';

        $this->load->view("main/layout/index", $this->data);
    }

    /* Get Data to List Table */

    public function ajax_list()
    {
        header('Access-Control-Allow-Origin: *');
        ini_set('max_execution_time', 300);
        $sql = "SELECT * "
            . "FROM mas_position "
            . "WHERE "
            . "record_status='N' "
            . "ORDER BY sort ASC";
        $data['data'] = $this->db->query($sql)->result();
        /* Init $items NULL */
        $items['data'] = array();
        foreach ($data['data'] as $key => $item) {
            $items['data'][$key]['id'] = $this->efs_lib->encrypt_segment($item->id);
            $items['data'][$key]['name'] = $item->name;
            $items['data'][$key]['short_name'] = $item->short_name;
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
            . "FROM mas_position "
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
        $this->db->update('mas_position', $data);
        $this->log_lib->write_log('Position => ' . 'จัดลำดับข้อมูลเสร็จสิ้น', json_encode($this->db->last_query()));
    }

    /* Add Data */

    function add()
    {
        $data = array(
            'name' => $this->input->post('name'),
            'short_name' => $this->input->post('short_name'),
            'description' => $this->input->post('description'),
            'created_username' => $this->session->userdata("user_profile")->username,
            'created_at' => date('Y-m-d H:i:s'),
            'record_status' => 'N'
        );
        if (!$this->db->insert('mas_position', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_add);
            /* Create Log Data  */
            $this->log_lib->write_log('error-position-add=> ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Position => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Edit Data */

    function edit()
    {
        $data = array(
            'name' => $this->input->post('name'),
            'short_name' => $this->input->post('short_name'),
            'description' => $this->input->post('description'),
            'updated_username' => $this->session->userdata("user_profile")->username,
            'updated_at' => date('Y-m-d H:i:s')
        );
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $this->db->where('id', $id);
        if (!$this->db->update('mas_position', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-position-update=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Position => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
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
        if (!$this->db->update('mas_position', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_delete);
            /* Create Log Data  */
            $this->log_lib->write_log('error-position-delete=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_delete_success);
            $this->log_lib->write_log('Position => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
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
        if (!$this->db->update('mas_position', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-position-active=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Position => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* <!-- ============================================================== -->
      <!-- Function -->
      <!-- ============================================================== --> */
}
