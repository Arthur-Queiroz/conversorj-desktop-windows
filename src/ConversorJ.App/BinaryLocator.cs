using System.IO;

namespace ConversorJ.App;

public sealed record BinaryLocation(string YtDlpPath, string? FfmpegDirectory);

public static class BinaryLocator
{
    public static BinaryLocation Locate()
    {
        string binDirectory = Path.Combine(AppContext.BaseDirectory, "bin");
        string bundledYtDlp = Path.Combine(binDirectory, "yt-dlp.exe");
        string bundledFfmpeg = Path.Combine(binDirectory, "ffmpeg.exe");

        string ytDlpPath = File.Exists(bundledYtDlp) ? bundledYtDlp : "yt-dlp";
        string? ffmpegDirectory = File.Exists(bundledFfmpeg) ? binDirectory : null;

        return new BinaryLocation(ytDlpPath, ffmpegDirectory);
    }
}
