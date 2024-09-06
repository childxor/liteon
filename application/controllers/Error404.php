<?php

defined('BASEPATH') or exit('No direct script access allowed');

class Error404 extends CI_Controller
{

    function __construct()
    {
        /* Call the Model constructor */
        parent::__construct();
        $this->data['var'] = $this->efs_lib->get_var_sys();
    }

    public function index()
    {
        $this->load->view("errors/error404", $this->data);
    }
}
