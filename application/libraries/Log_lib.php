<?php

defined('BASEPATH') or exit('No direct script access allowed');

class Log_lib
{

    private $CI;

    function __construct()
    {
        // Assign by reference with "&" so we don't create a copy
        $this->CI = &get_instance();
    }

    /* write_log => $remark is String, $last_query is SQL Syntax */

    public function write_log($remark = '', $last_query = '')
    {
        $device = ($this->CI->agent->is_mobile() ? ($this->CI->agent->is_mobile('iphone') ? 'Iphone' : 'Android') : 'Computer');
        $data = array(
            'datetime' => date('Y-m-d H:i:s'),
            'username' => $this->CI->session->userdata('user_profile')->username,
            'ip' => $this->CI->input->ip_address(),
            'os' => $this->CI->agent->platform(),
            'device' => $device,
            'browser' => $this->CI->agent->agent_string(),
            'page' => $this->CI->uri->uri_string(),
            'query' => 'N*' . $last_query,
            'remark' => 'N*' . $remark
        );
        $this->CI->db->select('id');
        $this->CI->db->where($data);
        $nrows = $this->CI->db->count_all_results('sys_log');
        if ($nrows == 0) {
            if ($this->CI->db->query($this->CI->efs_lib->query_insert_jp('sys_log', $data))) {
                return TRUE;
            } else {
                return FALSE;
            }
        } else {
            return FALSE;
        }
    }

    /* read_log => Get All Access Log in System */

    public function read_log()
    {
        $query = $this->CI->db->query('SELECT * FROM sys_log ORDER BY datetime DESC');
        if ($query) {
            $nrows = $query->num_rows();
            if ($nrows > 0) {
                return $query->result();
            } else {
                return 'Log Empry.';
            }
        } else {
            return FALSE;
        }
    }

    /* get_type_log => $remark is String Used For Profile Timeline log */

    public function get_type_log($remark)
    {
        if (strstr(strtolower($remark), "authen")) {
            $data['color'] = 'info';
            $data['icon'] = 'ti-lock';
        } else if (strstr(strtolower($remark), "error")) {
            $data['color'] = 'danger';
            $data['icon'] = 'ti-close';
        } else {
            $data['color'] = 'warning';
            $data['icon'] = 'ti-info';
        }
        return $data;
    }

    /* Log for Purchase Requestion(PR) */

    public function pr_log($data = array())
    {
        if ($this->CI->db->insert('trn_pr', $data)) {
            return $this->CI->db->insert_id();
        } else {
            return FALSE;
        }
    }

    /* Get History Purchase Requestion(PR) */

    public function pr_history($id)
    {

        $sql = "SELECT check_remark "
            . "FROM pur_pr_check_status "
            . "WHERE record_status='N' AND pur_pr_check_status.id='" . $id . "'";
        $ls = '';
        $item = $this->CI->db->query($sql)->row();
        $ls .= '<span>' . $item->check_remark . '</span><hr/>';
        return $ls;
    }

    /* Log for  Purchase Requestion(PR) Detail */

    public function pr_detail_log($data = array())
    {
        if ($this->CI->db->insert('trn_pr_detail', $data)) {
            return TRUE;
        } else {
            return FALSE;
        }
    }
}
