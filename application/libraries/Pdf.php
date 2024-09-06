<?php

defined('BASEPATH') or exit('No direct script access allowed');

require_once(APPPATH . 'libraries/dompdf/autoload.inc.php');

use Dompdf\Dompdf;

class Pdf
{
    private $CI;

    function __construct()
    {
        $this->CI = &get_instance();
    }

    function generatePDF($html, $filename = '', $download = FALSE, $paper = 'A4', $orientation = 'portrait')
    {
        $dompdf = new Dompdf();
        $dompdf->set_option('debugLayout', false);
        $dompdf->set_option('isJavascriptEnabled', true);
        $dompdf->set_option('isPhpEnabled', true);
        $dompdf->set_option('isRemoteEnabled', true);
        $dompdf->set_option('isHtml5ParserEnabled', true);
        $dompdf->setPaper($paper, $orientation);
        $dompdf->loadHtml($html);
        $dompdf->render();

        // ถ้าต้องการเพิ่มข้อความท้ายกระดาษ สามารถเพิ่มโค้ดนี้
        $canvas = $dompdf->getCanvas();
        $canvas->page_text(43, 790, "DOC.NO. : PC-FM-23-00-13", "THSarabunNew", 12, array(0, 0, 0));
        $canvas->page_text(275, 790, "REV.NO. : 01", "THSarabunNew", 12, array(0, 0, 0));
        $canvas->page_text(490, 790, "EFF.DATE : 04-01-21", "THSarabunNew", 12, array(0, 0, 0));

        $dompdf->stream($filename, array('Attachment' => $download));
    }
}
