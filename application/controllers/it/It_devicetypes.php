 
<?php

defined('BASEPATH') or exit('No direct script access allowed');

class It_devicetypes extends CI_Controller
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
        $this->data['module'] = 'main/it/it_devicetypes';
        $this->load->view("main/layout/index", $this->data);
    }

    public function getDeviceTypes()
    {
        // SELECT TOP (200) id, name, description, is_active, created_at, created_username, updated_at, updated_username, record_status
        // FROM it_devicetypes
        $this->db->select('id, name, description, is_active, created_at, created_username, updated_at, updated_username, record_status');
        $this->db->from('it_devicetypes');
        // $this->db->where('is_active', $this->input->post('is_active'));
        (!empty($this->input->post('is_active'))) ? $this->db->where('is_active', $this->input->post('is_active')) : '';
        $this->db->where('record_status', 'N');
        $this->db->order_by('id', 'DESC');
        // die($this->db->get_compiled_select());
        $data = $this->db->get()->result();
        $data = array('data' => $data);

        return $this->output->set_content_type('application/json')->set_output(json_encode($data));
    }

    public function saveDeviceType()
    {
        $data = array(
            'name' => $this->input->post('name'),
            'description' => $this->input->post('description'),
            'is_active' => $this->input->post('is_active'),
            'created_username' => $this->session->userdata('username'),
            'created_at' => date('Y-m-d H:i:s'),
            'record_status' => 'N'
        );
        $this->db->insert('it_devicetypes', $data);
        return $this->output->set_content_type('application/json')->set_output(json_encode(array('success' => 'Device type saved successfully')));
    }

    public function getDeviceType($id)
    {
        // ตรวจสอบสิทธิ์การเข้าถึง
        if (!$this->efs_lib->is_can($this->permission, "view")) {
            echo json_encode(array('error' => 'Permission denied'));
            return;
        }

        $deviceType = $this->db->get_where('it_devicetypes', array('id' => $id))->row();
        echo json_encode($deviceType);
    }

    public function deleteDeviceType($id)
    {
        // ตรวจสอบสิทธิ์การเข้าถึง
        if (!$this->efs_lib->is_can($this->permission, "delete")) {
            echo json_encode(array('error' => 'Permission denied'));
            return;
        }

        $data = array(
            'record_status' => 'D',
            'updated_username' => $this->session->userdata('username'),
            'updated_at' => date('Y-m-d H:i:s')
        );

        $this->db->where('id', $id);
        $this->db->update('it_devicetypes', $data);

        if ($this->db->affected_rows() > 0) {
            echo json_encode(array('success' => 'Device type deleted successfully'));
        } else {
            echo json_encode(array('error' => 'Failed to delete device type'));
        }
    }
}
