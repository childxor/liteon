<?php
class It_assets_model extends CI_Model
{

    public function get_all_assets()
    {
        return $this->db->get('it_assets')->result();
    }

    public function get_all_asset_software()
    {
        return $this->db->get('it_assetsoftware')->result();
    }

    public function get_all_asset_statuses()
    {
        return $this->db->get('it_assetstatuses')->result();
    }

    public function get_all_brands()
    {
        return $this->db->get('it_brands')->result();
    }

    public function get_all_departments()
    {
        return $this->db->get('it_departments')->result();
    }

    public function get_all_device_types()
    {
        return $this->db->get('it_devicetypes')->result();
    }

    public function get_all_locations()
    {
        return $this->db->get('it_locations')->result();
    }

    public function get_all_maintenance_records()
    {
        return $this->db->get('it_maintenancerecords')->result();
    }

    public function get_all_models()
    {
        return $this->db->get('it_models')->result();
    }

    public function get_all_operating_systems()
    {
        return $this->db->get('it_operatingsystems')->result();
    }

    public function get_all_software_licenses()
    {
        return $this->db->get('it_softwarelicenses')->result();
    }

    public function get_all_users()
    {
        return $this->db->get('it_users')->result();
    }

    public function get_all_vendors()
    {
        return $this->db->get('it_vendors')->result();
    }
}
