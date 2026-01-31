namespace ObjectDetection.Model;

public class Video
{
    public required string Input { get; set; }
    public string OutputPath { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
    public string OutputFileName { get; set; } = "video_output.mp4";
    public bool NeedOutput { get; set; } = false;
}