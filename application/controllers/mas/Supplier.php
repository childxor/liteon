<?php

defined('BASEPATH') or exit('No direct script access allowed');

class Supplier extends CI_Controller
{

    public function __construct()
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
                "class" => '',
            )),
            array(2 => array(
                "name" => $module->{"name_" . $this->session->userdata("user_profile")->cng_lang},
                "module" => '',
                "class" => 'active',
            )),
        );
        $this->data['breadcrumb'] = $this->load->view('main/layout/breadcrumb', $breadcrumbs, true);
        $this->data['module'] = 'main/mas/supplier';

        $this->load->view("main/layout/index", $this->data);
    }

    /* Get Data to List Table */

    public function ajax_list()
    {
        header('Access-Control-Allow-Origin: *');
        ini_set('max_execution_time', 300);
        $sql = "SELECT id, contact, tel, name, description, is_active, created_username "
            . "FROM mas_supplier "
            . "WHERE record_status='N' "
            . "ORDER BY id ASC";
        $data['data'] = $this->db->query($sql)->result();
        /* Init $items NULL */
        $items['data'] = array();
        foreach ($data['data'] as $key => $item) {
            $items['data'][$key]['id'] = $this->efs_lib->encrypt_segment($item->id);
            $items['data'][$key]['code'] = $item->id;
            $items['data'][$key]['contact'] = $item->contact;
            $items['data'][$key]['tel'] = $item->tel;
            $items['data'][$key]['name'] = $item->name;
            $items['data'][$key]['description'] = $item->description;
            $items['data'][$key]['is_active'] = $item->is_active;
            $items['data'][$key]['created_username'] = $item->created_username;
        }
        echo json_encode($items);
    }

    /* Get Data to Item */

    public function ajax_data()
    {
        header('Access-Control-Allow-Origin: *');
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $sql = "SELECT * "
            . "FROM mas_supplier "
            . "WHERE record_status='N' AND id ='" . $id . "'";
        $data = $this->db->query($sql)->row();
        $data->id = $this->efs_lib->encrypt_segment($data->id);
        echo json_encode($data);
    }

    /* Add Data */

    public function add()
    {
        $data = array(
            'name' => $this->input->post('name'),
            'address' => $this->input->post('address'),
            'contact' => $this->input->post('contact'),
            'tel' => $this->input->post('tel'),
            'fax' => $this->input->post('fax'),
            'description' => $this->input->post('description'),
            'created_username' => $this->session->userdata("user_profile")->username,
            'created_at' => date('Y-m-d H:i:s'),
            'record_status' => 'N',
        );
        if (!$this->db->insert('mas_supplier', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_add);
            /* Create Log Data  */
            $this->log_lib->write_log('error-08-001=> ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Supplier => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Edit Data */

    public function edit()
    {
        $data = array(
            'name' => $this->input->post('name'),
            'address' => $this->input->post('address'),
            'contact' => $this->input->post('contact'),
            'tel' => $this->input->post('tel'),
            'fax' => $this->input->post('fax'),
            'description' => $this->input->post('description'),
            'updated_username' => $this->session->userdata("user_profile")->username,
            'updated_at' => date('Y-m-d H:i:s'),
        );
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $this->db->where('id', $id);
        if (!$this->db->update('mas_supplier', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-08-002=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Supplier => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Delete Data */

    public function delete()
    {
        $id = $this->efs_lib->decrypt_segment($this->uri->segment(4));
        $data = array(
            'record_status' => 'D',
            'updated_username' => $this->session->userdata("user_profile")->username,
            'updated_at' => date('Y-m-d H:i:s'),
        );
        $this->db->where('id', $id);
        if (!$this->db->update('mas_supplier', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_delete);
            /* Create Log Data  */
            $this->log_lib->write_log('error-08-003=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_delete_success);
            $this->log_lib->write_log('Supplier => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Change Active Data */

    public function change_active()
    {
        $status = explode('.', $this->uri->segment(4));
        $data['is_active'] = ($status[1] == 1 ? 0 : 1);
        $id = $this->efs_lib->decrypt_segment($status[0]);
        $this->db->where('id', $id);
        if (!$this->db->update('mas_supplier', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-08-004=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Supplier => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Address Book Index */

    public function address_book()
    {
        $supplier_id = $this->efs_lib->decrypt_segment($this->uri->segment(4));
        if (empty($supplier_id)) {
            redirect(base_url("error404"));
        }
        /* IF Return Null Redirect to default URL */

        $this->data['topbar'] = 'main/layout/topbar';
        $this->data['sidebar'] = 'main/layout/sidebar';
        $this->data['footer'] = 'main/layout/footer';
        /* Get Current Module */
        $parent_module = $this->efs_lib->get_parent_module($this->uri->uri_string());

        /* Get Parent Module For Address Book */
        $sql = "SELECT * FROM mas_supplier WHERE record_status = 'N' ";
        $sql .= "AND id = '" . $supplier_id . "'";
        $supplier = $this->db->query($sql)->row();

        /* System Language */
        $this->data['lang_sys'] = $this->efs_lib->language_system();
        /* Module Language */
        $this->data['lang_module'] = $this->efs_lib->language_module(13);

        $breadcrumbs['breadcrumb'] = array(
            array(1 => array(
                "name" => '<i class="mdi mdi-home font-20" style="line-height: 20px;"></i>',
                "module" => base_url(),
                "class" => '',
            )),
            array(2 => array(
                "name" => $parent_module->{"name_" . $this->session->userdata("user_profile")->cng_lang},
                "module" => base_url($parent_module->module),
                "class" => '',
            )),
            array(3 => array(
                "name" => $supplier->name,
                "module" => '',
                "class" => 'active',
            )),
        );
        $this->data['breadcrumb'] = $this->load->view('main/layout/breadcrumb', $breadcrumbs, true);
        $this->data['module'] = 'main/mas/address_book';

        $this->load->view("main/layout/index", $this->data);
    }

    public function address_book_ajax_data()
    {
        header('Access-Control-Allow-Origin: *');
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $sql = "SELECT * "
            . "FROM mas_supplier_contact_list "
            . "WHERE record_status='N' AND id ='" . $id . "'";
        $data = $this->db->query($sql)->row();
        $data->id = $this->efs_lib->encrypt_segment($data->id);
        echo json_encode($data);
    }

    /* Get Data to List Table */

    public function address_book_ajax_list()
    {
        header('Access-Control-Allow-Origin: *');
        ini_set('max_execution_time', 300);
        $sql = "SELECT id, supplier_id, name, email, description, tel, contact_group, remark, is_active, created_username "
            . "FROM  mas_supplier_contact_list "
            . "WHERE record_status='N' "
            . "AND supplier_id ='" . $this->efs_lib->decrypt_segment($this->uri->segment(4)) . "'";
        $sql .= "ORDER BY name ASC";
        $data['data'] = $this->db->query($sql)->result();
        /* Init $items NULL */
        $items['data'] = array();
        foreach ($data['data'] as $key => $item) {
            $items['data'][$key]['id'] = $this->efs_lib->encrypt_segment($item->id);
            $items['data'][$key]['supplier_id'] = $item->supplier_id;
            $items['data'][$key]['name'] = $item->name;
            $items['data'][$key]['email'] = $item->email;
            $items['data'][$key]['tel'] = $item->tel;
            $items['data'][$key]['description'] = $item->description;
            $items['data'][$key]['remark'] = $item->remark;
            $items['data'][$key]['contact_group'] = $item->contact_group;
            $items['data'][$key]['is_active'] = $item->is_active;
            $items['data'][$key]['created_username'] = $item->created_username;
        }
        echo json_encode($items);
    }

    public function table_select_item()
    {

        /* Get Material From Group Data to Select Modal */
        header('Access-Control-Allow-Origin: *');
        ini_set('max_execution_time', 300);
        $table = '';
        $sql = "SELECT * "
            . "FROM mas_supplier_contact_list "
            . "WHERE record_status='N' AND supplier_id IS NULL ";
        $data['sql'] = $sql;
        if ($items = $this->db->query($sql)->num_rows() > 0) {
            $items = $this->db->query($sql)->result();
            foreach ($items as $i => $item) {
                $table .= '<tr>' .
                    '<td class="text-center"><input type="checkbox" class="selectItem" name="checked[]" value="' . $item->id . '"></td>'
                    . '<td class="font-12" style="max-width:400px">' . $item->name . '</td>'
                    . '<td class="font-12" style="max-width:400px">' . $item->email . '</td>'
                    . '<td class="font-12" style="max-width:400px">' . $item->tel . '</td>';
                $table .= '</tr>';
            }
        } else {
            $table .= '<tr>' .
                '<td class="text-center" colspan="4">-- No Data Found --</td>';
            $table .= '</tr>';
        }
        $data['table'] = $table;
        echo json_encode($data);
    }

    public function address_book_add()
    {
        /* var_dump($this->input->post());die(); */
        $supplier_id = $this->efs_lib->decrypt_segment($this->input->post('supplier_id'));
        $data['supplier_id'] = $supplier_id;
        if ($this->input->post('checked')) {
            foreach ($this->input->post('checked') as $id) {
                $this->db->where('id', $id);
                $this->db->update('mas_supplier_contact_list', $data);
            }
        } else {
            $data['name'] = $this->input->post('name');
            $data['email'] = $this->input->post('email');
            $data['tel'] = $this->input->post('tel');
            $data['description'] = $this->input->post('description');
            $this->db->insert('mas_supplier_contact_list', $data);
        }
        redirect($this->agent->referrer());
    }

    /* Edit Data */

    public function address_book_edit()
    {
        $data = array(
            'name' => $this->input->post('name'),
            'email' => $this->input->post('email'),
            'tel' => $this->input->post('tel'),
            'description' => $this->input->post('description'),
            'updated_username' => $this->session->userdata("user_profile")->username,
            'updated_at' => date('Y-m-d H:i:s'),
        );
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $this->db->where('id', $id);
        if (!$this->db->update('mas_supplier_contact_list', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-08-005=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Supplier => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Delete Data */

    public function address_book_delete()
    {
        $id = $this->efs_lib->decrypt_segment($this->uri->segment(4));
        $data = array(
            'record_status' => 'D',
            'updated_username' => $this->session->userdata("user_profile")->username,
            'updated_at' => date('Y-m-d H:i:s'),
        );
        $this->db->where('id', $id);
        if (!$this->db->update('mas_supplier_contact_list', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_delete);
            /* Create Log Data  */
            $this->log_lib->write_log('error-08-006=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_delete_success);
            $this->log_lib->write_log('Supplier => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Change Active Data */

    public function address_book_change_active()
    {
        $status = explode('.', $this->uri->segment(4));
        $data['is_active'] = ($status[1] == 1 ? 0 : 1);
        $id = $this->efs_lib->decrypt_segment($status[0]);
        $this->db->where('id', $id);
        if (!$this->db->update('mas_supplier_contact_list', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-08-007=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Supplier => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Money Book Index */

    public function money_book()
    {
        $supplier_id = $this->efs_lib->decrypt_segment($this->uri->segment(4));
        if (empty($supplier_id)) {
            redirect(base_url("error404"));
        }
        /* IF Return Null Redirect to default URL */

        $this->data['topbar'] = 'main/layout/topbar';
        $this->data['sidebar'] = 'main/layout/sidebar';
        $this->data['footer'] = 'main/layout/footer';
        /* Get Current Module */
        $parent_module = $this->efs_lib->get_parent_module($this->uri->uri_string());

        /* Get Parent Module For Address Book */
        $sql = "SELECT * FROM mas_supplier WHERE record_status = 'N' ";
        $sql .= "AND id = '" . $supplier_id . "'";
        $supplier = $this->db->query($sql)->row();

        /* System Language */
        $this->data['lang_sys'] = $this->efs_lib->language_system();
        /* Module Language */
        $this->data['lang_module'] = $this->efs_lib->language_module(13);

        $breadcrumbs['breadcrumb'] = array(
            array(1 => array(
                "name" => '<i class="mdi mdi-home font-20" style="line-height: 20px;"></i>',
                "module" => base_url(),
                "class" => '',
            )),
            array(2 => array(
                "name" => $parent_module->{"name_" . $this->session->userdata("user_profile")->cng_lang},
                "module" => base_url($parent_module->module),
                "class" => '',
            )),
            array(3 => array(
                "name" => $supplier->name,
                "module" => '',
                "class" => 'active',
            )),
        );
        $this->data['breadcrumb'] = $this->load->view('main/layout/breadcrumb', $breadcrumbs, true);
        $this->data['module'] = 'main/mas/money_book';

        $this->load->view("main/layout/index", $this->data);
    }

    /* Get Data to List Table */

    public function money_book_ajax_list()
    {
        header('Access-Control-Allow-Origin: *');
        ini_set('max_execution_time', 300);
        $datetime_lang = 'datetime_to_' . ($this->session->userdata("user_profile")->cng_lang == 'th' ? 'th' : 'en');
        $sql = "SELECT id, supplier_id, name_th, price, material_id, remark, is_active, created_username, created_at, updated_username, updated_at "
            . "FROM  mas_supplier_material_price "
            . "WHERE record_status='N' "
            . "AND supplier_id ='" . $this->efs_lib->decrypt_segment($this->uri->segment(4)) . "'";
        $sql .= "ORDER BY name_th ASC";
        $data['data'] = $this->db->query($sql)->result();
        /* Init $items NULL */
        $items['data'] = array();

        foreach ($data['data'] as $key => $item) {
            $items['data'][$key]['id'] = $this->efs_lib->encrypt_segment($item->id);
            $items['data'][$key]['supplier_id'] = $item->supplier_id;
            $items['data'][$key]['material_id'] = $item->material_id;
            $items['data'][$key]['name_th'] = $item->name_th;
            $items['data'][$key]['price'] = $item->price;
            $items['data'][$key]['remark'] = $item->remark;
            $items['data'][$key]['is_active'] = $item->is_active;
            $items['data'][$key]['created_username'] = $item->created_username;
            $items['data'][$key]['created_at'] = $this->efs_lib->$datetime_lang($item->created_at, false);
            $items['data'][$key]['updated_username'] = $item->updated_username;
            $items['data'][$key]['updated_at'] = $this->efs_lib->$datetime_lang($item->updated_at, false);
        }
        echo json_encode($items);
    }

    public function money_book_ajax_data()
    {
        header('Access-Control-Allow-Origin: *');
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $sql = "SELECT * "
            . "FROM mas_supplier_material_price "
            . "WHERE record_status='N' AND id ='" . $id . "'";
        $data = $this->db->query($sql)->row();
        $data->id = $this->efs_lib->encrypt_segment($data->id);
        echo json_encode($data);
    }

    public function money_book_add()
    {
        /* var_dump($this->input->post());die(); */
        $pre_jp = 'N*';
        $supplier_id = $this->efs_lib->decrypt_segment($this->input->post('supplier_id'));
        $data['supplier_id'] = $supplier_id;

        $data['name_th'] = $pre_jp . $this->input->post('name_th');
        $data['price'] = $pre_jp . $this->input->post('price');
        $data['remark'] = $pre_jp . $this->input->post('remark');
        $data['use_qty'] = (strtoupper($this->input->post('use_qty'))  == 'ON' ? '1' : '0');
        $data['created_username'] = $this->session->userdata("user_profile")->username;
        $data['created_at'] = date('Y-m-d H:i:s');
        $data['updated_username'] = $this->session->userdata("user_profile")->username;
        $data['updated_at'] = date('Y-m-d H:i:s');
        $this->db->query($this->efs_lib->query_insert_jp('mas_supplier_material_price', $data));

        redirect($this->agent->referrer());
    }

    /* Edit Data */

    public function money_book_edit()
    {
        /* var_dump($this->input->post()); die(); */
        $pre_jp = 'N*';

        $data = array(
            'name_th' => $pre_jp . $this->input->post('name_th'),
            'price' => $pre_jp . $this->input->post('price'),
            'remark' => $pre_jp . $this->input->post('remark'),
            'use_qty' => (strtoupper($this->input->post('use_qty'))  == 'ON' ? '1' : '0'),
            'updated_username' => $this->session->userdata("user_profile")->username,
            'updated_at' => date('Y-m-d H:i:s'),
        );
        $id = $this->efs_lib->decrypt_segment($this->input->post('id'));
        $where['id'] = $id;
        if (!$this->db->query($this->efs_lib->query_update_jp('mas_supplier_material_price', $data, $where))) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-08-008=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Supplier => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Delete Data */

    public function money_book_delete()
    {
        $id = $this->efs_lib->decrypt_segment($this->uri->segment(4));
        $data = array(
            'record_status' => 'D',
            'updated_username' => $this->session->userdata("user_profile")->username,
            'updated_at' => date('Y-m-d H:i:s'),
        );
        $this->db->where('id', $id);
        if (!$this->db->update('mas_supplier_material_price', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_delete);
            /* Create Log Data  */
            $this->log_lib->write_log('error-08-009=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_delete_success);
            $this->log_lib->write_log('Supplier => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* Change Active Data */

    public function money_book_change_active()
    {
        $status = explode('.', $this->uri->segment(4));
        $data['is_active'] = ($status[1] == 1 ? 0 : 1);
        $id = $this->efs_lib->decrypt_segment($status[0]);
        $this->db->where('id', $id);
        if (!$this->db->update('mas_supplier_material_price', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_cannot_update);
            /* Create Log Data  */
            $this->log_lib->write_log('error-08-010=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_msg_save_success);
            $this->log_lib->write_log('Supplier => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* <!-- ============================================================== -->
<!-- Function -->
<!-- ============================================================== --> */
}
