<?php
defined('BASEPATH') or exit('No direct script access allowed');

class Manager_Model extends CI_Model
{
    public function __construct()
    {
        parent::__construct();
        $this->hqms_db = $this->load->database('HQMS_IPS', TRUE);
    }

    public function get_event_data($start_date, $end_date, $door_id, $in_out, $shift_type, $dept)
    {
        $this->hqms_db->select('eventTime, SUBSTRING(personID, 1, LEN(personID) - 2) as basePersonID, personID, eventCard, personName, deptCode, deptName, deptID, CONVERT(DATE, eventTime) AS rDate, deviceName');
        $this->hqms_db->from('PubEvent');
        $this->hqms_db->where('eventType', '1');

        // Complex WHERE conditions (unchanged)
        $this->hqms_db->where("(
            (eventCode = '0001' AND deviceL1Type IN ('GCU', 'RAC2400', 'HDE200', 'NCU', 'RAC2400N', 'RAC2000EL', 'RAC852PMFV'))
            OR (eventCode IN ('0000', '0009') AND deviceL1Type IN ('HTA860', 'HTA850', 'HTA852PMF', 'RAC2000P', 'HDP100', 'RAC900', 'RAC940', 'RAC960', 'RAC970', 'RTA650', 'RAC852PMFV'))
            OR (eventCode = '003D' AND deviceL1Type IN ('RAC940', 'RAC960'))
            OR (eventCode IN ('0000', '0001', '0009') AND deviceL1Type = 'RAC960PMF')
            OR (eventCode = '0014' AND deviceL1Type = 'RAC960')
            OR (eventCode IN ('0007', '7', '1') AND deviceL1Type IN ('NCU', 'RAC2000EL'))
            OR (eventCode IN ('01', '02', '21', '22') AND deviceL1Type = 'UNI')
            OR ('HMS' + eventCode IN ('HMS0000', 'HMS0009', 'HMS003D', 'HMS003F'))
        )");

        $this->hqms_db->where('eventTime >=', $start_date);
        $this->hqms_db->where('eventTime <=', $end_date);

        if ($door_id != 'all' && strpos($door_id, 'HR Attendance') === false) {
            $this->hqms_db->like('deviceName', $door_id);
        } elseif (strpos($door_id, 'HR Attendance') !== false) {
            $this->hqms_db->where_in('deviceName', ['Build1 - HR Attendance 01', 'Build1 - HR Attendance 02']);
        }

        if ($dept != 'all') {
            $this->hqms_db->where('deptCode', $dept);
        }

        if ($in_out != 'all') {
            // Add logic for in/out filtering if needed
        }

        if ($shift_type != 'all') {
            // Add logic for shift type filtering if needed
        }

        $this->hqms_db->order_by('eventTime', 'ASC');
        $query = $this->hqms_db->get();
        return $query->result();
    }

    public function get_person_shift_data()
    {
        $this->db = $this->load->database('EFINS', TRUE);
        $this->db->select('name, shiftMent');
        $this->db->from('Person');
        $this->db->where('name IS NOT NULL');
        $this->db->where('shiftMent IS NOT NULL');
        return $this->db->get()->result();
    }
}
