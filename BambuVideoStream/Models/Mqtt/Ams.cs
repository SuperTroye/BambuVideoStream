using System.Collections.Generic;

namespace BambuVideoStream.Models.Mqtt;

public class Ams
{
    public string humidity { get; set; }
    public string id { get; set; }
    public string temp { get; set; }
    public List<Tray> tray { get; set; }
    public List<Ams> ams { get; set; }
    public string ams_exist_bits { get; set; }
    public bool insert_flag { get; set; }
    public bool power_on_flag { get; set; }
    public string tray_exist_bits { get; set; }
    public string tray_is_bbl_bits { get; set; }
    public string tray_now { get; set; }
    public string tray_read_done_bits { get; set; }
    public string tray_reading_bits { get; set; }
    public string tray_tar { get; set; }
    public int version { get; set; }
}
