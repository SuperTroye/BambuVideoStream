using System;
using System.IO;
using System.Linq;
using System.Reflection;
using BambuVideoStream;
using BambuVideoStream.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Extract embedded resources to the file system
{
    Directory.CreateDirectory(Constants.OBS.ImageDir);
    const string imagePrefix = "BambuVideoStream.Images.";
    var images = Assembly.GetExecutingAssembly().GetManifestResourceNames().Where(r => r.StartsWith(imagePrefix));
    foreach (var image in images)
    {
        var fileName = image[imagePrefix.Length..];
        var filePath = Path.Combine(Constants.OBS.ImageDir, fileName);
        if (!File.Exists(filePath))
        {
            using var resource = Assembly.GetExecutingAssembly().GetManifestResourceStream(image);
            using var file = File.Create(filePath);
            resource.CopyTo(file);
        }
    }
}

var builder = new HostApplicationBuilder(args);
// Optional config file that can contain user settings
builder.Configuration.AddJsonFile("secrets.json", optional: true);

string fileLogFormat = builder.Configuration.GetValue<string>("Logging:File:FilenameFormat");
if (!string.IsNullOrEmpty(fileLogFormat))
{
    if (!Enum.TryParse(builder.Configuration.GetValue<string>("Logging:File:MinimumLevel"), out LogLevel minLevel))
    {
        minLevel = LogLevel.Information;
    }
    builder.Logging.AddFile(fileLogFormat, minimumLevel: minLevel, isJson: false);
    builder.Logging.AddFile(Path.ChangeExtension(fileLogFormat, ".json"), minimumLevel: minLevel, isJson: true);
}
builder.Services.Configure<BambuSettings>(builder.Configuration.GetSection(nameof(BambuSettings)));
builder.Services.Configure<OBSSettings>(builder.Configuration.GetSection(nameof(OBSSettings)));
builder.Services.Configure<AppSettings>(builder.Configuration.GetSection(nameof(AppSettings)));
builder.Services.AddTransient<FtpService>();
builder.Services.AddTransient<MyOBSWebsocket>();
builder.Services.AddHostedService<BambuStreamBackgroundService>();

var host = builder.Build();

await host.RunAsync();