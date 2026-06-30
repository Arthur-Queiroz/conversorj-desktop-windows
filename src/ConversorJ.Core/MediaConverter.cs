namespace ConversorJ.Core;

public sealed class MediaConverter
{
    private readonly IYtDlpRunner runner;

    public MediaConverter(IYtDlpRunner runner)
    {
        this.runner = runner;
    }

    public static IReadOnlyList<string> BuildArgs(
        string url,
        OutputFormat format,
        string outputDirectory,
        VideoResolution videoResolution = VideoResolution.Best)
    {
        string[] baseArguments =
        [
            "--no-playlist",
            "--no-warnings",
            "--no-part",
            "-P",
            outputDirectory,
            "-o",
            "%(title).80B.%(ext)s",
        ];

        string[] formatArguments = format switch
        {
            OutputFormat.Mp3 => ["-x", "--audio-format", "mp3", "--audio-quality", "0"],
            OutputFormat.Mp4 =>
            [
                "-f",
                GetFormatSelector(videoResolution),
                "--merge-output-format",
                "mp4",
            ],
            _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Formato de saida desconhecido."),
        };

        return [.. formatArguments, .. baseArguments, url];
    }

    public async Task<ConversionResult> ConvertAsync(
        string url,
        OutputFormat format,
        VideoResolution videoResolution,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputDirectory);

        var startedAt = DateTimeOffset.UtcNow;
        IReadOnlyList<string> arguments = BuildArgs(url, format, outputDirectory, videoResolution);
        CommandResult result = await runner.RunAsync(arguments, cancellationToken);

        if (result.ExitCode != 0)
        {
            throw new ConversionException("conversion_failed", "Erro ao converter o video.");
        }

        FileInfo? outputFile = FindNewestOutputFile(outputDirectory, startedAt);
        if (outputFile is null)
        {
            throw new ConversionException("output_not_found", "Arquivo de saida nao encontrado apos a conversao.");
        }

        return new ConversionResult(outputFile.Name, outputFile.FullName);
    }

    private static FileInfo? FindNewestOutputFile(string outputDirectory, DateTimeOffset startedAt)
    {
        return new DirectoryInfo(outputDirectory)
            .EnumerateFiles()
            .Where(file => file.LastWriteTimeUtc >= startedAt.AddSeconds(-2).UtcDateTime)
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static string GetFormatSelector(VideoResolution videoResolution)
    {
        return videoResolution switch
        {
            VideoResolution.Best => "bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best",
            VideoResolution.P1080 => BuildMaxHeightSelector(1080),
            VideoResolution.P720 => BuildMaxHeightSelector(720),
            VideoResolution.P480 => BuildMaxHeightSelector(480),
            VideoResolution.P360 => BuildMaxHeightSelector(360),
            _ => throw new ArgumentOutOfRangeException(nameof(videoResolution), videoResolution, "Resolucao de video desconhecida."),
        };
    }

    private static string BuildMaxHeightSelector(int maxHeight)
    {
        return $"bestvideo[height<={maxHeight}][ext=mp4]+bestaudio[ext=m4a]/best[height<={maxHeight}][ext=mp4]/best[height<={maxHeight}]/best";
    }
}
