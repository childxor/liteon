<?php

defined('BASEPATH') or exit('No direct script access allowed');

class Efs_lib
{

    private $CI;

    function __construct()
    {
        /* Assign by reference with "&" so we don't create a copy */
        $this->CI = &get_instance();
        $this->CI->load->library('session');
    }

    /* Load Variable System */

    public function get_var_sys()
    {
        $query = $this->CI->db->query('SELECT name, value_en, value_th, value_jp, value_cn, description FROM sys_variable WHERE is_active = 1 ORDER BY sort ASC');
        if ($query) {
            $nrows = $query->num_rows();
            if ($nrows > 0) {
                $arr = array();
                foreach ($query->result() as $item) {
                    $arr[$item->name] = $item->{"value_" . (empty($this->CI->session->userdata('user_profile')->cng_lang) ? 'th' : $this->CI->session->userdata('user_profile')->cng_lang)};
                }
                return (object) $arr;
            } else {
                return FALSE;
            }
        } else {
            return FALSE;
        }
    }

    /* Load MD Info */

    public function get_md_info($user_id = '')
    {
        $sql = "SELECT sys_role_user.role_id, sys_role_user.user_id, sys_user.emp_code, sys_user.username, "
            . "sys_user.prefix_name, sys_user.gender_id, sys_user.first_name, sys_user.last_name, sys_user.tel, sys_user.email, "
            . "mas_signature.sign, mas_signature.description AS sign_description "
            . "FROM sys_role_user INNER JOIN sys_user ON sys_role_user.user_id = sys_user.id "
            . "LEFT JOIN mas_signature ON mas_signature.user_id = sys_user.id  AND (mas_signature.is_active = '1')"
            . "WHERE (sys_role_user.record_status = 'N') ";
        $sql .= (!empty($user_id) ? "AND sys_user.id ='" . $user_id . "'" : "AND (sys_role_user.role_id = '7') ");
        $query = $this->CI->db->query($sql);
        if ($query) {
            $nrows = $query->num_rows();
            if ($nrows > 0) {
                return $query->row();
            } else {
                return FALSE;
            }
        } else {
            return FALSE;
        }
    }

    /* Generate New Password */

    public function paswd_encrypt($paswd, $hash = PASSWORD_DEFAULT)
    {
        /* $hash Only PASSWORD_DEFAULT, PASSWORD_BCRYPT, PASSWORD_ARGON2I */
        /* ext. $this->efs_lib->paswd_encrypt('123') OR $this->efs_lib->paswd_encrypt('123',PASSWORD_BCRYPT)  return HASH */
        return password_hash($paswd, $hash);
    }

    /* Verify Password */

    public function paswd_decrypt($paswd, $hash)
    {
        /* ext. $this->efs_lib->paswd_decrypt('123', '$2y$10$wF/50jQC9x782KMnljWLb.QEN8lSvGup0XhGEWa2VJNw8ZufqPz.K') RETURN boolean */
        return password_verify($paswd, $hash);
    }

    /* Get Permission User */

    public function get_permission($module = '', $role_id = '', $user_id = '')
    {
        $modules = explode('/', $module);
        // $count = count($modules);
        // die($count);

        $module = $modules[0] . '/' . $modules[1];
        $user_id = (empty($user_id) ? $this->CI->session->userdata('user_profile')->id : $user_id);
        $sql = "SELECT module_id, permission_id, permission, role_id "
            . "FROM vw_permission "
            . "WHERE module = '" . $module . "' ";
        $sql .= (!empty($role_id) ? "AND role_id = '" . $role_id . "'" : "AND user_id = '" . $user_id . "'");
        $query = $this->CI->db->query($sql);
        // die($sql);
        foreach ($query->result() as $item) {
            $data[] = $item->permission_id;
        }
        return $data;
    }

    /* Get Permission Module Role */

    public function get_vw_permission_module_role($module = '', $role_id = '')
    {
        $modules = explode('/', $module);
        $module = $modules[0] . '/' . $modules[1];
        $sql = "SELECT module_id, permission_id, permission, role_id "
            . "FROM vw_permission_module_role "
            . "WHERE module = '" . $module . "' ";
        $sql .= (!empty($role_id) ? "AND role_id = '" . $role_id . "'" : "");
        $query = $this->CI->db->query($sql);
        foreach ($query->result() as $item) {
            $data[] = $item->permission_id;
        }
        return $data;
    }

    /* Get Current Module */

    public function get_current_module($module = '')
    {
        $modules = explode('/', $module);
        $count = count($modules);
        // if ($count == 3) {
        //     $module = $modules[0] . '/' . $modules[1] . '/' . $modules[2];
        // } else {
        //     $module = $modules[0] . '/' . $modules[1];
        // }
        switch ($count) {
            case 4:
                $module = $modules[0] . '/' . $modules[1] . '/' . $modules[2];
                break;
            default:
                $module = $modules[0] . '/' . $modules[1];
                break;
        }

        // $this->print_r($module);
        // print_r($module); 
        // die();
        $id = $this->CI->efs_lib->decrypt_segment($modules[count($modules) - 1]);
        $sql = "SELECT * FROM sys_module WHERE record_status = 'N' ";
        // $sql .= (count($modules) == 4 ? "AND id = '" . $id . "'" : "AND module = '" . $module . "'");
        $sql .= " AND module = '" . $module . "'";
        // die($sql);
        $row = array();
        $row = $this->CI->db->query($sql)->row();
        // เช็คจำนวนแถวที่ได้
        if ($this->CI->db->query($sql)->num_rows() > 0) {
            return $row;
        } else {
            $module = $modules[0] . '/' . $modules[1];
            $sql = "SELECT * FROM sys_module WHERE record_status = 'N' ";
            $sql .= "AND module = '" . $module . "'";
            // $row = $this->CI->db->query($sql)->row();
            if ($this->CI->db->query($sql)->num_rows() > 0) {
                return $this->CI->db->query($sql)->row();
            } else {
                $module = $modules[0];
                $sql = "SELECT * FROM sys_module WHERE record_status = 'N' ";
                $sql .= "AND module = '" . $module . "'";
                if ($this->CI->db->query($sql)->num_rows() > 0) {
                    return $this->CI->db->query($sql)->row();
                } else {
                    return $row;
                }
            }
        }
        // echo '<pre>';
        // print_r($row);
        // echo '</pre>';
        // die();


        return $row;
    }

    /* Get Parent Module */

    public function get_parent_module($module = '')
    {
        $modules = explode('/', $module);
        $module = $modules[0] . '/' . $modules[1];
        $sql = "SELECT * FROM sys_module WHERE record_status = 'N' ";
        $sql .= "AND module = '" . $module . "'";
        // echo $sql; die();
        $row = $this->CI->db->query($sql)->row();
        return $row;
    }

    /* Check Permission Can Do This Data */

    public function is_can($permission, $type)
    {
        /* ext. Check Can View Permission $this->efs_lib->is_can($permission, "view") */
        /* Defind of Type When Insert Or Update permission in DB sys_permission Must Change $type_id */
        $type_id = array("view" => 1, "add" => 2, "edit" => 3, "delete" => 4, "approve" => 5, "report" => 6, "check" => 7);
        foreach ($permission as $val) {
            if ($val === @$type_id[strtolower($type)]) {
                return TRUE;
            }
        }
        return FALSE;
    }

    public function increment_date($day = '90')
    {
        $date = strtotime("+" . $day . " day", strtotime(date("Y-m-d H:i:s")));
        return date('Y-m-d H:i:s', $date);
    }

    /* Check Password User Expired */

    public function check_expired_date($datetime)
    {
        $current_datetime = strtotime(date('Y-m-d H:i:s'));
        $check_datetime = strtotime($datetime);
        // Compare the timestamp date  
        if ($current_datetime > $check_datetime)
            return TRUE;
        else
            return FALSE;
    }

    /* Datetime to Thai */

    public function datetime_to_th($strDate, $time = true)
    {
        if (!empty($strDate)) {
            $strYear = date("Y", strtotime($strDate)) + 543;
            $strMonth = date("n", strtotime($strDate));
            $strDay = date("j", strtotime($strDate));
            $strHour = date("H", strtotime($strDate));
            $strMinute = date("i", strtotime($strDate));
            $strSeconds = date("s", strtotime($strDate));
            $strMonthCut = array("", "ม.ค.", "ก.พ.", "มี.ค.", "เม.ย.", "พ.ค.", "มิ.ย.", "ก.ค.", "ส.ค.", "ก.ย.", "ต.ค.", "พ.ย.", "ธ.ค.");
            $strMonthThai = $strMonthCut[$strMonth];
            return "$strDay $strMonthThai $strYear " . ($time == true ? ", $strHour:$strMinute" : "");
        } else {
            return "";
        }
    }

    /* Datetime to English */

    public function datetime_to_en($strDate, $time = true)
    {
        if (!empty($strDate)) {
            $strYear = date("Y", strtotime($strDate));
            $strMonth = date("n", strtotime($strDate));
            $strDay = date("j", strtotime($strDate));
            $strHour = date("H", strtotime($strDate));
            $strMinute = date("i", strtotime($strDate));
            $strSeconds = date("s", strtotime($strDate));
            $strMonthCut = array("", "JAN", "FEB", "MAR", "APR", "MAY", "JUN", "JUL", "AUG", "SEP", "OCT", "NOV", "DEC");
            $strMonthThai = $strMonthCut[$strMonth];
            return "$strDay $strMonthThai $strYear " . ($time == true ? ", $strHour:$strMinute" : "");
        } else {
            return "";
        }
    }

    /* Date in PO */

    public function date_in_po($strDate)
    {
        if (!empty($strDate)) {
            $strYear = date("Y", strtotime($strDate));
            $strMonth = date("n", strtotime($strDate));
            $strDay = date("j", strtotime($strDate));
            $strMonthCut = array("", "1", "2", "3", "4", "5", "6", "7", "8", "9", "10", "11", "12");
            $strMonth = $strMonthCut[$strMonth];
            return "$strDay/$strMonth/$strYear ";
        } else {
            return "";
        }
    }

    /* Date in national format */

    public function date_format($strDate)
    {
        if (!empty($strDate)) {
            $strYear = date("Y", strtotime($strDate));
            $strMonth = date("n", strtotime($strDate));
            $strDay = date("j", strtotime($strDate));
            $strMonthCut = array("", "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12");
            $strMonth = $strMonthCut[$strMonth];
            return str_pad($strDay, 2, '0', STR_PAD_LEFT) . "/$strMonth/$strYear ";
        } else {
            return "";
        }
    }

    /* Encrypt & Decrypt URI Segment ID */

    function encrypt_segment($string)
    {
        /* you may change these values to your own */
        $secret_key = $this->CI->session->userdata("user_profile")->id;
        $secret_iv = $this->CI->session->userdata("token");

        $output = false;
        $encrypt_method = "AES-256-CBC";
        $key = hash('sha256', $secret_key);
        $iv = substr(hash('sha256', $secret_iv), 0, 16);
        $output = base64_encode(openssl_encrypt($string, $encrypt_method, $key, 0, $iv));
        return $output;
    }

    function decrypt_segment($string)
    {
        // echo $string;
        // die();
        /* you may change these values to your own */
        $secret_key = $this->CI->session->userdata("user_profile")->id;
        $secret_iv = $this->CI->session->userdata("token");

        $output = false;
        $encrypt_method = "AES-256-CBC";
        $key = hash('sha256', $secret_key);
        $iv = substr(hash('sha256', $secret_iv), 0, 16);
        $output = openssl_decrypt(base64_decode($string), $encrypt_method, $key, 0, $iv);
        return $output;
    }

    /* Japanese Insert & Update Query */
    /* Data Type Must use Nvarchar Only in DB */
    /* Insert $table => table name, $data => Array  */

    function query_insert_jp($table, $data)
    {
        foreach ($data as $key => $val) {
            $val = (strstr($val, 'N*') ? "N'" . str_replace("N*", "", str_replace("'", "&#039;", $val) . "'") : "'" . str_replace("'", "&#039;", $val) . "'");
            $fields[] = $key;
            $values[] = $val;
        }
        return 'INSERT INTO ' . $table . ' (' . implode(', ', $fields) . ') VALUES (' . implode(', ', $values) . ')';
    }

    /* Insert $table => table name, $values => Array, $wheres => Array */

    function query_update_jp($table, $values, $wheres)
    {
        foreach ($values as $key => $val) {
            $val = (strstr($val, 'N*') ? "N'" . str_replace("N*", "", str_replace("'", "&#039;", $val) . "'") : "'" . str_replace("'", "&#039;", $val) . "'");
            $valstr[] = $key . " = " . $val;
        }
        foreach ($wheres as $key => $val) {
            $where[] = $key . " = '" . $val . "'";
        }

        return 'UPDATE ' . $table . ' SET ' . implode(', ', $valstr) . ' WHERE ' . implode(', ', $where);
    }
    // test
    /* GET Language For Login System */

    function language_login()
    {
        // $lang = substr($_SERVER['HTTP_ACCEPT_LANGUAGE'], 0, 2);
        // $this->session->set_userdata('lang', $_POST['lang']);
        // $lang = $this->CI->session->userdata('lang');
        $lang = (!empty($this->CI->session->userdata('lang')) ? $this->CI->session->userdata('lang') : 'th');
        // die($lang);
        $this->CI->session->set_userdata('lang', $lang);



        // $this->print_r($_SERVER['HTTP_ACCEPT_LANGUAGE']);
        // $this->print_r($this->CI->session->userdata('user_profile'));
        // $this->print_r($this->CI->uri->segment(2));
        // [HTTP_ACCEPT_LANGUAGE] => en-US,en;q=0.9
        $sql = "SELECT * "
            . "FROM sys_language "
            . "WHERE record_status = 'N' AND module_id = '0' ";
        $sql .= (!empty($type) ? "AND keyword LIKE 'sys_login_%'" : "");
        $query = $this->CI->db->query($sql);
        foreach ($query->result() as $item) {
            $data[$item->keyword] = $item->{($lang == 'ja' ? 'jp' : $lang)};
        }
        return (object) $data;
    }

    /* GET Language For Core System */

    function language_system($type = '')
    {
        // $sql = "SELECT * "
        $sql = "SELECT distinct keyword, th, en, jp, cn "
            . "FROM sys_language "
            . "WHERE record_status = 'N'  ";
        $sql .= (!empty($type) ? "AND keyword LIKE 'sys_" . $type . "%'" : "");
        // die($sql);
        $query = $this->CI->db->query($sql);
        $data = array();
        foreach ($query->result() as $item) {
            // ถ้า $data มี keyword ซ้ำกัน ไม่ต้องเพิ่มเข้าไป 
            if (!isset($data[$item->keyword])) {
                $data[$item->keyword] = $item->{(empty($this->CI->session->userdata('user_profile')->cng_lang) ? 'th' : $this->CI->session->userdata('user_profile')->cng_lang)};
            }
        }
        if (!empty($type)) {
            echo json_encode($data);
        } else {
            return (object) $data;
        }
    }

    /* GET Language For Module */
    function language_module($module_id = '')
    {
        $data = array();
        $sql = "SELECT * "
            . "FROM sys_language "
            . "WHERE record_status = 'N' " . (empty($module_id) ? "" : "AND module_id in (" . $module_id . ") ");
        // die($sql);
        $query = $this->CI->db->query($sql);
        if ($query->num_rows() > 10) {
            foreach ($query->result() as $item) {
                // if (!isset($data[$item->keyword])) {
                $data[$item->keyword] = $item->{(empty($this->CI->session->userdata('user_profile')->cng_lang) ? 'th' : $this->CI->session->userdata('user_profile')->cng_lang)};
                // }
            }
        } else {
            // die('Error: Language Module');
            $sql = "SELECT * "
                . "FROM sys_language "
                . "WHERE record_status = 'N' ";
            $query = $this->CI->db->query($sql);
            $data = array();
            foreach ($query->result() as $item) {
                if (!isset($data[$item->keyword])) {
                    $data[$item->keyword] = $item->{(empty($this->CI->session->userdata('user_profile')->cng_lang) ? 'th' : $this->CI->session->userdata('user_profile')->cng_lang)};
                }
            }
        }
        return (object) $data;
    }

    /* GET Notification By User */

    function get_notification($top = '', $has_read = '0')
    {
        $sql = "SELECT " . $top . " mas_announcement.id, mas_announcement.sort, mas_announcement.title, mas_announcement.txt, mas_announcement.show, mas_announcement.hide, sys_inbox.has_accept, sys_inbox.has_read "
            . "FROM mas_announcement LEFT OUTER JOIN sys_inbox ON mas_announcement.id = sys_inbox.announcement_id "
            . "WHERE (mas_announcement.record_status = 'N') AND (mas_announcement.is_active = '1') AND (sys_inbox.has_read = '" . $has_read . "')"
            . "AND sys_inbox.user_id = '" . $this->CI->session->userdata('user_profile')->id . "' "
            . "ORDER BY sort ASC, show DESC";
            // die($sql);
        $data['nrow'] = $this->CI->db->query($sql)->num_rows();
        $data['result'] = $this->CI->db->query($sql)->result();
        return (object) $data;
    }

    /* Remove all In Directory */

    function rmdir_recursive($dir)
    {
        if (is_dir($dir)) {
            $files = scandir($dir);
            foreach ($files as $file) {
                if ($file == '.' or $file == '..') {
                    continue;
                }
                $file = "$dir/$file";
                if (is_dir($file)) {
                    rmdir_recursive($file);
                    rmdir($file);
                } else {
                    unlink($file);
                }
            }
            return rmdir($dir);
        } else {
            return FALSE;
        }
    }

    /* PDF TO TEXT Reader */

    function mapped_drive($find = "EFT", $len = "9")
    {
        $directory = 'application/third_party/pdfparser/';
        include $directory . 'autoload.php';
        $parser = new \Smalot\PdfParser\Parser();
        // Open a known directory, and proceed to read its contents
        $dir = '\\\\eft-fl1-server\Share\System-Master\test\\'; // PATH TO SERVER DRIVE
        if (is_dir($dir)) {
            if ($dh = opendir($dir)) {
                while (($file = readdir($dh)) !== false) {
                    if ($file != "." && $file != "..") {
                        $pdf = $dir . $file;
                        echo file_exists($pdf) ? '' : 'File: Was not found.';
                        try {
                            $test_encoding0 = 'test encoding: ' . mb_convert_encoding('hello world', 'UTF-8', 'Windows-1252');
                        } catch (Exception $e) {
                            echo 'You need to install php-mbstring to use the parser.';
                            exit();
                        }
                        $po = $parser->parseFile($pdf);

                        $text = $po->getText();
                        $pos = strpos($text, $find);
                        $po_no = substr($text, $pos, $len);
                        rename($pdf, $dir . "/" . $po_no . ".pdf");
                        if (copy($dir . "/" . $po_no . ".pdf", "uploads/po_signed/" . $po_no . ".pdf")) {
                            unlink($dir . "/" . $po_no . ".pdf");
                        }
                    }
                }
                closedir($dh);
            }
        }
    }

    /* Check Limit time */
    public function check_limit_time($datetime, $hours = 0)
    {
        $current_datetime = strtotime(date('Y-m-d H:i:s'));
        $check_datetime = strtotime(strftime($datetime, time() + $hours * 60 * 60));
        //echo $current_datetime .'>'.$check_datetime.'<br>';
        // Compare the timestamp date  
        if ($current_datetime > $check_datetime)
            return 1;
        else
            return 0;
    }

    public function check_not_use_qty()
    {
        $sql = "SELECT DISTINCT supplier_id, material_id
                FROM mas_supplier_material_price
                WHERE (use_qty = '0')";
        $result = $this->CI->db->query($sql)->result();
        foreach ($result as $key => $item) {
            $data['supplier_id'][$key + 1] = $item->supplier_id;
            $data['material_id'][$key + 1] = $item->material_id;
        }
        return $data;
    }

    public function check_not_use_formula()
    {
        $sql = "SELECT DISTINCT supplier_id, material_id
                FROM mas_supplier_material_price
                WHERE (use_formula = '0')";
        $result = $this->CI->db->query($sql)->result();
        foreach ($result as $key => $item) {
            $data['supplier_id'][$key + 1] = $item->supplier_id;
            $data['material_id'][$key + 1] = $item->material_id;
        }
        return $data;
    }

    public function calc_price($pr_id = NULL)
    {
        //$pr_id =  (empty($pr_id) ? $this->CI->uri->segment(3) : $pr_id);

        $not_use_qty = $this->check_not_use_qty();

        $sql = "SELECT sto_pr.discount "
            . "FROM sto_pr "
            . "WHERE sto_pr.record_status='N' AND sto_pr.id='" . $pr_id . "'";
        $pr = $this->CI->db->query($sql)->row();

        $sql = "SELECT sto_pr_detail.id, sto_pr_detail.pr_id, sto_pr_detail.material_type_id, sto_pr_detail.material_id, sto_pr_detail.material_name, sto_pr_detail.amount, sto_pr_detail.unit_id, 
        sto_pr_detail.quotation_request, sto_pr_detail.quotation_group, sto_pr_detail.quotation_id, sto_pr_detail.supplier_material_price_id, sto_pr_detail.pr_detail_status, sto_pr_detail.remark, 
        sto_pr_detail.specifications, sto_pr_detail.budget_code, sto_pr_detail.formula, sto_pr_detail.price_per_unit, sto_pr_detail.created_username, sto_pr_detail.created_at, 
        sto_pr_detail.updated_username, sto_pr_detail.updated_at, sto_pr_detail.record_status, mas_unit.name_th AS unit_name, { fn CONCAT({ fn CONCAT(sto_material_type.code, ' ') }, 
        sto_material_type.name) } AS material_type_name, sys_status.value AS status, sys_status.color AS status_color, mas_supplier_material_price.price, 
        mas_supplier_material_price.use_formula, mas_supplier_material_price.use_qty, mas_supplier_material_price.supplier_id, sto_pr_detail.discount_price 
        FROM sto_pr_detail INNER JOIN mas_unit ON sto_pr_detail.unit_id = mas_unit.id INNER JOIN
        sto_material_type ON sto_pr_detail.material_type_id = sto_material_type.id INNER JOIN
        sys_status ON sys_status.value = sto_pr_detail.pr_detail_status INNER JOIN
        sys_status_type ON sys_status_type.alias = 'pr_detail_status' AND sys_status.status_type_id = sys_status_type.id LEFT OUTER JOIN
        mas_supplier_material_price ON sto_pr_detail.supplier_material_price_id = mas_supplier_material_price.id
        WHERE (sto_pr_detail.record_status = 'N') AND (sto_pr_detail.pr_id = '" . $pr_id . "')
        ORDER BY sto_pr_detail.quotation_group, sto_material_type.code, sto_pr_detail.id";
        //echo $sql."<hr>";

        $total = null;

        $discount_price = null;
        $discount_price += $pr->discount;

        $items = $this->CI->db->query($sql)->result();
        foreach ($items as $i => $item) {
            $discount_price += ($item->discount_price * $item->amount);
            $quotation_price = null;
            $static_price = null;
            $amount = null;
            //$data = array(null);
            $quotation_price = (!empty($item->price_per_unit) ? @eval('return ' . $item->price_per_unit . '*' . (!empty(array_search($item->supplier_id, $not_use_qty['supplier_id']))  ? '1' : $item->amount) . '*' .  (!empty(trim($item->formula)) ? trim($item->formula) : '1') . ';') : '');
            $static_price = (!empty($item->supplier_material_price_id) ? @eval('return ' . $item->price . '*' . ($item->use_qty == '0'  ? '1' : $item->amount) . '*' . (!empty($item->formula && $item->use_formula == '1') ? trim($item->formula) : '1') . ';') : '');
            if ($static_price > 0 || $quotation_price > 0) {
                $amount = (!empty($item->supplier_material_price_id) ? $static_price : $quotation_price);
                $total += floatval($amount);

                $data['pr_id'] = $pr_id;
                $data['pr_detail_id'] = $item->id;
                $data['amount'] = $amount;

                $nrows = $this->CI->db->query("SELECT pr_detail_id FROM tmp_pr_price WHERE pr_detail_id = '" .  $item->id . "'")->num_rows();
                if (empty($nrows)) {
                    $data['created_at'] = date('Y-m-d H:i:s');
                    $data['created_username'] = $this->CI->session->userdata('user_profile')->username;
                    $this->CI->db->insert('tmp_pr_price', $data);
                } else {
                    $data['updated_at'] = date('Y-m-d H:i:s');
                    $data['updated_username'] = $this->CI->session->userdata('user_profile')->username;
                    $this->CI->db->where('pr_detail_id', $item->id);
                    $this->CI->db->update('tmp_pr_price', $data);
                }
            } else {
                $data['amount'] = null;
                $data['updated_at'] = date('Y-m-d H:i:s');
                $data['updated_username'] = $this->CI->session->userdata('user_profile')->username;
                $this->CI->db->where('pr_detail_id', $item->id);
                $this->CI->db->update('tmp_pr_price', $data);
            }
            $this->CI->db->where('pr_id', $pr_id);
            $this->CI->db->update('tmp_pr_price', array('total' => $total, 'total_discount' => $discount_price));
            // echo "pr_id:".$pr_id." total:".$total."<hr>";
        }
    }

    /* ดึงเลขใบเสนอซื้อ(PO NO.) ทั้งหมด */
    public function get_po_no_list($order_by = "ASC", $sent_mail = '1')
    {
        $sql = "SELECT no AS po_no
                FROM pur_po ";
        $sql .= "WHERE sent_mail = " . $sent_mail;
        $sql .= "ORDER BY no " . $order_by;
        // echo $sql; die();
        $result = $this->CI->db->query($sql)->result();
        return $result;
    }

    public function item_name_auto_change($pr_id)
    {
        $sql = "SELECT id, material_id, material_name, material_name_change FROM sto_pr_detail WHERE pr_id = " . $pr_id;
        $items = $this->CI->db->query($sql)->result();
        foreach ($items as $item) {
            if ($item->material_id > 0) {
                $material_code = explode(" ", $item->material_name);
                if (trim($item->material_name_change) != '') {
                    $sql = "UPDATE sto_material SET name_th = '" . $item->material_name_change . "', name_en = '' WHERE id = '" . $item->material_id . "'";
                    $this->CI->db->query($sql);
                }
                /* เปลี่ยนชื่อ Material ให้ตรงกับใบเสนอราคาที่เลือก 13/11/20 */
                $sql_update_material_name = "UPDATE sto_pr_detail SET material_name = '" . $material_code[0] . " " . $item->material_name_change . "' WHERE id = '" . $item->id . "' AND material_name_change NOT IN ('')";
                $this->CI->db->query($sql_update_material_name);
                $sql_update_material_name_change = "UPDATE sto_pr_detail SET material_name_change = '' WHERE id = '" . $item->id . "' AND material_name_change NOT IN ('')";
                $this->CI->db->query($sql_update_material_name_change);
                /* เปลี่ยนชื่อ Material ให้ตรงกับใบเสนอราคาที่เลือก */
            } else {
                /* เปลี่ยนชื่อ Material ให้ตรงกับใบเสนอราคาที่เลือก 13/11/20 */
                $sql_update_material_name = "UPDATE sto_pr_detail SET material_name = material_name_change WHERE id = '" . $item->id . "' AND material_name_change NOT IN ('')";
                $this->CI->db->query($sql_update_material_name);
                $sql_update_material_name_change = "UPDATE sto_pr_detail SET material_name_change = '' WHERE id = '" . $item->id . "' AND material_name_change NOT IN ('')";
                $this->CI->db->query($sql_update_material_name_change);
                /* เปลี่ยนชื่อ Material ให้ตรงกับใบเสนอราคาที่เลือก */
            }
        }
    }
    public function Check_save_yok_pai($material_id, $start)
    {
        $start = date('Y-m-d', strtotime('-1 month', strtotime($start)));
        $month = date('m', strtotime($start));
        $year = date('Y', strtotime($start));
        $this->CI->db->select('sto_report_sumery_detail.id, sto_report_sumery_detail.sto_report_sumery_id, sto_report_sumery_detail.material_id, sto_report_sumery_detail.yok_pai_qty,sto_report_sumery_detail.yok_pai_price, sto_report_sumery_detail.record_status, sto_report_sumery.time');
        $this->CI->db->from('sto_report_sumery_detail');
        $this->CI->db->join('sto_report_sumery', 'sto_report_sumery_detail.sto_report_sumery_id = sto_report_sumery.id', 'INNER');
        $this->CI->db->where('sto_report_sumery_detail.material_id', $material_id);
        $this->CI->db->where('month(sto_report_sumery.time)', $month);
        $this->CI->db->where('year(sto_report_sumery.time)', $year);
        $this->CI->db->where('sto_report_sumery_detail.record_status', 'N');
        $this->CI->db->where('sto_report_sumery.record_status', 'N');
        // die($this->CI->db->get_compiled_select());
        $query = $this->CI->db->get()->row();
        $data[] = array();
        if ($query) {
            $data['yok_pai_qty'] = $query->yok_pai_qty;
            $data['yok_pai_price'] = $query->yok_pai_price;
            return $data;
        } else {
            return false;
        }
    }

    public function  print_r($data)
    {
        echo "<pre>";
        print_r($data);
        echo "</pre>";
        die();
    }

    public function calc_bbf($material_id = NULL, $start = NULL, $end = NULL, $all_show = NULL)
    {
        $data_save = $this->Check_save_yok_pai($material_id, $start);
        // $data_save = false;
        if ($data_save == false) {
            $sql_bbf_sum_rpt = "SELECT SUM(qty) AS qty, SUM(amount) AS amount, discount, receipt_note_id, (SELECT SUM(qty * price_per_unit) AS price
            FROM sto_receipt_note_detail AS ee WHERE (receipt_note_id = derivedtbl_1.receipt_note_id)) AS TotalPriceRp
            FROM (SELECT SUM(sto_receipt_note_detail.qty_use) AS qty, SUM(sto_receipt_note_detail.price_use * sto_receipt_note_detail.qty_use) AS amount, sto_receipt_note.discount, sto_receipt_note.id AS receipt_note_id
            FROM sto_receipt_note_detail INNER JOIN
            pur_po_detail ON sto_receipt_note_detail.po_detail_id = pur_po_detail.id INNER JOIN
            sto_pr_detail ON pur_po_detail.pr_detail_id = sto_pr_detail.id INNER JOIN
            sto_receipt_note ON sto_receipt_note_detail.receipt_note_id = sto_receipt_note.id
            WHERE (sto_receipt_note.record_status = 'N') AND (sto_receipt_note.receipt_date < CONVERT(datetime, '" . $start . " 00:00:00')) AND (sto_receipt_note_detail.record_status <> 'D') AND (sto_pr_detail.material_id = '" . $material_id . "')
            GROUP BY sto_receipt_note.discount, sto_receipt_note.id
            UNION ALL
            SELECT SUM(qty) AS qty, SUM(qty * price_per_unit) AS amount, NULL AS discount, NULL AS receipt_note_id
            FROM sto_bbf
            WHERE   (record_status = 'N') AND (material_id = '" . $material_id . "')) AS derivedtbl_1
            GROUP BY discount, receipt_note_id";

            $result = $this->CI->db->query($sql_bbf_sum_rpt)->result();
            $yok_ma_qty = 0;
            $yok_ma_price = 0;
            foreach ($result as $item) {
                $discount = 0;
                $yok_ma_qty += $item->qty;
                if ($item->discount != NULL) {
                    $discount = ($item->amount * ($item->discount / $item->TotalPriceRp * 100) / 100);
                }
                $yok_ma_price += ($item->amount - $discount);
            }
            $sql_issue_bbf = "SELECT SUM(sto_issue_detail.qty) AS qty, SUM(sto_issue_detail.price_per_unit * sto_issue_detail.qty) AS amount
            FROM sto_issue_detail INNER JOIN sto_issue ON sto_issue_detail.issue_id = sto_issue.id WHERE (sto_issue_detail.material_id = '" . $material_id . "') 
            AND (sto_issue_detail.record_status = 'N') AND (sto_issue.issue_date < CONVERT(datetime, '" . $start . "')) AND (sto_issue.issue_status = 'R')";
            // die($sql_issue_bbf);
            $yok_ma_qty = $yok_ma_qty - $this->CI->db->query($sql_issue_bbf)->row()->qty;
            $yok_ma_price = $yok_ma_price - $this->CI->db->query($sql_issue_bbf)->row()->amount;
        }

        // ใช้แสดงยอดรับเข้าไม่ได้เอาไปคำนวณ
        $sql_rpt = "SELECT SUM(vw_material_balance.total_price) AS total_price, SUM(vw_material_balance.qty_use) AS qty, sto_receipt_note.discount,
        (SELECT SUM(qty * price_per_unit) AS price FROM sto_receipt_note_detail AS ee 
        WHERE (receipt_note_id = vw_material_balance.receipt_note_id)) AS TotalPriceRp, vw_material_balance.receipt_note_id
        FROM vw_material_balance INNER JOIN
        sto_receipt_note ON vw_material_balance.receipt_note_id = sto_receipt_note.id WHERE vw_material_balance.po_id is not null ";
        $sql_rpt .= ($all_show == 0 ? (!empty($start) ? "AND (date BETWEEN '" . $start . " 00:00:00'  AND  '" . $end . " 23:59:59') " : "") : "");
        $sql_rpt .= (!empty($material_id) ? "AND vw_material_balance.material_id = '" . $material_id . "' " : "");
        $sql_rpt .= " GROUP BY sto_receipt_note.discount, vw_material_balance.receipt_note_id";
        // die($sql_rpt);
        $rpt = $this->CI->db->query($sql_rpt)->result();
        $rpt_qty = 0;
        $rpt_price = 0; // ราคารวม
        foreach ($rpt as $item) {
            $discount = 0;
            $rpt_qty += $item->qty;
            if ($item->discount > 0) {
                $discount = ($item->total_price * ($item->discount / $item->TotalPriceRp * 100) / 100);
            }
            $rpt_price += ($item->total_price - $discount);
        }

        // ใช้แสดงยอดจ่ายไม่ได้เอาไปคำนวน
        $sql_issue = "SELECT SUM(sto_issue_detail.qty) AS qty, SUM(sto_issue_detail.price_per_unit * sto_issue_detail.qty) AS amount
        FROM sto_issue_detail INNER JOIN sto_issue ON sto_issue_detail.issue_id = sto_issue.id
        WHERE (sto_issue_detail.record_status = 'N') AND (sto_issue.issue_status <> 'CL') AND (sto_issue.record_status = 'N') ";
        $sql_issue .= (!empty($material_id) ? " AND sto_issue_detail.material_id = '" . $material_id . "' " : "");
        $sql_issue .= ($all_show == 0 ? (!empty($start) ? "AND (sto_issue.issue_date BETWEEN '" . $start . " 00:00:00 '  AND  '" . $end . " 23:59:59') " : "") : "");

        // die($sql_issue);
        $issue_qty = $this->CI->db->query($sql_issue)->row()->qty;
        $issue_price = $this->CI->db->query($sql_issue)->row()->amount;

        $yok_pai_calc_bbf = "SELECT   qty, amount,{ fn IFNULL(discount, 0) } as discount, receipt_note_id, (SELECT   SUM(qty * price_per_unit) AS price FROM sto_receipt_note_detail AS ee
        WHERE (receipt_note_id = derivedtbl_1.receipt_note_id)) AS TotalPriceRp FROM (SELECT   SUM(sto_receipt_note_detail.qty_use) AS qty, SUM(sto_receipt_note_detail.price_use * sto_receipt_note_detail.qty_use) AS amount, sto_receipt_note.discount, sto_receipt_note_detail.receipt_note_id
        FROM sto_receipt_note_detail INNER JOIN pur_po_detail ON sto_receipt_note_detail.po_detail_id = pur_po_detail.id INNER JOIN
        sto_pr_detail ON pur_po_detail.pr_detail_id = sto_pr_detail.id INNER JOIN sto_receipt_note ON sto_receipt_note_detail.receipt_note_id = sto_receipt_note.id
        WHERE (sto_receipt_note.record_status = 'N') AND (sto_receipt_note.receipt_date < CONVERT(datetime, '" . $end . " 23:59:59')) AND (sto_receipt_note_detail.record_status <> 'D') AND (sto_pr_detail.material_id = '" . $material_id . "')
        GROUP BY sto_receipt_note.discount, sto_receipt_note_detail.receipt_note_id UNION ALL SELECT SUM(qty) AS qty, SUM(qty * price_per_unit) AS amount, { fn IFNULL(NULL, 0) } AS discount, { fn IFNULL(NULL, 0) } AS receipt_note_id
        FROM sto_bbf WHERE (record_status = 'N') AND (material_id = '" . $material_id . "')) AS derivedtbl_1";
        $yok_pai_calc_bbf = $this->CI->db->query($yok_pai_calc_bbf)->result();
        $yok_pai_qty = 0;
        $yok_pai_price = 0;
        foreach ($yok_pai_calc_bbf as $item) {
            $discount = 0;
            $yok_pai_qty += $item->qty;
            if ($item->discount > 0) {
                $discount = ($item->amount * ($item->discount / $item->TotalPriceRp * 100) / 100);
            }
            $yok_pai_price += ($item->amount - $discount);
        }
        $yok_pai_calc_rpt = "SELECT SUM(sto_issue_detail.qty) AS qty, SUM(sto_issue_detail.price_per_unit * sto_issue_detail.qty) AS amount
        FROM sto_issue_detail INNER JOIN sto_issue ON sto_issue_detail.issue_id = sto_issue.id WHERE (sto_issue_detail.material_id = '" . $material_id . "') 
        AND (sto_issue_detail.record_status = 'N') AND (sto_issue.issue_date < CONVERT(datetime, '" . $end . " 23:59:59')) AND (sto_issue.issue_status = 'R')  AND (sto_issue.record_status = 'N')";
        // die($yok_pai_calc_rpt);
        $yok_pai_qty = $yok_pai_qty - $this->CI->db->query($yok_pai_calc_rpt)->row()->qty;
        $yok_pai_price = $yok_pai_price - $this->CI->db->query($yok_pai_calc_rpt)->row()->amount;



        if ($data_save != false) {
            // $data['yok_ma_qty'] = $data_save['yok_pai_qty'];
            // $data['yok_ma_price'] = $data_save['yok_pai_price'];
            $data['yok_ma_qty'] = (floatval($data_save['yok_pai_qty']) > 0 ? floatval($data_save['yok_pai_qty']) : 0);
            $data['yok_ma_price'] = (floatval($data_save['yok_pai_price']) > 0 ? floatval($data_save['yok_pai_price']) : 0);
            $data['receipt_qty'] = $rpt_qty;
            $data['receipt_price'] = $rpt_price;
            $data['issue_qty'] = $issue_qty;
            $data['issue_price'] = $issue_price;
            $data['yok_pai_qty'] = $data_save['yok_pai_qty'] + $rpt_qty - $issue_qty;
            $data['yok_pai_price'] = $data_save['yok_pai_price'] + $rpt_price - $issue_price;
        } else {
            $data = array(
                'yok_ma_qty' => $yok_ma_qty,
                'yok_ma_price' => $yok_ma_price,
                'receipt_qty' => $rpt_qty,
                'receipt_price' => $rpt_price,
                'issue_qty' => $issue_qty,
                'issue_price' => $issue_price,
                'yok_pai_qty' => $yok_pai_qty,
                'yok_pai_price' => $yok_pai_price
            );
        }
        // $this->print_r($data);
        // ยอดมาของเดือนใหม่ไม่ตรงกับยอดยกไปเดือนเก่า 14/12/21
        return $data;
    }

    public function calc_bbf_mt($material_id = NULL, $start = NULL, $all_show = NULL, $end)
    {
        // คำนวนยอดยกมาและยอดรับก่อนหน้าวันที่เลือก
        $sql_bbf_sum_rpt = "SELECT SUM(qty) AS qty, SUM(amount) AS amount , discount , receipt_no , id
         FROM (SELECT SUM(sto_mt_receipt_note_detail.qty) AS qty, SUM(sto_mt_receipt_note_detail.price_per_unit * sto_mt_receipt_note_detail.qty) AS amount ,sto_mt_receipt_note.discount ,sto_mt_receipt_note.receipt_no,sto_mt_receipt_note.id
         FROM sto_mt_receipt_note_detail INNER JOIN pur_po_detail ON sto_mt_receipt_note_detail.po_detail_id = pur_po_detail.id INNER JOIN
         sto_pr_detail ON pur_po_detail.pr_detail_id = sto_pr_detail.id INNER JOIN sto_mt_receipt_note ON sto_mt_receipt_note_detail.receipt_note_id = sto_mt_receipt_note.id
         WHERE (sto_mt_receipt_note.record_status = 'N') AND (sto_mt_receipt_note.receipt_date < CONVERT(datetime, '" . $start . "')) AND (sto_mt_receipt_note_detail.record_status <> 'D') 
         AND (sto_pr_detail.material_id = '" . $material_id . "') group by sto_mt_receipt_note.discount,sto_mt_receipt_note.receipt_no , sto_mt_receipt_note.id UNION ALL SELECT SUM(qty) AS qty, SUM(qty*price_per_unit) AS amount, 
         NULL as discount ,NULL as receipt_no ,NULL as id
         FROM sto_mt_bbf WHERE (record_status = 'N') 
         AND (material_id = '" . $material_id . "')) AS derivedtbl_1 group by  derivedtbl_1.discount , receipt_no , id";
        $price_yok_ma = 0;
        $qty_yok_ma = 0;
        //  echo $sql_bbf_sum_rpt; die(); 
        foreach ($i = $this->CI->db->query($sql_bbf_sum_rpt)->result() as $data) {
            $qty_yok_ma += $data->qty;
            if ($data->discount > 0) {
                $sql = "SELECT receipt_note_id, sum(total_price) as total_price, record_status
                FROM sto_mt_receipt_note_detail
                WHERE (record_status = 'N') AND (receipt_note_id = '" . $data->id . "')
                GROUP BY receipt_note_id, record_status";
                $discount = ($data->amount * ($data->discount / $this->CI->db->query($sql)->row()->total_price * 100) / 100);
                $data->amount = $data->amount - $discount;
            }
            $price_yok_ma += $data->amount;
        }
        // ใช้คำนวนยอดจ่ายก่อนหน้าวันที่เลือกเพื่อหายอดยกมา
        $sql_issue_bbf = "SELECT SUM(sto_mt_issue_detail.qty) AS qty, SUM(sto_mt_issue_detail.price_per_unit * sto_mt_issue_detail.qty) AS amount
         FROM sto_mt_issue_detail INNER JOIN sto_mt_issue ON sto_mt_issue_detail.issue_id = sto_mt_issue.id WHERE (sto_mt_issue_detail.material_id = '" . $material_id . "') 
         AND (sto_mt_issue_detail.record_status = 'N') AND (sto_mt_issue.issue_date < CONVERT(datetime, '" . $start . "')) AND (sto_mt_issue.issue_status = 'R')";

        // ใช้แสดงยอดรับเข้าไม่ได้เอาไปคำนวณ
        $sql_rpt = "SELECT SUM(sto_mt_receipt_note_detail.qty) AS qty, SUM(sto_mt_receipt_note_detail.qty * sto_mt_receipt_note_detail.price_per_unit) AS total_price, sto_mt_receipt_note.discount , sto_mt_receipt_note.id
        FROM sto_mt_receipt_note INNER JOIN sto_mt_receipt_note_detail ON sto_mt_receipt_note.id = sto_mt_receipt_note_detail.receipt_note_id INNER JOIN
        pur_po_detail ON sto_mt_receipt_note_detail.po_detail_id = pur_po_detail.id INNER JOIN
        sto_pr_detail ON pur_po_detail.pr_detail_id = sto_pr_detail.id WHERE (sto_mt_receipt_note_detail.record_status = 'N') ";
        $sql_rpt .= ($all_show == 0 ? (!empty($start) ? "AND (sto_mt_receipt_note.receipt_date BETWEEN '" . $start . " 00:00:00'  AND  '" . $end . " 23:59:59') " : "") : "");
        $sql_rpt .= (!empty($material_id) ? "AND material_id = '" . $material_id . "' " : "");
        $sql_rpt .= " GROUP BY sto_mt_receipt_note.discount,sto_mt_receipt_note.id";
        $rpt_qty = 0;
        $rpt_price = 0;
        $rpt_qty_sum_price = $this->CI->db->query($sql_rpt)->result();
        // echo $sql_rpt; die();
        foreach ($rpt_qty_sum_price as $i) {
            $rpt_qty += (!empty($i->qty) ? $i->qty : 0);
            // print_r($i->qty);
            if ($i->discount > 0) {
                // print_r($i->discount);
                $sql = "SELECT receipt_note_id, sum(qty*price_per_unit) as total_price, record_status
                FROM sto_mt_receipt_note_detail
                WHERE (record_status = 'N') AND (receipt_note_id = '" . $i->id . "')
                GROUP BY receipt_note_id, record_status";
                $discount = ($i->discount / $this->CI->db->query($sql)->row()->total_price * 100);

                $rpt_price += $i->total_price - ($i->total_price * $discount / 100);
            } else {
                $rpt_price += $i->total_price;
            }
        }
        // ใช้แสดงยอดจ่ายไม่ได้เอาไปคำนวน
        $sql_issue = "SELECT SUM(sto_mt_issue_detail.qty) AS qty, SUM(sto_mt_issue_detail.price_per_unit * sto_mt_issue_detail.qty) AS amount
         FROM sto_mt_issue_detail INNER JOIN sto_mt_issue ON sto_mt_issue_detail.issue_id = sto_mt_issue.id
         WHERE (sto_mt_issue_detail.record_status = 'N') AND (sto_mt_issue.issue_status = 'R') ";
        $sql_issue .= (!empty($material_id) ? " AND sto_mt_issue_detail.material_id = '" . $material_id . "' " : "");
        $sql_issue .= ($all_show == 0 ? (!empty($start) ? "AND (sto_mt_issue.issue_date BETWEEN '" . $start . " 00:00:00 '  AND  '" . $end . " 23:59:59') " : "") : "");
        // echo $sql_issue; die();
        $issue_qty = $this->CI->db->query($sql_issue)->row()->qty;
        $issue_price = $this->CI->db->query($sql_issue)->row()->amount;
        $yok_ma_qty = $qty_yok_ma - $this->CI->db->query($sql_issue_bbf)->row()->qty;
        $yok_ma_price = $price_yok_ma - $this->CI->db->query($sql_issue_bbf)->row()->amount;
        // ใช้คำนวนยอดยกไปยกมาให้มันตรงกัน -*- 17-08-2021
        $data = array(
            'yok_ma_qty' => $yok_ma_qty,
            'yok_ma_price' => $yok_ma_price,
            'receipt_qty' => $rpt_qty,
            'receipt_price' => $rpt_price,
            'issue_qty' => $issue_qty,
            'issue_price' => $issue_price,
            'yok_pai_qty' => $yok_ma_qty + $rpt_qty - $issue_qty,
            'yok_pai_price' => $yok_ma_price + $rpt_price - $issue_price,
        );
        return $data;
    }

    public function calc_bbf2($material_id = NULL, $start = NULL)
    {
        // ยังมีแต่รับเข้ายังไม่มีการเบิก
        // รอดำเนินการทำให้สมบูรณ์ 

        // $not_use_qty = $this->check_not_use_qty();

        $by_price = array();
        $price_received = 0;
        $price = 1;
        $unit_per_price = 1;
        $qty = 0;

        // ยอดที่ยกมาจากต้นปี 
        $sql_sto_bbf = "SELECT  material_id, SUM(qty) AS qty, price_per_unit, SUM(amount) AS amount, sto_bbf.record_date    
            FROM sto_bbf 
            WHERE record_status ='N' AND year ='" . date('Y') . "' AND material_id = '" . $material_id . "' 
            GROUP BY material_id, price_per_unit, record_date ";
        // echo $sql_sto_bbf;
        $query = $this->CI->db->query($sql_sto_bbf)->result();
        $num = 0;
        $price = 0;
        foreach ($query as $bbf) {
            $num += $bbf->qty;
            $price += $bbf->price_per_unit * $bbf->qty;
        }

        $sql = "SELECT SUM(sto_receipt_note_detail.qty_use) AS qty, sto_pr_detail.material_id, sto_receipt_note_detail.price_per_unit, sto_receipt_note.receipt_date
        FROM sto_receipt_note_detail INNER JOIN
        pur_po_detail ON sto_receipt_note_detail.po_detail_id = pur_po_detail.id INNER JOIN
        sto_pr_detail ON pur_po_detail.pr_detail_id = sto_pr_detail.id INNER JOIN
        sto_receipt_note ON sto_receipt_note_detail.receipt_note_id = sto_receipt_note.id
        GROUP BY sto_receipt_note_detail.qty_use, sto_pr_detail.material_id, sto_receipt_note_detail.price_per_unit, sto_receipt_note.receipt_date
        HAVING material_id = '" . $material_id . "' AND receipt_date < convert(datetime,'" . $start . "') ";
        // echo $sql;
        $query = $this->CI->db->query($sql)->result();
        foreach ($query as $sumqty) {
            $num += $sumqty->qty;
            $price += $sumqty->price_per_unit * $sumqty->qty;
        }

        $sql_issue = "SELECT sto_material.id, SUM(sto_issue_detail.qty) AS qty, sto_issue_detail.price_per_unit
        FROM sto_material_type INNER JOIN
        sto_material ON sto_material_type.id = sto_material.material_type_id INNER JOIN
        sto_issue INNER JOIN
        sto_issue_detail ON sto_issue.id = sto_issue_detail.issue_id ON sto_material.id = sto_issue_detail.material_id LEFT OUTER JOIN
        mas_unit ON sto_material.unit_upper_id_1 = mas_unit.id
        WHERE (sto_issue.record_status = 'N') AND (sto_issue_detail.record_status = 'N') AND (sto_issue.issue_status IN ('I','P', 'R')) 
        AND (sto_issue_detail.price_per_unit = '" . ($price / $unit_per_price) . "') AND (sto_issue.issue_date < convert(datetime,'" . $start . "')) AND (sto_material.id = '" . $material_id . "')
        GROUP BY sto_material.id, sto_issue_detail.price_per_unit";
        $sql_issue = "SELECT sto_issue_detail.qty, sto_issue_detail.price_per_unit, sto_issue_detail.issue_id, sto_issue.issue_date
        FROM         sto_issue_detail INNER JOIN
                              sto_issue ON sto_issue_detail.issue_id = sto_issue.id
        WHERE     (sto_issue_detail.material_id = '" . $material_id . "') AND (sto_issue_detail.record_status = 'N') AND (sto_issue.issue_date < CONVERT(datetime, '" . $start . "'))";
        // echo $sql_issue; //die();
        $issue = $this->CI->db->query($sql_issue)->result();
        foreach ($issue as $delqty) {
            $num -= $delqty->qty;
            $price -= $delqty->price_per_unit * $delqty->qty;
        }
        $data[] = array(
            'qty' => $num,
            'price_received' => $price
        );
        return $data;
    }

    /* ACC MATERIAL CALCULATOR */

    public function calc_material_receipt($material_id = NULL, $all_show = 0, $start = NULL, $end = NULL)
    {
        $data[] = '';
        $not_use_qty = $this->check_not_use_qty();
        $sql = "SELECT po_id, receipt_note_id, receipt_note_detail_id, date, material_id, name_th, qty_use, balance, total_price, price_per_unit, price, unit, material_type_code, material_type_name, 
        unit_upper_id_1, cost_unit_id, unit_upper_rate_1, unit_upper_id_2, unit_upper_rate_2, unit_upper_id_3, unit_upper_rate_3, unit_upper_id_4, unit_upper_rate_4, unit_upper_id_5, unit_upper_rate_5, 
        receipt_no, formula, material_code, receipt_type, supplier_id, supplier_name, not_use_qty, not_use_formula, po_detail_id, receipt_unit_id, unit_id, id
        FROM vw_material_balance
        WHERE po_id is not null ";
        $sql .= ($all_show == 0 ? (!empty($start) ? "AND (date BETWEEN '" . $start . "'  AND  '" . $end . "') " : "") : "");
        $sql .= (!empty($material_id) ? "AND material_id = '" . $material_id . "' " : "");
        $sql .= "ORDER BY date ASC ";
        // echo $sql; die();
        $detail_receipt = $this->CI->db->query($sql)->result();
        $carry_min = 0;
        foreach ($detail_receipt as $detail) {
            $total_price = 0;
            $total_price += $detail->total_price;
            // เพิ่มเวลาเข้าไป
            $carry_min++;
            $datetime = date('Y-m-d H:i:s', strtotime('+' . $carry_min . ' minutes', strtotime($detail->date)));
            $data[] = array(
                'type' => $detail->supplier_name,
                'paper_no' => $detail->receipt_type . $detail->receipt_no,
                'date' => $this->date_format($detail->date),
                'material_type_code' => $detail->material_type_code,
                'material_code' => $detail->material_code,
                'material_name' => $detail->name_th,
                'unit' => $detail->unit,
                'qty' => $detail->qty_use,
                'price_per_unit' => number_format($detail->price_per_unit * $detail->qty_use, 2),
                'total_price' => $total_price,
                'datetime' => $datetime
            );
        }
        return $data;
    }

    public function calc_material_receipt_mt($material_id = NULL, $all_show = 0, $start = NULL, $end = NULL)
    {
        $data[] = '';
        $not_use_qty = $this->check_not_use_qty();
        $sql = "SELECT sto_mt_receipt_note.id AS receipt_note_id, pur_po_detail.po_id, sto_pr_detail.material_id, sto_mt_receipt_note.receipt_type, sto_mt_receipt_note_detail.id AS receipt_note_detail_id, 
        sto_pr_detail.material_name AS name_th, sto_mt_receipt_note_detail.qty_use, sto_mt_receipt_note_detail.qty, sto_mt_receipt_note_detail.total_price, sto_mt_receipt_note_detail.price_per_unit, 
        mas_unit.name_th AS unit, sto_mt_material.code AS material_type_code, sto_mt_material_type.name AS material_type_name, sto_mt_material.unit_upper_id_1, sto_mt_material.unit_upper_rate_1, 
        sto_mt_material.unit_upper_id_2, sto_mt_material.unit_upper_rate_2, sto_mt_material.unit_upper_id_3, sto_mt_material.unit_upper_rate_3, sto_mt_material.unit_upper_id_4, 
        sto_mt_material.unit_upper_rate_4, sto_mt_receipt_note.receipt_no, sto_mt_receipt_note_detail.formula, sto_mt_material.code AS material_code, pur_po.supplier_id, 
        mas_supplier.name AS supplier_name , sto_mt_receipt_note.receipt_date as date, sto_mt_receipt_note.discount
        FROM mas_unit INNER JOIN
        pur_po_detail INNER JOIN
        sto_pr_detail ON pur_po_detail.pr_detail_id = sto_pr_detail.id ON mas_unit.id = sto_pr_detail.unit_id INNER JOIN
        sto_mt_material ON sto_pr_detail.material_id = sto_mt_material.id INNER JOIN
        sto_mt_material_type ON sto_mt_material.mt_material_type_id = sto_mt_material_type.id INNER JOIN
        pur_po ON pur_po_detail.po_id = pur_po.id INNER JOIN
        mas_supplier ON pur_po.supplier_id = mas_supplier.id RIGHT OUTER JOIN
        sto_mt_receipt_note RIGHT OUTER JOIN
        sto_mt_receipt_note_detail ON sto_mt_receipt_note.id = sto_mt_receipt_note_detail.receipt_note_id ON pur_po_detail.id = sto_mt_receipt_note_detail.po_detail_id
        WHERE (sto_mt_receipt_note.receipt_type = 'SMT') AND sto_mt_receipt_note.record_status = 'N' ";
        $sql .= ($all_show == 0 ? (!empty($start) ? "AND (sto_mt_receipt_note.receipt_date BETWEEN '" . $start . "'  AND  '" . $end . "') " : "") : "");
        $sql .= (!empty($material_id) ? " AND material_id = '" . $material_id . "' " : "");
        $sql .= " ORDER BY date ASC ";
        // ตั้งเวลาให้เรียงลำดับให้ตรงตามจริง
        // echo $sql; die();
        $detail_receipt = $this->CI->db->query($sql)->result();
        // exit();
        $carry_min = 0;
        foreach ($detail_receipt as $detail) {
            $price_per_unit = str_replace(',', '', number_format($detail->price_per_unit, 2));
            // ถ้ามี discount ให้หาจำนวนรวมของใบรับนั้น
            // เพื่อเอาจำนวนที่ลดรวมมาหาเปอร์เซ็น 
            // จากนั้นเอาเปอร์เซ็นมาลบกับราคารวมของวัสดุนั้นๆ
            if ($detail->discount > 0) {
                $price_per_unit = str_replace(',', '', number_format($detail->total_price, 2));
                $perzen = 0;
                $sql = "select total_price from sto_mt_receipt_note_detail where receipt_note_id = '" . $detail->receipt_note_id . "'";
                // echo $sql;
                // die();
                $result = $this->CI->db->query($sql)->result();
                foreach ($result as $i) {
                    $perzen += $i->total_price;
                }
                // echo $perzen; die();
                $discount = ($detail->discount * 100 / $perzen) / 100;
                $price_per_unit -= ($price_per_unit * $discount);
                $total_price = $price_per_unit;
            } else {
                $total_price = 0;
                $total_price += $detail->total_price;
            }
            $carry_min++;
            $datetime = date('Y-m-d H:i:s', strtotime('+' . $carry_min . ' minutes', strtotime($detail->date)));
            $data[] = array(
                'type' => $detail->supplier_name,
                'paper_no' => $detail->receipt_type . $detail->receipt_no,
                'date' => $this->date_format($detail->date),
                'material_type_code' => $detail->material_type_code,
                'material_code' => $detail->material_code,
                'material_name' => $detail->name_th,
                'unit' => $detail->unit,
                'qty' => $detail->qty,
                'price_per_unit' => $price_per_unit,
                'total_price' => $total_price,
                'datetime' => $datetime
            );
        }
        // die();
        return $data;
    }

    public function setunit($receipt_note_id)
    {
        // echo $receipt_note_id;
        // die();
        // die('die');
        $sql = "SELECT     sto_receipt_note_detail.id,sto_receipt_note_detail.receipt_note_id, sto_receipt_note_detail.total, sto_receipt_note_detail.qty, sto_receipt_note_detail.qty_use, pur_po_detail.unit_id, sto_pr_detail.material_name, 
        sto_pr_detail.material_id,sto_receipt_note_detail.price_per_unit,price_use
        FROM         sto_receipt_note_detail INNER JOIN
        pur_po_detail ON sto_receipt_note_detail.po_detail_id = pur_po_detail.id INNER JOIN
        sto_pr_detail ON pur_po_detail.pr_detail_id = sto_pr_detail.id
        WHERE     (sto_receipt_note_detail.receipt_note_id = '" . $receipt_note_id . "')";
        $query = $this->CI->db->query($sql)->result();
        foreach ($query as $i) {
            $sql = "SELECT id, material_type_id, code, name_th, name_en, description, img, cost_unit_id, location_id, expire, unit_upper_id_1, unit_upper_rate_1, unit_upper_id_2, unit_upper_rate_2, 
            unit_upper_id_3, unit_upper_rate_3, unit_upper_id_4, unit_upper_rate_4, unit_upper_id_5, unit_upper_rate_5, last_cost_per_unit, current_qty, current_datetime, max_qty, reorder_qty, cate_1, 
            cate_2, cate_3, cate_4, cate_5, cate_6, cate_7, cate_8, cate_9, cate_10, remark_1, remark_2, remark_3, is_active, created_username, created_at, updated_username, updated_at, 
            record_status 
            FROM sto_material where id = '" . $i->material_id . "'";
            $query2 = $this->CI->db->query($sql)->result();
            foreach ($query2 as $i2) {
                $qty = 0;
                $price = 0;
                if ($i->unit_id <> $i2->unit_upper_id_1 and $i->qty > 0) {
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
                    $price = number_format(str_replace(',', '', $price), 4);
                    $qty = str_replace(',', '', $qty);
                    $price = str_replace(',', '', $price);
                    // $sql = "UPDATE sto_receipt_note_detail SET qty_use =  '" . $qty . "', price_use =  '" . $price . "'  where id = '" . $i->id . "'";
                    // if ($this->db->query($sql)) {
                    //     print_r($sql);
                    //     echo '<br>';
                    //     continue;
                    // } else {
                    //     print_r($sql);
                    //     echo 'อัพเดทไม่สำเร็จ';
                    //     echo '<br>';
                    // }
                    return $qty;
                } else {
                    $qty = $i->qty;
                    $price = $i->price_per_unit;
                    $qty = number_format($qty, 0);
                    $price = number_format(str_replace(',', '', $price), 4);
                    $qty = str_replace(',', '', $qty);
                    $price = str_replace(',', '', $price);
                    // $sql = "UPDATE sto_receipt_note_detail SET qty_use =  '" . $qty . "', price_use =  '" . $price . "'  where id = '" . $i->id . "'";
                    // if ($this->db->query($sql)) {
                    //     print_r($sql);
                    //     echo '<br>';
                    //     continue;
                    // } else {
                    //     echo 'อัพเดทไม่สำเร็จ';
                    //     echo '<br>';
                    // }
                    return $qty;
                }
            }
        }
        echo 'OK';
    }

    public function calc_material_issue($material_id, $all_show, $start, $end, $department, $agency)
    {
        $issue_status = "'R'";
        $data[] = '';

        $sql = "SELECT sto_issue.issue_no, sto_material_type.code AS material_type_code, RIGHT('000' + CAST(sto_material.code AS varchar), 4) AS material_code, sto_material.name_th AS material_name, 
        sto_issue_detail.qty, sto_issue_detail.price_per_unit, sto_issue.issue_status, sto_issue.issue_date AS issue_date, mas_unit.name_th AS unit, sto_issue.created_at ,
        sto_issue_detail.id, sto_issue_detail.receipt_note_detail_id, sto_issue.department_id, sto_issue.acc_department_id
        FROM sto_material_type INNER JOIN
                sto_material ON sto_material_type.id = sto_material.material_type_id INNER JOIN
                sto_issue INNER JOIN
                sto_issue_detail ON sto_issue.id = sto_issue_detail.issue_id ON sto_material.id = sto_issue_detail.material_id LEFT OUTER JOIN
                mas_unit ON sto_material.unit_upper_id_1 = mas_unit.id
        WHERE (sto_issue.record_status = 'N') AND (sto_issue_detail.record_status = 'N') AND (sto_issue.issue_status IN (" . $issue_status . ")) ";
        $sql .= (!empty($department) ? "AND  sto_issue.department_id = '" . $department . "'" : "");
        $sql .= (!empty($agency) ? "AND  sto_issue.acc_department_id = '" . $agency . "'" : "");
        $sql .= ($all_show == 0 ? (!empty($start) ? "AND (sto_issue.issue_date BETWEEN '" . $start . " 00:00:00' AND '" . $end . " 23:59:59') " : "") : "");
        $sql .= (!empty($material_id) ? "AND sto_material.id = '" . $material_id . "' " : "");

        $sql .= "ORDER BY sto_issue.issue_date ASC ";
        // echo $sql; die();
        $carry_min = 0;

        foreach ($this->CI->db->query($sql)->result() as $detail) {
            $total_price = 0;

            // AMOUNT
            $total_price = str_replace(',', '', (number_format($total_price + $detail->qty * $detail->price_per_unit, 4)));

            // เพิ่มเวลาเข้าไป
            $carry_min++;
            $datetime = date('Y-m-d H:i:s', strtotime('+' . $carry_min . ' minutes', strtotime($detail->issue_date)));

            $data[] = array(
                // 'sql' => $sql,
                'type' => 'ISS',
                'paper_no' => $detail->issue_no,
                'date' => $this->date_format($detail->issue_date),
                'material_type_code' => $detail->material_type_code,
                'material_code' => $detail->material_code,
                'material_name' => $detail->material_name,
                'unit' => $detail->unit,
                'qty' => $detail->qty,
                'price_per_unit' => $detail->price_per_unit,
                'total_price' => $total_price,
                'datetime' => $datetime
            );
        }
        // echo "asdf";
        return $data;
    }
    public function calc_material_issue_mt($material_id, $all_show, $start, $end)
    {
        $issue_status = "'R'";
        $data[] = '';

        $sql = "SELECT     sto_mt_issue.issue_no, sto_mt_material_type.code AS material_type_code, sto_mt_material.code as material_code, sto_mt_material.name_th AS material_name, sto_mt_issue_detail.qty, 
        sto_mt_issue_detail.price_per_unit, sto_mt_issue.issue_status, CONVERT(char(10), sto_mt_issue.issue_date, 126) AS issue_date, mas_unit.name_th AS unit, sto_mt_issue.created_at, 
        sto_mt_issue_detail.id, sto_mt_issue_detail.receipt_note_detail_id, sto_mt_issue.department_id, sto_mt_issue.acc_department_id
        FROM sto_mt_material_type INNER JOIN
        sto_mt_material ON sto_mt_material_type.id = sto_mt_material.mt_material_type_id INNER JOIN
        sto_mt_issue INNER JOIN
        sto_mt_issue_detail ON sto_mt_issue.id = sto_mt_issue_detail.issue_id ON sto_mt_material.id = sto_mt_issue_detail.material_id LEFT OUTER JOIN
        mas_unit ON sto_mt_material.unit_upper_id_1 = mas_unit.id
        WHERE (sto_mt_issue.record_status = 'N') AND (sto_mt_issue.issue_status IN (" . $issue_status . ")) ";
        // $sql .= (!empty($department) ? "AND  sto_issue.department_id = '" . $department . "'" : "");
        // $sql .= (!empty($agency) ? "AND  sto_issue.acc_department_id = '" . $agency . "'" : "");
        $sql .= ($all_show == 0 ? (!empty($start) ? "AND (sto_mt_issue.issue_date BETWEEN '" . $start . "'  AND  '" . $end . "') " : "") : "");

        $sql .= (!empty($material_id) ? " AND sto_mt_material.id = '" . $material_id . "' " : "");

        $sql .= " ORDER BY sto_mt_issue.issue_date ASC ";
        // echo $sql; die();
        $carry_min = 0;
        // echo $sql; die();
        $result = $this->CI->db->query($sql)->result();
        foreach ($result as $detail) {
            $total_price = 0;
            // AMOUNT
            $total_price = str_replace(',', '', (number_format($total_price + $detail->qty * $detail->price_per_unit, 4)));

            // เพิ่มเวลาเข้าไป
            $carry_min++;
            $datetime = date('Y-m-d H:i:s', strtotime('+' . $carry_min . ' minutes', strtotime($detail->issue_date)));

            $data[] = array(
                // 'sql' => $sql,
                'type' => 'ISS',
                'paper_no' => $detail->issue_no,
                'date' => $this->date_format($detail->issue_date),
                'material_type_code' => $detail->material_type_code,
                'material_code' => $detail->material_code,
                'material_name' => $detail->material_name,
                'unit' => $detail->unit,
                'qty' => $detail->qty,
                'price_per_unit' => $detail->price_per_unit,
                'total_price' => $total_price,
                'datetime' => $datetime
            );
        }
        return $data;
    }

    function calc_material_stock_control($material_id, $all_show, $start, $end)
    {
        $receipt = $this->calc_material_receipt_control($material_id, $all_show, $start, $end);
        $issue = $this->calc_material_issue_control($material_id, $all_show, $start, $end);
        $merge = array_merge_recursive($receipt, $issue);
        return $merge;
    }

    public function calc_material_receipt_control($material_id = NULL, $all_show = 0, $start = NULL, $end = NULL)
    {
        // echo $all_show; die();
        ini_set('max_execution_time', 8000);
        $data[] = '';
        $sql = "SELECT sto_receipt_note_detail.qty_use,sto_receipt_note.receipt_type, sto_receipt_note.receipt_no, sto_material_type.code AS material_type_code, RIGHT('000' + CAST(sto_material.code AS varchar), 4) AS material_code, 
        sto_material.name_th AS material_name_th, mas_unit.name_th AS unit, sto_material.unit_upper_id_1, sto_material.unit_upper_rate_1, sto_material.unit_upper_id_2, 
        sto_material.unit_upper_rate_2, sto_material.unit_upper_id_3, sto_material.unit_upper_rate_3,sto_material.unit_upper_id_4, sto_material.unit_upper_rate_4, 
        sto_material.unit_upper_id_5, sto_material.unit_upper_rate_5, sto_receipt_note_detail.qty, sto_receipt_note_detail.formula, sto_receipt_note_detail.not_use_qty, 
        sto_receipt_note_detail.not_use_formula, sto_receipt_note_detail.price_use AS receipt_price_per_unit, pur_po_detail.unit_id AS receipt_unit_id, sto_receipt_note.receipt_date, 
        mas_supplier.id AS supplier_id, mas_supplier.name AS supplier_name, sto_receipt_note.created_at 
        FROM sto_material INNER JOIN
                sto_pr_detail ON sto_material.id = sto_pr_detail.material_id INNER JOIN
                pur_po_detail ON sto_pr_detail.id = pur_po_detail.pr_detail_id INNER JOIN
                sto_receipt_note_detail ON pur_po_detail.id = sto_receipt_note_detail.po_detail_id INNER JOIN
                sto_receipt_note ON sto_receipt_note_detail.receipt_note_id = sto_receipt_note.id INNER JOIN
                sto_material_type ON sto_material.material_type_id = sto_material_type.id INNER JOIN
                pur_po ON pur_po_detail.po_id = pur_po.id INNER JOIN
                mas_supplier ON pur_po.supplier_id = mas_supplier.id LEFT OUTER JOIN
                mas_unit ON sto_material.unit_upper_id_1 = mas_unit.id
        WHERE (sto_material.record_status = 'N') AND (sto_receipt_note_detail.record_status = 'N') AND (sto_receipt_note.record_status = 'N') AND (sto_receipt_note_detail.qty > 0) ";
        $sql .= ($all_show == 0 ? (!empty($start) ? "AND (sto_receipt_note.receipt_date BETWEEN '" . $start . "'  AND  '" . $end . " 23:59:59') " : "") : "");
        $sql .= (!empty($material_id) ? "AND sto_material.id = '" . $material_id . "' " : "");
        $sql .= "ORDER BY sto_receipt_note.receipt_no ASC ";
        // echo $sql; die();
        // ตั้งเวลาให้เรียงลำดับให้ตรงตามจริง
        $carry_min = 0;
        foreach ($this->CI->db->query($sql)->result() as $detail) {
            // เพิ่มเวลาเข้าไป
            $carry_min++;
            $datetime = date('Y-m-d H:i:s', strtotime('+' . $carry_min . ' minutes', strtotime($detail->created_at)));

            $data[] = array(
                // 'sql' => $sql,
                'type' => $detail->supplier_name,
                'paper_no' => $detail->receipt_type . $detail->receipt_no,
                'date' => $this->date_format($detail->receipt_date),
                'material_type_code' => $detail->material_type_code,
                'material_code' => $detail->material_code,
                'material_name' => $detail->material_name_th,
                'unit' => $detail->unit,
                'qty' => $detail->qty_use,
                'price_per_unit' => $detail->receipt_price_per_unit,
                'total_price' => $detail->qty_use * $detail->receipt_price_per_unit,
                'datetime' => $detail->receipt_date
            );
        }
        return $data;
    }

    public function calc_material_issue_control($material_id,  $all_show, $start, $end)
    {
        // $data[] = '';
        // echo $all_show;
        // die();
        ini_set('max_execution_time', 8000);
        $sql = "SELECT   sto_issue.issue_no, sto_material_type.code AS material_type_code, RIGHT('000' + CAST(sto_material.code AS varchar), 4) AS material_code, sto_material.name_th AS material_name, SUM(sto_issue_detail.qty) AS qty, 
        sto_issue_detail.price_per_unit, sto_issue.issue_status, sto_issue.issue_date, mas_unit.name_th AS unit, sto_issue.created_at
        FROM sto_material_type INNER JOIN
                sto_material ON sto_material_type.id = sto_material.material_type_id INNER JOIN
                sto_issue INNER JOIN
                sto_issue_detail ON sto_issue.id = sto_issue_detail.issue_id ON sto_material.id = sto_issue_detail.material_id LEFT OUTER JOIN
                mas_unit ON sto_material.unit_upper_id_1 = mas_unit.id
        WHERE (sto_issue.record_status = 'N') AND (sto_issue_detail.record_status = 'N') AND (sto_issue.issue_status IN ('P','R')) ";
        $sql .= ($all_show == 0 ? (!empty($start) ? "AND (sto_issue.issue_date BETWEEN '" . $start . "'  AND  '" . $end . " 23:59:59') " : "") : "");
        $sql .= (!empty($material_id) ? "AND sto_material.id = '" . $material_id . "' " : "");
        $sql .= " GROUP BY sto_issue.issue_no, sto_material_type.code, RIGHT('000' + CAST(sto_material.code AS varchar), 4), sto_material.name_th, sto_issue_detail.price_per_unit, sto_issue.issue_status, sto_issue.issue_date, mas_unit.name_th, 
        sto_issue.created_at ";
        $sql .= "ORDER BY sto_issue.issue_no ASC ";

        // ตั้งเวลาให้เรียงลำดับให้ตรงตามจริง
        // echo $sql;
        // die();
        $carry_min = 0;
        foreach ($this->CI->db->query($sql)->result() as $detail) {
            $total_price = 0;

            // AMOUNT
            $total_price += $detail->qty * $detail->price_per_unit;

            // เพิ่มเวลาเข้าไป
            $carry_min++;
            $datetime = date('Y-m-d H:i:s', strtotime('+' . $carry_min . ' minutes', strtotime($detail->issue_date)));

            $data[] = array(
                // 'sql' => $sql,
                'type' => 'ISS',
                'paper_no' => $detail->issue_no,
                'date' => $this->date_format($detail->issue_date),
                'material_type_code' => $detail->material_type_code,
                'material_code' => $detail->material_code,
                'material_name' => $detail->material_name,
                'unit' => $detail->unit,
                'qty' => $detail->qty,
                'price_per_unit' => $detail->price_per_unit,
                'total_price' => $total_price,
                'datetime' => $detail->issue_date
            );
        }
        $data[] = array(
            // 'sql' => $sql,
            'type' => NULL,
            'paper_no' => NULL,
            'date' => NULL,
            'material_type_code' => NULL,
            'material_code' => NULL,
            'material_name' => NULL,
            'unit' => NULL,
            'qty' => NULL,
            'price_per_unit' => NULL,
            'total_price' => NULL,
            'datetime' => NULL
        );
        return $data;
    }

    function calc_material_stock($material_id = NULL, $all_show = 0, $start, $end, $department, $agency)
    {
        // echo $start; die();
        $receipt = $this->calc_material_receipt($material_id, $all_show, $start, $end);
        $issue = $this->calc_material_issue($material_id, $all_show, $start, $end, $department, $agency);
        // foreach($issue as $i){
        //     echo '<pre>';
        //     print_r($i);
        //     echo '</pre>';
        // }
        // die();
        $merge = array_merge_recursive($receipt, $issue);
        return $merge;
    }

    function calc_material_stock_mt($material_id = NULL, $all_show = 0, $start, $end)
    {
        $receipt = $this->calc_material_receipt_mt($material_id, $all_show, $start, $end);
        $issue = $this->calc_material_issue_mt($material_id, $all_show, $start, $end);
        $merge = array_merge_recursive($receipt, $issue);
        return $merge;
        // echo $start; die();
    }

    /* เลือกราคาที่ยังมีจำนวนสินค้าอยู่ในคลัง */
    function get_receipt_material($material_id = null)
    {
        $receipt_price_per_unit = array();
        $issue_price_per_unit = array();

        $sql = "SELECT     SUM(vw_qty_material.qty) AS qty, vw_qty_material.price_per_unit, vw_material.material_name
        FROM         vw_qty_material INNER JOIN
        vw_material ON vw_qty_material.material_id = vw_material.id FULL OUTER JOIN
        vw_material AS vw_material_1 ON vw_material.material_code = vw_material_1.material_code
        WHERE material_id = '" . $material_id . "'
        GROUP BY vw_material.id, vw_qty_material.price_per_unit, vw_material.material_name
        ORDER BY vw_material.material_name";
        // echo $sql; die();
        foreach ($this->CI->db->query($sql)->result() as $bbf) {
            @$receipt_price_per_unit[str_replace('.', '_', $bbf->price_per_unit)] = @$bbf->qty;
        }
        foreach ($receipt_price_per_unit as $i => $item) {
            $material[$i] = @$receipt_price_per_unit[$i];
        }
        return @$material;
    }

    /* เลือกราคาที่ยังมีจำนวนสินค้าอยู่ในคลัง */
    function get_receipt_material_mt($material_id = null)
    {
        $receipt_price_per_unit = array();
        $issue_price_per_unit = array();

        $sql = "SELECT     vw_qty_mt_stock.balance AS qty, vw_qty_mt_stock.price_per_unit, sto_mt_material.name_th
        FROM         vw_qty_mt_stock INNER JOIN
                              sto_mt_material ON vw_qty_mt_stock.material_id = sto_mt_material.id
        WHERE     (vw_qty_mt_stock.material_id = '" . $material_id . "')";
        // echo $sql;
        // die();
        foreach ($this->CI->db->query($sql)->result() as $bbf) {
            @$receipt_price_per_unit[str_replace('.', '_', $bbf->price_per_unit)] = @$bbf->qty;
        }
        foreach ($receipt_price_per_unit as $i => $item) {
            $material[$i] = @$receipt_price_per_unit[$i];
        }
        return @$material;
    }

    function strDigit($str, $digit = 2)
    // $digit จำนวน ทศนิยม ค่าพื่้นฐาน 2 หลัก (ถ้าไม่มีใส่ เป็น 0)
    {
        // echo $str; 
        if (strpos($str, ".") > -1) {
            //die("yes");
            $ex = explode('.', $str);
            $s = substr($ex[1], 0, $digit);
            if ($s == '') {
                $s = str_pad('0', $digit, '0', STR_PAD_RIGHT);
            }
            if ($ex[0] == '') {
                $ex[0] = '0';
            }
            $dec =  number_format($ex[0]) . "." . str_pad($s, $digit, '0', STR_PAD_RIGHT);
        } else {
            //die("no");
            $dec = number_format($str, $digit);
        }
        // $dec = str_replace(",", "", $dec);
        return $dec;
    }

    // แสดงตัวอักขระพิเศษในชื่อวัสดุเพื่อแสดงใน PDF
    function special_char_to_pdf($str)
    {
        $special_char = array(
            "∅",
            "▢",
            "⁓",
            "‐",
            "①",
            "②",
            "μ"
        );
        $replace_char = array(
            "<img src='assets/images/symbol/o.png' width='10'>",
            "<img src='assets/images/symbol/box.png' width='10'>",
            "<img src='assets/images/symbol/tilde.png' width='10'>",
            "-",
            "<img src='assets/images/symbol/1.png' width='10'>",
            "<img src='assets/images/symbol/2.png' width='10'>",
            "<img src='assets/images/symbol/u.png' width='10'>"
        );
        $return_str = str_replace($special_char,  $replace_char, $str);
        return $return_str;
    }

    /* การจับคู่ใบ invoice ที่สแกนจากเครื่อง เข้าระบบ */
    function mapped_invoice_scanner()
    {
        // ต้องตั้งชื่อไฟล์ตามเลขที่ใบ invoice ไว้ที่ S:\System-Master\store\scan\
        // ใส่ Department name 
        $department = strtolower($this->CI->session->userdata('user_profile')->department_name);
        // var_dump($this->CI->session->userdata());
        if ($department === 'store' || $this->CI->session->userdata('user_profile')->id === 1) {

            $dir = '\\\\eft-fl1-server\Share\System-Master\store\scan\\'; // PATH TO SERVER DRIVE
            if (is_dir($dir)) {
                if ($dh = opendir($dir)) {
                    while (($file = readdir($dh)) !== false) {
                        if ($file != "." && $file != "..") {
                            $doc = $dir . $file;
                            echo file_exists($doc) ? '' : 'File: was not found.';
                            $ext = pathinfo($doc, PATHINFO_EXTENSION);
                            $inv_no = pathinfo($doc, PATHINFO_FILENAME);
                            $sql = "SELECT sto_receipt_note.id, LOWER({ fn CONCAT(sto_receipt_note.receipt_type, CONVERT(VARCHAR, sto_receipt_note.receipt_no)) }) AS receipt_no, 
                            LOWER(pur_po.no) AS po_no, sto_receipt_note.inv_no
                            FROM sto_receipt_note INNER JOIN pur_po ON sto_receipt_note.po_id = pur_po.id
                                    WHERE LOWER(sto_receipt_note.inv_no) = '" . strtolower($inv_no) . "' AND sto_receipt_note.record_status ='N' ";
                            $nrow = $this->CI->db->query($sql)->num_rows();
                            if ($nrow > 0) {
                                $row = $this->CI->db->query($sql)->row();
                                $file_name = strtolower($row->receipt_no . "." . $ext);
                                rename($doc, $dir . "/" . $file_name);
                                if (copy($dir . "/" . $file_name, "uploads/receipt/" . $row->po_no . "/" . $file_name)) {
                                    unlink($dir . "/" . $file_name);
                                    // อัพเดทชื่อไฟล์
                                    $this->CI->db->query("UPDATE sto_receipt_note SET inv_filename ='" . $file_name . "' WHERE id = '" . $row->id . "'");
                                }
                            }
                        }
                    }
                    closedir($dh);
                }
            } else {
                echo 'Directory not found.';
            }
        } else {
            // echo 'Access denied'; 
        }
    }

    function clean_formula($formula)
    {
        $clean = "";
        $special_char = array(
            "x",
            ".."
        );
        $replace_char = array(
            "*",
            "."
        );
        $clean = str_replace($special_char,  $replace_char,  strtolower($formula));

        return $clean;
    }

    public function calc_bbf_control($material_id, $all_show, $start = NULL,  $end)
    {
        $sql_bbf_sum_rpt = "SELECT SUM(qty) AS qty, SUM(amount) AS amount, AVG(price_per_unit) AS price_per_unit
        FROM (SELECT SUM(sto_receipt_note_detail.qty_use) AS qty, SUM(sto_receipt_note_detail.price_use * sto_receipt_note_detail.qty_use) AS amount, 
        sto_receipt_note_detail.price_use AS price_per_unit
        FROM sto_receipt_note_detail INNER JOIN
        pur_po_detail ON sto_receipt_note_detail.po_detail_id = pur_po_detail.id INNER JOIN
        sto_pr_detail ON pur_po_detail.pr_detail_id = sto_pr_detail.id INNER JOIN
        sto_receipt_note ON sto_receipt_note_detail.receipt_note_id = sto_receipt_note.id
        WHERE (sto_receipt_note.record_status = 'N') AND (sto_receipt_note.receipt_date < CONVERT(datetime, '" . $start . "')) AND (sto_receipt_note_detail.record_status <> 'D') AND 
        (sto_pr_detail.material_id = '" . $material_id . "') AND (sto_receipt_note_detail.qty > 0)
        GROUP BY sto_receipt_note_detail.price_use
        UNION ALL
        SELECT SUM(qty) AS qty, ROUND(SUM(qty * price_per_unit), 2) AS amount, price_per_unit
        FROM sto_bbf
        WHERE     (record_status = 'N') AND (material_id = '" . $material_id  . "')
        GROUP BY price_per_unit) AS derivedtbl_1";
        // echo $sql_bbf_sum_rpt;
        // die();

        // ใช้คำนวนยอดจ่ายก่อนหน้าวันที่เลือกเพื่อหายอดยกมา
        $sql_issue_bbf = "SELECT SUM(sto_issue_detail.qty) AS qty, SUM(sto_issue_detail.price_per_unit * sto_issue_detail.qty) AS amount
        FROM sto_issue_detail INNER JOIN sto_issue ON sto_issue_detail.issue_id = sto_issue.id WHERE (sto_issue_detail.material_id = '" . $material_id . "') 
        AND (sto_issue_detail.record_status = 'N') AND (sto_issue.issue_date < CONVERT(datetime, '" . $start . "')) AND (sto_issue.issue_status = 'R')";
        // echo $sql_issue_bbf;
        // die();
        // // ฟังก์ชัน ดูว่า วัสดุไหนใช้จำนวนมาคิดราคา
        $not_use_qty = $this->check_not_use_qty();
        // echo $start;

        $by_price = array();
        $price_received = 0;
        $price = 1;
        $unit_per_price = 1;
        $qty = 0;
        $date = strtotime("-1 day", strtotime($start));
        $by_price = array(
            'qty' => @$this->CI->db->query($sql_bbf_sum_rpt)->row()->qty - $this->CI->db->query($sql_issue_bbf)->row()->qty,
            'unit_per_price' => @@$this->CI->db->query($sql_bbf_sum_rpt)->row()->price_per_unit,
            'price_received' => @$this->CI->db->query($sql_bbf_sum_rpt)->row()->amount - $this->CI->db->query($sql_issue_bbf)->row()->amount,
            'bbf_date' => @$this->date_format(date("Y-m-d", $date))
        );

        $data = array();

        $data = $by_price;
        return $data;
    }

    // //  ดูการใช้งานระบบ 16-08-2021
    public function statistics($module)
    {
        // {
        //     if ($this->CI->session->userdata("user_profile")->username !== 'childxor') {
        //         $up = $this->CI->db->query("select state from sys_statistics where page = '" . $module . "'")->num_rows() + 1;
        //         if ($up == NULL || $up == 0) {
        //             $up = 1;
        //         }
        //         $data = array(
        //             'page' => $module,
        //             'state' => $up,
        //             'username_last_use' => $this->CI->session->userdata("user_profile")->username,
        //             'last_use' => date('Y-m-d H:i:s')
        //         );
        //         $this->CI->db->insert('sys_statistics', $data);
        //     }
    }

    public function calc_price_support_efins($pr_id)
    {
        $not_use_qty = $this->check_not_use_qty();
        $sql = "SELECT sto_pr.discount "
            . "FROM sto_pr "
            . "WHERE sto_pr.record_status='N' AND sto_pr.id='" . $pr_id . "'";
        $pr = $this->CI->db->query($sql)->row();
        $sql = "SELECT sto_pr_detail.id, sto_pr_detail.pr_id, sto_pr_detail.material_type_id, sto_pr_detail.material_id, sto_pr_detail.material_name, sto_pr_detail.amount, sto_pr_detail.unit_id, 
        sto_pr_detail.quotation_request, sto_pr_detail.quotation_group, sto_pr_detail.quotation_id, sto_pr_detail.supplier_material_price_id, sto_pr_detail.pr_detail_status, sto_pr_detail.remark, 
        sto_pr_detail.specifications, sto_pr_detail.budget_code, sto_pr_detail.formula, sto_pr_detail.price_per_unit, sto_pr_detail.created_username, sto_pr_detail.created_at, 
        sto_pr_detail.updated_username, sto_pr_detail.updated_at, sto_pr_detail.record_status, mas_unit.name_th AS unit_name, { fn CONCAT({ fn CONCAT(sto_material_type.code, ' ') }, 
        sto_material_type.name) } AS material_type_name, sys_status.value AS status, sys_status.color AS status_color, mas_supplier_material_price.price, 
        mas_supplier_material_price.use_formula, mas_supplier_material_price.use_qty, mas_supplier_material_price.supplier_id, sto_pr_detail.discount_price 
        FROM sto_pr_detail INNER JOIN mas_unit ON sto_pr_detail.unit_id = mas_unit.id INNER JOIN
        sto_material_type ON sto_pr_detail.material_type_id = sto_material_type.id INNER JOIN
        sys_status ON sys_status.value = sto_pr_detail.pr_detail_status INNER JOIN
        sys_status_type ON sys_status_type.alias = 'pr_detail_status' AND sys_status.status_type_id = sys_status_type.id LEFT OUTER JOIN
        mas_supplier_material_price ON sto_pr_detail.supplier_material_price_id = mas_supplier_material_price.id
        WHERE (sto_pr_detail.record_status = 'N') AND (sto_pr_detail.pr_id = '" . $pr_id . "') 
        ORDER BY sto_pr_detail.quotation_group, sto_material_type.code, sto_pr_detail.id";
        // echo $sql."<hr>"; die();
        $total = null;
        $discount_price = null;
        $discount_price += $pr->discount;
        $items = $this->CI->db->query($sql)->result();
        foreach ($items as $i => $item) {
            $discount_price += ($item->discount_price * $item->amount);
            $quotation_price = null;
            $static_price = null;
            $amount = null;
            //$data = array(null);
            $quotation_price = (!empty($item->price_per_unit) ? @eval('return ' . $item->price_per_unit . '*' . (!empty(array_search($item->supplier_id, $not_use_qty['supplier_id']))  ? '1' : $item->amount) . '*' .  (!empty(trim($item->formula)) ? trim($item->formula) : '1') . ';') : '');
            $static_price = (!empty($item->supplier_material_price_id) ? @eval('return ' . $item->price . '*' . ($item->use_qty == '0'  ? '1' : $item->amount) . '*' . (!empty($item->formula && $item->use_formula == '1') ? trim($item->formula) : '1') . ';') : '');
            // echo $static_price . "" . $quotation_price . "<hr>";
            if ($static_price > 0 || $quotation_price > 0) {
                $amount = (!empty($item->supplier_material_price_id) ? $static_price : $quotation_price);
                $total += floatval($amount);
                // echo $amount; die();
                $data['pr_id'] = $pr_id;
                $data['pr_detail_id'] = $item->id;
                $data['amount'] = $amount;

                $nrows = $this->CI->db->query("SELECT pr_detail_id FROM tmp_pr_price WHERE pr_detail_id = '" .  $item->id . "'")->num_rows();
                if (empty($nrows)) {
                    $data['created_at'] = date('Y-m-d H:i:s');
                    $data['created_username'] = $this->CI->session->userdata('user_profile')->username;
                    $this->CI->db->insert('tmp_pr_price', $data);
                } else {
                    $data['updated_at'] = date('Y-m-d H:i:s');
                    $data['updated_username'] = $this->CI->session->userdata('user_profile')->username;
                    $this->CI->db->where('pr_detail_id', $item->id);
                    $this->CI->db->update('tmp_pr_price', $data);
                }
            } else {
                $data['amount'] = null;
                $data['updated_at'] = date('Y-m-d H:i:s');
                $data['updated_username'] = $this->CI->session->userdata('user_profile')->username;
                $this->CI->db->where('pr_detail_id', $item->id);
                $this->CI->db->update('tmp_pr_price', $data);
            }
            $this->CI->db->where('pr_id', $pr_id);
            $this->CI->db->update('tmp_pr_price', array('total' => $total, 'total_discount' => $discount_price));
            // echo "pr_id:".$pr_id." total:".$total."<hr>";

        }
    }
}
