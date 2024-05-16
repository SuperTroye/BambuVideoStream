﻿using System;
using System.IO;
using BambuVideoStream;
using BambuVideoStream.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = new HostApplicationBuilder(args);
// Optional config file that can contain user settings
builder.Configuration.AddJsonFile("BambuVideoStream.json", optional: true);

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
builder.Services.AddHostedService<MqttClientBackgroundService>();

var host = builder.Build();

LoggerFactory = host.Services.GetRequiredService<ILoggerFactory>();

await host.RunAsync();


#region Additional code for Program.cs

public partial class Program
{
    public static ILoggerFactory LoggerFactory { get; private set; }
}

#endregion