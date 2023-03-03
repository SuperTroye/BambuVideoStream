using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;
using System.IO;
using WinSCP;

namespace BambuVideoStream
{
    public class FtpService
    {
        BambuSettings settings;
        SessionOptions sessionOptions;

        public FtpService(IConfiguration config)
        {
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



        public RemoteFileInfoCollection ListDirectory()
        {
            using (Session session = new Session())
            {
                session.Open(sessionOptions);

                RemoteDirectoryInfo directory = session.ListDirectory("/cache");

                return directory.Files;
            }
        }




        public byte[] GetFileThumbnail(string filename)
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

                            return memoryStream.ToArray();
                        }
                    }
                }
            }
        }


        public void TransferFileOverFtp()
        {
            using Session session = new Session();

            session.Open(sessionOptions);

            using var stream = System.IO.File.OpenRead("D:\\Desktop\\Models\\filament-spool-winder\\Print plate Axis Washers handle.3mf");
            session.PutFile(stream, "/cache/Print plate Axis Washers handle.3mf");
        }

    }
}
