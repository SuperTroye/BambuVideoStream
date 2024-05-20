using System;
using System.IO;

namespace BambuVideoStream.Models;

public class BambuSettings
{
    public string IpAddress { get; set; }
    public int Port { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string Serial { get; set; }
    public string PathToSDP { get; set; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "/Roaming/BambuStudio/cameratools/ffmpeg.sdp");
}
