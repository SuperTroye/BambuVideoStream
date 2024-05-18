using OBSWebsocketDotNet.Types;
using static BambuVideoStream.Constants.OBS;

namespace BambuVideoStream.Models.Wrappers;

public class ToggleIconInputSettings(
    InputSettings inputSettings, 
    InitialToggleIconSettings initialSettings)
{
    public InputSettings InputSettings { get; set; } = inputSettings;
    public InitialToggleIconSettings InitialToggleIconSettings { get; set; } = initialSettings;
}