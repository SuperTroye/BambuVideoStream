using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OBSProject;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddLogging(options => options.AddConsole());

builder.Services.AddCors(x => {
    x.AddDefaultPolicy(y => y.AllowAnyHeader().AllowAnyOrigin());
});


// Add services to the container
builder.Services.AddControllers();

builder.Services.AddHostedService<MqttClientBackgroundService>();

var app = builder.Build();

app.UseCors();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}


app.UseRouting();

app.MapControllers();

app.Run();