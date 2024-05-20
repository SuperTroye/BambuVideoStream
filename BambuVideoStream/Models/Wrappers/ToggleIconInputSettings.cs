using OBSWebsocketDotNet.Types;
using static BambuVideoStream.Constants.OBS;

namespace BambuVideoStream.Models.Wrappers;

/// <summary>
/// A simple wrapper to join a reference to an image input with the icon settings, so the image can be flipped as needed at runtime.
/// </summary>
public class ToggleIconInputSettings(
    InputSettings inputSettings, 
    InitialToggleIconSettings initialSettings)
{
    public InputSettings InputSettings { get; set; } = inputSettings;
    public InitialToggleIconSettings InitialToggleIconSettings { get; set; } = initialSettings;
}