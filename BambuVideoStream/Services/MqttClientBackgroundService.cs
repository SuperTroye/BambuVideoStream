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
    private static readonly string ImageContentRootPath = Path.Combine(AppContext.BaseDirectory, "Images");
    private readonly ILogger<MqttClientBackgroundService> log;
    private readonly IHostApplicationLifetime hostLifetime;

    private readonly AppSettings appSettings;
    private readonly BambuSettings bambuSettings;
    private readonly OBSSettings obsSettings;

    private readonly IMqttClient mqttClient;
    private readonly MqttClientOptions mqttClientOptions;
    private readonly MqttClientSubscribeOptions mqttSubscribeOptions;
    private readonly MyOBSWebsocket obs;
    private readonly FtpService ftpService;
    private readonly ConcurrentQueue<Action> queuedOperations = new();

    private bool obsInitialized;

    private InputSettings chamberTemp;
    private InputSettings bedTemp;
    private InputSettings targetBedTemp;
    private InputSettings nozzleTemp;
    private InputSettings targetNozzleTemp;
    private InputSettings nozzleTempIcon;
    private InputSettings bedTempIcon;
    private InputSettings percentComplete;
    private InputSettings layers;
    private InputSettings timeRemaining;
    private InputSettings subtaskName;
    private InputSettings stage;
    private InputSettings partFan;
    private InputSettings auxFan;
    private InputSettings chamberFan;
    private InputSettings filament;
    private InputSettings printWeight;
    private InputSettings partFanIcon;
    private InputSettings auxFanIcon;
    private InputSettings chamberFanIcon;
    private InputSettings previewImage;

    private string subtask_name;
    private int lastLayerNum;
    private PrintStage? lastPrintStage;

    public MqttClientBackgroundService(
        FtpService ftpService,
        MyOBSWebsocket obsWebsocket,
        IOptions<BambuSettings> bambuOptions,
        IOptions<OBSSettings> obsOptions,
        IOptions<AppSettings> appOptions,
        ILogger<MqttClientBackgroundService> logger,
        IHostApplicationLifetime hostLifetime)
    {
        this.bambuSettings = bambuOptions.Value;
        this.obsSettings = obsOptions.Value;
        this.appSettings = appOptions.Value;

        this.obs = obsWebsocket;
        this.obs.Connected += this.Obs_Connected;
        this.obs.Disconnected += this.Obs_Disconnected;

        var mqttFactory = new MqttFactory();
        this.mqttClient = mqttFactory.CreateMqttClient();
        this.mqttClient.ApplicationMessageReceivedAsync += this.OnMessageReceived;
        this.mqttClient.DisconnectedAsync += this.MqttClient_DisconnectedAsync;
        this.mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(this.bambuSettings.IpAddress, this.bambuSettings.Port)
            .WithCredentials(this.bambuSettings.Username, this.bambuSettings.Password)
            .WithTlsOptions(new MqttClientTlsOptions
            {
                UseTls = true,
                SslProtocol = SslProtocols.Tls12,
                CertificateValidationHandler = x => { return true; }
            })
            .Build();
        this.mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f =>
            {
                f.WithTopic($"device/{this.bambuSettings.Serial}/report");
            }).Build();

        this.ftpService = ftpService;
        this.log = logger;
        this.hostLifetime = hostLifetime;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        this.obs.ConnectAsync(this.obsSettings.WsConnection, this.obsSettings.WsPassword ?? string.Empty);
        stoppingToken.Register(() => this.obs.Disconnect());

        var mqttFactory = new MqttFactory();

        using var _ = this.mqttClient;
        try
        {
            var connectResult = await this.mqttClient.ConnectAsync(this.mqttClientOptions, stoppingToken);
            if (connectResult?.ResultCode != MqttClientConnectResultCode.Success)
            {
                throw new Exception($"Failed to connect to MQTT: {connectResult.ResultCode}");
            }

            this.log.LogInformation("connected to MQTT");

            await this.mqttClient.SubscribeAsync(this.mqttSubscribeOptions, stoppingToken);

            // Wait for the application to stop
            var waitForClose = new TaskCompletionSource();
            stoppingToken.Register(() => waitForClose.SetResult());
            await waitForClose.Task;

            // shutting down
            await this.mqttClient.DisconnectAsync(cancellationToken: CancellationToken.None);
        }
        catch (Exception ex)
        {
            this.log.LogError(ex, "MQTT failure");
            this.hostLifetime.StopApplication();
        }
    }

    private async void Obs_Connected(object sender, EventArgs e)
    {
        this.log.LogInformation("connected to OBS WebSocket");

        if (this.appSettings.PrintExistingSceneItemsOnStartup)
        {
            this.PrintSceneItems();
        }

        try
        {
            // ===========================================
            // Scene and video stream
            // ===========================================
            await this.obs.EnsureVideoSettingsAsync();
            await this.obs.EnsureBambuSceneAsync();
            await this.obs.EnsureBambuStreamSourceAsync();
            await this.obs.EnsureColorSourceAsync();

            // Z-index for inputs starts at 2 (will be incremented below), because the stream and color source are at 0 and 1
            int z_index = 2;

            // ===========================================
            // Text sources
            // ===========================================
            this.chamberTemp = await this.obs.EnsureTextInputSettingsAsync("ChamberTemp", 71, 1029, z_index++);
            this.bedTemp = await this.obs.EnsureTextInputSettingsAsync("BedTemp", 277, 1029, z_index++);
            this.targetBedTemp = await this.obs.EnsureTextInputSettingsAsync("TargetBedTemp", 313, 1029, z_index++);
            this.nozzleTemp = await this.obs.EnsureTextInputSettingsAsync("NozzleTemp", 527, 1028, z_index++);
            this.targetNozzleTemp = await this.obs.EnsureTextInputSettingsAsync("TargetNozzleTemp", 580, 1028, z_index++);
            this.percentComplete = await this.obs.EnsureTextInputSettingsAsync("PercentComplete", 1510, 1022, z_index++);
            this.layers = await this.obs.EnsureTextInputSettingsAsync("Layers", 1652, 972, z_index++);
            this.timeRemaining = await this.obs.EnsureTextInputSettingsAsync("TimeRemaining", 1791, 1024, z_index++);
            this.subtaskName = await this.obs.EnsureTextInputSettingsAsync("SubtaskName", 838, 971, z_index++);
            this.stage = await this.obs.EnsureTextInputSettingsAsync("Stage", 842, 1019, z_index++);
            this.partFan = await this.obs.EnsureTextInputSettingsAsync("PartFan", 58, 971, z_index++);
            this.auxFan = await this.obs.EnsureTextInputSettingsAsync("AuxFan", 277, 971, z_index++);
            this.chamberFan = await this.obs.EnsureTextInputSettingsAsync("ChamberFan", 521, 971, z_index++);
            this.filament = await this.obs.EnsureTextInputSettingsAsync("Filament", 1437, 1022, z_index++);
            this.printWeight = await this.obs.EnsureTextInputSettingsAsync("PrintWeight", 1303, 1021, z_index++);

            // ===========================================
            // Image sources
            // ===========================================
            this.nozzleTempIcon = await this.obs.EnsureImageInputSettingsAsync("NozzleTempIcon", Path.Combine(ImageContentRootPath, "monitor_nozzle_temp.png"), 471, 1025, 1m, z_index++);
            this.bedTempIcon = await this.obs.EnsureImageInputSettingsAsync("BedTempIcon", Path.Combine(ImageContentRootPath, "monitor_bed_temp.png"), 222, 1025, 1m, z_index++);
            this.partFanIcon = await this.obs.EnsureImageInputSettingsAsync("PartFanIcon", Path.Combine(ImageContentRootPath, "fan_off.png"), 10, 969, 1m + (2m / 3m), z_index++);
            this.auxFanIcon = await this.obs.EnsureImageInputSettingsAsync("AuxFanIcon", Path.Combine(ImageContentRootPath, "fan_off.png"), 227, 969, 1m + (2m / 3m), z_index++);
            this.chamberFanIcon = await this.obs.EnsureImageInputSettingsAsync("ChamberFanIcon", Path.Combine(ImageContentRootPath, "fan_off.png"), 475, 969, 1m + (2m / 3m), z_index++);
            this.previewImage = await this.obs.EnsureImageInputSettingsAsync("PreviewImage", Path.Combine(ImageContentRootPath, "preview_placeholder.png"), 1667, 0, 0.5m, z_index++);
            // Static image sources
            await this.obs.EnsureImageInputSettingsAsync("ChamberTempIcon", Path.Combine(ImageContentRootPath, "monitor_frame_temp.png"), 9, 1021, 1m, z_index++);
            await this.obs.EnsureImageInputSettingsAsync("TimeIcon", Path.Combine(ImageContentRootPath, "monitor_tasklist_time.png"), 1730, 1016, 1m, z_index++);
            await this.obs.EnsureImageInputSettingsAsync("FilamentIcon", Path.Combine(ImageContentRootPath, "filament.png"), 1250, 1017, 1m, z_index++);


            this.obsInitialized = true;

            if (this.obsSettings.StartStreamOnStartup && !this.obs.GetStreamStatus().IsActive)
            {
                this.obs.StartStream();
            }
        }
        catch (Exception ex)
        {
            this.log.LogError(ex, "Failed to initialize OBS inputs. Is your OBS Studio setup correctly?");
            this.hostLifetime.StopApplication();
        }
    }

    private void Obs_Disconnected(object sender, ObsDisconnectionInfo e)
    {
        this.log.LogWarning("OBS WebSocket disconnected: {reason}", e.DisconnectReason);
        if (e.ObsCloseCode == ObsCloseCodes.AuthenticationFailed)
        {
            this.log.LogError("OBS WebSocket authentication failed. Check your OBS settings.");
        }
        this.hostLifetime.StopApplication();
    }

    private Task MqttClient_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
    {
        this.log.LogInformation("MQTT disconnected: {reason}", arg.Reason);
        if (arg.Reason == MqttClientDisconnectReason.NotAuthorized)
        {
            this.log.LogError("MQTT authentication failed. Check your Bambu settings.");
        }
        this.hostLifetime.StopApplication();
        return Task.CompletedTask;
    }

    private Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            string json = Encoding.UTF8.GetString(e.ApplicationMessage.PayloadSegment);
            this.log.LogTrace("Received message: {json}", json);

            var doc = JsonDocument.Parse(json);

            var root = doc.RootElement.EnumerateObject().Select(x => x.Name).First();

            switch (root)
            {
                case "print":
                    this.log.LogTrace("Received 'print' message");

                    var p = doc.Deserialize<PrintMessage>();

                    if (!this.obs.IsConnected || !this.obsInitialized)
                    {
                        this.log.LogWarning("OBS not connected or initialized");
                        break;
                    }
                    this.UpdateSettingText(this.chamberTemp, $"{p.print.chamber_temper} °C");
                    this.UpdateSettingText(this.bedTemp, $"{p.print.bed_temper}");

                    this.UpdateBedTempIconSetting(this.bedTempIcon, p.print.bed_target_temper);
                    this.UpdateNozzleTempIconSetting(this.nozzleTempIcon, p.print.nozzle_target_temper);

                    string targetBedTempStr = $" / {p.print.bed_target_temper} °C";
                    if (p.print.bed_target_temper == 0)
                    {
                        targetBedTempStr = "";
                    }

                    this.UpdateSettingText(this.targetBedTemp, targetBedTempStr);
                    this.UpdateSettingText(this.nozzleTemp, $"{p.print.nozzle_temper}");

                    string targetNozzleTempStr = $" / {p.print.nozzle_target_temper} °C";
                    if (p.print.nozzle_target_temper == 0)
                    {
                        targetNozzleTempStr = "";
                    }

                    this.UpdateSettingText(this.targetNozzleTemp, targetNozzleTempStr);

                    string percentMsg = $"{p.print.mc_percent}% complete";
                    this.UpdateSettingText(this.percentComplete, percentMsg);
                    string layerMsg = $"Layers: {p.print.layer_num}/{p.print.total_layer_num}";
                    this.UpdateSettingText(this.layers, layerMsg);

                    if (this.lastLayerNum != p.print.layer_num)
                    {
                        this.log.LogInformation("{percentMsg}: {layerMsg}", percentMsg, layerMsg);
                        this.lastLayerNum = p.print.layer_num;
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

                    this.UpdateSettingText(this.timeRemaining, timeFormatted);
                    this.UpdateSettingText(this.subtaskName, $"Model: {p.print.subtask_name}");
                    this.UpdateSettingText(this.stage, $"Stage: {p.print.current_stage_str}");

                    this.UpdateSettingText(this.partFan, $"Part: {p.print.GetFanSpeed(p.print.cooling_fan_speed)}%");
                    this.UpdateSettingText(this.auxFan, $"Aux: {p.print.GetFanSpeed(p.print.big_fan1_speed)}%");
                    this.UpdateSettingText(this.chamberFan, $"Chamber: {p.print.GetFanSpeed(p.print.big_fan2_speed)}%");

                    this.UpdateFanIconSetting(this.partFanIcon, p.print.cooling_fan_speed);
                    this.UpdateFanIconSetting(this.auxFanIcon, p.print.big_fan1_speed);
                    this.UpdateFanIconSetting(this.chamberFanIcon, p.print.big_fan2_speed);

                    var tray = p.print.ams?.GetCurrentTray();
                    if (tray != null)
                    {
                        this.UpdateSettingText(this.filament, tray.tray_type);
                    }

                    if (!string.IsNullOrEmpty(p.print.subtask_name) && p.print.subtask_name != this.subtask_name)
                    {
                        this.subtask_name = p.print.subtask_name;
                        this.DownloadFileImagePreview($"/cache/{this.subtask_name}.3mf");

                        var weight = this.ftpService.GetPrintJobWeight($"/cache/{this.subtask_name}.3mf");
                        this.UpdateSettingText(this.printWeight, $"{weight}g");
                    }

                    this.CheckStreamStatus(p);

                    break;

                default:
                    this.log.LogTrace("Unknown message type: {root}", root);
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
            this.log.LogError(ex, "Failed to process message");
        }

        return Task.CompletedTask;
    }

    private void UpdateSettingText(InputSettings setting, string text)
    {
        setting.Settings["text"] = text;
        this.obs.SetInputSettings(setting);
    }

    private void UpdateBedTempIconSetting(InputSettings setting, double value)
    {
        setting.Settings["file"] = Path.Combine(ImageContentRootPath, value == 0 ? "monitor_bed_temp.png" : "monitor_bed_temp_active.png");
        this.obs.SetInputSettings(setting);
    }

    private void UpdateNozzleTempIconSetting(InputSettings setting, double value)
    {
        setting.Settings["file"] = Path.Combine(ImageContentRootPath, value == 0 ? "monitor_nozzle_temp.png" : "monitor_nozzle_temp_active.png");
        this.obs.SetInputSettings(setting);
    }

    private void UpdateFanIconSetting(InputSettings setting, string value)
    {
        setting.Settings["file"] = Path.Combine(ImageContentRootPath, value == "0" ? "fan_off.png" : "fan_icon.png");
        this.obs.SetInputSettings(setting);
    }

    private void DownloadFileImagePreview(string fileName)
    {
        using var op = this.log.BeginScope(nameof(DownloadFileImagePreview));
        this.log.LogInformation("getting {fileName} from ftp", fileName);
        try
        {
            var bytes = this.ftpService.GetFileThumbnail(fileName);

            File.WriteAllBytes(Path.Combine(ImageContentRootPath, "preview.png"), bytes);
            this.log.LogInformation("got image preview");

            this.previewImage.Settings["file"] = Path.Combine(ImageContentRootPath, "preview.png");
            this.obs.SetInputSettings(this.previewImage);
            this.log.LogInformation("updated image preview");
        }
        catch (Exception ex)
        {
            this.log.LogError(ex, "Failed to get image preview");
        }
    }

    /// <summary>
    /// Checks the status of the obs stream and stops it if the print is complete
    /// </summary>
    /// <param name="p">The PrintMessage from MQTT</param>
    private void CheckStreamStatus(PrintMessage p)
    {
        try
        {
            if (p.print.current_stage == PrintStage.Idle &&
                this.lastPrintStage != null &&
                this.lastPrintStage != PrintStage.Idle)
            {
                this.log.LogInformation("Print complete!");
            }

            // Stop stream?
            if (this.queuedOperations.IsEmpty)
            {
                if (p.print.current_stage == PrintStage.Idle &&
                    this.obs.GetStreamStatus().IsActive &&
                    this.obsSettings.StopStreamOnPrinterIdle)
                {
                    this.log.LogInformation("Stopping stream in 5s");
                    this.queuedOperations.Enqueue(() =>
                    {
                        // Must check again - StopStream() throws if stream already stopped
                        if (this.obs.GetStreamStatus().IsActive)
                        {
                            this.obs.StopStream();
                        }
                    });
                }
                // Check for app shutdown conditions
                if (p.print.current_stage == PrintStage.Idle &&
                    this.appSettings.ExitOnIdle)
                {
                    this.log.LogInformation("Printer is idle. Exiting in 5s.");
                    this.queuedOperations.Enqueue(() => this.hostLifetime.StopApplication());
                }

                if (!this.queuedOperations.IsEmpty)
                {
                    Task.Run(async () =>
                    {
                        await Task.Delay(5000);
                        while (this.queuedOperations.TryDequeue(out var action))
                        {
                            action();
                        }
                    });
                }
            }

            // Start stream?
            if (this.queuedOperations.IsEmpty &&
                p.print.current_stage != PrintStage.Idle &&
                this.obsSettings.StartStreamOnStartup &&
                !this.obs.GetStreamStatus().IsActive)
            {
                this.log.LogInformation("Printer has resumed printing. Starting stream.");
                this.obs.StartStream();
            }
        }
        finally
        {
            this.lastPrintStage = p.print.current_stage;
        }
    }

    /// <summary>
    /// Utility method for getting scene items
    /// </summary>
    private void PrintSceneItems()
    {
        this.log.LogInformation("Video settings:\n{settings}", JsonConvert.SerializeObject(this.obs.GetVideoSettings(), Formatting.Indented));

        var list = this.obs.GetInputList();

        foreach (var input in list)
        {
            string scene = "BambuStream";
            string source = input.InputName;

            try
            {
                int itemId = this.obs.GetSceneItemId(scene, source, 0);
                var transform = this.obs.GetSceneItemTransform(scene, itemId);
                var settings = this.obs.GetInputSettings(source);
                this.log.LogInformation("{inputKind} {source}:\n{transform}\nSettings:\n{settings}", input.InputKind, source, JsonConvert.SerializeObject(transform, Formatting.Indented), settings.Settings.ToString(Formatting.Indented));
            }
            catch (Exception ex)
            {
                this.log.LogTrace(ex, "Failed to get scene item {source}", source);
            }
        }
    }
}

