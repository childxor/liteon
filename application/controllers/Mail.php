<?php
defined('BASEPATH') or exit('No direct script access allowed');

class Mail extends CI_Controller
{

    function __construct()
    {
        /* Call the Model constructor */
        parent::__construct();
        /* Get System Variable */
        $this->data['var'] = $this->efs_lib->get_var_sys();

        /* Check Authen & Permission User */
        /* ตรวจสอบการเข้าระบบและสิทธิการใช้งานของผู้ใช้ */
        if (empty($this->session->userdata("user_profile")->id)) {
            redirect('authen', 'refresh');
            exit();
        } else {
            /* Get Permission Module */
            /* ดึงค่าสิทธิในโมดูล */
            $this->data['permission'] = @$this->efs_lib->get_permission($this->uri->uri_string());
        }
    }

    public function index()
    {
        echo 'No direct script access allowed';
    }

    /* ส่งอีเมลออกโดยผู้ใช้งาน */
    public function send()
    {
        $this->load->helper('mail');
        $send_from = $this->session->userdata("user_profile")->first_name . ' ' . $this->session->userdata("user_profile")->last_name . ' (' . $this->session->userdata("user_profile")->email . ')';

        $send_to = '';
        $send_name = '';

        $send_subj = '';
        $send_body = '';

        $file_attach = '';

        $error = send_mail($send_from, $send_to, $send_name, $send_subj, $send_body, $file_attach);

        if (!empty($error)) {
            echo $error;
        }
    }
}
