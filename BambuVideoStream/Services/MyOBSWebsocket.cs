using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BambuVideoStream.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using static BambuVideoStream.Constants.OBS;

namespace BambuVideoStream;

public class MyOBSWebsocket(
    ILogger<OBSWebsocket> logger,
    IOptions<OBSSettings> obsSettings,
    IOptions<BambuSettings> bambuSettings) : OBSWebsocket()
{
    private static readonly TimeSpan BackoffDelay = TimeSpan.FromMilliseconds(100);
    private readonly ILogger<OBSWebsocket> log = logger;
    private readonly OBSSettings obsSettings = obsSettings.Value;
    private readonly BambuSettings bambuSettings = bambuSettings.Value;

    public bool InputExists(string sourceName, out InputSettings input)
    {
        try
        {
            input = base.GetInputSettings(sourceName);
            return true;
        }
        catch (ErrorResponseException e) when (e.ErrorCode == NotFoundErrorCode)
        {
            input = null;
            return false;
        }
    }

    public bool SceneExists(string sceneName)
        => base.GetSceneList().Scenes.Any(s => s.Name == sceneName);

    // TODO file issue on this!
    // Can't customize ObsVideoSettings because all setters are internal
    public async Task EnsureVideoSettingsAsync()
    {
        var settings = base.GetVideoSettings();
        if (settings.BaseWidth == VideoWidth && settings.OutputWidth == VideoWidth &&
            settings.BaseHeight == VideoHeight && settings.OutputHeight == VideoHeight)
        {
            return;
        }

        this.log.LogInformation("Setting video settings to {width}x{height}", VideoWidth, VideoHeight);
        settings.BaseWidth = settings.OutputWidth = VideoWidth;
        settings.BaseHeight = settings.OutputHeight = VideoHeight;
        base.SetVideoSettings(settings);

        // Sleep before returning as to not overwhelm OBS :)
        await Task.Delay(BackoffDelay);
    }

    public async Task EnsureBambuSceneAsync()
    {
        if (this.SceneExists(BambuScene))
        {
            return;
        }

        this.log.LogInformation("Creating scene {sceneName}", BambuScene);
        base.CreateScene(BambuScene);
        base.SetCurrentProgramScene(BambuScene);

        // Sleep before returning as to not overwhelm OBS :)
        await Task.Delay(BackoffDelay);
    }

    public async Task EnsureBambuStreamSourceAsync()
    {
        if (this.InputExists(BambuStreamSource, out var _))
        {
            if (this.obsSettings.ForceCreateInputs)
            {
                base.RemoveInput(BambuStreamSource);
            }
            else
            {
                return;
            }
        }

        this.log.LogInformation("Creating stream source BambuStream");

        // ===========================================
        // BambuStreamSource
        // ===========================================
        var bambuStream = new JObject
            {
                { "ffmpeg_options", FfmpegOptions },
                { "hw_decode", true },
                { "input", $"file:{Path.Combine(this.bambuSettings.PathToSDP)}" },
                { "is_local_file", false },
                { "reconnect_delay_sec", 2 }
            };

        var id = base.CreateInput(BambuScene, BambuStreamSource, VideoInputType, bambuStream, true);

        // Wait for stream to start
        while (base.GetMediaInputStatus(BambuStreamSource).State != MediaState.OBS_MEDIA_STATE_PLAYING)
        {
            this.log.LogInformation("Waiting for stream to start...");
            await Task.Delay(1000);
        }

        // Transition can only be applied after the stream has been started
        var transform = new JObject
        {
            { "positionX", 0.0 },
            { "positionY", 0.0 },
            { "scaleX", 1.0 },
            { "scaleY", 1.0 },
            { "boundsType", "OBS_BOUNDS_SCALE_INNER" },
            { "boundsAlignment", 0 },
            { "boundsHeight", this.obsSettings.Output.VideoHeight },
            { "boundsWidth", this.obsSettings.Output.VideoWidth },
        };
        base.SetSceneItemTransform(BambuScene, id, transform);

        // Make sure video source is in the background
        base.SetSceneItemIndex(BambuScene, id, 0);
        if (this.obsSettings.LockInputs)
        {
            base.SetSceneItemLocked(BambuScene, id, true);
        }

        // Sleep before returning as to not overwhelm OBS :)
        await Task.Delay(BackoffDelay);
    }

    public async Task EnsureColorSourceAsync()
    {
        const string ColorSource = "ColorSource";
        if (this.InputExists(ColorSource, out _))
        {
            if (this.obsSettings.ForceCreateInputs)
            {
                base.RemoveInput(ColorSource);
            }
            else
            {
                return;
            }
        }

        this.log.LogInformation($"Creating color source {ColorSource}");

        // ===========================================
        // ColorSource
        // ===========================================
        var colorSource = new JObject
        {
            {"color", 4026531840},
            {"height", 130},
            {"width", VideoWidth}
        };

        var id = base.CreateInput(BambuScene, ColorSource, ColorInputType, colorSource, true);

        var transform = new JObject
        {
            { "positionX", 0 },
            { "positionY", 950 }
        };
        base.SetSceneItemTransform(BambuScene, id, transform);

        // Make sure color source is in the foreground
        base.SetSceneItemIndex(BambuScene, id, 1);
        if (this.obsSettings.LockInputs)
        {
            base.SetSceneItemLocked(BambuScene, id, true);
        }

        // Sleep before returning as to not overwhelm OBS :)
        await Task.Delay(100);
    }

    public async Task<InputSettings> EnsureTextInputSettingsAsync(
        string sourceName,
        decimal defaultPositionX,
        decimal defaultPositionY,
        int zIndex)
    {
        if (this.InputExists(sourceName, out var input))
        {
            if (this.obsSettings.ForceCreateInputs)
            {
                base.RemoveInput(sourceName);
            }
            else
            {
                return input;
            }
        }

        this.log.LogInformation("Creating text source {sourceName}", sourceName);

        JObject itemData = new JObject
        {
            { "text", "test" },
            { "font", new JObject
                {
                    { "face", "Arial" },
                    { "size", 36 },
                    { "style", "regular" }
                }
            }
        };
        var id = base.CreateInput(BambuScene, sourceName, TextInputType, itemData, true);

        var transform = new JObject
        {
            { "positionX", defaultPositionX },
            { "positionY", defaultPositionY }
        };
        base.SetSceneItemTransform(BambuScene, id, transform);

        base.SetSceneItemIndex(BambuScene, id, zIndex);
        if (this.obsSettings.LockInputs)
        {
            base.SetSceneItemLocked(BambuScene, id, true);
        }

        // Sleep before returning as to not overwhelm OBS :)
        await Task.Delay(100);
        return base.GetInputSettings(sourceName);
    }

    public async Task<InputSettings> EnsureImageInputSettingsAsync(
        string sourceName,
        string icon,
        decimal defaultPositionX,
        decimal defaultPositionY,
        decimal defaultScaleFactor,
        int zIndex)
    {
        if (this.InputExists(sourceName, out var input))
        {
            if (this.obsSettings.ForceCreateInputs)
            {
                base.RemoveInput(sourceName);
            }
            else
            {
                if (input.Settings["file"].Value<string>() != icon)
                {
                    input.Settings["file"] = icon;
                    base.SetInputSettings(input);
                    await Task.Delay(BackoffDelay);
                }
                return input;
            }
        }

        this.log.LogInformation("Creating icon source {sourceName}", sourceName);

        var imageInput = new JObject
        {
            {"file", icon },
            {"linear_alpha", true },
            {"unload", true }
        };
        var id = base.CreateInput(BambuScene, sourceName, ImageInputType, imageInput, true);

        var transform = new JObject
        {
            { "positionX", defaultPositionX },
            { "positionY", defaultPositionY },
            { "scaleX", defaultScaleFactor },
            { "scaleY", defaultScaleFactor }
        };
        base.SetSceneItemTransform(BambuScene, id, transform);

        base.SetSceneItemIndex(BambuScene, id, zIndex);
        if (this.obsSettings.LockInputs)
        {
            base.SetSceneItemLocked(BambuScene, id, true);
        }

        // Sleep before returning as to not overwhelm OBS :)
        await Task.Delay(100);
        return base.GetInputSettings(sourceName);
    }
}
