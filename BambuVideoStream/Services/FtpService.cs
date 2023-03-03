using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.IO.Compression;
using System.IO;
using WinSCP;
using Microsoft.Extensions.Options;
using System;
using System.Xml.Linq;
using System.Xml.XPath;

namespace BambuVideoStream
{
    public class FtpService
    {
        BambuSettings settings;
        SessionOptions sessionOptions;

        public FtpService(IOptions<BambuSettings> options)
        {
            settings = options.Value;

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
                        string previewFileName = "Metadata/plate_1.png";

                        if (filename.Contains("plate_2"))
                        {
                            previewFileName = "Metadata/plate_2.png";
                        }

                        using (var entryStream = archive.GetEntry(previewFileName).Open())
                        {
                            MemoryStream memoryStream = new MemoryStream();
                            entryStream.CopyTo(memoryStream);

                            return memoryStream.ToArray();
                        }
                    }
                }
            }
        }



        public string GetPrintJobWeight(string filename)
        {
            try
            {
                using (Session session = new Session())
                {
                    session.Open(sessionOptions);

                    using (Stream stream = session.GetFile(filename))
                    {
                        using (ZipArchive archive = new ZipArchive(stream, ZipArchiveMode.Read))
                        {
                            string configFileName = "Metadata/slice_info.config";

                            using (StreamReader reader = new StreamReader(archive.GetEntry(configFileName).Open()))
                            {
                                string xml = reader.ReadToEnd();

                                var doc = XDocument.Parse(xml);
                                var filamenNode = doc.XPathSelectElement("//filament");
                                var weight = filamenNode.Attribute("used_g").Value;
                                return weight;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return null;
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
