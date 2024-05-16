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
    private readonly ILogger<FtpService> log;
    private BambuSettings settings;
    private SessionOptions sessionOptions;
    private CancellationToken ct;

    public FtpService(
        IOptions<BambuSettings> options,
        ILogger<FtpService> logger,
        IHostApplicationLifetime lifetime)
    {
        this.ct = lifetime.ApplicationStopping;
        this.settings = options.Value;

        this.sessionOptions = new SessionOptions
        {
            Protocol = Protocol.Ftp,
            HostName = this.settings.IpAddress,
            PortNumber = 990,
            UserName = this.settings.Username,
            Password = this.settings.Password,
            FtpSecure = FtpSecure.Implicit,
            GiveUpSecurityAndAcceptAnyTlsHostCertificate = true
        };

        this.log = logger;
    }

    public RemoteFileInfoCollection ListDirectory()
    {
        try
        {
            this.ct.ThrowIfCancellationRequested();
            using (var session = CreateSession())
            using (this.ct.Register(session.Abort))
            {
                session.Open(this.sessionOptions);

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
            this.ct.ThrowIfCancellationRequested();
            using var file = new MemoryStream();
            using (var session = CreateSession())
            {
                session.Open(this.sessionOptions);

                using var stream = session.GetFile(filename);
                stream.CopyToAsync(file, this.ct).Wait(this.ct);
            }

            file.Position = 0;
            this.ct.ThrowIfCancellationRequested();
            using var archive = new ZipArchive(file, ZipArchiveMode.Read);

            string previewFileName = "Metadata/plate_1.png";
            if (filename.Contains("plate_2"))
            {
                previewFileName = "Metadata/plate_2.png";
            }

            this.ct.ThrowIfCancellationRequested();
            using var entryStream = archive.GetEntry(previewFileName).Open();
            this.ct.ThrowIfCancellationRequested();

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
            this.ct.ThrowIfCancellationRequested();
            using var file = new MemoryStream();
            using (var session = CreateSession())
            {
                session.Open(this.sessionOptions);

                using var stream = session.GetFile(filename);
                stream.CopyToAsync(file, this.ct).Wait(this.ct);
            }

            file.Position = 0;
            this.ct.ThrowIfCancellationRequested();
            using var archive = new ZipArchive(file, ZipArchiveMode.Read);

            string configFileName = "Metadata/slice_info.config";

            this.ct.ThrowIfCancellationRequested();
            using var reader = new StreamReader(archive.GetEntry(configFileName).Open());
            this.ct.ThrowIfCancellationRequested();
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
            this.log.LogError(ex, "Error getting print job weight");
        }

        return null;
    }

    private static Session CreateSession() =>
        new()
        {
            ExecutablePath = Path.Combine(AppContext.BaseDirectory, "WinSCP.exe")
        };
}
