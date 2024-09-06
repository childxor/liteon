<?php
	/*
	The autoload.php gets generated when we use composer and we 
    include it in our scripts for PdfParser access. 
    If we wish to freeze our install and manage it without 
    using Composer, this said file can be created to have the following:
	*/
    exit('You have inluded autoload.php');
    
	$vendorDir = './vendor';
$tcpdf_files = Array(
    'Datamatrix' => $vendorDir . '/include/barcodes/datamatrix.php',
    'PDF417' => $vendorDir . '/include/barcodes/pdf417.php',
    'QRcode' => $vendorDir . '/include/barcodes/qrcode.php',
    'TCPDF' => $vendorDir . '/tcpdf.php',
    'TCPDF2DBarcode' => $vendorDir . '/tcpdf_barcodes_2d.php',
    'TCPDFBarcode' => $vendorDir . '/tcpdf_barcodes_1d.php',
    'TCPDF_COLORS' => $vendorDir . '/include/tcpdf_colors.php',
    'TCPDF_FILTERS' => $vendorDir . '/include/tcpdf_filters.php',
    'TCPDF_FONTS' => $vendorDir . '/include/tcpdf_fonts.php',
    'TCPDF_FONT_DATA' => $vendorDir . '/include/tcpdf_font_data.php',
    'TCPDF_IMAGES' => $vendorDir . '/include/tcpdf_images.php',
    'TCPDF_IMPORT' => $vendorDir . '/tcpdf_import.php',
    'TCPDF_PARSER' => $vendorDir . '/tcpdf_parser.php',
    'TCPDF_STATIC' => $vendorDir . '/include/tcpdf_static.php'
);

foreach ($tcpdf_files as $key => $file) {
    include_once $file;
}

$srcDir = './src/Smalot/PdfParser';
include_once  $srcDir . '/Parser.php';
include_once  $srcDir . '/Document.php';
include_once  $srcDir . '/Header.php';
include_once  $srcDir . '/Object.php';
include_once  $srcDir . '/Element.php';
include_once  $srcDir . '/Encoding.php';
include_once  $srcDir . '/Font.php';
include_once  $srcDir . '/Page.php';
include_once  $srcDir . '/Pages.php';
include_once  $srcDir . '/Element/ElementArray.php';
include_once  $srcDir . '/Element/ElementBoolean.php';
include_once  $srcDir . '/Element/ElementString.php';
include_once  $srcDir . '/Element/ElementDate.php';
include_once  $srcDir . '/Element/ElementHexa.php';
include_once  $srcDir . '/Element/ElementMissing.php';
include_once  $srcDir . '/Element/ElementName.php';
include_once  $srcDir . '/Element/ElementNull.php';
include_once  $srcDir . '/Element/ElementNumeric.php';
include_once  $srcDir . '/Element/ElementStruct.php';
include_once  $srcDir . '/Element/ElementXRef.php';

include_once  $srcDir . '/Encoding/StandardEncoding.php';
include_once  $srcDir . '/Encoding/ISOLatin1Encoding.php';
include_once  $srcDir . '/Encoding/ISOLatin9Encoding.php';
include_once  $srcDir . '/Encoding/MacRomanEncoding.php';
include_once  $srcDir . '/Encoding/WinAnsiEncoding.php';
include_once  $srcDir . '/Font/FontCIDFontType0.php';
include_once  $srcDir . '/Font/FontCIDFontType2.php';
include_once  $srcDir . '/Font/FontTrueType.php';
include_once  $srcDir . '/Font/FontType0.php';
include_once  $srcDir . '/Font/FontType1.php';
include_once  $srcDir . '/XObject/Form.php';
include_once  $srcDir . '/XObject/Image.php';

/*
// Information for comparison with composer
use Datamatrix;
use PDF417;
use QRcode;
use TCPDF;
use TCPDF2DBarcode;
use TCPDFBarcode;
use TCPDF_COLORS;
use TCPDF_FILTERS;
use TCPDF_FONTS;
use TCPDF_FONT_DATA;
use TCPDF_IMAGES;
use TCPDF_IMPORT;
use TCPDF_PARSER;
use TCPDF_STATIC;
*/

/************* Usage ******************
Example 1:

<?php
include './classes/pdfparser/autoload.php';
$parser = new \Smalot\PdfParser\Parser();
$pdf    = $parser->parseFile('specs.pdf');

$text = $pdf->getText();
echo $text;
?>


Example 2:
<?php
include "../vendor/autoload.php";

$directory = getcwd();
$file = 'INV001.pdf';
$fullfile = $directory . '/' . $file;
$content = '';
$out = '';
$parser = new \Smalot\PdfParser\Parser();

$document = $parser->parseFile($fullfile);
$pages    = $document->getPages();
$page     = $pages[0];
$content  = $page->getText();
$out      = $content;
echo '<pre>' . $out . '</pre>';

**************************************/