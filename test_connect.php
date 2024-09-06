<?php
$serverName = "eft-db1-server"; //serverName\instanceName
$connectionInfo = array("Database" => "EFS", "UID" => "khem", "PWD" => "w,j,u8iy[");
$conn = sqlsrv_connect($serverName, $connectionInfo);




if (!sqlsrv_connect($serverName, $connectionInfo)) {
     echo 'not conn';
} else {
     echo 'ok conn';
}

$params = array();
$option = array();

$sql = sqlsrv_query(
     $conn,
     "select * from sto_pr where record_status = 'N'"
);
foreach (sqlsrv_fetch_array($sql) as $i) {
     echo '<pre>';
     print_r($i);
     echo '</pre>';
}
// foreach($sql as $i){ 
//      print_r($i);
// }
