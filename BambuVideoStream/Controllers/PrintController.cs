using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MQTTnet.Client;
using MQTTnet;
using System;
using System.Security.Authentication;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;


namespace BambuVideoStream
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PrintController : ControllerBase
    {
        BambuSettings settings;
        FtpService service;

        public PrintController(IConfiguration config, FtpService service)
        {
            this.service = service;
        }


        // http://localhost:5000/api/Print/ListDirectory
        public IActionResult ListDirectory()
        {
            return Ok(service.ListDirectory());
        }



        // http://localhost:5000/api/Print/TransferFileOverFtp
        public IActionResult TransferFileOverFtp()
        {
            service.TransferFileOverFtp();

            return Ok();
        }



        // http://localhost:5000/api/Print/GetFileThumbnail?filename=/cache/panel%20clips.3mf
        public IActionResult GetFileThumbnail(string filename)
        {
            try
            {
                var bytes = service.GetFileThumbnail(filename);

                return File(bytes, "image/png", "preview.png");
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }



        // http://localhost:5000/api/Print/StartPrint
        public async Task<IActionResult> StartPrint()
        {
            IMqttClient mqttClient;
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


            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

            var request = new
            {
                print = new
                {
                    sequence_id = 0,
                    command = "project_file",
                    param = "Metadata/plate_1.gcode",
                    subtask_name = "baseboard_mount",
                    url = "ftp://cache/baseboard_mount.3mf",
                    timelapse = false,
                    bed_leveling = false,
                    flow_cali = false,
                    vibration_cali = false,
                    layer_inspect = true,
                    use_ams = true
                }
            };

            string requestJson = JsonSerializer.Serialize(request);

            await mqttClient.PublishStringAsync($"device/{settings.serial}/request", requestJson);

            return Ok();
        }
    }
}

