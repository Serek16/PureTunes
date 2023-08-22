namespace EmySoundProject.Models;

public class PathModel
{
    public string Path { get; }

    public string FileName { get; }

    public PathModel(string path)
    {
        Path = path;

        var lastSlashIndex = path.LastIndexOf('\\');
        FileName = path.Substring(lastSlashIndex + 1);
    }
}