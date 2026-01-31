using ObjectDetection.Model;
using SkiaSharp;
using Telegram.Bot;
using YoloDotNet;
using YoloDotNet.Enums;
using YoloDotNet.ExecutionProvider.Cpu;
using YoloDotNet.Extensions;
using YoloDotNet.Models;
using YoloDotNet.Trackers;
using YoloDotNet.Video;

namespace ObjectDetection;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly Onnx _onnx;
    private readonly Video _video;
    private readonly TelegramBotClient _botClient;
    private DetectionDrawingOptions _drawingOptions = default!;
    private readonly SortTracker _sortTracker;
    
    public Worker(ILogger<Worker> logger, Video video, Onnx onnx, TelegramBot bot)
    {
        _logger = logger;
        _sortTracker = new SortTracker(0.5f, 5, 60);
        _video = video;
        _onnx = onnx;
        _botClient = new TelegramBotClient(bot.Token);
        CreateOutputFolder();
        SetDrawingOptions();

    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var yolo = new Yolo(new YoloOptions()
        {
            ExecutionProvider = new CpuExecutionProvider(_onnx.ModelPath),

            ImageResize = ImageResize.Proportional,

            SamplingOptions = new(SKFilterMode.Nearest, SKMipmapMode.None)
        });
        Console.WriteLine($"Loaded ONNX Model: {yolo.ModelInfo}");
        var videoOptions = new VideoOptions()
        {
            VideoInput = _video.Input,
            FrameRate = FrameRate.AUTO,
            Width = 720,
            Height = -2,
            CompressionQuality = 30,
            VideoChunkDuration = 0,
            FrameInterval = 0

        };
        if (_video.NeedOutput)
            videoOptions.VideoOutput = Path.Combine(_video.OutputPath, _video.OutputFileName);

        yolo.InitializeVideo(videoOptions);
        var metadata = yolo.GetVideoMetaData();
        ////////////////////////////////////////////////////////
        var devices = Yolo.GetVideoDevices();
        var listedDevices = devices.Count == 0 ? 1 : devices.Count;
        var progressStats = "Progress: ";
        var progressStatsLength = progressStats.Length;
        var textRow = 17 + listedDevices; // What row in the console window to draw progress
        var progress = 0;

        Console.WriteLine();
        Console.WriteLine("Running Object Detection on Video with YOLOv11");
        Console.WriteLine(new string('=', 80));
        Console.Write(progressStats);
        ////////////////////////////////////////////////////////
        yolo.OnVideoFrameReceived = (SKBitmap frame, long frameIndex) =>
        {
            var result = yolo.RunObjectDetection(frame, confidence: 0.2, iou: 0.2).Track(_sortTracker);
            if (result.Count != 0)
            {
                if (frameIndex > 100)
                    yolo.StopVideoProcessing();
            }

            frame.Draw(result, _drawingOptions);

            progress = (int)((double)(frameIndex) / metadata.TargetTotalFrames * 100);
            var str = $"{progress}% [frame {frameIndex} of {metadata.TargetTotalFrames}]";

            Console.SetCursorPosition(progressStatsLength, textRow);
            Console.Write(new string(' ', str.Length));
            Console.SetCursorPosition(progressStatsLength, textRow);
            Console.Write(str);
        };
        yolo.OnVideoEnd = () =>
        {
            Console.WriteLine();
            Console.WriteLine();

            if (progress == 100)
            {
                Console.WriteLine("Video processing completed successfully.");
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Warning: Video processing did not complete successfully.");
            }

            Console.ForegroundColor = ConsoleColor.Gray;
        };
        await Task.Run(() =>yolo.StartVideoProcessing());
    }

    private void SetDrawingOptions()
    {
        _drawingOptions = new DetectionDrawingOptions
        {
            DrawBoundingBoxes = true,
            DrawConfidenceScore = true,
            DrawLabels = true,
            EnableFontShadow = true,
            Font = SKTypeface.Default,

            FontSize = 18,
            FontColor = SKColors.White,
            DrawLabelBackground = true,
            EnableDynamicScaling = true,
            BorderThickness = 2,

            BoundingBoxOpacity = 128,
            
            DrawTrackedTail = true,
            TailPaintColorStart = new SKColor(255, 105, 180),
            TailPaintColorEnd = SKColor.Empty.WithAlpha(0),  
            TailThickness = 4,
        };
    }
    private void CreateOutputFolder()
    {
        if (!_video.NeedOutput)
            return;

        if (Directory.Exists(_video.OutputPath) is false)
            Directory.CreateDirectory(_video.OutputPath);
    }
}