using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Xml.Linq;
using System.Xml.XPath;
using BambuVideoStream.Models;
using FluentFTP;
using FluentFTP.GnuTLS;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;


namespace BambuVideoStream;

public class FtpService(
    IOptions<BambuSettings> options,
    ILogger<FtpService> logger,
    IHostApplicationLifetime lifetime)
{
    private readonly ILogger<FtpService> log = logger;
    private readonly BambuSettings settings = options.Value;
    private readonly CancellationToken ct = lifetime.ApplicationStopping;

    public IList<FtpListItem> ListDirectory()
    {
        try
        {
            this.ct.ThrowIfCancellationRequested();

            using var ftp = this.GetFtpClient();
            using var _ = this.ct.Register(ftp.Disconnect);
            var directory = ftp.GetListing("/cache");
            return directory;
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

            using (var ftp = this.GetFtpClient())
            using (this.ct.Register(ftp.Disconnect))
            {
                if (!ftp.DownloadStream(file, filename))
                {
                    throw new FileNotFoundException($"File {filename} not found");
                }
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

            using (var ftp = this.GetFtpClient())
            using (this.ct.Register(ftp.Disconnect))
            {
                if (!ftp.DownloadStream(file, filename))
                {
                    throw new FileNotFoundException($"File {filename} not found");
                }
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

    private FtpClient GetFtpClient()
    {
        var ftp = new FtpClient(
            this.settings.IpAddress,
            this.settings.Username,
            this.settings.Password,
            990,
            new FtpConfig
            {
                LogHost = true,
                ValidateAnyCertificate = true,
                EncryptionMode = FtpEncryptionMode.Implicit,
                CustomStream = typeof(GnuTlsStream),
                DataConnectionType = FtpDataConnectionType.EPSV,
                DownloadDataType = FtpDataType.Binary
            },
            new FtpLogger(this.log));
        ftp.Connect();
        return ftp;
    }

    private class FtpLogger(ILogger<FtpService> logger) : IFtpLogger
    {
        private readonly ILogger<FtpService> log = logger;

        public void Log(FtpLogEntry entry)
        {
            var level = entry.Severity switch
            {
                FtpTraceLevel.Error => LogLevel.Error,
                FtpTraceLevel.Warn => LogLevel.Warning,
                _ => LogLevel.Trace
            };
            log.Log(level, entry.Exception, "{message}", entry.Message);
        }
    }
}
