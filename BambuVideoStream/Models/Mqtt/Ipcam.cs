namespace BambuVideoStream.Models.Mqtt;

public class Ipcam
{
    /// <summary>
    /// status
    /// </summary>
    public string ipcam_dev { get; set; }
    public string ipcam_record { get; set; }
    public string resolution { get; set; }
    public string timelapse { get; set; }

    public string GetIPCamInfo
        => this.ipcam_dev switch
        {
            "1" => "On",
            "0" => "Off",
            _ => this.ipcam_dev,
        };
}
