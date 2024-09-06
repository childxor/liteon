<?php

defined('BASEPATH') or exit('No direct script access allowed');

class Log extends CI_Controller
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
        $this->data['module'] = 'main/sys/log';

        $this->load->view("main/layout/index", $this->data);
    }

    /* Get Data to List Table */

    public function ajax_list()
    {
        header('Access-Control-Allow-Origin: *');
        ini_set('max_execution_time', 0);
        // var_dump($_GET); die();
        ## Read value
        $draw = $_GET['draw'];
        $row = $_GET['start'];
        $rowperpage = $_GET['start'] + $_GET['length']; // Rows display per page
        $searchValue = $_GET['search']['value']; // Search value

        // totalRecordwithFilter
        $sql = "SELECT top(5000) id ";
        $sql .= "FROM sys_log ";
        $sql .= "WHERE (id LIKE '%" . $searchValue . "%') OR ";
        $sql .= "(CONVERT(varchar(50), datetime,120) LIKE '%" . $searchValue . "%') OR ";
        $sql .= "(username LIKE '%" . $searchValue . "%') OR ";
        $sql .= "(ip LIKE '%" . $searchValue . "%') OR ";
        $sql .= "(os LIKE '%" . $searchValue . "%') OR ";
        $sql .= "(device LIKE '%" . $searchValue . "%') OR ";
        $sql .= "(browser LIKE '%" . $searchValue . "%') OR ";
        $sql .= "(page LIKE '%" . $searchValue . "%') OR ";
        $sql .= "(remark LIKE '%" . $searchValue . "%')";
        // die($sql);
        $totalRecordwithFilter = $this->db->query($sql)->num_rows();
        // totalRecords
        $sql = "SELECT top(5000) id FROM sys_log";
        $totalRecords = $this->db->query($sql)->num_rows();

        $sql = "SELECT id, datetime, username, ip, os, device, browser, page, remark ";
        $sql .= "FROM ";
        $sql .= "(SELECT top(5000) id, datetime, username, ip, os, device, browser, page, remark,
        ROW_NUMBER() OVER (ORDER BY id  DESC) AS Seq
        FROM sys_log ";
        $sql .= "WHERE (id LIKE '%" . $searchValue . "%') OR ";
        $sql .= "(CONVERT(varchar(50), datetime,120) LIKE '%" . $searchValue . "%') OR ";
        $sql .= "(username LIKE '%" . $searchValue . "%') OR ";
        $sql .= "(ip LIKE '%" . $searchValue . "%') OR ";
        $sql .= "(os LIKE '%" . $searchValue . "%') OR ";
        $sql .= "(device LIKE '%" . $searchValue . "%') OR ";
        $sql .= "(browser LIKE '%" . $searchValue . "%') OR ";
        $sql .= "(page LIKE '%" . $searchValue . "%') OR ";
        $sql .= "(remark LIKE '%" . $searchValue . "%')";
        $sql .= ")t WHERE (Seq BETWEEN " . $row . " AND " . $rowperpage . ") ";
        // die($sql);
        $data = $this->db->query($sql)->result();
        $items = array();
        foreach ($data as $key => $item) {
            $items[$key]['id'] = $item->id;
            $items[$key]['datetime'] = $item->datetime;
            $items[$key]['username'] = $item->username;
            $items[$key]['ip'] = $item->ip;
            $items[$key]['os'] = $item->os;
            $items[$key]['device'] = $item->device;
            $items[$key]['browser'] = $item->browser;
            $items[$key]['page'] = $item->page;
            $items[$key]['remark'] = $item->remark;
            $items[$key]['prefix_name'] = @$this->user_info($item->username)->prefix_name;
            $items[$key]['first_name'] = @$this->user_info($item->username)->first_name;
            $items[$key]['last_name'] = @$this->user_info($item->username)->last_name;
            $items[$key]['emp_code'] = @$this->user_info($item->username)->emp_code;
            $items[$key]['department_name'] = @$this->user_info($item->username)->department_name;
        }
        ## Response
        $response = array(
            "draw" => intval($draw),
            "iTotalRecords" => $totalRecords,
            "iTotalDisplayRecords" => $totalRecordwithFilter,
            "aaData" => $items
        );

        echo json_encode($response);
    }

    /* Get Data to View */

    public function ajax_data()
    {
        header('Access-Control-Allow-Origin: *');
        ini_set('max_execution_time', 300);
        $id = $this->input->post('id');
        $sql = "SELECT * FROM sys_log WHERE id = '" . $id . "'";
        $data = $this->db->query($sql)->row();
        echo json_encode($data);
    }

    public function user_info($username)
    {
        $sql = "SELECT sys_user.id, sys_user.position_id, sys_user.prefix_name, sys_user.emp_code, sys_user.username, sys_user.first_name, sys_user.last_name, sys_user.tel, sys_user.email, sys_user.department_id , sys_user.sub_department_id "
            . ",mas_department.name AS department_name, mas_department.description AS department_description, mas_department.color "
            . "FROM sys_user INNER JOIN mas_department ON sys_user.department_id = mas_department.id "
            . "WHERE sys_user.username = '" . $username . "' "
            . "AND sys_user.record_status = 'N'";
        return $this->db->query($sql)->row();
    }
}
