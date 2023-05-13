using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BambuVideoStream;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(options => options.AddConsole());

// Add services to the container
builder.Services.Configure<BambuSettings>(builder.Configuration.GetSection(nameof(BambuSettings)));

builder.Services.AddTransient<FtpService>();
builder.Services.AddHostedService<MqttClientBackgroundService>();

var app = builder.Build();


app.Run();