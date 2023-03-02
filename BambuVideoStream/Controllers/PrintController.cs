using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MQTTnet.Client;
using MQTTnet;
using System;
using System.Security.Authentication;
using System.Threading.Tasks;
using System.Threading;
using System.Text.Json;
using WinSCP;
using System.IO.Compression;
using System.IO;
using System.Text;
using System.Reflection.Metadata;

namespace OBSProject
{
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class PrintController : ControllerBase
    {
        BambuSettings settings;
        IConfiguration config;

        SessionOptions sessionOptions;

        public PrintController(IConfiguration config)
        {
            this.config = config;

            settings = new BambuSettings();
            config.GetSection("BambuSettings").Bind(settings);

            sessionOptions = new SessionOptions
            {
                Protocol = Protocol.Ftp,
                HostName = settings.ipAddress,
                PortNumber = 990,
                UserName = settings.username,
                Password = settings.password,
                FtpSecure = FtpSecure.Implicit,
                GiveUpSecurityAndAcceptAnyTlsHostCertificate = true
            };

        }


        // http://localhost:5000/api/Print/ListDirectory
        public IActionResult ListDirectory()
        {
            using (Session session = new Session())
            {
                session.Open(sessionOptions);

                RemoteDirectoryInfo directory = session.ListDirectory("/cache");

                return Ok(directory.Files);
            }
        }



        // http://localhost:5000/api/Print/TransferFileOverFtp
        public IActionResult TransferFileOverFtp()
        {
            using Session session = new Session();

            session.Open(sessionOptions);

            using var stream = System.IO.File.OpenRead("D:\\Desktop\\Models\\filament-spool-winder\\Print plate Axis Washers handle.3mf");
            session.PutFile(stream, "/cache/Print plate Axis Washers handle.3mf");

            return Ok();
        }



        // http://localhost:5000/api/Print/GetFileThumbnail?filename=/cache/Print%20plate%20Axis%20Washers%20handle.3mf
        public IActionResult GetFileThumbnail(string filename)
        {
            using (Session session = new Session())
            {
                session.Open(sessionOptions);

                using (var stream = session.GetFile(filename))
                {
                    using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
                    {
                        using (var entryStream = archive.GetEntry("Metadata/plate_1.png").Open())
                        {
                            MemoryStream memoryStream = new MemoryStream();
                            entryStream.CopyTo(memoryStream);

                            return File(memoryStream, "image/png", "preview.png");
                        }
                    }
                }
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

