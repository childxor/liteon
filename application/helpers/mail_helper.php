<?php
defined('BASEPATH') or exit('No direct script access allowed');

use PHPMailer\PHPMailer\PHPMailer;
use PHPMailer\PHPMailer\Exception;

include(APPPATH . "helpers/mail_helper/Exception.php");
include(APPPATH . "helpers/mail_helper/PHPMailer.php");
include(APPPATH . "helpers/mail_helper/SMTP.php");

if (!function_exists('send_mail')) {
    /**
     * Send mail
     *
     * @param string $send_from
     * @param string $send_to
     * @return string  $send_name
     * @return string  $send_subj
     * @return string  $send_body
     */
    // function send_mail($send_from = NULL, $send_to = NULL, $send_name = NULL, $send_subj = NULL, $send_body = NULL, $file_attach = NULL)
    function send_mail($send_from = NULL, $send_to = [], $send_subj = NULL, $send_body = NULL, $file_attach = NULL)
    {
        $CI = &get_instance();
        $CI->var = (object) $CI->efs_lib->get_var_sys();
        $company =  $CI->var->company_delivery_to;
        $smtp_host = 'smtp.office365.com';
        $mail = new PHPMailer(true);
        $mail->IsSMTP();
        $mail->SMTPOptions = array(
            'ssl' => array(
                'verify_peer' => false,
                'verify_peer_name' => false,
                'allow_self_signed' => true
            )
        );

        $smtp_user = $CI->session->userdata("user_profile")->email;
        $smtp_pass = '';
        switch ($CI->session->userdata("user_profile")->sub_department_name) {
            case 'info':
                $smtp_user = 'info_user';
                $smtp_pass = 'info_pass';
                break;
            case 'PURCHASING':
                // $smtp_user = 'eft-pur@endoeft.com'; // ใช้ไปก่อน authen แล้วแล้ว ปิดคอมเม้นไว้
                switch ($smtp_user) {
                    case 'eft-pur@endoeft.com':
                        $smtp_pass = 'Quy02927';
                        $mail->addBCC('eft-pur2@endoeft.com', 'Sukanya');
                        break;
                    case 'eft-pur2@endoeft.com':
                        $smtp_pass = 'Coj91886';
                        $mail->addBCC('eft-pur@endoeft.com', 'Sompit');
                        break;
                    default:
                        $smtp_user = 'eft-pur@endoeft.com';
                        $smtp_pass = 'Quy02927';
                        break;
                }
                break;
            case 'IT':
                // $smtp_user = 'eft-it@endoeft.com'; // ใช้ไปก่อน authen แล้วแล้ว ปิดคอมเม้นไว้
                switch ($smtp_user) {
                    case 'adisak@endoeft.com':
                        $smtp_pass = 'Voc44257';
                        $mail->addBCC('eft-it@endoeft.com', 'EFT-IT');
                        break;
                    case 'eft-it@endoeft.com':
                        $smtp_pass = 'Koj18415';
                        $mail->addBCC('adisak@endoeft.com', 'Adisak');
                        break;
                    default:
                        $smtp_user = 'eft-it@endoeft.com';
                        $smtp_pass = 'Koj18415';
                        break;
                }
                break;
            default:
                $smtp_user = 'eft-it@endoeft.com';
                $smtp_pass = 'Koj18415';
                $mail->addBCC('adisak@endoeft.com', 'Adisak');
                break;
        }

        $mail->IsSMTP();
        $mail->SMTPOptions = array(
            'ssl' => array(
                'verify_peer' => false,
                'verify_peer_name' => false,
                'allow_self_signed' => true
            )
        );

        $mail->CharSet = "utf-8";
        $mail->SMTPAuth   = true;
        $mail->Host       = $smtp_host;
        $mail->Port       = 587;
        $mail->SMTPSecure = "tls";
        $mail->Username   = $smtp_user;
        $mail->Password   = $smtp_pass;
        $mail->SMTPDebug  = 0;

        $err = array();

        $history = '';
        foreach ($send_to as $key => $to) {
            $tos = explode("|", $to);
            $mail->addBCC($tos[1], $tos[0]);
            $history .= $tos[0] . ' - ' . $tos[1] . ', ';
        }
        $history = substr($history, 0, -2);
        // die($history);

        $mail->setFrom($smtp_user, $CI->session->userdata("user_profile")->sub_department_name . ' - ' . $company);

        $mail->Subject = $send_subj;
        $mail->Body    = $send_body;

        if (!empty($file_attach)) {
            if (is_array($file_attach)) {
                foreach ($file_attach as $attach) {
                    $mail->addAttachment($attach);
                }
            } else {
                $mail->addAttachment($file_attach);
            }
        }

        // $mail->SMTPDebug = 2;
        $mail->IsHTML(true);
        if (!$mail->send()) {
            $CI->log_lib->write_log('ส่งเมลไปยัง =>' . $history, json_encode($send_body));
            return "Mailer Error: " . $mail->ErrorInfo;
        } else {
            // $CI->log_lib->write_log('ส่งเมลไปยัง =>' . $send_name . ' - ' . $send_to, json_encode($send_body));
            $CI->log_lib->write_log('ส่งเมลไปยัง =>' . $history, json_encode($send_body));
            return "";
        }
    }

    function send_po_to_mail($send_from = NULL, $send = NULL, $send_subj = NULL, $send_body = NULL, $file_attach = NULL)
    {
        $CI = &get_instance();
        $CI->var = (object) $CI->efs_lib->get_var_sys();
        $company =  $CI->var->company_delivery_to;
        $smtp_host = 'smtp.office365.com';
        $mail = new PHPMailer(true);
        $mail->IsSMTP();
        $mail->SMTPOptions = array(
            'ssl' => array(
                'verify_peer' => false,
                'verify_peer_name' => false,
                'allow_self_signed' => true
            )
        );

        $smtp_user = $CI->session->userdata("user_profile")->email;
        $smtp_pass = '';
        switch ($CI->session->userdata("user_profile")->sub_department_name) {
            case 'info':
                $smtp_user = 'info_user';
                $smtp_pass = 'info_pass';
                break;
            case 'PURCHASING':
                // $smtp_user = 'eft-pur@endoeft.com'; // ใช้ไปก่อน authen แล้วแล้ว ปิดคอมเม้นไว้
                switch ($smtp_user) {
                    case 'eft-pur@endoeft.com':
                        $smtp_pass = 'Quy02927';
                        // $mail->Addcc('eft-pur2@endoeft.com', 'Sompit');
                        $mail->Addcc('eft-pur2@endoeft.com', 'Sompit');
                        break;
                    case 'eft-pur2@endoeft.com':
                        $smtp_pass = 'Coj91886';
                        $mail->Addcc('eft-pur@endoeft.com', 'Sukanya');
                        break;
                    default:
                        # code...
                        break;
                }
                break;
            case 'IT':
                // $smtp_user = 'eft-it@endoeft.com'; // ใช้ไปก่อน authen แล้วแล้ว ปิดคอมเม้นไว้
                switch ($smtp_user) {
                    case 'adisak@endoeft.com':
                        $smtp_pass = 'Voc44257';
                        $mail->Addcc('eft-it@endoeft.com', 'EFT-IT');
                        break;
                    case 'eft-it@endoeft.com':
                        $smtp_pass = 'Koj18415';
                        $mail->Addcc('adisak@endoeft.com', 'Adisak');
                        break;
                }
                break;
            default:
                $smtp_user = 'eft-it@endoeft.com';
                $smtp_pass = 'Koj18415';
                break;
        }

        $mail->CharSet = "utf-8";
        $mail->SMTPAuth   = true;
        $mail->Host       = $smtp_host;
        $mail->Port       = 587;
        $mail->SMTPSecure = "tls";
        $mail->Username   = $smtp_user;
        $mail->Password   = $smtp_pass;
        $mail->SMTPDebug  = 0;

        $err = array();

        //$mail->AddAddress('phongphan@endoeft.com', 'TESTING');
        $mail->setFrom($smtp_user, $CI->session->userdata("user_profile")->sub_department_name . ' - ' . $company);
        foreach ($send as $key => $to) {
            // ส่งถึงทุกคนที่เกี่ยวข้องในการจัดซื้อ
            $mail->AddAddress($to[1], $to[0]);
        }

        // ส่งสำเนาถึงผู้ส่งเองเพื่อดูว่าถึงใครบ้าง
        $mail->Addcc($smtp_user, $CI->session->userdata("user_profile")->sub_department_name);

        $mail->Subject = $send_subj;
        $mail->Body    = $send_body;

        if (!empty($file_attach)) {
            if (is_array($file_attach)) {
                foreach ($file_attach as $attach) {
                    $mail->addAttachment($attach);
                }
            } else {
                $mail->addAttachment($file_attach);
            }
        }
        // $mail->SMTPDebug = 3;
        $mail->IsHTML(true);

        if (!$mail->send()) {
            // die("hi");
            $CI->log_lib->write_log('Mailer Error: ', $mail->ErrorInfo);
            return "Mailer Error: " . $mail->ErrorInfo;
        } else {
            //die("ho");
            $CI->log_lib->write_log('ส่งเมลไปยัง =>', json_encode($send_body));
            return "";
        }
    }
}
