<?php
defined('BASEPATH') or exit('No direct script access allowed');

$active_group = 'EFINS';
$query_builder = TRUE;

$db['EFINS'] = array(
	'dsn'	=> '',
	'hostname' => 'THDA23005',
	'username' => 'khem',
	'password' => 'w,j,u8iy[@32',
	'database' => 'liteon',
	'dbdriver' => 'sqlsrv',
    'dbprefix' => '',
    'pconnect' => FALSE,
    'db_debug' => TRUE, // เปิดใช้งานการแสดงข้อผิดพลาด
    'cache_on' => FALSE,
    'cachedir' => '',
    'char_set' => 'utf8',
    'dbcollat' => 'utf8_general_ci',
    'swap_pre' => '',
    'encrypt' => FALSE,
    'compress' => FALSE,
    'stricton' => FALSE,
    'failover' => array(),
    'save_queries' => TRUE
);

$db['HQMS_IPS'] = array(
	'dsn'	=> '',
	'hostname' => 'SQL944',
	'username' => 'sa',
	'password' => 'Vrmis?1516**',
	'database' => 'HQMS_IPS',
	'dbdriver' => 'sqlsrv',
	'dbprefix' => '',
	'pconnect' => FALSE,
	'db_debug' => 0,
	'cache_on' => FALSE,
	'cachedir' => '',
	'char_set' => 'utf8',
	'dbcollat' => 'utf8_general_ci',
	'swap_pre' => '',
	'encrypt' => FALSE,
	'compress' => FALSE,
	'stricton' => FALSE,
	'failover' => array(),
	'save_queries' => TRUE
);
