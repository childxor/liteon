<?php

defined('BASEPATH') or exit('No direct script access allowed');

class Role extends CI_Controller
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
        $this->data['module'] = 'main/sys/role';

        $this->load->view("main/layout/index", $this->data);
    }

    /* Get Data to List Table */

    public function ajax_list()
    {
        header('Access-Control-Allow-Origin: *');
        ini_set('max_execution_time', 300);
        $sql = "SELECT id, name, description, created_username FROM sys_role WHERE record_status='N' ORDER BY sort ASC";
        $data['data'] = $this->db->query($sql)->result();
        /* Init $items NULL */
        $items['data'] = array();
        foreach ($data['data'] as $key => $item) {
            $items['data'][$key]['id'] = $this->efs_lib->encrypt_segment($item->id);
            $items['data'][$key]['name'] = $item->name;
            $items['data'][$key]['description'] = $item->description;
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
            . "FROM sys_role "
            . "WHERE record_status='N' AND id ='" . $this->input->post('id') . "'";
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
        $this->db->update('sys_role', $data);
        $this->log_lib->write_log('Role => ' . $this->lang->sys_msg_sort_complete, json_encode($this->db->last_query()));
    }

    /* Form Add Role Index */

    public function add_role()
    {
        $this->data['topbar'] = 'main/layout/topbar';
        $this->data['sidebar'] = 'main/layout/sidebar';
        $this->data['footer'] = 'main/layout/footer';
        /* Get Current Module */
        $parent_module = $this->efs_lib->get_parent_module($this->uri->uri_string());
        /* System Language */
        $this->data['lang_sys'] = $this->efs_lib->language_system();
        /* Module Language */
        $this->data['lang_module'] = $this->efs_lib->language_module(8);

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
                "name" => $this->data['lang_sys']->sys_add_role,
                "module" => '',
                "class" => 'active'
            ))
        );
        $this->data['breadcrumb'] = $this->load->view('main/layout/breadcrumb', $breadcrumbs, true);
        $this->data['module'] = 'main/sys/add_role';

        $this->load->view("main/layout/index", $this->data);
    }

    /* Add Data */

    function add()
    {
        $data = array(
            'name' => $this->input->post('name'),
            'description' => $this->input->post('description'),
            'default_module_id' => $this->input->post('default_module_id'),
            'created_username' => $this->session->userdata("user_profile")->username,
            'created_at' => date('Y-m-d H:i:s'),
            'record_status' => 'N'
        );
        if (!$this->db->insert('sys_role', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_add);
            /* Create Log Data  */
            $this->log_lib->write_log('error-role-add=> ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        } else {
            /* Get Last role ID */
            $role_id = $this->db->insert_id();

            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Role => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));

            foreach ($_POST['module_ids'] as $module_id) {
                $permission = array(
                    'module_id' => $module_id,
                    'permission_id' => '1',
                    'role_id' => $role_id,
                    'created_username' => $this->session->userdata("user_profile")->username,
                    'created_at' => date('Y-m-d H:i:s'),
                    'record_status' => 'N'
                );
                $this->db->insert('sys_permission_module_role', $permission);
            }
        }
        redirect($this->agent->referrer());
    }

    /* Form Edit Role Index */

    public function edit_role()
    {
        $id = $this->efs_lib->decrypt_segment($this->uri->segment(4));

        if (empty($id))
            redirect(base_url("error404")); /* IF Return Null Redirect to default URL */

        $this->data['topbar'] = 'main/layout/topbar';
        $this->data['sidebar'] = 'main/layout/sidebar';
        $this->data['footer'] = 'main/layout/footer';
        /* Get Current Module */
        $parent_module = $this->efs_lib->get_parent_module($this->uri->uri_string());
        /* Get Current Role */
        $this->data['role'] = $this->db->select('*')->from('sys_role')->where('record_status', 'N')->where('id', $id)->get()->row();
        /* System Language */
        $this->data['lang_sys'] = $this->efs_lib->language_system();
        /* Module Language */
        $this->data['lang_module'] = $this->efs_lib->language_module(8);

        $sql = "SELECT DISTINCT module_id FROM sys_permission_module_role WHERE record_status='N' AND role_id ='" . $id . "'";
        $this->data['permission_module_id'] = $this->db->query($sql)->result();

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
                "name" => $this->data['lang_sys']->sys_edit_role,
                "module" => '',
                "class" => 'active'
            ))
        );
        $this->data['breadcrumb'] = $this->load->view('main/layout/breadcrumb', $breadcrumbs, true);
        $this->data['module'] = 'main/sys/edit_role';

        $this->load->view("main/layout/index", $this->data);
    }

    /* Edit Data */

    function edit()
    {
        $role_id = $this->efs_lib->decrypt_segment($this->input->post('id'));

        //var_dump($_POST['module_ids']);
        $module_roles = $this->db->query("SELECT module_id FROM sys_permission_module_role WHERE record_status ='N' AND role_id = '" . $role_id . "'")->result();
        foreach ($module_roles as $item) {
            $module_role[] = $item->module_id;
        }
        // permission role ที่ยังมีอยู่ ไม่ต้องลบออก
        $permission_role_intersect = array_intersect($_POST['module_ids'], $module_role);
        $permission_role_diff = array_diff($_POST['module_ids'], $module_role);

        $data = array(
            'description' => $this->input->post('description'),
            'default_module_id' => $this->input->post('default_module_id'),
            'updated_username' => $this->session->userdata("user_profile")->username,
            'updated_at' => date('Y-m-d H:i:s'),
            'record_status' => 'N'
        );

        $this->db->where('id', $role_id);

        if (!$this->db->update('sys_role', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-role-update=> ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Role => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));

            /* Clear Permission All */
            $this->db->set(array('updated_username' => $this->session->userdata("user_profile")->username, 'updated_at' => date('Y-m-d H:i:s'), 'record_status' => 'D'));
            $this->db->where('role_id', $role_id);
            $this->db->where('record_status', 'N');
            $this->db->where_not_in('module_id', $permission_role_intersect);
            $this->db->update('sys_permission_module_role');

            if ($role_id == 1) {/* ผู้ใช้ adminnistrator */
                $permissions = $this->db->query("SELECT * FROM sys_permission WHERE record_status ='N'")->result();
                foreach ($permissions as $permission) {
                    $modules = $this->db->query("SELECT * FROM sys_module WHERE record_status ='N'")->result();
                    foreach ($modules as $module) {
                        $item = array(
                            'module_id' => $module->id,
                            'permission_id' => $permission->id,
                            'role_id' => $role_id,
                            'created_username' => $this->session->userdata("user_profile")->username,
                            'created_at' => date('Y-m-d H:i:s'),
                            'record_status' => 'N'
                        );
                        $this->db->insert('sys_permission_module_role', $item);
                    }
                }
            } else {
                foreach ($permission_role_diff as $module_id) {
                    $permission = array(
                        'module_id' => $module_id,
                        'permission_id' => '1',
                        'role_id' => $role_id,
                        'created_username' => $this->session->userdata("user_profile")->username,
                        'created_at' => date('Y-m-d H:i:s'),
                        'record_status' => 'N'
                    );
                    $this->db->insert('sys_permission_module_role', $permission);
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
        $sql = "SELECT * FROM role_user WHERE record_status='N' AND role_id ='" . $id . "'";
        $nrow = $this->db->query($sql)->num_rows();
        if ($nrow > 0) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_delete_user . ' [' . $nrow . ']');
            /* Create Log Data  */
            $this->log_lib->write_log('error-role-delete-1=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->db->where('id', $id);
            if (!$this->db->update('sys_role', $data)) {
                $this->session->set_flashdata('type', 'error');
                $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_delete);
                /* Create Log Data  */
                $this->log_lib->write_log('error-role-delete-2=> ' . $this->session->flashdata('msg'), json_encode($data));
            } else {
                $this->session->set_flashdata('type', 'success');
                $this->session->set_flashdata('msg', $this->lang->sys_msg_delete_success);
                $this->log_lib->write_log('Role => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
            }
        }
        redirect($this->agent->referrer());
    }

    /* Child Module Index */

    public function permission()
    {
        $id = $this->efs_lib->decrypt_segment($this->uri->segment(4));

        if (empty($id))
            redirect(base_url("error404")); /* IF Return Null Redirect to default URL */

        $this->data['topbar'] = 'main/layout/topbar';
        $this->data['sidebar'] = 'main/layout/sidebar';
        $this->data['footer'] = 'main/layout/footer';
        /* Get Current Module */
        $parent_module = $this->efs_lib->get_parent_module($this->uri->uri_string());
        /* System Language */
        $this->data['lang_sys'] = $this->efs_lib->language_system();
        /* Module Language */
        $this->data['lang_module'] = $this->efs_lib->language_module(8);

        /* Get Current Role */
        $this->data['role'] = $this->db->select('*')->from('sys_role')->where('record_status', 'N')->where('id', $id)->get()->row();

        $sql = "SELECT sys_permission_module_role.module_id, sys_permission_module_role.permission_id, sys_permission.name AS permission_name "
            . "FROM sys_permission_module_role "
            . "INNER JOIN sys_permission ON sys_permission_module_role.permission_id = sys_permission.id "
            . " WHERE sys_permission_module_role.record_status ='N' AND sys_permission_module_role.role_id ='" . $id . "'";
        $permission_module_role = $this->db->query($sql)->result();
        foreach ($permission_module_role as $item) {
            $this->data[strtolower($item->permission_name) . '_' . $item->module_id] = 'T';
        }

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
                "name" => $this->data['lang_sys']->sys_assign_permission,
                "module" => '',
                "class" => 'active'
            ))
        );
        $this->data['breadcrumb'] = $this->load->view('main/layout/breadcrumb', $breadcrumbs, true);
        $this->data['module'] = 'main/sys/permission';

        $this->load->view("main/layout/index", $this->data);
    }

    /* Change Permission User */

    function permission_change()
    {
        $role_id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $sql = "SELECT id, name "
            . "FROM sys_permission "
            . "WHERE (is_active = 1) "
            . "AND (record_status = 'N') "
            . "AND id NOT IN('1')"
            . "ORDER BY sort";
        foreach ($this->db->query($sql)->result() as $item_permis) {
            $modules = array();
            if (!isset($_POST[strtolower($item_permis->name)])) {
                $data = array(
                    'updated_at' => date('Y-m-d H:i:s'),
                    'record_status' => 'D'
                );
                $this->db->where('role_id', $role_id);
                $this->db->where('permission_id', $item_permis->id);
                $this->db->where_not_in('permission_id', 1);
                $this->db->update('sys_permission_module_role', $data);
                continue;
            }
            foreach ($_POST[strtolower($item_permis->name)] as $module_id) {
                $modules[] = $module_id;
                $sql = "SELECT * FROM sys_permission_module_role "
                    . "WHERE record_status = 'N' "
                    . "AND role_id = '" . $role_id . "' "
                    . "AND module_id = '" . $module_id . "' "
                    . "AND permission_id ='" . $item_permis->id . "'";
                $nrow = $this->db->query($sql)->num_rows();
                if ($nrow == 0) {
                    $data = array(
                        'module_id' => $module_id,
                        'permission_id' => $item_permis->id,
                        'role_id' => $role_id,
                        'created_username' => $this->session->userdata("user_profile")->username,
                        'created_at' => date('Y-m-d H:i:s'),
                        'record_status' => 'N'
                    );
                    $this->db->insert('sys_permission_module_role', $data);
                }
                $data = array(
                    'updated_at' => date('Y-m-d H:i:s'),
                    'record_status' => 'D'
                );
            }
            $this->db->where('role_id', $role_id);
            $this->db->where('permission_id', $item_permis->id);
            $this->db->where_not_in('module_id', $modules);
            $this->db->where_not_in('permission_id', 1);
            $this->db->update('sys_permission_module_role', $data);
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Permission Module Role => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }

        redirect($this->agent->referrer());
    }

    /* <!-- ============================================================== -->
      <!-- Function -->
      <!-- ============================================================== --> */

    function ajax_exist()
    {
        /* Check Exist Role name */
        header('Access-Control-Allow-Origin: *');
        ini_set('max_execution_time', 300);
        $value = $_REQUEST["value"];
        $sql = "SELECT id FROM sys_role WHERE record_status='N' AND name ='" . trim($_REQUEST["value"]) . "'";
        $nrows = $this->db->query($sql)->num_rows();
        if ($nrows > 0) {
            echo json_encode(
                array(
                    "value" => $_REQUEST["value"],
                    "valid" => 0,
                    "message" => "<span class='text-danger'>" . $value . "</span> " . $this->lang->sys_msg_already_data
                )
            );
        } else {
            echo json_encode(
                array(
                    "value" => $_REQUEST["value"],
                    "valid" => 1,
                    "message" => ""
                )
            );
        }
    }
}
