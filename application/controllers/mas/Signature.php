<?php

defined('BASEPATH') or exit('No direct script access allowed');

class Signature extends CI_Controller
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
        $this->data['module'] = 'main/mas/signature';

        $this->load->view("main/layout/index", $this->data);
    }

    /* Get Data to List Table */

    public function ajax_list()
    {
        header('Access-Control-Allow-Origin: *');
        ini_set('max_execution_time', 300);
        $sql = "SELECT sys_user.id, sys_user.department_id, sys_user.sub_department_id, sys_user.username "
            . ", sys_user.first_name, sys_user.last_name, sys_user.tel,sys_user.email, sys_user.is_active "
            . ", sys_user.cng_per_page, sys_user.cng_font_size, sys_user.cng_table_font_size, sys_user.cng_lang "
            . ", sys_user.cng_alert_time,mas_department.name AS department_name, mas_department.description AS department_description "
            . ", mas_sub_department.ref_code AS sub_department_ref_code, mas_sub_department.name AS sub_department_name "
            . ", mas_sub_department.description AS sub_department_description "
            . ", mas_signature.sign, mas_signature.description AS signature_description "
            . ", mas_signature.id AS signature_id, mas_signature.is_active AS signature_is_active, mas_signature.created_username "
            . " FROM sys_user INNER JOIN mas_department ON sys_user.department_id = mas_department.id "
            . " LEFT OUTER JOIN mas_signature ON sys_user.id = mas_signature.user_id "
            . " LEFT OUTER JOIN mas_sub_department ON sys_user.sub_department_id = mas_sub_department.id "
            . " WHERE (mas_signature.record_status = 'N') AND (sys_user.record_status = 'N') ORDER BY sys_user.first_name ASC ";
        // echo $sql; die();
        $data['data'] = $this->db->query($sql)->result();
        /* Init $items NULL */
        $items['data'] = array();
        foreach ($data['data'] as $key => $item) {
            $items['data'][$key]['id'] = $this->efs_lib->encrypt_segment($item->signature_id);
            $items['data'][$key]['username'] = $item->username;
            $items['data'][$key]['first_name'] = $item->first_name;
            $items['data'][$key]['last_name'] = $item->last_name;
            $items['data'][$key]['department_name'] = $item->department_name;
            $items['data'][$key]['sub_department_name'] = $item->sub_department_name;
            $items['data'][$key]['sign'] = $item->sign;
            $items['data'][$key]['description'] = $item->signature_description;
            $items['data'][$key]['is_active'] = $item->signature_is_active;
            $items['data'][$key]['created_username'] = $item->created_username;
        }
        echo json_encode($items);
    }

    /* Get Data to Item */
    function ajax_data()
    {
        header('Access-Control-Allow-Origin: *');
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $sql = "SELECT * FROM mas_signature WHERE record_status='N' AND id ='" . $id . "'";
        $data = $this->db->query($sql)->row();
        $data->id = $this->efs_lib->encrypt_segment($data->id);
        // print_r(json_encode($data));
        // die();
        echo json_encode($data);
    }

    /* Add Data */
    function add()
    {
        // var_dump($this->input->post('sign')); die();
        $data = array(
            'user_id' => $this->input->post('user_id'),
            'sign' => str_replace("[removed]", "data:image/png;base64,", $this->input->post('sign')),
            'description' => $this->input->post('description'),
            'created_username' => $this->session->userdata("user_profile")->username,
            'created_at' => date('Y-m-d H:i:s'),
            'record_status' => 'N'
        );
        if (!$this->db->insert('mas_signature', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_add);
            /* Create Log Data  */
            $this->log_lib->write_log('error-signature-add=> ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Signature => ' . $this->session->flashdata('msg'), "user_id => " . $this->input->post('user_id'));
        }
        redirect($this->agent->referrer());
    }

    /* Edit Data */

    function edit()
    {
        $data = array(
            'user_id' => $this->input->post('user_id'),
            'description' => $this->input->post('description'),
            'updated_username' => $this->session->userdata("user_profile")->username,
            'updated_at' => date('Y-m-d H:i:s')
        );
        if ($this->input->post('sign')) {
            $data['sign'] = str_replace("[removed]", "data:image/png;base64,", $this->input->post('sign'));
        }
        // echo '<pre>';
        // print_r($data);
        // echo '</pre>';
        // die();
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $this->db->where('id', $id);
        if (!$this->db->update('mas_signature', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-signature-update=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Signature => ' . $this->session->flashdata('msg'), "user_id => " . $this->input->post('user_id'));
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
        if (!$this->db->update('mas_signature', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_delete);
            /* Create Log Data  */
            $this->log_lib->write_log('error-signature-delete=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_delete_success);
            $this->log_lib->write_log('Signature => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
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
        if (!$this->db->update('mas_signature', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-signature-active=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Signature => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* <!-- ============================================================== -->
      <!-- Function -->
      <!-- ============================================================== --> */
}
