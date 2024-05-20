namespace BambuVideoStream.Models;

public class AppSettings
{
    public bool ExitOnIdle { get; set; } = true;
    public bool ExitOnEndpointDisconnect { get; set; } = true;
    public bool PrintSceneItemsAndExit { get; set; }
}
