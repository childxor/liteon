<?php
defined('BASEPATH') or exit('No direct script access allowed');

// Code written by TangleSkills

class Qrimages extends CI_Controller
{

    public function __construct()
    {
        parent::__construct();
        $this->load->helper('url');
    }

    public function index()
    {
    }

    /* สร้าง QR CODE จากค่าที่ส่งผ่านทาง URL */
    public function generate()
    {
        $this->load->library('ciqrcode');
        $this->ciqrcode->png($this->uri->segment(3));
    }
}
