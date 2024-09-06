<?php

defined('BASEPATH') or exit('No direct script access allowed');

class Remote extends CI_Controller
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
        // echo '<pre>';
        // print_r($module);
        // echo '</pre>';
        // die();

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
        $this->data['module'] = 'main/remote/remote';

        $this->load->view("main/layout/index", $this->data);
    }

    public function exec_spupdatestate()
    {
        $this->db->query("exec sp_updatestats");
        return $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'success')));
    }

    public function get_dataTable()
    {
        $this->db->select('*');
        $this->db->from('vw_material');
        $query = $this->db->get();
        $data = $query->result_array();
        return $this->output->set_content_type('application/json')->set_output(json_encode($data));
    }

    public function see_id()
    {
        $pr = $this->input->post('PRsearch');
        $po = $this->input->post('POsearch');
        $pr_id = '';
        $po_id = '';
        if ($pr != '') {
            $this->db->select('id');
            $this->db->from('sto_pr');
            $this->db->where('no', $pr);
            $query = $this->db->get()->result();
            $pr_id = $query[0]->id;
        }
        if ($po != '') {
            $this->db->select('id');
            $this->db->from('pur_po');
            $this->db->where('no', $po);
            $query2 = $this->db->get()->result();
            $po_id = $query2[0]->id;
        }
        return $this->output->set_content_type('application/json')->set_output(json_encode(array('sto_pr' => $pr_id, 'pur_po' => $po_id, 'status' => 'success')));
    }

    public function del_pr_khem()
    {
        $this->db->where('created_username', 'childxor');
        $this->db->delete('sto_pr');

        $this->db->where('created_username', 'childxor');
        $this->db->delete('sto_pr_detail');
        return $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'success')));
    }
    public function get_material_type_id()
    {
        $this->db->select('*');
        $this->db->from('sto_material_type');
        $this->db->where('record_status', 'N');
        $this->db->order_by('code', 'ASC');
        $query = $this->db->get()->result();

        $this->db->select('*');
        $this->db->from('mas_unit');
        $this->db->where('record_status', 'N');
        $this->db->order_by('name_th', 'ASC');
        $query2 = $this->db->get()->result();

        $this->db->select('*');
        $this->db->from('mas_acc_department');
        $this->db->where('record_status', 'N');
        $this->db->where('is_active', '1');
        $this->db->where_not_in('id', array(90, 91));
        $this->db->order_by('code');
        $query3 = $this->db->get()->result();

        $this->db->select("vw_material.is_active,vw_material.material_code AS material_type, vw_material.material_name AS name_th, SUM(vw_qty_material.qty) AS qty, vw_material_1.id AS material_id, avg(vw_qty_material.price_per_unit) as price_per_unit");
        $this->db->from("vw_qty_material");
        $this->db->join("vw_material", "vw_qty_material.material_id = vw_material.id", "INNER");
        $this->db->join("vw_material AS vw_material_1", "vw_material.material_code = vw_material_1.material_code", "FULL OUTER");
        $this->db->group_by("vw_material.is_active,vw_material.material_code, vw_material.material_name, vw_material.id, vw_material_1.id");
        $this->db->order_by("vw_material.material_code", "asc");
        // $this->db->limit(80);
        // die
        $result = $this->db->get()->result();
        $data = array();
        if (!empty($result)) {
            foreach ($result as $key => $value) {
                $data[$key]['material_id'] = $value->material_id;
                $data[$key]['material_type'] = $value->material_type;
                $data[$key]['name_th'] = $value->name_th;
                $data[$key]['price_per_unit'] = $value->price_per_unit;
                $data[$key]['qty'] = $value->qty;
                $data[$key]['is_active'] = $value->is_active;
            }
        } else {
            $data = array();
        }

        return $this->output->set_content_type('application/json')->set_output(json_encode(array('sto_material_type' => $query, 'mas_unit' => $query2, 'mas_acc_department' => $query3, 'vw_qty_material' => $data, 'status' => 'success')));
    }

    public function get_material_type()
    {
        $this->db->select('*');
        $this->db->from('sto_material_type');
        $this->db->where('record_status', 'N');
        $this->db->order_by('code', 'ASC');
        $query = $this->db->get()->result();
        return $this->output->set_content_type('application/json')->set_output(json_encode(array('sto_material_type' => $query, 'status' => 'success')));
    }
}
