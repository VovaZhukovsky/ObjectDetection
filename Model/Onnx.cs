namespace ObjectDetection.Model;

public class Onnx
{
    public required string ModelPath { get; set; }
    public string[] ObjectsForSearch { get; set; } = [];
}