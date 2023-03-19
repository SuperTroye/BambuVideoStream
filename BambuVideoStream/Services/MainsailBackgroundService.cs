using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Linq;
using System.Security.Authentication;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BambuVideoStream
{
    public class MainsailBackgroundService : BackgroundService
    {
        IMqttClient mqttClient;

        public MainsailBackgroundService(IConfiguration config)
        {
        }


        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var mqttFactory = new MqttFactory();

            mqttClient = mqttFactory.CreateMqttClient();

            var mqttClientOptions = new MqttClientOptionsBuilder()
                .WithTcpServer("", 5000)
                .WithCredentials("", "")
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
                    f.WithTopic($"");
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
            string json = Encoding.UTF8.GetString(e.ApplicationMessage.Payload);

            var doc = JsonDocument.Parse(json);

            var root = doc.RootElement.EnumerateObject().Select(x => x.Name).First();
        }







        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            await mqttClient.DisconnectAsync();
            await base.StopAsync(stoppingToken);
        }

    }
}
