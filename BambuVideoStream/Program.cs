using System;
using System.IO;
using BambuVideoStream;
using BambuVideoStream.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = new HostApplicationBuilder(args);
// Optional config file that can contain user settings
builder.Configuration.AddJsonFile("secrets.json", optional: true);

string fileLogFormat = builder.Configuration.GetValue<string>("Logging:File:FileFormat");
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