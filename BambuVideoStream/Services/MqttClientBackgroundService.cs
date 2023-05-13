using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json.Linq;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.IO;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;


namespace BambuVideoStream;

public class MqttClientBackgroundService : BackgroundService
{
    IMqttClient mqttClient;
    BambuSettings settings;


    OBSWebsocket obs;
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



    private FtpService ftpService;

    public MqttClientBackgroundService(
        IConfiguration config,
        FtpService ftpService,
        IOptions<BambuSettings> options)
    {
        settings = options.Value;

        string ObsWsConnection = config.GetValue<string>("ObsWsConnection");

        obs = new OBSWebsocket();
        obs.Connected += Obs_Connected;
        obs.ConnectAsync(ObsWsConnection, "");

        this.ftpService = ftpService;
    }


    private void Obs_Connected(object sender, EventArgs e)
    {
        Console.WriteLine("connected to OBS WebSocket");

        //GetSceneItems();
        //InitSceneInputs();

        chamberTemp = obs.GetInputSettings("ChamberTemp");
        bedTemp = obs.GetInputSettings("BedTemp");

        nozzleTempIcon = obs.GetInputSettings("NozzleTempIcon");
        bedTempIcon = obs.GetInputSettings("BedTempIcon");

        targetBedTemp = obs.GetInputSettings("TargetBedTemp");
        nozzleTemp = obs.GetInputSettings("NozzleTemp");
        targetNozzleTemp = obs.GetInputSettings("TargetNozzleTemp");
        percentComplete = obs.GetInputSettings("PercentComplete");
        layers = obs.GetInputSettings("Layers");
        timeRemaining = obs.GetInputSettings("TimeRemaining");
        subtaskName = obs.GetInputSettings("SubtaskName");
        stage = obs.GetInputSettings("Stage");
        partFan = obs.GetInputSettings("PartFan");
        auxFan = obs.GetInputSettings("AuxFan");
        chamberFan = obs.GetInputSettings("ChamberFan");
        filament = obs.GetInputSettings("Filament");
        printWeight = obs.GetInputSettings("PrintWeight");
        partFanIcon = obs.GetInputSettings("PartFanIcon");
        auxFanIcon = obs.GetInputSettings("AuxFanIcon");
        chamberFanIcon = obs.GetInputSettings("ChamberFanIcon");
    }



    string subtask_name;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var mqttFactory = new MqttFactory();

        mqttClient = mqttFactory.CreateMqttClient();

        var mqttClientOptions = new MqttClientOptionsBuilder()
            .WithTcpServer(settings.ipAddress, settings.port)
            .WithCredentials(settings.username, settings.password)
            .WithTls(new MqttClientOptionsBuilderTlsParameters()
            {
                UseTls = true,
                SslProtocol = SslProtocols.Tls12,
                CertificateValidationHandler = x => { return true; }
            })
            .Build();

        mqttClient.ApplicationMessageReceivedAsync += OnMessageReceived;

        try
        {
            var connectResult = await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            Console.WriteLine("connected to MQTT");

            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
            .WithTopicFilter(f =>
            {
                f.WithTopic($"device/{settings.serial}/report");
            }).Build();

            await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }


    async Task OnMessageReceived(MqttApplicationMessageReceivedEventArgs e)
    {
        try
        {
            string json = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            var doc = JsonDocument.Parse(json);

            var root = doc.RootElement.EnumerateObject().Select(x => x.Name).First();

            switch (root)
            {
                case "print":

                    //System.IO.File.AppendAllText("D:\\Desktop\\log.json", json + Environment.NewLine + ", " + Environment.NewLine);

                    var p = doc.Deserialize<PrintMessage>();

                    if (obs.IsConnected)
                    {
                        UpdateSettingText(chamberTemp, $"{p.print.chamber_temper} °C");
                        UpdateSettingText(bedTemp, $"{p.print.bed_temper}");

                        UpdateBedTempIconSetting(bedTempIcon, p.print.bed_target_temper);
                        UpdateNozzleTempIconSetting(nozzleTempIcon, p.print.nozzle_target_temper);

                        string targetBedTempStr = $" / {p.print.bed_target_temper} °C";
                        if (p.print.bed_target_temper == 0)
                            targetBedTempStr = "";

                        UpdateSettingText(targetBedTemp, targetBedTempStr);
                        UpdateSettingText(nozzleTemp, $"{p.print.nozzle_temper}");

                        string targetNozzleTempStr = $" / {p.print.nozzle_target_temper} °C";
                        if (p.print.nozzle_target_temper == 0)
                            targetNozzleTempStr = "";

                        UpdateSettingText(targetNozzleTemp, targetNozzleTempStr);

                        UpdateSettingText(percentComplete, $"{p.print.mc_percent}%");
                        UpdateSettingText(layers, $"Layers: {p.print.layer_num}/{p.print.total_layer_num}");

                        var time = TimeSpan.FromMinutes(p.print.mc_remaining_time);
                        string timeFormatted = "";
                        if (time.TotalMinutes > 59)
                            timeFormatted = string.Format("-{0}h{1}m", (int)time.TotalHours, time.Minutes);
                        else
                            timeFormatted = string.Format("-{0}m", time.Minutes);

                        UpdateSettingText(timeRemaining, timeFormatted);
                        UpdateSettingText(subtaskName, $"{p.print.subtask_name}");
                        UpdateSettingText(stage, $"{p.print.current_stage}");

                        UpdateSettingText(partFan, $"Part: {p.print.GetFanSpeed(p.print.cooling_fan_speed)}%");
                        UpdateSettingText(auxFan, $"Aux: {p.print.GetFanSpeed(p.print.big_fan1_speed)}%");
                        UpdateSettingText(chamberFan, $"Chamber: {p.print.GetFanSpeed(p.print.big_fan2_speed)}%");

                        UpdateFanIconSetting(partFanIcon, p.print.cooling_fan_speed);
                        UpdateFanIconSetting(auxFanIcon, p.print.big_fan1_speed);
                        UpdateFanIconSetting(chamberFanIcon, p.print.big_fan2_speed);

                        var tray = GetCurrentTray(p.print.ams);
                        if (tray != null)
                            UpdateSettingText(filament, tray.tray_type);

                        if (!string.IsNullOrEmpty(p.print.subtask_name) && p.print.subtask_name != subtask_name)
                        {
                            subtask_name = p.print.subtask_name;
                            GetFileImagePreview($"/cache/{subtask_name}.3mf");

                            var weight = ftpService.GetPrintJobWeight($"/cache/{subtask_name}.3mf");
                            UpdateSettingText(printWeight, $"{weight}g");
                        }

                        CheckStreamStatus(p);
                    }

                    break;

                case "mc_print":

                    var mc_print = doc.Deserialize<McPrintMessage>();

                    // not sure how to deserialize this message. maybe later.
                    //Console.WriteLine($"sequence_id: {mc_print.mc_print.sequence_id}");

                    break;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }


    void UpdateSettingText(InputSettings setting, string text)
    {
        setting.Settings["text"] = text;
        obs.SetInputSettings(setting);
    }


    void UpdateBedTempIconSetting(InputSettings setting, double value)
    {
        if (value == 0)
            setting.Settings["file"] = "D:/Projects/BambuVideoStream/Images/monitor_bed_temp.png";
        else
            setting.Settings["file"] = "D:/Projects/BambuVideoStream/Images/monitor_bed_temp_active.png";

        obs.SetInputSettings(setting);
    }


    void UpdateNozzleTempIconSetting(InputSettings setting, double value)
    {
        if (value == 0)
            setting.Settings["file"] = "D:/Projects/BambuVideoStream/Images/monitor_nozzle_temp.png";
        else
            setting.Settings["file"] = "D:/Projects/BambuVideoStream/Images/monitor_nozzle_temp_active.png";

        obs.SetInputSettings(setting);
    }


    void UpdateFanIconSetting(InputSettings setting, string value)
    {
        if (value == "0")
            setting.Settings["file"] = "D:/Projects/BambuVideoStream/Images/fan_off.png";
        else
            setting.Settings["file"] = "D:/Projects/BambuVideoStream/Images/fan_icon.png";

        obs.SetInputSettings(setting);
    }


    void GetFileImagePreview(string fileName)
    {
        Console.WriteLine($"getting {fileName} from ftp");
        try
        {
            var bytes = ftpService.GetFileThumbnail(fileName);

            File.WriteAllBytes(@"D:\Projects\BambuVideoStream\Images\preview.png", bytes);

            var stream = ftpService.GetPrintJobWeight(fileName);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
        }
    }



    Tray GetCurrentTray(Ams msg)
    {
        if (!string.IsNullOrEmpty(msg?.tray_now))
        {
            foreach (var ams in msg.ams)
            {
                foreach (var tray in ams.tray)
                {
                    if (tray.id == msg.tray_now)
                    {
                        if (string.IsNullOrEmpty(tray.tray_type))
                        {
                            tray.tray_type = "Empty";
                        }
                        return tray;
                    }
                }
            }
        }

        return null;
    }


    /// <summary>
    /// Checks the status of the obs stream and stops it if the print is complete
    /// </summary>
    /// <param name="p">The PrintMessage from MQTT</param>
    void CheckStreamStatus(PrintMessage p)
    {
        var status = obs.GetStreamStatus();

        var percent = p.print.mc_percent;

        if (percent == 100 && status.IsActive)
        {
            obs.StopStream();
        }
    }



    /// <summary>
    /// Do this once, and when they are created then don't run again.
    /// </summary>
    void InitSceneInputs()
    {
        GetSceneItems();

        // ===========================================
        // BambuStreamSource
        // ===========================================
        var bambuStream = new JObject
            {
                {"ffmpeg_options", "protocol_whitelist=file,udp,rtp" },
                {"hw_decode", false },
                {"input", $"file:{settings.pathToSDP}" },
                {"is_local_file", false },
            };

        obs.CreateInput("BambuStream", "BambuStreamSource", "ffmpeg_source", bambuStream, true);



        // ===========================================
        // PreviewImage
        // ===========================================
        var previewImage = new JObject
            {
                {"file", "D:/Desktop/preview.png" },
                {"linear_alpha", true },
                {"unload", true }
            };

        var newSceneId = obs.CreateInput("BambuStream", "PreviewImage", "image_source", previewImage, true);

        var transform = new JObject
            {
                { "positionX", 1664 },
                { "positionY", 0 }
             };

        obs.SetSceneItemTransform("BambuStream", newSceneId, transform);



        // ===========================================
        // ColorSource
        // ===========================================
        var colorSource = new JObject
            {
                {"color", 4278190080},
                {"height", 130},
                {"width", 1920}
            };

        newSceneId = obs.CreateInput("BambuStream", "ColorSource", "color_source_v3", colorSource, true);

        transform = new JObject
            {
                { "positionX", 0 },
                { "positionY", 950 }
             };

        obs.SetSceneItemTransform("BambuStream", newSceneId, transform);


        CreateTextInput("PrintWeight", 1303, 979);
        CreateTextInput("ChamberTemp", 56, 1021);
        CreateTextInput("BedTemp", 342, 1020);
        CreateTextInput("TargetBedTemp", 474, 1019);
        CreateTextInput("NozzleTemp", 588, 1020);
        CreateTextInput("TargetNozzleTemp", 770, 1019);
        CreateTextInput("PercentComplete", 1707, 1023);
        CreateTextInput("Layers", 1687, 978);
        CreateTextInput("TimeRemaining", 1803, 1023);
        CreateTextInput("SubtaskName", 960, 978);
        CreateTextInput("Stage", 962, 1021);
        CreateTextInput("PartFan", 58, 978);
        CreateTextInput("AuxFan", 256, 978);
        CreateTextInput("ChamberFan", 472, 978);
        CreateTextInput("Filament", 1487, 978);
    }


    void GetSceneItems()
    {
        var list = obs.GetInputList();

        foreach (var input in list)
        {
            string scene = "BambuStream";
            string source = input.InputName;

            try
            {
                int itemId = obs.GetSceneItemId(scene, source, 0);

                var settings = obs.GetInputSettings(source);

                var transform = obs.GetSceneItemTransform(scene, itemId);

                Console.WriteLine($"{input.InputKind} {source} {transform.X}, {transform.Y}");

                //Console.WriteLine($"{JsonSerializer.Serialize(settings)}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }



    void CreateTextInput(string inputName, decimal positionX, decimal positionY)
    {
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

        var newSceneId = obs.CreateInput("BambuStream", inputName, "text_gdiplus_v2", itemData, true);

        var transform = new JObject
            {
                { "positionX", positionX },
                { "positionY", positionY }
             };

        obs.SetSceneItemTransform("BambuStream", newSceneId, transform);
    }



    public override async Task StopAsync(CancellationToken stoppingToken)
    {
        await mqttClient.DisconnectAsync();
        obs.Disconnect();
        await base.StopAsync(stoppingToken);
    }

}

