<?php

defined('BASEPATH') or exit('No direct script access allowed');

class Upload extends CI_Controller
{

    function __construct()
    {
        /* Call the Model constructor */
        parent::__construct();
        $this->data['var'] = $this->efs_lib->get_var_sys();
        if (empty($this->session->userdata("user_profile")->id)) {
            redirect('authen', 'refresh');
            exit();
        }
    }

    public function index()
    {
        /* Return to User Default Module */
        /* ให้กลับไปยังหน้าหลักของผู้ใช้นั้น */
        redirect(base_url($this->session->userdata("default_module")));
    }

    /* View attach File */
    /* ดูไฟล์แนบในใบขอซื้อ */

    public function attach()
    {
        echo '<link rel="icon" type="image/png" sizes="16x16" href="' . base_url($this->data['var']->favicon) . '">';
        echo '<title>' . $this->data['var']->project . '</title>';
        $attach = $this->efs_lib->decrypt_segment($this->uri->segment(4));
        if (strpos($attach, ".pdf")) {
            echo '<object data="' . base_url('uploads/attach/' . $this->uri->segment(3) . '/' . $attach) . '" type="application/pdf" width="100%" height="100%"></object>';
        } else {
            $style = "<style>
            html, body
                {
                    height: 100%;
                    margin:0;
                    padding:0;
                }

                div {
                    position:relative;
                    height: 100%;
                    width:100%;
                }

                div img {
                    position:absolute;
                    top:0;
                    left:0;
                    right:0;
                    bottom:0;
                    margin:auto;
                }
            </style>";
            echo $style;
            echo '<div><img src="' . base_url('uploads/attach/' . $this->uri->segment(3) . '/' . $attach) . '" style="height: 100%; object-fit:cover;"></div>';
        }
    }

    /* View Quotation File */
    /* ดูไฟล์ใบเสนอราคา */

    public function quotation()
    {
        echo '<link rel="icon" type="image/png" sizes="16x16" href="' . base_url($this->data['var']->favicon) . '">';
        echo '<title>' . $this->data['var']->project . '</title>';
        $quotation = $this->efs_lib->decrypt_segment($this->uri->segment(4));
        if (strpos($quotation, ".pdf")) {
            echo '<object data="' . base_url('uploads/quotation/' . $this->uri->segment(3) . '/' . $this->efs_lib->decrypt_segment($this->uri->segment(4))) . '" type="application/pdf" width="100%" height="100%"></object>';
        } else {
            $style = "<style>
            html, body
                {
                    height: 100%;
                    margin:0;
                    padding:0;
                }

                div {
                    position:relative;
                    height: 100%;
                    width:100%;
                }

                div img {
                    position:absolute;
                    top:0;
                    left:0;
                    right:0;
                    bottom:0;
                    margin:auto;
                }
            </style>";
            echo $style;
            echo '<div><img src="' . base_url('uploads/quotation/' . $this->uri->segment(3) . '/' . $quotation) . '" style="height: 100%; object-fit:cover;"></div>';
        }
    }
    
    /* View Invoice File */
    /* ดูไฟล์ใบแจ้งหนี้ */

    public function invoice()
    {
        echo '<link rel="icon" type="image/png" sizes="16x16" href="' . base_url($this->data['var']->favicon) . '">';
        echo '<title>' . $this->data['var']->project . '</title>';
        $invoice = $this->efs_lib->decrypt_segment($this->uri->segment(4));
        if (strpos($invoice, ".pdf")) {
            echo '<object data="' . base_url('uploads/receipt/' . $this->uri->segment(3) . '/' . $this->efs_lib->decrypt_segment($this->uri->segment(4))) . '" type="application/pdf" width="100%" height="100%"></object>';
        } else {
            echo base_url('uploads/receipt/' . $this->uri->segment(3) . '/' . $invoice);
        }
    }
}
