using Microsoft.Extensions.Options;
using ObjectDetection;
using ObjectDetection.Model;

var builder = Host.CreateApplicationBuilder(args);
builder.Services
    .Configure<Onnx>(builder.Configuration.GetSection("Onnx"))
    .AddSingleton(s => s.GetRequiredService<IOptions<Onnx>>().Value)
    .Configure<TelegramBot>(builder.Configuration.GetSection("TelegramBot"))
    .AddSingleton(s => s.GetRequiredService<IOptions<TelegramBot>>().Value)
    .Configure<Video>(builder.Configuration.GetSection("Video"))
    .AddSingleton(s => s.GetRequiredService<IOptions<Video>>().Value)
    .AddHostedService<Worker>();

var host = builder.Build();
host.Run();