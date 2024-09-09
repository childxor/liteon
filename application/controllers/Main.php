<?php

defined('BASEPATH') or exit('No direct script access allowed');

class Main extends CI_Controller
{
    public function __construct()
    {
        /* Call the Model constructor */
        parent::__construct();

        /* Get System Variable */
        $this->data['var'] = $this->efs_lib->get_var_sys();
        /* System Language */
        $this->lang = $this->efs_lib->language_system();
        $this->load->library('cart');
        // $this->load->library('PHPExcel');

        if (empty($this->session->userdata('user_profile')->id)) {
            redirect('authen', 'refresh');
            exit();
        }
    }

    public function index()
    {
        /* กลับไปหน้าหลักของผู้ใช้งาน */
        redirect(base_url($this->session->userdata('default_module')));
    }

    /* ดูข้อมูลโปรไฟล์ของผู้ใช้งาน */
    public function profile()
    {
        $this->data['lang_sys'] = $this->lang;
        $this->data['topbar'] = 'main/layout/topbar';
        $this->data['sidebar'] = 'main/layout/sidebar';
        $this->data['footer'] = 'main/layout/footer';
        $breadcrumbs['breadcrumb'] = [
            [1 => [
                'name' => '<i class="mdi mdi-home font-20" style="line-height: 20px;"></i>',
                'module' => base_url(),
                'class' => '',
            ]],
            [2 => [
                'name' => $this->lang->sys_view_profile,
                'module' => '',
                'class' => 'active',
            ]],
        ];
        $this->data['breadcrumb'] = $this->load->view('main/layout/breadcrumb', $breadcrumbs, true);
        $this->data['module'] = 'main/page/profile';
        $this->data['role'] = $this->get_role_user();
        $this->load->view('main/layout/index', $this->data);
    }

    public function get_role_user()
    {
        header('Access-Control-Allow-Origin: *');
        $role = '';
        $sql = 'SELECT sys_role.name '
            . 'FROM sys_role_user INNER JOIN sys_role ON sys_role_user.role_id = sys_role.id '
            . "WHERE sys_role_user.user_id = '" . $this->session->userdata('user_profile')->id . "' "
            . "AND sys_role_user.record_status = 'N'";
        $result = $this->db->query($sql)->result();
        foreach ($result as $row) {
            $role .= '<span class="badge badge-info mr-1 mb-1">' . $row->name . '</span>';
        }

        return $role;
    }

    // language_module_del
    function language_module_del()
    {
        $data = array(
            'record_status' => 'D',
            'updated_username' => $this->session->userdata('user_profile')->username,
            'updated_at' => date('Y-m-d H:i:s'),
        );
        $wheres = array('id' => $this->input->post('id'));
        if (!$this->db->query($this->efs_lib->query_update_jp('sys_language', $data, $wheres))) {
            $this->log_lib->write_log('error-main-del-language=> ' . $this->session->flashdata('msg'), json_encode($data));
            return $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'error')));
        } else {
            $this->log_lib->write_log('Main Del Language => ' . $this->session->flashdata('msg'), json_encode($data));
            return $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'success')));
        }
    }

    // language_module_add
    function language_module_add_update()
    {
        $pre_fix = 'N*';
        $data = array(
            // 'module_id' => $this->input->post('module_id'),
            'th' => $pre_fix . ($this->input->post('th') ? $this->input->post('th') : ''),
            'en' => $pre_fix . ($this->input->post('en') ? $this->input->post('en') : ''),
            'jp' => $pre_fix . ($this->input->post('jp') ? $this->input->post('jp') : ''),
            'cn' => $pre_fix . ($this->input->post('cn') ? $this->input->post('cn') : ''),
            'keyword' => $pre_fix . ($this->input->post('keyword') ? $this->input->post('keyword') : ''),
            'created_username' => $this->session->userdata('user_profile')->username,
            'created_at' => date('Y-m-d H:i:s'),
        );
        if ($this->input->post('module_id')) {
            $data['module_id'] = $this->input->post('module_id');
        } else {
            $data['module_id'] = 0;
        }
        // function query_insert_jp($table, $data)
        // function query_update_jp($table, $values, $wheres)
        if (!$this->input->post('id')) {
            if (!$this->db->query($this->efs_lib->query_insert_jp('sys_language', $data))) {
                $this->log_lib->write_log('error-main-add-language=> ' . $this->session->flashdata('msg'), json_encode($data));
                return $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'error')));
            } else {
                $this->log_lib->write_log('Main Add Language => ' . $this->session->flashdata('msg'), json_encode($data));
                return $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'success')));
            }
        } else {
            $data['updated_username'] = $this->session->userdata('user_profile')->username;
            $data['updated_at'] = date('Y-m-d H:i:s');
            $wheres = array('id' => $this->input->post('id'));
            if (!$this->db->query($this->efs_lib->query_update_jp('sys_language', $data, $wheres))) {
                $this->log_lib->write_log('error-main-update-language=> ' . $this->session->flashdata('msg'), json_encode($data));
                return $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'error')));
            } else {
                $this->log_lib->write_log('Main Update Language => ' . $this->session->flashdata('msg'), json_encode($data));
                return $this->output->set_content_type('application/json')->set_output(json_encode(array('status' => 'success')));
            }
        }
    }

    // public function translate()
    // {
    //     $text = "สวัสดีครับ";
    //     $targetLang = 'en';
    //     $url = "https://translate.google.com/translate_a/single?client=at&dt=t&q=" . urlencode($text) . "&tl=" . $targetLang;

    //     die($url);
    //     $ch = curl_init();
    //     curl_setopt($ch, CURLOPT_URL, $url);
    //     curl_setopt($ch, CURLOPT_RETURNTRANSFER, 1);
    //     $output = curl_exec($ch);
    //     curl_close($ch);

    //     // Decode JSON response
    //     $decoded = json_decode($output, true);

    //     // Extract translated text
    //     $translatedText = isset($decoded['sentences']) ? str_replace('<span></span>', '', $decoded['sentences'][0]['trans']) : 'Error translating text';

    //     var_dump($translatedText);
    //     die();
    //     return $this->output
    //         ->set_content_type('application/json')
    //         ->set_output(json_encode(['translatedText' => $translatedText]));
    // }

    // function translate($text, $source_lang = 'en', $target_lang = 'es')
    // {
    //     $text = "สวัสดีครับ";
    //     $curl = curl_init();
    //     curl_setopt_array($curl, array(
    //         CURLOPT_URL => 'https://translate.googleapis.com/translate_a/single?client=gtx&dt=t',
    //         CURLOPT_RETURNTRANSFER => true,
    //         CURLOPT_ENCODING => '',
    //         CURLOPT_MAXREDIRS => 10,
    //         CURLOPT_TIMEOUT => 0,
    //         CURLOPT_FOLLOWLOCATION => true,
    //         CURLOPT_HTTP_VERSION => CURL_HTTP_VERSION_1_1,
    //         CURLOPT_CUSTOMREQUEST => 'POST',
    //         CURLOPT_POSTFIELDS => 'sl=' . $source_lang . '&tl=' . $target_lang . '&q=' . urlencode($text),
    //         CURLOPT_HTTPHEADER => array(
    //             'Content-Type: application/x-www-form-urlencoded'
    //         ),
    //     ));
    //     $response = curl_exec($curl);
    //     curl_close($curl);

    //     $sentencesArray = json_decode($response, true);
    //     $sentences = "";
    //     foreach ($sentencesArray[0] as $s) {
    //         $sentences .= isset($s[0]) ? $s[0] : '';
    //     }

    //     return $sentences;
    // }

    function language_module_get() //add language footer
    {
        $url_name = explode('/', $this->input->post('url'));
        // $url = $url_name[2] . (isset($url_name[3]) ? '/' . $url_name[3] : '') . (isset($url_name[4]) ? '/' . $url_name[4] : '');
        $url = (isset($url_name[2]) ? $url_name[2] : '') . (isset($url_name[3]) ? '/' . $url_name[3] : '') . (isset($url_name[4]) ? '/' . $url_name[4] : '');
        if ($url == '') {
            $url = '';
        }
        // die($url);
        $data = array();

        if ($url != '') {
            $this->db->select('sys_module.id as module_id, sys_language.th, sys_language.en, sys_language.jp,sys_language.cn,sys_language.keyword, sys_language.id');
        } else {
            $this->db->select("'0' as module_id, sys_language.th, sys_language.en, sys_language.jp,sys_language.cn,sys_language.keyword, sys_language.id");
        }
        if ($url != '') {
            $this->db->from('sys_module');
        } else {
            $this->db->from('sys_language');
        }
        if ($url != '') {
            $this->db->join('sys_language', 'sys_module.id = sys_language.module_id', 'full outer');
        }
        if ($url != '') {
            $this->db->where('sys_module.module', $url);
            $this->db->where('sys_module.record_status', 'N');
        } else {
            $this->db->where('sys_language.module_id', '0');
        }
        $this->db->where('sys_language.record_status', 'N');
        $this->db->order_by('sys_language.id', 'desc');
        $query = $this->db->get();
        $result = $query->result();
        // die($this->db->last_query());
        // $lastQuery = $this->db->last_query();
        // die($lastQuery);

        return $this->output->set_content_type('application/json')->set_output(json_encode($result));
    }

    public function test()
    {
        ini_set('max_execution_time', 0);
        // die('test');
        // $this->db->reset_query();
        $sql = "SELECT    sto_pr_detail.id, pur_po_detail.id AS po_detail_id, pur_po.discount, sto_pr_detail.material_type_id, sto_pr.no AS pr_no, sto_pr.pr_status, sto_pr.date, sto_pr.within, sto_pr_detail.amount AS qty,
                sto_pr_detail.price_per_unit, pur_po.no AS po_no, pur_po.delivery_date, sto_pr.user_id, sto_pr.id AS pr_id, pur_po.id AS po_id, sto_pr.department_id, sto_location.name AS location_name, pur_po.supplier_id, pur_po.sent_mail, pur_po.remark,
                pur_po.remark_revise, sys_user.prefix_name, sys_user.last_name, sto_pr.sub_department , mas_supplier_material_price.price, pur_pr_quotation.ref_no,
                pur_pr_quotation.filename, { fn CONCAT(sto_receipt_note.receipt_type, CONVERT(varchar, sto_receipt_note.receipt_no)) } AS receipt_note
        FROM      sto_location INNER JOIN
                sto_pr INNER JOIN
                sto_pr_detail ON sto_pr.id = sto_pr_detail.pr_id ON sto_location.id = sto_pr.location_id INNER JOIN
                sys_user ON sto_pr.user_id = sys_user.id LEFT OUTER JOIN
                pur_po INNER JOIN
                pur_po_detail ON pur_po.id = pur_po_detail.po_id ON sto_pr_detail.id = pur_po_detail.pr_detail_id AND pur_po_detail.record_status = 'N' AND pur_po.record_status = 'N' LEFT OUTER JOIN
                mas_supplier_material_price ON sto_pr_detail.supplier_material_price_id = mas_supplier_material_price.id LEFT OUTER JOIN
                pur_pr_quotation ON pur_pr_quotation.id = sto_pr_detail.quotation_id FULL OUTER JOIN
                pur_pr_check_status ON sto_pr.id = pur_pr_check_status.id FULL OUTER JOIN
                sto_receipt_note ON pur_po_detail.po_id = sto_receipt_note.po_id
        WHERE   (sto_pr.record_status = 'N') AND (sto_pr_detail.record_status = 'N')  OR
                        (sto_pr.record_status = 'N') AND (sto_pr_detail.record_status = 'N')
        ORDER BY sto_pr_detail.id DESC";
        // die($sql);

        $result = $this->db->query($sql)->result();
        foreach ($result as $row) {
            echo '<pre>';
            var_dump($row);
            echo '</pre>';
        }
    }

    public function get_role($id)
    {
        $this->db->select('sys_role_user.id, sys_role_user.role_id, sys_role_user.user_id, sys_role_user.created_username, sys_role_user.created_at, sys_role_user.updated_username, sys_role_user.updated_at,
        sys_role_user.record_status, sys_role.name');
        $this->db->from('sys_role_user');
        $this->db->join('sys_role', 'sys_role_user.role_id = sys_role.id');
        $this->db->where('sys_role_user.user_id', $id);
        $this->db->where('sys_role_user.record_status', 'N');
        $query = $this->db->get()->result();
        return $query;
    }


    public function edit_profile()
    {
        $data = [
            'gender_id' => ($this->input->post('prefix_name') == 'นาย' or strtoupper($this->input->post('prefix_name')) == 'MR.' ? '1' : '2'),
            'prefix_name' => $this->input->post('prefix_name'),
            'first_name' => $this->input->post('first_name'),
            'last_name' => $this->input->post('last_name'),
            'tel' => $this->input->post('tel'),
            'email' => $this->input->post('email'),
            'default_module_id' => $this->input->post('default_module_id'),
            'cng_per_page' => $this->input->post('cng_per_page'),
            'cng_font_size' => $this->input->post('cng_font_size'),
            'cng_table_font_size' => $this->input->post('cng_table_font_size'),
            'cng_lang' => $this->input->post('cng_lang'),
            'cng_alert_time' => $this->input->post('cng_alert_time'),
            'updated_username' => $this->session->userdata('user_profile')->username,
            'updated_at' => date('Y-m-d H:i:s'),
        ];
        $this->db->where('id', $this->session->userdata('user_profile')->id);
        if (!$this->db->update('sys_user', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_main_error_update_profile);
            /* Write Log */
            $this->log_lib->write_log('error-main-update-profile=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $sql = 'SELECT sys_user.id, sys_user.position_id, mas_position.name AS position_name, sys_user.username,sys_user.emp_code, sys_user.prefix_name, sys_user.first_name, sys_user.last_name, sys_user.tel, sys_user.email, sys_user.is_active '
                . ', sys_user.cng_per_page, sys_user.cng_font_size, sys_user.cng_table_font_size, sys_user.cng_lang, sys_user.cng_alert_time, sys_user.default_module_id '
                . ', mas_department.id AS department_id, mas_department.name AS department_name, mas_department.description AS department_description, mas_department.color '
                . ', mas_sub_department.name AS sub_department_name '
                . 'FROM sys_user INNER JOIN mas_department ON sys_user.department_id = mas_department.id '
                . 'LEFT JOIN mas_sub_department ON sys_user.sub_department_id = mas_sub_department.id '
                . 'INNER JOIN mas_position ON sys_user.position_id = mas_position.id '
                . "WHERE sys_user.id = '" . $this->session->userdata('user_profile')->id . "' "
                . "AND sys_user.record_status = 'N'";
            $user_profile = $this->db->query($sql)->row();
            $data['user_profile'] = $user_profile;
            $data['user_profile']->role = $this->get_role($user_profile->id);


            /* Get Default Module Let Srart */
            $sql_module1 = 'SELECT module AS default_module '
                . 'FROM sys_module '
                . "WHERE (id = '" . $user_profile->default_module_id . "')";

            $sql_module2 = 'SELECT DISTINCT sys_module.module AS default_module '
                . 'FROM sys_role_user INNER JOIN sys_role ON sys_role_user.role_id = sys_role.id '
                . 'INNER JOIN sys_module ON sys_role.default_module_id = sys_module.id '
                . "WHERE sys_role_user.record_status='N'   AND sys_role_user.user_id ='" . $user_profile->id . "' ";

            $module = $this->db->query(!empty($user_profile->default_module_id) ? $sql_module1 : $sql_module2)->row();
            $data['default_module'] = $module->default_module;

            /* Set token For Session */
            /* ตั้งค่าโทเค็นสำหรับเซสชันผู้ใช้ปัจจุบัน */
            $data['token'] = strtotime(date('Y-m-d H:i:s'));

            $this->session->set_userdata($data);
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_main_update_profile);
            $this->log_lib->write_log('Main Profile => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    public function change_language()
    {
        $lang = substr($_SERVER['HTTP_ACCEPT_LANGUAGE'], 0, 2);
        // $_SERVER['HTTP_ACCEPT_LANGUAGE'] = 'en';
        $data = [
            'cng_lang' => $lang,
            'updated_username' => $this->session->userdata('user_profile')->username,
            'updated_at' => date('Y-m-d H:i:s'),
        ];
        $this->db->where('id', $this->session->userdata('user_profile')->id);
        if (!$this->db->update('sys_user', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_main_error_change_language);
            /* Create Log Data  */
            $this->log_lib->write_log('error-main-change-language=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $sql = 'SELECT sys_user.id, sys_user.position_id, mas_position.name AS position_name, sys_user.prefix_name,sys_user.emp_code, sys_user.username, sys_user.first_name, sys_user.last_name, sys_user.tel, sys_user.email, sys_user.is_active '
                . ', sys_user.cng_per_page, sys_user.cng_font_size, sys_user.cng_table_font_size, sys_user.cng_lang, sys_user.cng_alert_time, sys_user.default_module_id '
                . ', mas_department.id AS department_id, mas_department.name AS department_name, mas_sub_department.name AS sub_department_name, mas_department.description AS department_description, mas_department.color '
                . 'FROM sys_user INNER JOIN mas_department ON sys_user.department_id = mas_department.id '
                . 'LEFT JOIN mas_sub_department ON sys_user.sub_department_id = mas_sub_department.id '
                . 'INNER JOIN mas_position ON sys_user.position_id = mas_position.id '
                . "WHERE sys_user.id = '" . $this->session->userdata('user_profile')->id . "' "
                . "AND sys_user.record_status = 'N'";
            $user_profile = $this->db->query($sql)->row();
            $data['user_profile'] = $user_profile;
            $data['user_profile']->name_sys = 'efs';
            $data['user_profile']->role = $this->get_role($user_profile->id);
            // $data['user_profile']->emp_code = $user_profile->emp_code;
            $this->session->set_userdata($data);
            $this->lang = $this->efs_lib->language_system();
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_main_change_language_success);
            $this->log_lib->write_log('Main Change Language => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    public function changePassword()
    {
        $passwd = $this->input->post('re_new_password');
        $hash = $this->efs_lib->paswd_encrypt($passwd);
        $expired = $this->efs_lib->increment_date();
        $data = [
            'hash' => $hash,
            'expired' => $expired,
            'updated_username' => $this->session->userdata('user_profile')->username,
            'updated_at' => date('Y-m-d H:i:s'),
        ];
        $this->db->where('record_status', 'N');
        $this->db->where('user_id', $this->session->userdata('user_profile')->id);
        if (!$this->db->update('sys_user_passwd', $data)) {
            $this->session->set_flashdata('type', 'error');
            $this->session->set_flashdata('msg', $this->lang->sys_main_error_change_password);
            /* Create Log Data  */
            $this->log_lib->write_log('error-main-change-password=> ' . $this->session->flashdata('msg'), json_encode($data));
        } else {
            $data['expired_passwd'] = $expired;
            $this->session->set_userdata($data);
            $this->session->set_flashdata('type', 'success');
            $this->session->set_flashdata('msg', $this->lang->sys_main_success_change_password);
            $this->log_lib->write_log('Main Change Password Profile => ' . $this->session->flashdata('msg'), json_encode($this->db->last_query()));
        }
        redirect($this->agent->referrer());
    }

    /* <!-- ============================================================== -->
    <!-- Function -->
    <!-- ============================================================== --> */

    /* Get Language From libraries Efs_lib.php */

    public function get_language()
    {
        $type = $this->uri->segment(3);

        return $this->efs_lib->language_system($type);
    }

    // function check work ajax

    /* Check Current User Password */

    public function check_current_password()
    {
        header('Access-Control-Allow-Origin: *');
        ini_set('max_execution_time', 300);
        $lang_sys = $this->efs_lib->language_system();
        $value = $this->input->get_post('value');
        $sql = "SELECT * FROM sys_user_passwd WHERE record_status='N' AND user_id ='" . $this->session->userdata('user_profile')->id . "'";
        $nrow = $this->db->query($sql)->num_rows();
        if ($nrow > 0) {
            $item_passwd = $this->db->query($sql)->row();
            if ($this->efs_lib->paswd_decrypt($value, $item_passwd->hash) == false) {
                echo json_encode(
                    [
                        'value' => $_REQUEST['value'],
                        'valid' => 0,
                        'message' => $lang_sys->sys_incurrect_password,
                    ]
                );
            } else {
                echo json_encode(
                    [
                        'value' => $_REQUEST['value'],
                        'valid' => 1,
                        'message' => '',
                    ]
                );
            }
        } else {
            echo json_encode(
                [
                    'value' => $_REQUEST['value'],
                    'valid' => 0,
                    'message' => $lang_sys->sys_incurrect_password,
                ]
            );
        }
    }

    // ยกเลิก pr po
    public function clpr()
    {
?>
        <title>CANCELLED PR PO</title>
        <form method="POST" class="form-control">
            <input type="text" name="id_txt" size="20" class="form-control"><br><br>
            <input type="submit" value="send" class="mt-2">
        </form>
        <?php
        $id = $this->efs_lib->decrypt_segment($_POST['id_txt']);
        // cancelled หัวตาราง
        $sql = "update sto_pr set pr_status = 'CL' where id = '" . $id . "'";
        $this->db->query("update sto_pr_detail set pr_detail_status = 'CH' where pr_id = '" . $id . "'");
        if ($this->db->query($sql)) {
            echo $sql . 'ยกเลิกสำเร็จ' . '<br>';
        } else {
            echo $sql . 'ยกเลิกไม่สำเร็จ' . '<br>';
        }
        $this->db->query("update pur_po set record_status = 'D' where pr_id = '" . $id . "'");
    }

    // ถอดรหัส
    public function see_id()
    {
        ?>
        <title>InputId</title>
        <div class="container">
            <form method="POST" class="form-control">
                <input type="text" name="id_txt2" size="20" class="form-control"><br><br>
                <input type="submit" value="send" class="mt-2">
            </form>
        </div>
        <?php
        $id = (!empty($_POST['id_txt2']) ? $_POST['id_txt2'] : '');
        // ลบช่องว่าง

        $id = str_replace(' ', '', $id);
        // ถ้า $id มีค่า ให้ทำงาน
        if ($id == '') {
            die('ไม่มีค่า');
        }
        echo $this->efs_lib->decrypt_segment($id) . '<br>';
        // echo $this->efs_lib->decrypt_segment($id) . '<br>';
    }

    // systex query codeigniter
    public function query()
    {
        $sql = "select * from sys_user where record_status = 'N'";
        $query = $this->db->query($sql);
        $nrow = $query->num_rows();
        if ($nrow > 0) {
            foreach ($query->result() as $row) {
                echo $row->username . '<br>';
            }
        }
    }

    // include file phpexel

    /* อัพเดทราคาใน PR */
    public function calc_price()
    {
        ?>
        <form method="POST" class="form-control" action="<?php echo base_url('main/setting'); ?>">
            <p>Please select your favorite Web language:</p>
            <input type="radio" id="html" name="fav_language" value="HTML">
            <label for="html">HTML</label><br>
            <input type="radio" id="css" name="fav_language" value="CSS">
            <label for="css">CSS</label><br>
            <input type="radio" id="javascript" name="fav_language" value="JavaScript">
            <label for="javascript">JavaScript</label><br><br>

            <input type="text" name="post_txt" size="20" class="form-control"><br><br>
            <input type="submit" value="send" class="mt-2">
        </form>
    <?php
    }

    public function setting()
    {
        $this->efs_lib->calc_price($this->efs_lib->decrypt_segment($_POST['post_txt']));
        redirect(base_url('main/calc_price'));
    }

    /* ดูว่า Quotation ยังปกติดีหรือไม่ */
    public function check_quotation_exist()
    {
        $sql = "SELECT sto_pr.no, pur_pr_quotation.filename, pur_pr_quotation.ref_no, sto_pr.pr_status
            FROM pur_pr_quotation INNER JOIN
            sto_pr ON pur_pr_quotation.pr_id = sto_pr.id
            WHERE sto_pr.record_status ='N' AND pur_pr_quotation.record_status ='N' ";
        $result = $this->db->query($sql)->result();
        foreach ($result as $row) {
            if (!file_exists('uploads/quotation/' . $row->no . '/' . $row->filename)) {
                echo 'Not found File : ' . $row->filename . ' | STATUS: ' . $row->pr_status . '<br>';
            }
        }
    }

    public function reset_bbf()
    {
        $id = (!empty($this->input->post('material_id')) ? $this->input->post('material_id') : '');
        if (empty($id)) {
            return $this->output->set_content_type('application/json')->set_output(json_encode(['status' => 'error', 'msg' => 'ไม่พบรหัสวัสดุ']));
        } else {
            $sql = 'select * from vw_material where ' . (strlen($id) < 5 ? 'id = ' : 'material_code = ') . "'" . $id . "' ";
            $this->setunit($this->db->query($sql)->row()->id);
            $this->OK2($this->db->query($sql)->row()->id);
        }
    }

    public function print_r($data)
    {
        echo '<pre>';
        print_r($data);
        echo '</pre>';
        die();
    }

    public function OK2($code)
    {
        ini_set('max_execution_time', -1);

        $this->db->select('sto_issue_detail.issue_id, sto_issue_detail.material_id, SUM(sto_issue_detail.qty) AS qty, sto_issue_detail.remark');
        $this->db->from('sto_issue_detail');
        $this->db->join('sto_issue', 'sto_issue_detail.issue_id = sto_issue.id');
        $this->db->where('sto_issue_detail.material_id', $code);
        $this->db->where('sto_issue_detail.record_status', 'N');
        $this->db->where('sto_issue.record_status', 'N');
        $this->db->group_by('sto_issue_detail.issue_id, sto_issue_detail.material_id, sto_issue_detail.remark, sto_issue_detail.issue_id');
        $this->db->order_by('sto_issue_detail.issue_id');
        // die($this->db->get_compiled_select());
        $query = $this->db->get()->result();
        foreach ($query as $issue) {
            $this->db->query("delete from sto_issue_detail where issue_id = '" . $issue->issue_id . "' and material_id = '" . $issue->material_id . "'");
            $data = array(
                'issue_id' => $issue->issue_id,
                'material_id' => $issue->material_id,
                'qty' => $issue->qty,
                'remark' => $issue->remark,
            );
            if (!$this->db->insert('sto_issue_detail', $data)) {
                die('ติดต่อ IT ok2');
            } else {
                $last_id = $this->db->insert_id();
                $this->db->select('qty, material_id, receipt_note_detail_id, price_per_unit, date, bbf_id');
                $this->db->from('vw_check_qty_material');
                $this->db->where('material_id', $issue->material_id);
                $this->db->where('qty >', '0');
                $this->db->order_by('receipt_note_detail_id, bbf_id');
                $this->db->limit(1);
                // die($this->db->get()->num_rows());
                $material = $this->db->get()->row();
                if ($material == null) {
                    $this->db->query("delete from sto_issue_detail where id = '" . $last_id . "'");
                    continue;
                } else {
                    if ($issue->qty > $material->qty) {
                        $this->db->set('qty', $material->qty);
                        $this->db->set('bbf_id', $material->bbf_id);
                        $this->db->set('receipt_note_detail_id', $material->receipt_note_detail_id);
                        $this->db->set('price_per_unit', $material->price_per_unit);
                        $this->db->where('id', $last_id);
                        if (!$this->db->update('sto_issue_detail')) {
                            die('ติดต่อ IT ok2 up');
                        } else {
                            $ses = $issue->qty - $material->qty;
                            $data = [
                                'issue_id' => $issue->issue_id,
                                'material_id' => $issue->material_id,
                                'qty' => $ses,
                                'price_per_unit' => null,
                                'remark' => $issue->remark,
                                'bbf_id' => null,
                                'receipt_note_detail_id' => null,
                                'record_status' => 'N',
                            ];
                            (!$this->db->insert('sto_issue_detail', $data) ? die('ติดต่อ IT ok2 isert') : '');
                            $this->price($issue->material_id, $this->db->insert_id(), $ses, $issue);
                        }
                    } else {
                        $this->db->set('bbf_id', $material->bbf_id);
                        $this->db->set('receipt_note_detail_id', $material->receipt_note_detail_id);
                        $this->db->set('price_per_unit', $material->price_per_unit);
                        $this->db->where('id', $last_id);
                        if (!$this->db->update('sto_issue_detail')) {
                            die('ติดต่อ IT ok2 up');
                        }
                    }
                }
            }
        }
    }

    public function price($material_id, $id, $qty, $issue)
    {
        $this->db->select('qty, material_id, receipt_note_detail_id, price_per_unit, date, bbf_id');
        $this->db->from('vw_check_qty_material');
        $this->db->where('material_id', $material_id);
        $this->db->where('qty >', '0');
        $this->db->order_by('receipt_note_detail_id, bbf_id');
        $this->db->limit(1);
        $material = $this->db->get()->row();
        if ($material == null) {
            $this->db->query("delete from sto_issue_detail where id = '" . $id . "'");
            return;
        } else {
            if ($qty > $material->qty) {
                $this->db->set('qty', $material->qty);
                $this->db->set('bbf_id', $material->bbf_id);
                $this->db->set('receipt_note_detail_id', $material->receipt_note_detail_id);
                $this->db->set('price_per_unit', $material->price_per_unit);
                $this->db->where('id', $id);
                if ((!$this->db->update('sto_issue_detail'))) {
                    die('ติดต่อ IT price');
                } else {
                    $ses = $qty - $material->qty;
                    $data = [
                        'issue_id' => $id,
                        'material_id' => $material_id,
                        'qty' => $ses,
                        'remark' => $issue->remark,
                        'record_status' => 'N',
                    ];
                    if (!$this->db->insert('sto_issue_detail', $data)) {
                        die('ติดต่อ IT price');
                    } else {
                        $this->price2($material_id, $this->db->insert_id(), $ses, $issue);
                    }
                }
            } else {
                $this->db->set('bbf_id', $material->bbf_id);
                $this->db->set('receipt_note_detail_id', $material->receipt_note_detail_id);
                $this->db->set('price_per_unit', $material->price_per_unit);
                $this->db->where('id', $id);
                if (!$this->db->update('sto_issue_detail')) {
                    die('ติดต่อ IT price');
                }
            }
        }
    }

    public function price2($material_id, $id, $qty, $issue)
    {
        $this->db->select('qty, material_id, receipt_note_detail_id, price_per_unit, date, bbf_id');
        $this->db->from('vw_check_qty_material');
        $this->db->where('material_id', $material_id);
        $this->db->where('qty >', '0');
        $this->db->order_by('receipt_note_detail_id, bbf_id');
        $this->db->limit(1);
        $material = $this->db->get()->row();
        if ($material == null) {
            $this->db->query("delete from sto_issue_detail where id = '" . $id . "'");
            return;
        } else {
            if ($qty > $material->qty) {
                $this->db->set('qty', $material->qty);
                $this->db->set('bbf_id', $material->bbf_id);
                $this->db->set('receipt_note_detail_id', $material->receipt_note_detail_id);
                $this->db->set('price_per_unit', $material->price_per_unit);
                $this->db->where('id', $id);
                if ((!$this->db->update('sto_issue_detail'))) {
                    die('ติดต่อ IT price');
                } else {
                    $ses = $qty - $material->qty;
                    $data = [
                        'issue_id' => $id,
                        'material_id' => $material_id,
                        'qty' => $ses,
                        'remark' => $issue->remark,
                        'record_status' => 'N',
                    ];
                    if (!$this->db->insert('sto_issue_detail', $data)) {
                        die('ติดต่อ IT price');
                    } else {
                        $this->price($material_id, $this->db->insert_id(), $ses, $issue);
                    }
                }
            } else {
                $this->db->set('bbf_id', $material->bbf_id);
                $this->db->set('receipt_note_detail_id', $material->receipt_note_detail_id);
                $this->db->set('price_per_unit', $material->price_per_unit);
                $this->db->where('id', $id);
                if (!$this->db->update('sto_issue_detail')) {
                    die('ติดต่อ IT price');
                }
            }
        }
    }

    public function setunit($id)
    {
        // echo $id; die();
        $sql = "SELECT     sto_receipt_note_detail.id,sto_receipt_note_detail.receipt_note_id, sto_receipt_note_detail.total, sto_receipt_note_detail.qty, sto_receipt_note_detail.qty_use, pur_po_detail.unit_id, sto_pr_detail.material_name,
        sto_pr_detail.material_id,sto_receipt_note_detail.price_per_unit,price_use
        FROM         sto_receipt_note_detail INNER JOIN
        pur_po_detail ON sto_receipt_note_detail.po_detail_id = pur_po_detail.id INNER JOIN
        sto_pr_detail ON pur_po_detail.pr_detail_id = sto_pr_detail.id where material_id = '" . $id . "'";
        // echo $sql;
        // echo '<br>';
        // die();
        $query = $this->db->query($sql)->result();
        foreach ($query as $i) {
            $sql = "SELECT id, material_type_id, code, name_th, name_en, description, img, cost_unit_id, location_id, expire, unit_upper_id_1, unit_upper_rate_1, unit_upper_id_2, unit_upper_rate_2,
            unit_upper_id_3, unit_upper_rate_3, unit_upper_id_4, unit_upper_rate_4, unit_upper_id_5, unit_upper_rate_5, last_cost_per_unit, current_qty, current_datetime, max_qty, reorder_qty, cate_1,
            cate_2, cate_3, cate_4, cate_5, cate_6, cate_7, cate_8, cate_9, cate_10, remark_1, remark_2, remark_3, is_active, created_username, created_at, updated_username, updated_at,
            record_status
            FROM sto_material where id = '" . $i->material_id . "'";
            // echo $sql; die();
            $query2 = $this->db->query($sql)->result();
            foreach ($query2 as $i2) {
                $qty = 0;
                $price = 0;
                if ($i->unit_id != $i2->unit_upper_id_1 and $i->qty > 0) {
                    if ($i->unit_id == $i2->unit_upper_id_3) {
                        $qty = $i->qty * $i2->unit_upper_rate_3;
                        $qty = $qty * $i2->unit_upper_rate_2;
                        $qty = $qty * $i2->unit_upper_rate_1;
                        $price = ($i->price_per_unit * $i->qty) / $qty;
                    } elseif ($i->unit_id == $i2->unit_upper_id_2) {
                        $qty = $i->qty * $i2->unit_upper_rate_2;
                        $qty = $qty * $i2->unit_upper_rate_1;
                        $price = ($i->price_per_unit * $i->qty) / $qty;
                    } else {
                        $qty = $i->qty;
                        $price = $i->price_per_unit;
                    }
                    $qty = number_format($qty, 0);
                    $price = str_replace(',', '', $price);
                    $qty = str_replace(',', '', $qty);
                    $price = str_replace(',', '', $price);
                    $sql = "UPDATE sto_receipt_note_detail SET qty_use =  '" . $qty . "', price_use =  '" . $price . "'  where id = '" . $i->id . "'";
                    // echo $sql; die();
                    if ($this->db->query($sql)) {
                        print_r($sql);
                        echo '<br>';
                        continue;
                    } else {
                        print_r($sql);
                        echo 'อัพเดทไม่สำเร็จ';
                        echo '<br>';
                    }
                } else {
                    // die('no');
                    $qty = $i->qty;
                    $price = $i->price_per_unit;
                    $qty = number_format($qty, 0);
                    $price = str_replace(',', '', $price);
                    $qty = str_replace(',', '', $qty);
                    $price = str_replace(',', '', $price);
                    $sql = "UPDATE sto_receipt_note_detail SET qty_use =  '" . $qty . "', price_use =  '" . $price . "'  where id = '" . $i->id . "'";
                    if ($this->db->query($sql)) {
                        print_r($sql);
                        echo '<br>';
                        continue;
                    } else {
                        echo 'อัพเดทไม่สำเร็จ';
                        echo '<br>';
                    }
                }
            }
        }
        echo 'OK';
    }

    public function cl_issue()
    {
    ?>
        <form method="POST" class="form-control">
            <input type="text" name="post_txt" size="20" class="form-control"><br><br>
            <input type="submit" value="send" class="mt-2">
        </form>
        <?php
        $id = $_POST['post_txt'];
        // echo $id; die();
        $sql = "select * from sto_issue where issue_no = '" . $id . "'";
        $cl = $this->db->query($sql)->result();
        foreach ($cl as $cls) {
            $sql = "update sto_issue set record_status = 'D' where id = '" . $cls->id . "'";
            $this->db->query($sql);
            $sql = "update sto_issue_detail set record_status = 'D' where issue_id = '" . $cls->id . "'";
            $this->db->query($sql);
        }
        echo 'OK';
    }

    public function reset_issue_null()
    {
        ini_set('max_execution_time', 800);
        $sql = "SELECT DISTINCT material_id
        FROM         sto_issue_detail
        WHERE     (bbf_id IS NULL) AND (receipt_note_detail_id IS NULL) AND (record_status <> 'D')";
        $id = $this->db->query($sql)->result();
        foreach ($id as $i) {
            $sql = "update sto_issue_detail set price_per_unit = NULL ,bbf_id = NULL, receipt_note_detail_id = NULL where material_id = '" . $i->material_id . "'";
            $this->db->query($sql);
            $code = $i->material_id;
            $this->OK2($code);
        }
    }

    public function checkwork()
    {
        $role_check = 'User';
        foreach ($this->session->userdata("user_profile")->role as $key => $item) {
            if ($item->name == 'ISSUE') {
                if ($this->session->userdata("user_profile")->department_name == 'PERSONNEL') {
                    $role_check = 'HR';
                } else if ($this->session->userdata("user_profile")->position_name == 'DIRECTOR') {
                    $role_check = 'DIR';
                } else {
                    $role_check = 'ISS';
                }
            }
        }
        $this->db->select('sto_issue.id AS issue_id, sto_issue.issue_no, mas_acc_department.code, sto_issue.remark, mas_acc_department.name ,
         vw_user.first_name , vw_user.last_name , CONVERT(varchar, sto_issue.issue_date, 23) AS date, sys_status.color, sto_issue.issue_status,sto_issue.issue_by
         , sto_issue.created_at as issue_date_time');
        $this->db->from('sto_issue');
        $this->db->join('vw_user', 'sto_issue.issue_by = vw_user.id');
        $this->db->join('sys_status', 'sto_issue.issue_status = sys_status.value');
        $this->db->join('mas_acc_department', 'sto_issue.acc_department_id = mas_acc_department.id');
        $this->db->where('sto_issue.record_status', 'N');
        $this->db->where('sys_status.status_type_id', 3); // type ของสโตร์
        if ($role_check == 'HR') {
            $this->db->where_in('sto_issue.issue_status', ($this->input->post('issue_status') == '' ? array('I', 'A', 'DF', 'AD', 'CS') : $this->input->post('issue_status')));
            $this->db->where('sto_issue.department_id', $this->session->userdata("user_profile")->department_id);
            $this->db->or_where('sto_issue.issue_status', 'CH');
            $this->db->where('sto_issue.record_status', 'N');
            $this->db->where('sys_status.status_type_id', 3); // type ของสโตร์
        } else if ($role_check == 'DIR') {
            $this->db->where_in('sto_issue.issue_status', ($this->input->post('issue_status') == '' ? array('I', 'A', 'DF', 'AD', 'CS') : $this->input->post('issue_status')));
            // เฉพาะป๋าเอก เซ้นได้ทุกแผนก
            if ($this->session->userdata("user_profile")->id != 3) {
                $this->db->where('sto_issue.department_id', $this->session->userdata("user_profile")->department_id);
            }
            $this->db->or_where('sto_issue.issue_status', 'CD');
            $this->db->where('sto_issue.record_status', 'N');
            $this->db->where('sys_status.status_type_id', 3); // type ของสโตร์
        } else if ($role_check == 'ISS') {
            $this->db->where_in('sto_issue.issue_status', ($this->input->post('issue_status') == '' ? array('I', 'A', 'DF', 'AD', 'CS') : $this->input->post('issue_status')));
            $this->db->where('sto_issue.department_id', $this->session->userdata("user_profile")->department_id);
        } else {
            $this->db->where_in('sto_issue.issue_status', ($this->input->post('issue_status') == '' ? array('I', 'A', 'DF', 'AD', 'CS') : $this->input->post('issue_status')));
            $this->db->where('sto_issue.issue_by', $this->session->userdata("user_profile")->id);
        }
        $result = $this->db->get()->result();
        $items['data'] = array();
        $count_work = 0;
        foreach ($result as $key => $item) {
            if (in_array($item->issue_status, ['I', 'CD', 'CH',]) && in_array($role_check, ['ISS', 'DIR', 'HR'])) {
                $count_work++;
            }
        }
        if ($count_work > 0) {
            echo json_encode($count_work);
        }
    }

    public function set_price_bbf()
    {
        $sql = 'select * from sto_mt_bbf';
        foreach ($a = $this->db->query($sql)->result() as $data) {
            if ($data->amount == $data->qty * $data->price_per_unit) {
                continue;
            } else {
                $price_per_unit = $data->amount / $data->qty;
                $datas = [
                    'price_per_unit' => $price_per_unit,
                ];
                $this->db->where('id', $data->id);
                $this->db->update('sto_mt_bbf', $datas);
            }
        }
        echo 'ok';
    }

    // ฟังชั่นเซตราคาไฟโฟ้ mt ใหม่ 1 6 22
    // public function set_st_mt($issue_id)
    public function set_st_mt()
    {
        $this->db->set('bbf_id', null);
        $this->db->set('receipt_note_detail_id', null);
        $this->db->update('sto_mt_issue_detail');
        // $this->db->set('price_per_unit', $material->price_per_unit);

        $sql = "select * from sto_mt_issue_detail where record_status = 'N' order by issue_id,material_id,id";
        // ดึงใบเบิกมา
        foreach ($is = $this->db->query($sql)->result() as $issue) {
            // หาข้อมูลจากไอเบิกโดยใช้ไอดีวัสดุ
            $sql = "SELECT total_receipt_note,material_id, balance as qty, price_per_unit, record_date, receipt_note_detail_id, bbf_id , discount , id ,balance * price_per_unit as total_price
            FROM vw_qty_mt_stock
            WHERE material_id = '" . $issue->material_id . "' and (balance <> 0) ORDER BY receipt_note_detail_id,bbf_id";
            $material = $this->db->query($sql)->row();
            if (empty($material)) {
                continue;
            }
            if ($material->discount > 0) {
                $material->price_per_unit = $material->price_per_unit - ($material->price_per_unit * (($material->discount / $material->total_receipt_note) * 100) / 100);
            }
            if ($issue->qty > $material->qty) {
                $this->db->set('qty', $material->qty);
                $this->db->set('price_per_unit', $material->price_per_unit);
                $this->db->set('bbf_id', $material->bbf_id);
                $this->db->set('receipt_note_detail_id', $material->receipt_note_detail_id);
                $this->db->where('id', $issue->id);
                $this->db->update('sto_mt_issue_detail');
                $ses = $issue->qty - $material->qty;
                $data = [
                    'issue_id' => $issue->issue_id,
                    'material_id' => $issue->material_id,
                    'qty' => $ses,
                    'remark' => $issue->remark,
                    'record_status' => 'N',
                ];
                (!$this->db->insert('sto_mt_issue_detail', $data) ? die('contact IT') : '');
                $this->price_mt($issue->material_id, $this->db->insert_id());
            } else {
                $this->db->set('price_per_unit', $material->price_per_unit);
                $this->db->set('bbf_id', $material->bbf_id);
                $this->db->set('receipt_note_detail_id', $material->receipt_note_detail_id);
                $this->db->where('id', $issue->id);
                $this->db->update('sto_mt_issue_detail');
            }
        }
    }

    public function price_mt($material_id, $id)
    {
        $sql = "select * from sto_mt_issue_detail where id = '" . $id . "' and record_status = 'N'";
        $issue = $this->db->query($sql)->row();

        $sql = "SELECT total_receipt_note,material_id, balance as qty, price_per_unit, record_date, receipt_note_detail_id, bbf_id , discount , id ,balance * price_per_unit as total_price
        FROM vw_qty_mt_stock
        WHERE material_id = '" . $material_id . "' and (balance <> 0) ORDER BY receipt_note_detail_id,bbf_id";
        $material = $this->db->query($sql)->row();
        if (empty($material)) {
            $this->db->delete('sto_mt_issue_detail', ['id' => $id]);

            return;
        }
        if ($material->discount > 0) {
            $material->price_per_unit = $material->price_per_unit - ($material->price_per_unit * (($material->discount / $material->total_receipt_note) * 100) / 100);
        }
        if ($issue->qty > $material->qty) {
            $this->db->set('qty', $material->qty);
            $this->db->set('price_per_unit', $material->price_per_unit);
            $this->db->set('bbf_id', $material->bbf_id);
            $this->db->set('receipt_note_detail_id', $material->receipt_note_detail_id);
            $this->db->where('id', $issue->id);
            $this->db->update('sto_mt_issue_detail');
            $ses = $issue->qty - $material->qty;
            $data = [
                'issue_id' => $issue->issue_id,
                'material_id' => $issue->material_id,
                'qty' => $ses,
                'remark' => $issue->remark,
                'record_status' => 'N',
            ];
            (!$this->db->insert('sto_mt_issue_detail', $data) ? die('contact IT') : '');
            $this->price_mt2($issue->material_id, $this->db->insert_id());
        } else {
            $this->db->set('price_per_unit', $material->price_per_unit);
            $this->db->set('bbf_id', $material->bbf_id);
            $this->db->set('receipt_note_detail_id', $material->receipt_note_detail_id);
            $this->db->where('id', $issue->id);
            $this->db->update('sto_mt_issue_detail');
        }
    }

    public function price_mt2($material_id, $id)
    {
        $sql = "select * from sto_mt_issue_detail where id = '" . $id . "' and record_status = 'N'";
        $issue = $this->db->query($sql)->row();

        $sql = "SELECT total_receipt_note,material_id, balance as qty, price_per_unit, record_date, receipt_note_detail_id, bbf_id , discount , id ,balance * price_per_unit as total_price
        FROM vw_qty_mt_stock
        WHERE material_id = '" . $material_id . "' and (balance <> 0) ORDER BY receipt_note_detail_id,bbf_id";
        $material = $this->db->query($sql)->row();
        if (empty($material)) {
            $this->db->delete('sto_mt_issue_detail', ['id' => $id]);

            return;
        }
        if ($material->discount > 0) {
            $material->price_per_unit = $material->price_per_unit - ($material->price_per_unit * (($material->discount / $material->total_receipt_note) * 100) / 100);
        }
        if ($issue->qty > $material->qty) {
            $this->db->set('qty', $material->qty);
            $this->db->set('price_per_unit', $material->price_per_unit);
            $this->db->set('bbf_id', $material->bbf_id);
            $this->db->set('receipt_note_detail_id', $material->receipt_note_detail_id);
            $this->db->where('id', $issue->id);
            $this->db->update('sto_mt_issue_detail');
            $ses = $issue->qty - $material->qty;
            $data = [
                'issue_id' => $issue->issue_id,
                'material_id' => $issue->material_id,
                'qty' => $ses,
                'remark' => $issue->remark,
                'record_status' => 'N',
            ];
            (!$this->db->insert('sto_mt_issue_detail', $data) ? die('contact IT') : '');
            $this->price_mt($issue->material_id, $this->db->insert_id());
        } else {
            $this->db->set('price_per_unit', $material->price_per_unit);
            $this->db->set('bbf_id', $material->bbf_id);
            $this->db->set('receipt_note_detail_id', $material->bbf_id);
            $this->db->where('id', $issue->id);
            $this->db->update('sto_mt_issue_detail');
        }
    }

    // private $CI;
    public function see_material_id()
    {
        ?>
        <form method="POST" class="form-control">
            <input type="text" name="post_txt" size="20" class="form-control"><br><br>
            <input type="submit" value="send" class="mt-2">
        </form>
        <?php
        $id = $_POST['post_txt'];
        if (empty($id)) {
            die('no data');
        }
        $sql = "SELECT id, material_name, material_code FROM (SELECT id, material_name, material_code
        FROM vw_material UNION ALL SELECT id, name_th AS material_name, code as material_code FROM sto_mt_material) AS derivedtbl_1
        where material_code = '" . $id . "'";
        echo $id;
        echo '<br>';
        echo $this->db->query($sql)->row()->id;
    }

    //สำหรับใส่ material_id ของใบรับ SMT(stock maintenance)
    //ใส่โค้ด segment ที่ยังไม่ได้แปลงเท่านั้น ใช้ดู pr_detail_id ที่ material_id = 0 สำหรับของที่เข้า stock mt
    public function see_pr_detail_id_from_smt()
    {
        ?>
        <form method="POST" class="form-control">
            <input type="text" name="post_txt" size="20" class="form-control"><br><br>
            <input type="submit" value="send" class="mt-2">
        </form>
        <?php
        $id = $this->efs_lib->decrypt_segment($_POST['post_txt']);
        // echo $id; die();
        if (empty($id)) {
            die('no data');
        }
        $sql = "SELECT     sto_pr_detail.id, sto_pr_detail.material_id, sto_pr_detail.material_name
        FROM         sto_mt_receipt_note_detail INNER JOIN
                              pur_po_detail ON sto_mt_receipt_note_detail.po_detail_id = pur_po_detail.id INNER JOIN
                              sto_pr_detail ON pur_po_detail.pr_detail_id = sto_pr_detail.id
        WHERE     (sto_mt_receipt_note_detail.receipt_note_id = '" . $id . "')";
        echo 'select * from sto_pr_detail where ';
        foreach ($id = $this->db->query($sql)->result() as $pr_detail_id) {
            echo ' id = ';
            print_r($pr_detail_id->id);
            echo ' or ';
        }
        // die();
        // echo $id;
        // echo '<br>';
        // echo $this->db->query("select id from vw_material where material_code = '" . $id . "'")->row()->id;
    }

    public function count_item()
    {
        $num = 0;
        foreach ($cart = $this->cart->contents() as $i) {
            $num += $i['qty'];
        }
        if ($num > 0) {
            echo json_encode($num);
        }
    }

    public function calc_sp_efins()
    {
        $pr_id = $this->uri->segment(3);
        $this->efs_lib->calc_price_support_efins($pr_id); ?>
        <script>
            window.close()
        </script>
<?php
    }
}
