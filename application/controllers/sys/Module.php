<?php

defined('BASEPATH') or exit('No direct script access allowed');

class Module extends CI_Controller
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
        $this->data['module'] = 'main/sys/module';

        $this->load->view("main/layout/index", $this->data);
    }

    /* Get Data to List Table */

    public function ajax_list()
    {
        header('Access-Control-Allow-Origin: *');
        ini_set('max_execution_time', 300);
        $sql = "SELECT id, name_th, name_en, name_jp, icon, created_username, is_active FROM sys_module WHERE record_status='N' AND parent_module_id ='0' ORDER BY sort ASC";
        $data['data'] = $this->db->query($sql)->result();
        /* Init $items NULL */
        $items['data'] = array();
        foreach ($data['data'] as $key => $item) {
            $items['data'][$key]['id'] = $this->efs_lib->encrypt_segment($item->id);
            $items['data'][$key]['name_th'] = $item->name_th;
            $items['data'][$key]['name_en'] = $item->name_en;
            $items['data'][$key]['name_jp'] = $item->name_jp;
            $items['data'][$key]['icon'] = $item->icon;
            $items['data'][$key]['created_username'] = $item->created_username;
            $items['data'][$key]['is_active'] = $item->is_active;
        }
        echo json_encode($items);
    }

    /* Get Data to Item */

    function ajax_data()
    {
        header('Access-Control-Allow-Origin: *');
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $sql = "SELECT * FROM sys_module WHERE record_status='N' AND id ='" . $id . "'";
        $data = $this->db->query($sql)->row();
        $data->permission = explode("|", $data->permission);
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
        $this->db->update('sys_module', $data);
        $this->log_lib->write_log('Module => ' . $this->lang->sys_msg_sort_complete, json_encode($this->db->last_query()));
    }

    /* Add Data */

    function add()
    {
        $pre_jp = 'N*';
        $data = array(
            'sort' => $this->input->post('sort'),
            'name_th' => $this->input->post('name_th'),
            'name_en' => $this->input->post('name_en'),
            'name_jp' => $pre_jp . $this->input->post('name_jp'),
            'module' => $this->input->post('module'),
            'icon' => $this->input->post('icon'),
            'parent_module_id' => $this->input->post('parent_module_id'),
            'created_username' => $this->session->userdata("user_profile")->username,
            'created_at' => date('Y-m-d H:i:s'),
            'record_status' => 'N'
        );

        /* ระบุว่าโมดูลนี้มีสิทธิอะไรที่สามารถตั้งค่าได้บ้าง 17/11/20 */
        $data['permission'] = '1|';
        if (!empty($_POST['permission'])) {
            foreach (@$_POST['permission'] as $item) {
                $data['permission'] .= $item . '|';
            }
            $data['permission'] = rtrim($data['permission'], ".");
        }
        /* ระบุว่าโมดูลนี้มีสิทธิอะไรที่สามารถตั้งค่าได้บ้าง 17/11/20 */

        if (!$this->db->query($this->efs_lib->query_insert_jp('sys_module', $data))) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_add);
            /* Create Log Data  */
            $this->log_lib->write_log('error-module-add=> ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Module => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Edit Data */

    function edit()
    {
        $pre_jp = 'N*';
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $data = array(
            'name_th' => $this->input->post('name_th'),
            'name_en' => $this->input->post('name_en'),
            'name_jp' => $pre_jp . $this->input->post('name_jp'),
            'module' => $this->input->post('module'),
            'icon' => $this->input->post('icon'),
            'parent_module_id' => $this->input->post('parent_module_id'),
            'updated_username' => $this->session->userdata("user_profile")->username,
            'updated_at' => date('Y-m-d H:i:s')
        );

        /* ระบุว่าโมดูลนี้มีสิทธิอะไรที่สามารถตั้งค่าได้บ้าง 17/11/20 */
        $data['permission'] = '1|';
        if (!empty($_POST['permission'])) {
            foreach (@$_POST['permission'] as $item) {
                $data['permission'] .= $item . '|';
            }
            $data['permission'] = rtrim($data['permission'], ".");
        }
        /* ระบุว่าโมดูลนี้มีสิทธิอะไรที่สามารถตั้งค่าได้บ้าง 17/11/20 */

        $where['id'] = $id;
        if (!$this->db->query($this->efs_lib->query_update_jp('sys_module', $data, $where))) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-module-update=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Module => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
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
        $sql = "SELECT module FROM sys_module WHERE record_status='N' AND parent_module_id ='" . $id . "'";
        $nrow = $this->db->query($sql)->num_rows();
        if ($nrow > 0) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_delete_module . ' [' . $nrow . ']');
            /* Create Log Data  */
            $this->log_lib->write_log('error-module-delete-1=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->db->where('id', $id);
            if (!$this->db->update('sys_module', $data)) {
                $this->session->set_flashdata('type', 'error');
                $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_delete);
                /* Create Log Data  */
                $this->log_lib->write_log('error-module-delete-2=> ' . $this->session->flashdata('msg'), json_encode($data));
            } else {
                $this->session->set_flashdata('type', 'success');
                $this->session->set_flashdata('msg', $this->lang->sys_msg_delete_success);
                $this->log_lib->write_log('Module => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
            }
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
        if (!$this->db->update('sys_module', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-module-active=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Module => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Child Module Index */

    public function child()
    {
        $module_parent_id = $this->efs_lib->decrypt_segment($this->uri->segment(4));
        if (empty($module_parent_id))
            redirect(base_url("error404")); /* IF Return Null Redirect to default URL */

        $this->data['topbar'] = 'main/layout/topbar';
        $this->data['sidebar'] = 'main/layout/sidebar';
        $this->data['footer'] = 'main/layout/footer';
        /* Get Current Module */
        $parent_module = $this->efs_lib->get_parent_module($this->uri->uri_string());
        $module = $this->efs_lib->get_current_module($this->uri->uri_string());
        /* System Language */
        $this->data['lang_sys'] = $this->efs_lib->language_system();
        /* Module Language */
        // $this->efs_lib->print_r($module);
        $this->data['lang_module'] = $this->efs_lib->language_module($module->id);

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
                "name" => $module->{"name_" . $this->session->userdata("user_profile")->cng_lang},
                "module" => '',
                "class" => 'active'
            ))
        );
        $this->data['breadcrumb'] = $this->load->view('main/layout/breadcrumb', $breadcrumbs, true);
        $this->data['module'] = 'main/sys/module_child';

        $this->load->view("main/layout/index", $this->data);
    }

    /* Get Data to List Table */

    public function child_ajax_list()
    {
        header('Access-Control-Allow-Origin: *');
        ini_set('max_execution_time', 300);
        $module_parent_id = $this->efs_lib->decrypt_segment($this->uri->segment(4));
        $sql = "SELECT * FROM sys_module WHERE record_status='N' AND parent_module_id ='" . $module_parent_id . "' ORDER BY sort ASC";
        $data['data'] = $this->db->query($sql)->result();
        /* Init $items NULL */
        $items['data'] = array();
        foreach ($data['data'] as $key => $item) {
            $items['data'][$key]['id'] = $this->efs_lib->encrypt_segment($item->id);
            $items['data'][$key]['name_th'] = $item->name_th;
            $items['data'][$key]['name_en'] = $item->name_en;
            $items['data'][$key]['name_jp'] = $item->name_jp;
            $items['data'][$key]['module'] = $item->module;
            $items['data'][$key]['created_username'] = $item->created_username;
            $items['data'][$key]['is_active'] = $item->is_active;
        }
        echo json_encode($items);
    }

    /* Get Data to Item */

    function child_ajax_data()
    {
        header('Access-Control-Allow-Origin: *');
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $sql = "SELECT * FROM sys_module WHERE record_status='N' AND id ='" . $id . "'";
        $data = $this->db->query($sql)->row();
        $data->permission = explode("|", $data->permission);
        $data->id = $this->efs_lib->encrypt_segment($data->id);
        echo json_encode($data);
    }

    /* Sort Data List Table */

    function child_sort()
    {
        $data = array(
            'sort' => $this->input->post('newPos')
        );
        $id = $this->efs_lib->encrypt_segment($this->input->post('id'));
        $this->db->where('id', $id);
        $this->db->update('sys_module', $data);
        $this->log_lib->write_log('Module => ' . $this->lang->sys_msg_sort_complete, json_encode($this->db->last_query()));
    }

    /* Add Data */

    function child_add()
    {
        $pre_jp = 'N*';
        $data = array(
            'sort' => $this->input->post('sort'),
            'name_th' => $this->input->post('name_th'),
            'name_en' => $this->input->post('name_en'),
            'name_jp' => $pre_jp . $this->input->post('name_jp'),
            'name_cn' => $pre_jp . $this->input->post('name_cn'),
            'module' => $this->input->post('module'),
            'icon' => $this->input->post('icon'),
            'parent_module_id' => $this->efs_lib->decrypt_segment($this->input->post('parent_module_id')),
            'created_username' => $this->session->userdata("user_profile")->username,
            'created_at' => date('Y-m-d H:i:s'),
            'record_status' => 'N'
        );

        /* ระบุว่าโมดูลนี้มีสิทธิอะไรที่สามารถตั้งค่าได้บ้าง 17/11/20 */
        $data['permission'] = '1|';
        if (!empty($_POST['permission'])) {
            foreach (@$_POST['permission'] as $item) {
                $data['permission'] .= $item . '|';
            }
            $data['permission'] = rtrim($data['permission'], ".");
        }
        /* ระบุว่าโมดูลนี้มีสิทธิอะไรที่สามารถตั้งค่าได้บ้าง 17/11/20 */

        if (!$this->db->query($this->efs_lib->query_insert_jp('sys_module', $data))) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_add);
            /* Create Log Data  */
            $this->log_lib->write_log('error-module-child-add=> ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        } else {

            /* SET adminstrator permission */
            /* Get Last module ID */
            $module_id = $this->db->insert_id();
            $permissions = $this->db->query("SELECT * FROM sys_permission WHERE record_status ='N'")->result();
            foreach ($permissions as $permission) {

                $item = array(
                    'module_id' => $module_id,
                    'permission_id' => $permission->id,
                    'role_id' => 1,
                    'created_username' => $this->session->userdata("user_profile")->username,
                    'created_at' => date('Y-m-d H:i:s'),
                    'record_status' => 'N'
                );
                $this->db->insert('sys_permission_module_role', $item);
            }
            /* SET adminstrator permission */

            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Module => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Edit Data */

    function child_edit()
    {
        $pre_jp = 'N*';
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $data = array(
            'name_th' => $this->input->post('name_th'),
            'name_en' => $this->input->post('name_en'),
            'name_jp' => $pre_jp . $this->input->post('name_jp'),
            'module' => $this->input->post('module'),
            'icon' => $this->input->post('icon'),
            'parent_module_id' => $this->efs_lib->decrypt_segment($this->input->post('parent_module_id')),
            'updated_username' => $this->session->userdata("user_profile")->username,
            'updated_at' => date('Y-m-d H:i:s')
        );

        /* ระบุว่าโมดูลนี้มีสิทธิอะไรที่สามารถตั้งค่าได้บ้าง 17/11/20 */
        $data['permission'] = '1|';
        if (!empty($_POST['permission'])) {
            foreach (@$_POST['permission'] as $item) {
                $data['permission'] .= $item . '|';
            }
            $data['permission'] = rtrim($data['permission'], ".");
        }
        /* ระบุว่าโมดูลนี้มีสิทธิอะไรที่สามารถตั้งค่าได้บ้าง 17/11/20 */

        $where['id'] = $id;
        if (!$this->db->query($this->efs_lib->query_update_jp('sys_module', $data, $where))) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-module-child-update=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->sys_msg_save_success);
            $this->log_lib->write_log('Module => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Delete Data */

    function child_delete()
    {
        $id = $this->efs_lib->decrypt_segment($this->uri->segment(4));
        $data = array(
            'record_status' => 'D',
            'updated_username' => $this->session->userdata("user_profile")->username,
            'updated_at' => date('Y-m-d H:i:s'),
        );
        $this->db->where('id', $id);
        if (!$this->db->update('sys_module', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_delete);
            /* Create Log Data  */
            $this->log_lib->write_log('error-module-child-delete=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {

            /* Delete from sys_permission_module_role when delete module */
            $this->db->where('module_id', $id);
            $this->db->update('sys_permission_module_role', $data);

            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_delete_success);
            $this->log_lib->write_log('Module => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }

        redirect($this->agent->referrer());
    }

    /* Language Module Item */

    public function language()
    {
        $module_id = $this->efs_lib->decrypt_segment($this->uri->segment(4));
        if (empty($module_id))
            redirect(base_url("error404")); /* IF Return Null Redirect to default URL */

        $this->data['topbar'] = 'main/layout/topbar';
        $this->data['sidebar'] = 'main/layout/sidebar';
        $this->data['footer'] = 'main/layout/footer';
        /* Get Current Module */
        $parent_module = $this->efs_lib->get_parent_module($this->uri->uri_string());
        $module = $this->efs_lib->get_current_module($this->uri->uri_string());
        /* System Language */
        $this->data['lang_sys'] = $this->efs_lib->language_system();
        /* Module Language */
        $this->data['lang_module'] = $this->efs_lib->language_module(3);

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
                "name" => $module->{"name_" . $this->session->userdata("user_profile")->cng_lang},
                "module" => '',
                "class" => 'active'
            ))
        );
        $this->data['breadcrumb'] = $this->load->view('main/layout/breadcrumb', $breadcrumbs, true);
        $this->data['module'] = 'main/sys/module_language';

        $this->load->view("main/layout/index", $this->data);
    }

    /* Get Data to List Table */

    public function language_ajax_list()
    {
        header('Access-Control-Allow-Origin: *');
        ini_set('max_execution_time', 300);
        $module_id = $this->efs_lib->decrypt_segment($this->uri->segment(4));
        $sql = "SELECT * FROM sys_language WHERE record_status='N' AND module_id ='" . $module_id . "' ORDER BY keyword ASC";
        $data['data'] = $this->db->query($sql)->result();
        /* Init $items NULL */
        $items['data'] = array();
        foreach ($data['data'] as $key => $item) {
            $items['data'][$key]['id'] = $this->efs_lib->encrypt_segment($item->id);
            $items['data'][$key]['keyword'] = $item->keyword;
            $items['data'][$key]['th'] = $item->th;
            $items['data'][$key]['en'] = $item->en;
            $items['data'][$key]['jp'] = $item->jp;
            $items['data'][$key]['module_id'] = $item->module_id;
            $items['data'][$key]['created_username'] = $item->created_username;
        }
        echo json_encode($items);
    }

    /* Get Data to Item */

    function language_ajax_data()
    {
        header('Access-Control-Allow-Origin: *');
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $sql = "SELECT * FROM sys_language WHERE record_status='N' AND id ='" . $id . "'";
        $data = $this->db->query($sql)->row();
        $data->id = $this->efs_lib->encrypt_segment($data->id);
        echo json_encode($data);
    }

    /* Add Data */

    function language_add()
    {
        $pre_jp = 'N*';
        $jp = trim(preg_replace('/\s+/', ' ', $this->input->post('jp')));
        $data = array(
            'module_id' => $this->efs_lib->decrypt_segment($this->input->post('module_id')),
            'keyword' => $this->input->post('keyword'),
            'th' => $this->input->post('th'),
            'en' => $this->input->post('en'),
            'jp' => $pre_jp . $jp,
            'created_username' => $this->session->userdata("user_profile")->username,
            'created_at' => date('Y-m-d H:i:s'),
            'record_status' => 'N'
        );

        if (!$this->db->query($this->efs_lib->query_insert_jp('sys_language', $data))) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_add);
            /* Create Log Data  */
            $this->log_lib->write_log('error-module-language-add=> ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Language Module => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Edit Data */

    function language_edit()
    {
        $pre_jp = "N*";
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $jp = trim(preg_replace('/\s+/', ' ', $this->input->post('jp')));
        $data = array(
            'keyword' => $this->input->post('keyword'),
            'th' => $this->input->post('th'),
            'en' => $this->input->post('en'),
            'jp' => $pre_jp .  $jp,
            'module_id' => $this->efs_lib->decrypt_segment($this->input->post('module_id')),
            'updated_username' => $this->session->userdata("user_profile")->username,
            'updated_at' => date('Y-m-d H:i:s')
        );
        $where['id'] = $id;
        if (!$this->db->query($this->efs_lib->query_update_jp('sys_language', $data, $where))) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-module-language_update=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Module => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Delete Data */

    function language_delete()
    {
        $id = $this->efs_lib->decrypt_segment($this->uri->segment(4));
        $data = array(
            'record_status' => 'D',
            'updated_username' => $this->session->userdata("user_profile")->username,
            'updated_at' => date('Y-m-d H:i:s'),
        );
        $this->db->where('id', $id);
        if (!$this->db->update('sys_language', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_delete);
            /* Create Log Data  */
            $this->log_lib->write_log('error-module-language-delete=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_delete_success);
            $this->log_lib->write_log('Module => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }

        redirect($this->agent->referrer());
    }

    /* <!-- ============================================================== -->
      <!-- Function -->
      <!-- ============================================================== --> */
}
