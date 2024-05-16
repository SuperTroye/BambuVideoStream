namespace BambuVideoStream.Models;

public class OBSSettings
{
    public string WsConnection { get; set; }
    public string WsPassword { get; set; }
    public string BambuScene { get; set; }
    public string BambuStreamSource { get; set; }
    public bool StartStreamOnStartup { get; set; }
    public bool StopStreamOnPrinterIdle { get; set; }
    public bool ForceCreateInputs { get; set; }
    public bool LockInputs { get; set; }
}
