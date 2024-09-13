<?php

defined('BASEPATH') or exit('No direct script access allowed');

class It_softwarelicenses extends CI_Controller
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
        $this->data['module'] = 'main/it/it_softwarelicenses';
        $this->load->view("main/layout/index", $this->data);
    }

    public function print_r($data)
    {
        echo '<pre>';
        print_r($data);
        echo '</pre>';
        die();
    }

    public function getsoftwarelicenses()
    {
        // SELECT id, name, publisher, purchasedate, expirationdate, licensekey, numberoflicenses, cost, notes, is_active, created_at, created_username, updated_at, updated_username, record_status
        // FROM it_softwarelicenses
        $this->db->select('id, name, publisher, purchasedate, expirationdate, licensekey, numberoflicenses, cost, notes, is_active, created_at, created_username, updated_at, updated_username, record_status');
        $this->db->from('it_softwarelicenses');

        // กรองตาม is_active ถ้ามีการส่งค่ามา
        if ($this->input->post('is_active') !== '') {
            $this->db->where('is_active', $this->input->post('is_active'));
        }

        // กรองตาม name ถ้ามีการส่งค่ามา
        if ($this->input->post('name') !== null) {
            $this->db->like('name', $this->input->post('name'));
        }

        // แสดงเฉพาะรายการที่ไม่ถูกลบ
        $this->db->where('record_status', 'N');

        // เรียงลำดับตาม id จากมากไปน้อย
        $this->db->order_by('id', 'DESC');

        // die($this->db->get_compiled_select());
        // ดึงข้อมูล
        $data = $this->db->get()->result();

        // จัดรูปแบบข้อมูลให้ตรงกับที่ DataTables ต้องการ
        $data = array('data' => $data);

        // ส่งข้อมูลกลับเป็น JSON
        return $this->output
            ->set_content_type('application/json')
            ->set_output(json_encode($data));
    }

    public function savesoftwarelicenses()
    {
        $data = array(
            'name' => $this->input->post('name'),
            'publisher' => $this->input->post('publisher'),
            'purchasedate' => $this->input->post('purchasedate'),
            'expirationdate' => $this->input->post('expirationdate'),
            'licensekey' => $this->input->post('licensekey'),
            'numberoflicenses' => $this->input->post('numberoflicenses'),
            'cost' => $this->input->post('cost'),
            'notes' => $this->input->post('notes'),
            'is_active' => 'Y',
            'created_at' => date('Y-m-d H:i:s'),
            'created_username' => $this->session->userdata("user_profile")->username,
            'updated_at' => date('Y-m-d H:i:s'),
            'updated_username' => $this->session->userdata("user_profile")->username,
            'record_status' => 'N'
        );

        $this->db->insert('it_softwarelicenses', $data);

        return $this->output
            ->set_content_type('application/json')
            ->set_output(json_encode(array('status' => 'success')));
    }
}
