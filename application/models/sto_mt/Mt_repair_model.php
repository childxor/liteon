<?php
class Mt_repair_model extends CI_Model
{

    function __construct()
    {
        parent::__construct();
    }

    function print_r($data)
    {
        echo "<pre>";
        print_r($data);
        echo "</pre>";
        die();
    }

    function get_repair($id)
    {
        $this->db->where('id', $id);
        $query = $this->db->get('mt_repair');
        return $query->row();
    }

    // โหลดข้อมูล department ทั้งหมด
    function get_sub_department()
    {
        $this->db->where('record_status', 'N'); // ต้องเป็น N เท่านั้น
        $this->db->order_by('name', 'ASC');
        $query = $this->db->get('mas_sub_department');
        return $query->result();
    }

    function get_data($data)
    {
        // $this->print_r($data);
        // month_report: 2023-12
        $this->db->select('mas_department.name as department_name,sto_mt_report.id, sto_mt_report.report_no, 
        sto_mt_report.description, sto_mt_report.remark, sto_mt_report.equipment_id , 
        sto_mt_report.description, sto_mt_report.severity, sto_mt_report.remark, sto_mt_report.issue_status, 
        sys_user.first_name, sys_user.last_name, sto_mt_report.remark, sto_mt_report.issue_date,sto_mt_report.sub_department_name');
        $this->db->from('sto_mt_report');
        $this->db->join('sys_user', 'sto_mt_report.issue_by = sys_user.id', 'inner');
        $this->db->join('mas_department', 'sto_mt_report.department_id = mas_department.id', 'inner');
        $this->db->where('sto_mt_report.department_id', $this->session->userdata('user_profile')->department_id);
        if ($data['issue_status'] != '') {
            $this->db->where('sto_mt_report.issue_status', $data['issue_status']);
        } else {
            $this->db->where("sto_mt_report.issue_status not in ('DRP','FN')");
        }
        if ($data['month_report'] != '') {
            $this->db->where('MONTH(sto_mt_report.issue_date)', date('m', strtotime($data['month_report'])));
            $this->db->where('YEAR(sto_mt_report.issue_date)', date('Y', strtotime($data['month_report'])));
        }
        $this->db->order_by('sto_mt_report.issue_date', 'ASC');
        // die($this->db->get_compiled_select());
        $data = [];
        foreach ($this->db->get()->result() as $key => $value) {
            // $data['data'][$key]['id'] = $this->efs_lib->encrypt_segment($value->id);
            // $data['data'][$key]['department_name'] = $value->department_name;
            // $data['data'][$key]['report_no'] = $value->report_no;
            // $data['data'][$key]['description'] = $value->description;
            // $data['data'][$key]['remark'] = $value->remark;
            // $data['data'][$key]['severity'] = $value->severity;
            // $data['data'][$key]['issue_status'] = $value->issue_status;
            // $data['data'][$key]['issue_by'] = $value->first_name . ' ' . $value->last_name;
            // $data['data'][$key]['issue_date'] = date('d/m/Y', strtotime($value->issue_date));
            // $data['data'][$key]['sub_department_name'] = $value->sub_department_name;
            $data[$key]['id'] = $this->efs_lib->encrypt_segment($value->id);
            $data[$key]['department_name'] = $value->department_name;
            $data[$key]['report_no'] = $value->report_no;
            $data[$key]['description'] = $value->description;
            $data[$key]['remark'] = $value->remark;
            $data[$key]['severity'] = $value->severity;
            $data[$key]['issue_status'] = $value->issue_status;
            $data[$key]['issue_by'] = $value->first_name . ' ' . $value->last_name;
            $data[$key]['issue_date'] = $value->issue_date;
            $data[$key]['sub_department_name'] = $value->sub_department_name;
            $data[$key]['equipment_id'] = $value->equipment_id;
        }
        return $data;
    }

    function add_repair($data)
    {
        // $this->print_r($data);
        $month = date('m', strtotime($data['date_start']));
        $year = date('Y', strtotime($data['date_start']));

        $this->db->select('count(*) as count');
        $this->db->where('MONTH(issue_date)', $month);
        $this->db->where('YEAR(issue_date)', $year);
        $query = $this->db->get('sto_mt_report')->row();

        if ($query->count > 0) {
            $issue_no = "RPMT" . $month . date('y', strtotime($data['date_start'])) . sprintf("%04d", $query->count + 1);
        } else {
            $issue_no = "RPMT" . $month . date('y', strtotime($data['date_start'])) . "0001";
        }
        $eqid = '';
        foreach ($data['equipment'] as $key => $value) {
            $eqid .= $value . ',';
        }
        $eqid = substr($eqid, 0, -1);
        $data = array(
            'report_no' => $issue_no,
            'department_id' => $this->session->userdata('user_profile')->department_id,
            'sub_department_name' => $data['sub_department_name'],
            'email' => $data['email'], // อีเมล์
            'tel' => (@$data['tel'] == '') ? null : $data['tel'], // เบอร์โทรศัพท์
            'issue_by' => $this->session->userdata('user_profile')->id,
            'issue_date' => $data['date_start'],
            // 'equipment_id' => $eqid,
            'description' => $data['issue_text'],
            'remark' => $data['notes'],
            'severity' => $data['severity'], // ความรุนแรง 
            'issue_status' => 'R',
            'created_username' => $this->session->userdata('user_profile')->username,
            'created_at' => date('Y-m-d H:i:s'),
        );
        if (!$this->db->insert('sto_mt_report', $data)) {
            return false;
        } else {
            // SELECT TOP (200) id, report_id, equipment_id, status, remark, check_by, remark_detail, created_at, created_username, updated_at, updated_username, record_status
            // FROM sto_mt_report_detail
            $id = $this->db->insert_id();
            $data['equipment'] = explode(',', $eqid);
            foreach ($data['equipment'] as $key => $value) {
                $data = array(
                    'report_id' => $id,
                    'equipment_id' => $value,
                    'created_username' => $this->session->userdata('user_profile')->username,
                    'created_at' => date('Y-m-d H:i:s'),
                );
                if (!$this->db->insert('sto_mt_report_detail', $data)) {
                    $error = $this->db->error();
                    return false;
                }
            }
            return true;
        }
    }

    function edit_repair($data)
    {
        $this->print_r($data);
        $data = array(
            'sub_department_id' => $data['sub_department'],
            'email' => $data['email'], // อีเมล์
            'tel' => $data['tel'], // เบอร์โทรศัพท์
            'issue_date' => $data['date_start'],
            'equipment' => $data['equipment'],
            'description' => $data['issue_text'],
            'remark' => $data['notes'],
            'severity' => $data['severity'], // ความรุนแรง
            'updated_username' => $this->session->userdata('user_profile')->username,
            'updated_at' => date('Y-m-d H:i:s'),
        );
        $this->db->where('id', $data['id']);
        if (!$this->db->update('sto_mt_report', $data)) {
            return false;
        } else {
            return true;
        }
    }
}
