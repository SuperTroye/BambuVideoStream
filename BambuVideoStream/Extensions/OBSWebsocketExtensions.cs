using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BambuVideoStream.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using static BambuVideoStream.Constants.OBS;

namespace BambuVideoStream.Extensions;

public static class OBSWebsocketExtensions
{
    private static readonly TimeSpan BackoffDelay = TimeSpan.FromMilliseconds(100);
    private static readonly Lazy<ILogger<OBSWebsocket>> logLazy = new(() => Program.LoggerFactory.CreateLogger<OBSWebsocket>());
    private static ILogger<OBSWebsocket> Log => logLazy.Value;

    public static bool InputExists(this OBSWebsocket obs, string sourceName, out InputSettings input)
    {
        try
        {
            input = obs.GetInputSettings(sourceName);
            return true;
        }
        catch (ErrorResponseException e) when (e.ErrorCode == NotFoundErrorCode)
        {
            input = null;
            return false;
        }
    }

    public static bool SceneExists(this OBSWebsocket obs, string sceneName)
        => obs.GetSceneList().Scenes.Any(s => s.Name == sceneName);

    // TODO file issue on this!
    // Can't customize ObsVideoSettings because all setters are internal
    public static async Task EnsureVideoSettingsAsync(this OBSWebsocket obs)
    {
        var settings = obs.GetVideoSettings();
        if (settings.BaseWidth == VideoWidth && settings.OutputWidth == VideoWidth &&
            settings.BaseHeight == VideoHeight && settings.OutputHeight == VideoHeight)
        {
            return;
        }

        Log.LogInformation("Setting video settings to {width}x{height}", VideoWidth, VideoHeight);
        settings.BaseWidth = settings.OutputWidth = VideoWidth;
        settings.BaseHeight = settings.OutputHeight = VideoHeight;
        obs.SetVideoSettings(settings);

        // Sleep before returning as to not overwhelm OBS :)
        await Task.Delay(BackoffDelay);
    }

    public static async Task EnsureBambuSceneAsync(this OBSWebsocket obs)
    {
        if (obs.SceneExists(BambuScene))
        {
            return;
        }

        Log.LogInformation("Creating scene {sceneName}", BambuScene);
        obs.CreateScene(BambuScene);
        obs.SetCurrentProgramScene(BambuScene);

        // Sleep before returning as to not overwhelm OBS :)
        await Task.Delay(BackoffDelay);
    }

    public static async Task EnsureBambuStreamSourceAsync(this OBSWebsocket obs, BambuSettings bambuSettings, OBSSettings obsSettings)
    {
        if (obs.InputExists(BambuStreamSource, out var _))
        {
            if (obsSettings.ForceCreateInputs)
            {
                obs.RemoveInput(BambuStreamSource);
            }
            else
            {
                return;
            }
        }

        Log.LogInformation("Creating stream source BambuStream");

        // ===========================================
        // BambuStreamSource
        // ===========================================
        var bambuStream = new JObject
            {
                { "ffmpeg_options", FfmpegOptions },
                { "hw_decode", true },
                { "input", $"file:{Path.Combine(bambuSettings.PathToSDP)}" },
                { "is_local_file", false },
                { "reconnect_delay_sec", 2 }
            };

        var id = obs.CreateInput(BambuScene, BambuStreamSource, VideoInputType, bambuStream, true);

        // Wait for stream to start
        while (obs.GetMediaInputStatus(BambuStreamSource).State != MediaState.OBS_MEDIA_STATE_PLAYING)
        {
            Log.LogInformation("Waiting for stream to start...");
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
            { "boundsHeight", obsSettings.Output.VideoHeight },
            { "boundsWidth", obsSettings.Output.VideoWidth },
        };
        obs.SetSceneItemTransform(BambuScene, id, transform);

        // Make sure video source is in the background
        obs.SetSceneItemIndex(BambuScene, id, 0);
        if (obsSettings.LockInputs)
        {
            obs.SetSceneItemLocked(BambuScene, id, true);
        }

        // Sleep before returning as to not overwhelm OBS :)
        await Task.Delay(BackoffDelay);
    }

    public static async Task EnsureColorSourceAsync(this OBSWebsocket obs, OBSSettings obsSettings)
    {
        const string ColorSource = "ColorSource";
        if (obs.InputExists(ColorSource, out _))
        {
            if (obsSettings.ForceCreateInputs)
            {
                obs.RemoveInput(ColorSource);
            }
            else
            {
                return;
            }
        }

        Log.LogInformation($"Creating color source {ColorSource}");

        // ===========================================
        // ColorSource
        // ===========================================
        var colorSource = new JObject
        {
            {"color", 4026531840},
            {"height", 130},
            {"width", VideoWidth}
        };

        var id = obs.CreateInput(BambuScene, ColorSource, ColorInputType, colorSource, true);

        var transform = new JObject
        {
            { "positionX", 0 },
            { "positionY", 950 }
        };
        obs.SetSceneItemTransform(BambuScene, id, transform);

        // Make sure color source is in the foreground
        obs.SetSceneItemIndex(BambuScene, id, 1);
        if (obsSettings.LockInputs)
        {
            obs.SetSceneItemLocked(BambuScene, id, true);
        }

        // Sleep before returning as to not overwhelm OBS :)
        await Task.Delay(100);
    }

    public static async Task<InputSettings> EnsureTextInputSettingsAsync(
        this OBSWebsocket obs, 
        string sourceName, 
        decimal defaultPositionX, 
        decimal defaultPositionY, 
        int zIndex,
        OBSSettings obsSettings)
    {
        if (obs.InputExists(sourceName, out var input))
        {
            if (obsSettings.ForceCreateInputs)
            {
                obs.RemoveInput(sourceName);
            }
            else
            {
                return input;
            }
        }

        Log.LogInformation("Creating text source {sourceName}", sourceName);

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
        var id = obs.CreateInput(BambuScene, sourceName, TextInputType, itemData, true);

        var transform = new JObject
        {
            { "positionX", defaultPositionX },
            { "positionY", defaultPositionY }
        };
        obs.SetSceneItemTransform(BambuScene, id, transform);

        obs.SetSceneItemIndex(BambuScene, id, zIndex);
        if (obsSettings.LockInputs)
        {
            obs.SetSceneItemLocked(BambuScene, id, true);
        }

        // Sleep before returning as to not overwhelm OBS :)
        await Task.Delay(100);
        return obs.GetInputSettings(sourceName);
    }

    public static async Task<InputSettings> EnsureImageInputSettingsAsync(
        this OBSWebsocket obs,
        string sourceName,
        string icon,
        decimal defaultPositionX,
        decimal defaultPositionY,
        decimal defaultScaleFactor,
        int zIndex,
        OBSSettings obsSettings)
    {
        if (obs.InputExists(sourceName, out var input))
        {
            if (obsSettings.ForceCreateInputs)
            {
                obs.RemoveInput(sourceName);
            }
            else
            {
                if (input.Settings["file"].Value<string>() != icon)
                {
                    input.Settings["file"] = icon;
                    obs.SetInputSettings(input);
                    await Task.Delay(BackoffDelay);
                }
                return input;
            }
        }

        Log.LogInformation("Creating icon source {sourceName}", sourceName);

        var imageInput = new JObject
        {
            {"file", icon },
            {"linear_alpha", true },
            {"unload", true }
        };
        var id = obs.CreateInput(BambuScene, sourceName, ImageInputType, imageInput, true);

        var transform = new JObject
        {
            { "positionX", defaultPositionX },
            { "positionY", defaultPositionY },
            { "scaleX", defaultScaleFactor },
            { "scaleY", defaultScaleFactor }
        };
        obs.SetSceneItemTransform(BambuScene, id, transform);

        obs.SetSceneItemIndex(BambuScene, id, zIndex);
        if (obsSettings.LockInputs)
        {
            obs.SetSceneItemLocked(BambuScene, id, true);
        }

        // Sleep before returning as to not overwhelm OBS :)
        await Task.Delay(100);
        return obs.GetInputSettings(sourceName);
    }
}
