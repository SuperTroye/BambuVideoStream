﻿using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using BambuVideoStream.Models;
using BambuVideoStream.Models.Wrappers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using static BambuVideoStream.Constants.OBS;

namespace BambuVideoStream;

/// <summary>
/// Overload of <see cref="OBSWebsocket"/> that adds additional custom functionality for the application
/// </summary>
public class MyOBSWebsocket(
    ILogger<OBSWebsocket> logger,
    IOptions<OBSSettings> obsSettings,
    IOptions<BambuSettings> bambuSettings) : OBSWebsocket()
{
    private static readonly TimeSpan BackoffDelay = TimeSpan.FromMilliseconds(100);
    private readonly ILogger<OBSWebsocket> log = logger;
    private readonly OBSSettings obsSettings = obsSettings.Value;
    private readonly BambuSettings bambuSettings = bambuSettings.Value;

    /// <summary>
    /// Whether an input source with the given name exists.
    /// </summary>
    /// <param name="input">If the input exists, contains the input settings</param>
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

    /// <summary>
    /// Whether a scene with the given name exists.
    /// </summary>
    public bool SceneExists(string sceneName)
        => base.GetSceneList().Scenes.Any(s => s.Name == sceneName);

    /// <summary>
    /// Ensures the output video settings are configured as expected.
    /// </summary>
    public async Task EnsureVideoSettingsAsync()
    {
        var settings = base.GetVideoSettings();
        if (settings.BaseWidth == VideoWidth && settings.OutputWidth == VideoWidth &&
            settings.BaseHeight == VideoHeight && settings.OutputHeight == VideoHeight)
        {
            return;
        }
        else if (base.GetRecordStatus().IsRecording ||
                 base.GetStreamStatus().IsActive ||
                 base.GetVirtualCamStatus().IsActive)
        {
            throw new InvalidOperationException("Cannot change output video settings while recording, streaming, or virtual camera is active. Output settings must be 1920x1080.");
        }

        this.log.LogInformation("Setting video settings to {width}x{height}", VideoWidth, VideoHeight);
        settings.BaseWidth = settings.OutputWidth = VideoWidth;
        settings.BaseHeight = settings.OutputHeight = VideoHeight;
        settings.FpsNumerator = 30;
        settings.FpsDenominator = 1;
        base.SetVideoSettings(settings);

        // Sleep before returning as to not overwhelm OBS :)
        await Task.Delay(BackoffDelay);
    }

    /// <summary>
    /// Ensures an OBS scene named <see cref="BambuScene"/> exists.
    /// </summary>
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

    /// <summary>
    /// Creates the Bambu video feed input source if it doesn't exist.
    /// </summary>
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
            this.log.LogInformation("Waiting for stream to start... (Make sure you enabled streaming in Bambu Studio)");
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
            { "boundsHeight", VideoHeight },
            { "boundsWidth", VideoWidth },
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

    /// <summary>
    /// Creates the transparent status backdrop if it doesn't exist.
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Creates a text input source if it doesn't exist.
    /// </summary>
    public async Task<InputSettings> EnsureTextInputAsync(
        InitialTextSettings inputSettings,
        int zIndex)
    {
        if (this.InputExists(inputSettings.Name, out var input))
        {
            if (this.obsSettings.ForceCreateInputs)
            {
                base.RemoveInput(inputSettings.Name);
            }
            else
            {
                return input;
            }
        }

        this.log.LogInformation("Creating text source {sourceName}", inputSettings.Name);

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
        var id = base.CreateInput(BambuScene, inputSettings.Name, TextInputType, itemData, true);

        var transform = new JObject
        {
            { "positionX", inputSettings.DefaultPositionX },
            { "positionY", inputSettings.DefaultPositionY }
        };
        base.SetSceneItemTransform(BambuScene, id, transform);

        base.SetSceneItemIndex(BambuScene, id, zIndex);
        if (this.obsSettings.LockInputs)
        {
            base.SetSceneItemLocked(BambuScene, id, true);
        }

        // Sleep before returning as to not overwhelm OBS :)
        await Task.Delay(100);
        return base.GetInputSettings(inputSettings.Name);
    }

    /// <summary>
    /// Creates an icon input source if it doesn't exist, otherwise updates the existing source with the new icon.
    /// </summary>
    public async Task<InputSettings> EnsureImageInputAsync(
        InitialIconSettings inputSettings,
        int zIndex)
    {
        if (this.InputExists(inputSettings.Name, out var input))
        {
            if (this.obsSettings.ForceCreateInputs)
            {
                base.RemoveInput(inputSettings.Name);
            }
            else
            {
                if (input.Settings["file"].Value<string>() != inputSettings.DefaultIconPath)
                {
                    input.Settings["file"] = inputSettings.DefaultIconPath;
                    base.SetInputSettings(input);
                    await Task.Delay(BackoffDelay);
                }
                return input;
            }
        }

        this.log.LogInformation("Creating icon source {sourceName}", inputSettings.Name);

        var imageInput = new JObject
        {
            {"file", inputSettings.DefaultIconPath },
            {"linear_alpha", true },
            {"unload", true }
        };
        var id = base.CreateInput(BambuScene, inputSettings.Name, ImageInputType, imageInput, true);

        var transform = new JObject
        {
            { "positionX", inputSettings.DefaultPositionX },
            { "positionY", inputSettings.DefaultPositionY },
            { "scaleX", inputSettings.DefaultScaleFactor },
            { "scaleY", inputSettings.DefaultScaleFactor }
        };
        base.SetSceneItemTransform(BambuScene, id, transform);

        base.SetSceneItemIndex(BambuScene, id, zIndex);
        if (this.obsSettings.LockInputs)
        {
            base.SetSceneItemLocked(BambuScene, id, true);
        }

        // Sleep before returning as to not overwhelm OBS :)
        await Task.Delay(100);
        return base.GetInputSettings(inputSettings.Name);
    }

    /// <summary>
    /// Creates a togglable icon input source if it doesn't exist, otherwise updates the existing source with the "off state" icon.
    /// </summary>
    public async Task<ToggleIconInputSettings> EnsureImageInputAsync(
        InitialToggleIconSettings settings,
        int zIndex)
    {
        var inputSettings = await this.EnsureImageInputAsync((InitialIconSettings)settings, zIndex);
        return new ToggleIconInputSettings(inputSettings, settings);
    }

    /// <summary>
    /// Updates the text of an existing text input source.
    /// </summary>
    public void UpdateText(InputSettings setting, string text)
    {
        setting.Settings["text"] = text;
        this.SetInputSettings(setting);
    }

    /// <summary>
    /// Sets the icon path of an existing input source based on the state.
    /// </summary>
    public void SetIconState(ToggleIconInputSettings settings, bool isEnabled)
    {
        settings.InputSettings.Settings["file"] = isEnabled 
            ? settings.InitialToggleIconSettings.DefaultEnabledIconPath 
            : settings.InitialToggleIconSettings.DefaultIconPath;
        this.SetInputSettings(settings.InputSettings);
    }
}
