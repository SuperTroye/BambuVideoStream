using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Xml.Linq;
using System.Xml.XPath;
using BambuVideoStream.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WinSCP;


namespace BambuVideoStream;

public class FtpService
{
    readonly ILogger<FtpService> log;
    BambuSettings settings;
    SessionOptions sessionOptions;
    CancellationToken ct;

    public FtpService(
        IOptions<BambuSettings> options,
        ILogger<FtpService> logger,
        IHostApplicationLifetime lifetime)
    {
        ct = lifetime.ApplicationStopping;
        settings = options.Value;

        sessionOptions = new SessionOptions
        {
            Protocol = Protocol.Ftp,
            HostName = settings.IpAddress,
            PortNumber = 990,
            UserName = settings.Username,
            Password = settings.Password,
            FtpSecure = FtpSecure.Implicit,
            GiveUpSecurityAndAcceptAnyTlsHostCertificate = true
        };

        log = logger;
    }

    public RemoteFileInfoCollection ListDirectory()
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            using (Session session = new Session())
            using (ct.Register(session.Abort))
            {
                session.Open(sessionOptions);

                RemoteDirectoryInfo directory = session.ListDirectory("/cache");

                return directory.Files;
            }
        }
        catch (OperationCanceledException e)
        {
            throw new ObjectDisposedException($"{nameof(FtpService)} is disposed", e);
        }
    }

    public byte[] GetFileThumbnail(string filename)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            using var file = new MemoryStream();
            using (var session = new Session())
            {
                session.Open(sessionOptions);

                using var stream = session.GetFile(filename);
                stream.CopyToAsync(file, ct).Wait(ct);
            }

            file.Position = 0;
            ct.ThrowIfCancellationRequested();
            using var archive = new ZipArchive(file, ZipArchiveMode.Read);

            string previewFileName = "Metadata/plate_1.png";
            if (filename.Contains("plate_2"))
            {
                previewFileName = "Metadata/plate_2.png";
            }

            ct.ThrowIfCancellationRequested();
            using var entryStream = archive.GetEntry(previewFileName).Open();
            ct.ThrowIfCancellationRequested();

            using var outputStream = new MemoryStream();
            entryStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }
        catch (OperationCanceledException e)
        {
           throw new ObjectDisposedException($"{nameof(FtpService)} is disposed", e);
        }
    }

    public string GetPrintJobWeight(string filename)
    {
        try
        {
            ct.ThrowIfCancellationRequested();
            using var file = new MemoryStream();
            using (Session session = new Session())
            {
                session.Open(sessionOptions);

                using var stream = session.GetFile(filename);
                stream.CopyToAsync(file, ct).Wait(ct);
            }

            file.Position = 0;
            ct.ThrowIfCancellationRequested();
            using var archive = new ZipArchive(file, ZipArchiveMode.Read);

            string configFileName = "Metadata/slice_info.config";

            ct.ThrowIfCancellationRequested();
            using var reader = new StreamReader(archive.GetEntry(configFileName).Open());
            ct.ThrowIfCancellationRequested();
            string xml = reader.ReadToEnd();

            var doc = XDocument.Parse(xml);
            var filamentNode = doc.XPathSelectElement("//filament");
            var weight = filamentNode.Attribute("used_g").Value;
            return weight;
        }
        catch (OperationCanceledException e)
        {
            throw new ObjectDisposedException($"{nameof(FtpService)} is disposed", e);
        }
        catch (Exception ex)
        {
            log.LogError(ex, "Error getting print job weight");
        }

        return null;
    }
}
