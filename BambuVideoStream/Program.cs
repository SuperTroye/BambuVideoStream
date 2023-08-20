using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using BambuVideoStream;
using Microsoft.Extensions.Hosting;


IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<BambuSettings>(context.Configuration.GetSection(nameof(BambuSettings)));
        services.AddTransient<FtpService>();
        services.AddHostedService<MqttClientBackgroundService>();
        services.AddLogging(options => options.AddConsole());
    })
    .Build();

await host.RunAsync();
