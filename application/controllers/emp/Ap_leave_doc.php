<?php

defined('BASEPATH') or exit('No direct script access allowed');

class Ap_leave_doc extends CI_Controller
{

    function __construct()
    {
        /* Call the Model constructor */
        parent::__construct();
        /* Get System Variable */
        $this->data['var'] = $this->efs_lib->get_var_sys();
        /* System Language */
        $this->lang = $this->efs_lib->language_system();
        $this->load->library('pdf');

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
                "class" => ''
            )),
            array(2 => array(
                "name" => $module->{"name_" . $this->session->userdata("user_profile")->cng_lang},
                "module" => '',
                "class" => 'active'
            ))
        );
        $this->db = $this->load->database('HQMS_IPS', TRUE);
        $this->db->select('doorID, doorName');
        $this->db->from('PubDoor');
        $this->data['Door'] = $this->db->get()->result();
        $this->db = $this->load->database('EFINS', TRUE);
        $this->data['breadcrumb'] = $this->load->view('main/layout/breadcrumb', $breadcrumbs, true);
        $this->data['module'] = 'main/emp/ap_leave_doc';
        $this->load->view("main/layout/index", $this->data);
    }

    public function print_r($data)
    {
        echo '<pre>';
        print_r($data);
        echo '</pre>';
        die();
    }


    public function get_data()
    {
        // ดึงข้อมูลทั้งหมดจากตาราง emp_leave
        $this->db->select('emp_leave.id, emp_leave.personid, emp_leave.cardnumber, emp_leave.deptname, emp_leave.leavedate, emp_leave.leaveduration, emp_leave.leaveunit, emp_leave.leavetype, emp_leave.leavepath, 
                                 emp_leave.leavereason, emp_leave.approver, emp_leave.created_at, emp_leave.created_username, emp_leave.updated_at, emp_leave.updated_username, emp_leave.record_status, Person.name, Person.personID AS Expr1,
                                  Person.serialNumber, Person.cardNumber AS Expr2, Person.rowAutoID, Person.englishName');
        $this->db->from('emp_leave');
        $this->db->join('Person', 'emp_leave.personid = Person.cardNumber', 'left');
        // เพิ่มเงื่อนไข filter ถ้ามี
        if ($this->input->post('dateRange') && $this->input->post('select_all_dates') === 'false') {
            $dates = explode(' - ', $this->input->post('dateRange'));
            $this->db->where('leavedate >=', date('Y-m-d', strtotime($dates[0])));
            $this->db->where('leavedate <=', date('Y-m-d', strtotime($dates[1])));
        }
        if ($this->input->post('department')) {
            $this->db->where('deptname', $this->input->post('department'));
        }
        if ($this->input->post('leaveType')) {
            $this->db->where('leavetype', $this->input->post('leaveType'));
        }
        if ($this->input->post('status')) {
            $this->db->where('leavestatus', $this->input->post('status'));
        }
        // die($this->db->get_compiled_select());

        $query = $this->db->get();
        $data['data'] = $query->result();

        return $this->output->set_content_type('application/json')->set_output(json_encode($data));
    }

    public function updateStatus()
    {
        $id = $this->input->post('id');
        $action = $this->input->post('action');

        $status = ($action === 'approve') ? 'A' : 'R'; // A for Approved, R for Rejected

        $this->db->where('id', $id);
        $result = $this->db->update('emp_leave', [
            'record_status' => $status,
            'updated_at' => date('Y-m-d H:i:s'),
            'updated_username' => $this->session->userdata('username') // ถ้าคุณใช้ session สำหรับเก็บ username
        ]);

        if ($result) {
            echo json_encode(['success' => true]);
        } else {
            echo json_encode(['success' => false, 'message' => 'ไม่สามารถอัพเดตสถานะได้']);
        }
    }

    public function getLeaveType()
    {
        $this->db->select('leavetype');
        $this->db->from('emp_leave');
        $this->db->group_by('leavetype');
        $query = $this->db->get();
        $data = $query->result();

        return $this->output->set_content_type('application/json')->set_output(json_encode($data));
    }


    public function loadPdf($id = NULL)
    {
        if ($id === NULL) {
            $id = $this->uri->segment(4);
        }


        $this->db->select('*');
        $this->db->from('emp_leave');
        $this->db->where('id', $id);
        $result = $this->db->get()->row();

        if (!$result) {
            show_error('ไม่พบข้อมูลสำหรับ ID ที่ระบุ', 404);
            return;
        }

        // สร้าง HTML สำหรับ PDF
        // $html = $this->load->view('pdf_template', $result, TRUE);
        $html = $this->generateHtml($result);

        // สร้างและแสดง PDF
        // die($html);
        try {
            // สร้างและแสดง PDF
            $this->pdf->generatePDF($html, 'leave_doc.pdf', FALSE, 'A4', 'portrait');
        } catch (Exception $e) {
            // แสดงข้อผิดพลาดที่เกิดขึ้น
            echo 'เกิดข้อผิดพลาด: ' . $e->getMessage();
        }
    }


    private function generateHtml($data)
    {
        // สร้าง HTML string
        $html = '
        <!DOCTYPE html>
        <html lang="th">
        <head>
            <meta charset="UTF-8">
            <title>ใบลา</title>
            <style>
                body { font-family: "Garuda", sans-serif; }
                .header { text-align: center; margin-bottom: 20px; }
                .content { margin-bottom: 20px; }
                .footer { text-align: right; }
            </style>
        </head>
        <body>
            <div class="header">
                <h1>ใบลา</h1>
            </div>
            <div class="content">
                <p><strong>ชื่อ-นามสกุล:</strong> ' . ($data->name ?? '') . '</p>
                <p><strong>แผนก:</strong> ' . ($data->deptname ?? '') . '</p>
                <p><strong>วันที่ลา:</strong> ' . ($data->leavedate ?? '') . '</p>
                <p><strong>ประเภทการลา:</strong> ' . ($data->leavetype ?? '') . '</p>
                <p><strong>เหตุผล:</strong> ' . ($data->leavereason ?? '') . '</p>
            </div>
            <div class="footer">
                <p>ลงชื่อ: ________________________</p>
                <p>วันที่: ________________________</p>
            </div>
        </body>
        </html>';

        return $html;
    }
}
