using System.IO;

namespace ConversorJ.App;

public sealed record BinaryLocation(
    string YtDlpPath,
    string? FfmpegDirectory,
    string WhisperPath,
    string WhisperModelsDirectory);

public static class BinaryLocator
{
    public static BinaryLocation Locate()
    {
        string binDirectory = Path.Combine(AppContext.BaseDirectory, "bin");
        string bundledYtDlp = Path.Combine(binDirectory, "yt-dlp.exe");
        string bundledFfmpeg = Path.Combine(binDirectory, "ffmpeg.exe");
        string bundledWhisper = Path.Combine(binDirectory, "whisper-cli.exe");
        string whisperModelsDirectory = Path.Combine(binDirectory, "models");

        string ytDlpPath = File.Exists(bundledYtDlp) ? bundledYtDlp : "yt-dlp";
        string? ffmpegDirectory = File.Exists(bundledFfmpeg) ? binDirectory : null;
        string whisperPath = File.Exists(bundledWhisper) ? bundledWhisper : "whisper-cli";

        return new BinaryLocation(ytDlpPath, ffmpegDirectory, whisperPath, whisperModelsDirectory);
    }
}
