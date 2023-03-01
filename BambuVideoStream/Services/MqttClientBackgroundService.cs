using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json.Linq;
using OBSWebsocketDotNet;
using OBSWebsocketDotNet.Types;
using System;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace OBSProject
{
    public class MqttClientBackgroundService : BackgroundService
    {
        IMqttClient mqttClient;
        BambuSettings settings;

        string ObsWsConnection;
        OBSWebsocket obs;
        InputSettings chamberTemp;
        InputSettings bedTemp;
        InputSettings nozzleTemp;
        InputSettings percentComplete;
        InputSettings layers;
        InputSettings timeRemaining;
        InputSettings subtaskName;
        InputSettings stage;
        InputSettings partFan;
        InputSettings auxFan;
        InputSettings chamberFan;


        public MqttClientBackgroundService(IConfiguration config)
        {
            settings = new BambuSettings();
            config.GetSection("BambuSettings").Bind(settings);

            ObsWsConnection = config.GetValue<string>("ObsWsConnection");

            obs = new OBSWebsocket();
            obs.Connected += Obs_Connected;
            obs.ConnectAsync(ObsWsConnection, "");
        }


        private void Obs_Connected(object sender, EventArgs e)
        {
            Console.WriteLine("connected to OBS WebSocket");

            //CreateTextInput("ChamberTemp");
            //CreateTextInput("BedTemp");
            //CreateTextInput("NozzleTemp");
            //CreateTextInput("PercentComplete");
            //CreateTextInput("Layers");
            //CreateTextInput("TimeRemaining");
            //CreateTextInput("SubtaskName");
            //CreateTextInput("Stage");
            //CreateTextInput("PartFan");
            //CreateTextInput("AuxFan");
            //CreateTextInput("ChamberFan");

            chamberTemp = obs.GetInputSettings("ChamberTemp");
            bedTemp = obs.GetInputSettings("BedTemp");
            nozzleTemp = obs.GetInputSettings("NozzleTemp");
            percentComplete = obs.GetInputSettings("PercentComplete");
            layers = obs.GetInputSettings("Layers");
            timeRemaining = obs.GetInputSettings("TimeRemaining");
            subtaskName = obs.GetInputSettings("SubtaskName");
            stage = obs.GetInputSettings("Stage");
            partFan = obs.GetInputSettings("PartFan");
            auxFan = obs.GetInputSettings("AuxFan");
            chamberFan = obs.GetInputSettings("ChamberFan");
        }


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

            mqttClient.ApplicationMessageReceivedAsync += e =>
            {
                string json = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

                var doc = JsonDocument.Parse(json);

                var root = doc.RootElement.EnumerateObject().Select(x => x.Name).First();

                switch (root)
                {
                    case "print":

                        try
                        {
                            var p = doc.Deserialize<PrintMessage>();

                            if (obs.IsConnected)
                            {
                                UpdateSettingText(chamberTemp, $"Chamber: {p.print.chamber_temper} °C");
                                UpdateSettingText(bedTemp, $"Bed: {p.print.bed_temper}/{p.print.bed_target_temper} °C");
                                UpdateSettingText(nozzleTemp, $"Nozzle: {p.print.nozzle_temper}/{p.print.nozzle_target_temper} °C");

                                UpdateSettingText(percentComplete, $"{p.print.mc_percent}%");
                                UpdateSettingText(layers, $"Layers: {p.print.layer_num}/{p.print.total_layer_num}");
                                UpdateSettingText(timeRemaining, $"-{p.print.mc_remaining_time}m");
                                UpdateSettingText(subtaskName, $"{p.print.subtask_name}");
                                UpdateSettingText(stage, $"{p.print.GetActionName(p.print.stg_cur)}");

                                UpdateSettingText(partFan, $"Part: {p.print.GetFanSpeed(p.print.cooling_fan_speed)}%");
                                UpdateSettingText(auxFan, $"Aux: {p.print.GetFanSpeed(p.print.big_fan1_speed)}%");
                                UpdateSettingText(chamberFan, $"Cham: {p.print.GetFanSpeed(p.print.big_fan2_speed)}%");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }

                        break;

                    case "mc_print":

                        var mc_print = doc.Deserialize<McPrintMessage>();

                        // not sure how to deserialize this message. maybe later.
                        //Console.WriteLine($"sequence_id: {mc_print.mc_print.sequence_id}");

                        break;
                }

                return Task.CompletedTask;
            };

            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            var mqttSubscribeOptions = mqttFactory.CreateSubscribeOptionsBuilder()
                .WithTopicFilter(f =>
                {
                    f.WithTopic($"device/{settings.serial}/report");
                }).Build();

            await mqttClient.SubscribeAsync(mqttSubscribeOptions, CancellationToken.None);
        }



        void UpdateSettingText(InputSettings setting, string text)
        {
            setting.Settings["text"] = text;
            obs.SetInputSettings(setting);
        }


        void CreateTextInput(string inputName)
        {
            //var defaults = obs.GetInputDefaultSettings("text_gdiplus_v2");

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

            var newSceneId = obs.CreateInput("Scene", inputName, "text_gdiplus_v2", itemData, true);

            var transform = new JObject
            {
                { "positionX", 500.0f },
                { "positionY", 500.0f }
             };

            obs.SetSceneItemTransform("Scene", newSceneId, transform);
        }


        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            await mqttClient.DisconnectAsync();
            obs.Disconnect();
            await base.StopAsync(stoppingToken);
        }

    }
}
