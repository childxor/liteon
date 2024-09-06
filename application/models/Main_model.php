<?php
class Main_model extends CI_Model
{
    public function addmember()
    {
        // echo '<pre>';
        // print_r($_POST);
        // echo '</pre>';
        // exit();
        $config['upload_path'] = './img/';
        $config['allowed_types'] = 'gif|jpg|png';
        $config['max_size'] = '10000';
        $config['max_width'] = '10000';
        $config['max_height'] = '10000';
        // ตั้งค่าภาพที่อัพ
        $this->load->library('upload', $config);
        if (!$this->upload->do_upload('img')) {
            echo $this->upload->display_errors();
        } else {
            $datalogin = $this->upload->data();
            $filename = $datalogin['file_name'];
            $datalogin = array(
                'username' => $this->input->post('username_login'),
                'password' => $this->input->post('password_login'),
                'img' => $filename
            );

            $query = $this->db->insert('tbl_member', $datalogin);

            if ($query) {
                redirect('', 'refresh');
            } else {
                echo 'add false';
            }
        }
    }

    public function addregister()
    {
        $config['upload_path'] = './img/';
        $config['allowed_types'] = 'gif|jpg|png';
        $config['max_size'] = '10000';
        $config['max_width'] = '10000';
        $config['max_height'] = '10000';
        $config['encrypt_name'] = TRUE; //ตั้งชื่อไฟล์ภาพแบบสุ่ม

        // ตั้งค่าภาพที่อัพ
        $this->load->library('upload', $config);
        if (!$this->upload->do_upload('img')) {
            echo $this->upload->display_errors();
        } else {
            $dataregister = $this->upload->data();
            $filename = $dataregister['file_name'];
            $dataregister = array(
                'username' => $this->input->post('username'),
                'password' => $this->input->post('password'),
                'firstname' => $this->input->post('firstname'),
                'lastname' => $this->input->post('lastname'),
                'email' => $this->input->post('email'),
                'phone' => $this->input->post('phone'),
                'img' => $filename
            );

            $query = $this->db->insert('tbl_register', $dataregister);

            if ($query) {
                redirect('', 'refresh');
            } else {
                echo 'add false';
            }
        }
    }

    public function show_sto_material()
    {
        $query = $this->db->query("SELECT sto_receipt_note_detail.receipt_note_id, sto_pr_detail.material_id, SUM(sto_receipt_note_detail.qty) AS qty, sto_pr_detail.material_name, sto_receipt_note_detail.price_per_unit, DATEDIFF(day, 
        sto_receipt_note.receipt_date, CURRENT_TIMESTAMP) AS datesum
        FROM sto_receipt_note_detail INNER JOIN
        sto_receipt_note ON sto_receipt_note_detail.receipt_note_id = sto_receipt_note.id INNER JOIN
        pur_po_detail ON sto_receipt_note_detail.po_detail_id = pur_po_detail.id INNER JOIN
        sto_pr_detail ON pur_po_detail.pr_detail_id = sto_pr_detail.id
        WHERE (sto_pr_detail.material_id <> 0) AND (sto_receipt_note_detail.record_status = 'N') AND (sto_receipt_note_detail.qty <> 0)
        GROUP BY sto_pr_detail.material_id, sto_pr_detail.material_name, sto_receipt_note.receipt_date, sto_receipt_note_detail.price_per_unit, sto_receipt_note_detail.id, 
        sto_receipt_note_detail.receipt_note_id
        ORDER BY sto_receipt_note_detail.id");
        // คำสั่งดึงข้อมูลจากตาราง เฉพาะ A
        // $query = $this->db->get();
        return $query->result();
    }
    public function show_sto_issue()
    {
        $query = $this->db->query("");
        // คำสั่งดึงข้อมูลจากตาราง เฉพาะ A
        // $query = $this->db->get();
        return $query->result();
    }
    // ดึงข้อมูลจาก sto_material

    public function setRsto()
    {
        $this->db->set('issue_status',"R");
        $this->db->where('issue_status',"I");
        $this->db->update('sto_issue');
        // set sto_issue
    }

    // ดึงข้อมูลจาก register
    public function showdata3()
    {
        $this->db->select('m.*,p.*');
        $this->db->from('tbl_register as m');
        $this->db->join('tbl_position as p', 'm.pid=p.pid', 'left'); //left join แสดงข้อมูลที่ไม่มีใน fk คีย์รอง 
        // คำสั่งดึงข้อมูลจากตาราง
        $query = $this->db->get();
        return $query->result();
    }
    // ดึงข้อมูลจาก register
    public function showdata4()
    {
        $this->db->select('m.id,m.img,m.username,m.firstname,m.lastname
        ,m.datesave,m.m_level,p.pname');
        $this->db->from('tbl_register as m');
        $this->db->join('tbl_position as p', 'm.pid=p.pid');
        $this->db->where('m.m_level', 'A');
        // คำสั่งดึงข้อมูลจากตาราง เฉพาะ A
        $query = $this->db->get();
        return $query->result();
    }
    // ดึงข้อมูลจาก register
    public function showdata5()
    {
        $this->db->select('m.id,m.img,m.username,m.firstname,m.lastname
        ,m.datesave,m.m_level,p.pname');
        $this->db->from('tbl_register as m');
        $this->db->join('tbl_position as p', 'm.pid=p.pid');
        $this->db->where('m.pid', '1');
        // คำสั่งดึงข้อมูลจากตาราง เฉพาะ A
        $query = $this->db->get();
        return $query->result();
    }
    // ดึงข้อมูลจาก register
    public function showdata6()
    {
        $this->db->select('m.id,m.img,m.username,m.firstname,m.lastname
        ,m.datesave,m.m_level,p.pname');
        $this->db->from('tbl_register as m');
        $this->db->join('tbl_position as p', 'm.pid=p.pid');
        $this->db->where_in('m.pid', array('1', '3', '4'));
        // คำสั่งดึงข้อมูลจากตาราง เลือกตัวที่จะแสดงหลายตัว
        $query = $this->db->get();
        return $query->result();
    }
    // ดึงข้อมูลจาก register
    public function showdata7()
    {
        $this->db->select('m.id,m.img,m.username,m.firstname,m.lastname
        ,m.datesave,m.m_level,p.pname');
        $this->db->from('tbl_register as m');
        $this->db->join('tbl_position as p', 'm.pid=p.pid');
        $this->db->where('m.pid >= 3');
        // คำสั่งดึงข้อมูลจากตาราง เลือกตัวที่จะแสดงหลายตัว
        $query = $this->db->get();
        return $query->result();
    }
    public function showdata8()
    {
        $this->db->select('m.id,m.img,p.pname,m.username,m.firstname,m.lastname,m.m_level,m.datesave');
        $this->db->from('tbl_register as m');
        $this->db->join('tbl_position as p', 'm.pid=p.pid');
        $this->db->order_by('m.id', 'asc');

        $query = $this->db->get();
        return $query->result();
    }

    public function read($id)
    {
        $this->db->select('*');
        $this->db->from('tbl_register');
        $this->db->where('id', $id);
        // คำสั่งดึงข้อมูลจากตาราง
        $query = $this->db->get();
        if ($query->num_rows() > 0) {
            $data = $query->row();
            return $data;
        }
        return FALSE;
    }

    public function editmember()
    {
        $editdata = array(
            'firstname' => $this->input->post('firstname'),
            'lastname' => $this->input->post('lastname')
        );

        $this->db->where('id', $this->input->post('id'));
        $query = $this->db->update('tbl_register', $editdata);

        if ($query) {
            echo 'edit OK';
        } else {
            echo 'edit false';
        }
        // echo '<pre>';
        // print_r($datalogin);
        // echo '</pre>';
        // exit();
    }

    public function deldata($id)
    {
        $this->db->delete('tbl_register', array('id' => $id));
    }
}
