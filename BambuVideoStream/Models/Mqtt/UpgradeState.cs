namespace BambuVideoStream.Models.Mqtt;

public class UpgradeState
{
    public string ahb_new_version_number { get; set; }
    public string ams_new_version_number { get; set; }
    public bool consistency_request { get; set; }
    public int dis_state { get; set; }
    public int err_code { get; set; }
    public bool force_upgrade { get; set; }
    public string message { get; set; }
    public string module { get; set; }
    public int new_version_state { get; set; }
    public string ota_new_version_number { get; set; }
    public string progress { get; set; }
    public int sequence_id { get; set; }
    public string status { get; set; }
}
