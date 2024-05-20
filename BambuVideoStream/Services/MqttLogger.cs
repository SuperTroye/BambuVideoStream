using System;
using Microsoft.Extensions.Logging;
using MQTTnet.Client;
using MQTTnet.Diagnostics;

namespace BambuVideoStream.Services;
public class MqttLogger(ILogger<MqttClient> logger) : IMqttNetLogger
{
    public bool IsEnabled { get; set; }

    public void Publish(MqttNetLogLevel logLevel, string source, string message, object[] parameters, Exception exception)
    {
        if (!this.IsEnabled)
        {
            return;
        }

        var level = logLevel switch
        {
            MqttNetLogLevel.Error => LogLevel.Error,
            MqttNetLogLevel.Warning => LogLevel.Warning,
            _ => LogLevel.Trace
        };
#pragma warning disable CA2254 // Template should be a static expression
        logger.Log(level, exception, $"{source}: {message}", parameters);
#pragma warning restore CA2254 // Template should be a static expression
    }
}
