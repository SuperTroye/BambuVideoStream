using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BambuVideoStream.Models;
using BambuVideoStream.Models.Mqtt;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using OBSWebsocketDotNet.Communication;
using OBSWebsocketDotNet.Types;

namespace BambuVideoStream;

public class MqttClientBackgroundService : BackgroundService
{
    static readonly string ImageContentRootPath = Path.Combine(AppContext.BaseDirectory, "Images");

    readonly ILogger<MqttClientBackgroundService> log;
    readonly IHostApplicationLifetime hostLifetime;

    readonly AppSettings appSettings;
    readonly BambuSettings bambuSettings;
    readonly OBSSettings obsSettings;

    readonly IMqttClient mqttClient;
    readonly MqttClientOptions mqttClientOptions;
    readonly MqttClientSubscribeOptions mqttSubscribeOptions;
    readonly MyOBSWebsocket obs;
    readonly FtpService ftpService;
    readonly ConcurrentQueue<Action> queuedOperations = new();

    bool obsInitialized;
    InputSettings chamberTemp;
    InputSettings bedTemp;
    InputSettings targetBedTemp;
    InputSettings nozzleTemp;
    InputSettings targetNozzleTemp;
    InputSettings nozzleTempIcon;
    InputSettings bedTempIcon;
    InputSettings percentComplete;
    InputSettings layers;
    InputSettings timeRemaining;
    InputSettings subtaskName;
    InputSettings stage;
    InputSettings partFan;
    InputSettings auxFan;
    InputSettings chamberFan;
    InputSettings filament;
    InputSettings printWeight;
    InputSettings partFanIcon;
    InputSettings auxFanIcon;
    InputSettings chamberFanIcon;
    InputSettings previewImage;

    string subtask_name;

    int lastLayerNum;
    PrintStage? lastPrintStage;

    public MqttClientBackgroundService(
        FtpService ftpService,
        MyOBSWebsocket obsWebsocket,
        IOptions<BambuSettings> bambuOptions,
        IOptions<OBSSettings> obsOptions,
        IOptions<AppSettings> appOptions,
        ILogger<MqttClientBackgroundService> logger,
        IHostApplicationLifetime hostLifetime)
    {
        bambuSettings = bambuOptions.Value;
        obsSettings = obsOptions.Value;
        appSettings = appOptions.Value;

        obs = obsWebsocket;
        obs.Connected += Obs_Connected;
        obs.Disconnected += Obs_Disconnected;

        var mqttFactory = new MqttFactory();
        mqttClient = mqttFactory.CreateMqttClient();
        mqttClient.ApplicationMessageReceivedAsync += OnMessageReceived;
        mqttClient.DisconnectedAsync += MqttClient_DisconnectedAsync;
        mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(bambuSettings.IpAddress, bambuSettings.Port)
            .WithCredentials(bambuSettings.Username, bambuSettings.Password)
            .WithTlsOptions(new MqttClientTlsOptions
            {
                UseTls = true,
                SslProtocol = SslProtocols.Tls12,
                CertificateValidationHandler = x => { return true; }
            })
            .Build();
        mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f =>
            {
                f.WithTopic($"device/{bambuSettings.Serial}/report");
            }).Build();

        this.ftpService = ftpService;
        this.log = logger;
        this.hostLifetime = hostLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        obs.ConnectAsync(obsSettings.WsConnection, obsSettings.WsPassword ?? string.Empty);
        stoppingToken.Register(() => obs.Disconnect());

        var mqttFactory = new MqttFactory();

        using var _ = mqttClient;
        try
        {
            var connectResult = await mqttClient.ConnectAsync(mqttClientOptions, stoppingToken);
            if (connectResult?.ResultCode != MqttClientConnectResultCode.Success)
            {
                throw new Exception($"Failed to connect to MQTT: {connectResult.ResultCode}");
            }

            log.LogInformation("connected to MQTT");

            await mqttClient.SubscribeAsync(mqttSubscribeOptions, stoppingToken);

            // Wait for the application to stop
            var waitForClose = new TaskCompletionSource();
            stoppingToken.Register(() => waitForClose.SetResult());
            await waitForClose.Task;

            // shutting down
            await mqttClient.DisconnectAsync(cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "MQTT failure");
            hostLifetime.StopApplication();
        }
    }

    private async void Obs_Connected(object sender, EventArgs e)
    {
        log.LogInformation("connected to OBS WebSocket");

        if (appSettings.PrintExistingSceneItemsOnStartup)
        {
            PrintSceneItems();
        }

        try
        {
            // ===========================================
            // Scene and video stream
            // ===========================================
            await obs.EnsureVideoSettingsAsync();
            await obs.EnsureBambuSceneAsync();
            await obs.EnsureBambuStreamSourceAsync();
            await obs.EnsureColorSourceAsync();

            // Z-index for inputs starts at 2 (will be incremented below), because the stream and color source are at 0 and 1
            int z_index = 2;

            // ===========================================
            // Text sources
            // ===========================================
            chamberTemp = await obs.EnsureTextInputSettingsAsync("ChamberTemp", 71, 1029, z_index++);
            bedTemp = await obs.EnsureTextInputSettingsAsync("BedTemp", 277, 1029, z_index++);
            targetBedTemp = await obs.EnsureTextInputSettingsAsync("TargetBedTemp", 313, 1029, z_index++);
            nozzleTemp = await obs.EnsureTextInputSettingsAsync("NozzleTemp", 527, 1028, z_index++);
            targetNozzleTemp = await obs.EnsureTextInputSettingsAsync("TargetNozzleTemp", 580, 1028, z_index++);
            percentComplete = await obs.EnsureTextInputSettingsAsync("PercentComplete", 1510, 1022, z_index++);
            layers = await obs.EnsureTextInputSettingsAsync("Layers", 1652, 972, z_index++);
            timeRemaining = await obs.EnsureTextInputSettingsAsync("TimeRemaining", 1791, 1024, z_index++);
            subtaskName = await obs.EnsureTextInputSettingsAsync("SubtaskName", 838, 971, z_index++);
            stage = await obs.EnsureTextInputSettingsAsync("Stage", 842, 1019, z_index++);
            partFan = await obs.EnsureTextInputSettingsAsync("PartFan", 58, 971, z_index++);
            auxFan = await obs.EnsureTextInputSettingsAsync("AuxFan", 277, 971, z_index++);
            chamberFan = await obs.EnsureTextInputSettingsAsync("ChamberFan", 521, 971, z_index++);
            filament = await obs.EnsureTextInputSettingsAsync("Filament", 1437, 1022, z_index++);
            printWeight = await obs.EnsureTextInputSettingsAsync("PrintWeight", 1303, 1021, z_index++);

            // ===========================================
            // Image sources
            // ===========================================
            nozzleTempIcon = await obs.EnsureImageInputSettingsAsync("NozzleTempIcon", Path.Combine(ImageContentRootPath, "monitor_nozzle_temp.png"), 471, 1025, 1m, z_index++);
            bedTempIcon = await obs.EnsureImageInputSettingsAsync("BedTempIcon", Path.Combine(ImageContentRootPath, "monitor_bed_temp.png"), 222, 1025, 1m, z_index++);
            partFanIcon = await obs.EnsureImageInputSettingsAsync("PartFanIcon", Path.Combine(ImageContentRootPath, "fan_off.png"), 10, 969, 1m + (2m / 3m), z_index++);
            auxFanIcon = await obs.EnsureImageInputSettingsAsync("AuxFanIcon", Path.Combine(ImageContentRootPath, "fan_off.png"), 227, 969, 1m + (2m / 3m), z_index++);
            chamberFanIcon = await obs.EnsureImageInputSettingsAsync("ChamberFanIcon", Path.Combine(ImageContentRootPath, "fan_off.png"), 475, 969, 1m + (2m / 3m), z_index++);
            previewImage = await obs.EnsureImageInputSettingsAsync("PreviewImage", Path.Combine(ImageContentRootPath, "preview_placeholder.png"), 1667, 0, 0.5m, z_index++);
            // Static image sources
            await obs.EnsureImageInputSettingsAsync("ChamberTempIcon", Path.Combine(ImageContentRootPath, "monitor_frame_temp.png"), 9, 1021, 1m, z_index++);
            await obs.EnsureImageInputSettingsAsync("TimeIcon", Path.Combine(ImageContentRootPath, "monitor_tasklist_time.png"), 1730, 1016, 1m, z_index++);
            await obs.EnsureImageInputSettingsAsync("FilamentIcon", Path.Combine(ImageContentRootPath, "filament.png"), 1250, 1017, 1m, z_index++);


            obsInitialized = true;

            if (obsSettings.StartStreamOnStartup && !obs.GetStreamStatus().IsActive)
            {
                obs.StartStream();
            }
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to initialize OBS inputs. Is your OBS Studio setup correctly?");
            hostLifetime.StopApplication();
        }
    }

    private void Obs_Disconnected(object sender, ObsDisconnectionInfo e)
    {
        log.LogWarning("OBS WebSocket disconnected: {reason}", e.DisconnectReason);
        if (e.ObsCloseCode == ObsCloseCodes.AuthenticationFailed)
        {
            log.LogError("OBS WebSocket authentication failed. Check your OBS settings.");
        }
        hostLifetime.StopApplication();
    }

    private Task MqttClient_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
        log.LogInformation("MQTT disconnected: {reason}", arg.Reason);
        if (arg.Reason == MqttClientDisconnectReason.NotAuthorized)
        {
            log.LogError("MQTT authentication failed. Check your Bambu settings.");
        }
        hostLifetime.StopApplication();
        return Task.CompletedTask;
    }

    Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            string json = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            log.LogTrace("Received message: {json}", json);

            var doc = JsonDocument.Parse(json);

            var root = doc.RootElement.EnumerateObject().Select(x => x.Name).First();

            switch (root)
            {
                case "print":
                    log.LogTrace("Received 'print' message");
                    
                    var p = doc.Deserialize<PrintMessage>();

                    if (!obs.IsConnected || !obsInitialized)
                    {
                        log.LogWarning("OBS not connected or initialized");
                        break;
                    }
                    UpdateSettingText(chamberTemp, $"{p.print.chamber_temper} °C");
                    UpdateSettingText(bedTemp, $"{p.print.bed_temper}");

                    UpdateBedTempIconSetting(bedTempIcon, p.print.bed_target_temper);
                    UpdateNozzleTempIconSetting(nozzleTempIcon, p.print.nozzle_target_temper);

                    string targetBedTempStr = $" / {p.print.bed_target_temper} °C";
                    if (p.print.bed_target_temper == 0)
                    {
                        targetBedTempStr = "";
                    }

                    UpdateSettingText(targetBedTemp, targetBedTempStr);
                    UpdateSettingText(nozzleTemp, $"{p.print.nozzle_temper}");

                    string targetNozzleTempStr = $" / {p.print.nozzle_target_temper} °C";
                    if (p.print.nozzle_target_temper == 0)
                    {
                        targetNozzleTempStr = "";
                    }

                    UpdateSettingText(targetNozzleTemp, targetNozzleTempStr);

                    string percentMsg = $"{p.print.mc_percent}% complete";
                    UpdateSettingText(percentComplete, percentMsg);
                    string layerMsg = $"Layers: {p.print.layer_num}/{p.print.total_layer_num}";
                    UpdateSettingText(layers, layerMsg);

                    if (lastLayerNum != p.print.layer_num)
                    {
                        log.LogInformation("{percentMsg}: {layerMsg}", percentMsg, layerMsg);
                        lastLayerNum = p.print.layer_num;
                    }

                    var time = TimeSpan.FromMinutes(p.print.mc_remaining_time);
                    string timeFormatted = "";
                    if (time.TotalMinutes > 59)
                    {
                        timeFormatted = string.Format("-{0}h{1}m", (int)time.TotalHours, time.Minutes);
                    }
                    else
                    {
                        timeFormatted = string.Format("-{0}m", time.Minutes);
                    }

                    UpdateSettingText(timeRemaining, timeFormatted);
                    UpdateSettingText(subtaskName, $"Model: {p.print.subtask_name}");
                    UpdateSettingText(stage, $"Stage: {p.print.current_stage_str}");

                    UpdateSettingText(partFan, $"Part: {p.print.GetFanSpeed(p.print.cooling_fan_speed)}%");
                    UpdateSettingText(auxFan, $"Aux: {p.print.GetFanSpeed(p.print.big_fan1_speed)}%");
                    UpdateSettingText(chamberFan, $"Chamber: {p.print.GetFanSpeed(p.print.big_fan2_speed)}%");

                    UpdateFanIconSetting(partFanIcon, p.print.cooling_fan_speed);
                    UpdateFanIconSetting(auxFanIcon, p.print.big_fan1_speed);
                    UpdateFanIconSetting(chamberFanIcon, p.print.big_fan2_speed);

                    var tray = p.print.ams?.GetCurrentTray();
                    if (tray != null)
                    {
                        UpdateSettingText(filament, tray.tray_type);
                    }

                    if (!string.IsNullOrEmpty(p.print.subtask_name) && p.print.subtask_name != subtask_name)
                    {
                        subtask_name = p.print.subtask_name;
                        DownloadFileImagePreview($"/cache/{subtask_name}.3mf");

                        var weight = ftpService.GetPrintJobWeight($"/cache/{subtask_name}.3mf");
                        UpdateSettingText(printWeight, $"{weight}g");
                    }

                    CheckStreamStatus(p);

                    break;

                default:
                    log.LogTrace("Unknown message type: {root}", root);
                    break;
            }
        }
        catch (ObjectDisposedException)
        {
            // Do nothing. This is expected when the service is shutting down.
        }
        catch (NullReferenceException ex) when (ex.Message == "Websocket is not initialized")
        {
            // Do nothing. This is expected when OBS is not connected or got disposed while processing a message.
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to process message");
        }

        return Task.CompletedTask;
    }

    void UpdateSettingText(InputSettings setting, string text)
    {
        setting.Settings["text"] = text;
        obs.SetInputSettings(setting);
    }

    void UpdateBedTempIconSetting(InputSettings setting, double value)
    {
        setting.Settings["file"] = Path.Combine(ImageContentRootPath, value == 0 ? "monitor_bed_temp.png" : "monitor_bed_temp_active.png");
        obs.SetInputSettings(setting);
    }

    void UpdateNozzleTempIconSetting(InputSettings setting, double value)
    {
        setting.Settings["file"] = Path.Combine(ImageContentRootPath, value == 0 ? "monitor_nozzle_temp.png" : "monitor_nozzle_temp_active.png");
        obs.SetInputSettings(setting);
    }

    void UpdateFanIconSetting(InputSettings setting, string value)
    {
        setting.Settings["file"] = Path.Combine(ImageContentRootPath, value == "0" ? "fan_off.png" : "fan_icon.png");
        obs.SetInputSettings(setting);
    }

    void DownloadFileImagePreview(string fileName)
    {
        using var op = log.BeginScope(nameof(DownloadFileImagePreview));
        log.LogInformation("getting {fileName} from ftp", fileName);
        try
        {
            var bytes = ftpService.GetFileThumbnail(fileName);

            File.WriteAllBytes(Path.Combine(ImageContentRootPath, "preview.png"), bytes);
            log.LogInformation("got image preview");

            previewImage.Settings["file"] = Path.Combine(ImageContentRootPath, "preview.png");
            obs.SetInputSettings(previewImage);
            log.LogInformation("updated image preview");
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Failed to get image preview");
        }
    }

    /// <summary>
    /// Checks the status of the obs stream and stops it if the print is complete
    /// </summary>
    /// <param name="p">The PrintMessage from MQTT</param>
    void CheckStreamStatus(PrintMessage p)
    {
        try
        {
            if (p.print.current_stage == PrintStage.Idle &&
                lastPrintStage != null &&
                lastPrintStage != PrintStage.Idle)
            {
                log.LogInformation("Print complete!");
            }

            // Stop stream?
            if (queuedOperations.Count == 0)
            {
                if (p.print.current_stage == PrintStage.Idle &&
                    obs.GetStreamStatus().IsActive &&
                    obsSettings.StopStreamOnPrinterIdle)
                {
                    log.LogInformation("Stopping stream in 5s");
                    queuedOperations.Enqueue(() =>
                    {
                        // Must check again - StopStream() throws if stream already stopped
                        if (obs.GetStreamStatus().IsActive)
                        {
                            obs.StopStream();
                        }
                    });
                }
                // Check for app shutdown conditions
                if (p.print.current_stage == PrintStage.Idle &&
                    appSettings.ExitOnIdle)
                {
                    log.LogInformation("Printer is idle. Exiting in 5s.");
                    queuedOperations.Enqueue(() => hostLifetime.StopApplication());
                }

                if (queuedOperations.Count > 0)
                {
                    Task.Run(async () =>
                    {
                        await Task.Delay(5000);
                        while (queuedOperations.TryDequeue(out var action))
                        {
                            action();
                        }
                    });
                }
            }

            // Start stream?
            if (queuedOperations.Count == 0 &&
                p.print.current_stage != PrintStage.Idle &&
                obsSettings.StartStreamOnStartup &&
                !obs.GetStreamStatus().IsActive)
            {
                log.LogInformation("Printer has resumed printing. Starting stream.");
                obs.StartStream();
            }
        }
        finally
        {
            lastPrintStage = p.print.current_stage;
        }
    }

    /// <summary>
    /// Utility method for getting scene items
    /// </summary>
    void PrintSceneItems()
    {
        log.LogInformation("Video settings:\n{settings}", JsonConvert.SerializeObject(obs.GetVideoSettings(), Formatting.Indented));

        var list = obs.GetInputList();

        foreach (var input in list)
        {
            string scene = "BambuStream";
            string source = input.InputName;

            try
            {
                int itemId = obs.GetSceneItemId(scene, source, 0);
                var transform = obs.GetSceneItemTransform(scene, itemId);
                var settings = obs.GetInputSettings(source);
                log.LogInformation("{inputKind} {source}:\n{transform}\nSettings:\n{settings}", input.InputKind, source, JsonConvert.SerializeObject(transform, Formatting.Indented), settings.Settings.ToString(Formatting.Indented));
            }
            catch (Exception ex)
            {
                log.LogTrace(ex, "Failed to get scene item {source}", source);
            }
        }
    }
}

