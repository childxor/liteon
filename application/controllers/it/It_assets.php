<?php

defined('BASEPATH') or exit('No direct script access allowed');

class It_assets extends CI_Controller
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
            $this->load->model('it/It_assets_model', 'it_assets_model');
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
        $this->data['module'] = 'main/it/it_assets';
        $this->data['assets'] = $this->it_assets_model->get_all_assets();
        $this->data['asset_software'] = $this->it_assets_model->get_all_asset_software();
        $this->data['asset_statuses'] = $this->it_assets_model->get_all_asset_statuses();
        $this->data['brands'] = $this->it_assets_model->get_all_brands();
        $this->data['departments'] = $this->it_assets_model->get_all_departments();
        $this->data['device_types'] = $this->it_assets_model->get_all_device_types();
        $this->data['locations'] = $this->it_assets_model->get_all_locations(); 
        $this->data['maintenance_records'] = $this->it_assets_model->get_all_maintenance_records();
        $this->data['models'] = $this->it_assets_model->get_all_models();
        $this->data['operating_systems'] = $this->it_assets_model->get_all_operating_systems();
        $this->data['software_licenses'] = $this->it_assets_model->get_all_software_licenses();
        $this->data['users'] = $this->it_assets_model->get_all_users();
        $this->data['vendors'] = $this->it_assets_model->get_all_vendors();


        $this->load->view("main/layout/index", $this->data);
    }

    public function print_r($data)
    {
        echo '<pre>';
        print_r($data);
        echo '</pre>';
        die();
    }
}
