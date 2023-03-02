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


            string host = settings.ipAddress;
            string username = settings.username;
            string password = settings.password;
            int port = 990;

            sessionOptions = new SessionOptions
            {
                Protocol = Protocol.Ftp,
                HostName = host,
                PortNumber = port,
                UserName = username,
                Password = password,
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



        // http://localhost:5000/api/Print/GetFileThumbnail
        public IActionResult GetFileThumbnail()
        {
            using Session session = new Session();

            session.Open(sessionOptions);

            var stream = session.GetFile("/cache/Print plate Axis Washers handle.3mf");

            using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

            using Stream entryStream = archive.GetEntry("Metadata/plate1.png").Open();

            using var reader = new StreamReader(entryStream, Encoding.UTF8);

            string data = reader.ReadToEnd();

            return Ok();

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

