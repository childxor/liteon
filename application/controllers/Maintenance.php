<?php

defined('BASEPATH') or exit('No direct script access allowed');

class Maintenance extends CI_Controller
{

    function __construct()
    {
        /* Call the Model constructor */
        parent::__construct();
        $this->data['var'] = $this->efs_lib->get_var_sys();
    }

    /* เปิดหน้าปิดปรับปรุงระบบเพื่อแสดงข้อมูล */
    public function index()
    {
        $this->load->view("errors/maintenance", $this->data);
    }
}
