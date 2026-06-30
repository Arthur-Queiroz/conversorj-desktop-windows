namespace ConversorJ.Core;

public sealed class TranscriptionService
{
    private const string WavPattern = "*.wav";

    private readonly IYtDlpRunner ytDlpRunner;
    private readonly IWhisperRunner whisperRunner;
    private readonly string modelsDirectory;

    public TranscriptionService(IYtDlpRunner ytDlpRunner, IWhisperRunner whisperRunner, string modelsDirectory)
    {
        this.ytDlpRunner = ytDlpRunner;
        this.whisperRunner = whisperRunner;
        this.modelsDirectory = modelsDirectory;
    }

    public static IReadOnlyList<string> BuildAudioArgs(string url, string outputDirectory)
    {
        return
        [
            "-x",
            "--audio-format",
            "wav",
            "--postprocessor-args",
            "ffmpeg:-ac 1 -ar 16000",
            "--no-playlist",
            "--no-warnings",
            "--no-part",
            "-P",
            outputDirectory,
            "-o",
            "%(title).80B.%(ext)s",
            url,
        ];
    }

    public static IReadOnlyList<string> BuildWhisperArgs(string modelPath, string wavPath, string outputBasePath)
    {
        return
        [
            "-m",
            modelPath,
            "-f",
            wavPath,
            "-otxt",
            "-of",
            outputBasePath,
            "-l",
            "auto",
        ];
    }

    public async Task<ConversionResult> TranscribeAsync(
        string url,
        TranscriptionModel model,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        string modelFileName = TranscriptionModels.FileName(model);
        string modelPath = Path.Combine(modelsDirectory, modelFileName);
        if (!File.Exists(modelPath))
        {
            throw new ConversionException(
                "missing_whisper_model",
                $"Modelo do Whisper nao encontrado. Coloque {modelFileName} em bin\\models.");
        }

        Directory.CreateDirectory(outputDirectory);

        string tempDirectory = Path.Combine(Path.GetTempPath(), "ConversorJ", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDirectory);

        try
        {
            CommandResult audioResult = await ytDlpRunner.RunAsync(BuildAudioArgs(url, tempDirectory), cancellationToken);
            if (audioResult.ExitCode != 0)
            {
                throw new ConversionException(
                    "transcription_audio_failed",
                    BuildCommandError("Erro ao preparar o audio para transcricao.", audioResult));
            }

            FileInfo wavFile = FindNewestFile(tempDirectory, WavPattern)
                ?? throw new ConversionException("transcription_audio_not_found", "Audio de transcricao nao encontrado.");

            string finalFilename = Path.ChangeExtension(wavFile.Name, ".txt");
            string whisperInputPath = Path.Combine(tempDirectory, "input.wav");
            File.Copy(wavFile.FullName, whisperInputPath, overwrite: true);

            string outputBasePath = Path.Combine(tempDirectory, "transcription");
            CommandResult transcriptionResult = await whisperRunner.RunAsync(
                BuildWhisperArgs(modelPath, whisperInputPath, outputBasePath),
                cancellationToken);

            if (transcriptionResult.ExitCode != 0)
            {
                throw new ConversionException(
                    "transcription_failed",
                    BuildCommandError("Erro ao transcrever o audio com whisper.cpp.", transcriptionResult));
            }

            string generatedTxtPath = outputBasePath + ".txt";
            if (!File.Exists(generatedTxtPath))
            {
                if (string.IsNullOrWhiteSpace(transcriptionResult.StandardOutput))
                {
                    throw new ConversionException("transcription_output_not_found", "Arquivo de transcricao nao encontrado.");
                }

                File.WriteAllText(generatedTxtPath, transcriptionResult.StandardOutput);
            }

            string destinationPath = GetAvailablePath(outputDirectory, finalFilename);
            File.Move(generatedTxtPath, destinationPath);

            return new ConversionResult(Path.GetFileName(destinationPath), destinationPath);
        }
        finally
        {
            DeleteDirectory(tempDirectory);
        }
    }


    private static string BuildCommandError(string message, CommandResult result)
    {
        string details = string.IsNullOrWhiteSpace(result.StandardError)
            ? result.StandardOutput
            : result.StandardError;

        if (string.IsNullOrWhiteSpace(details))
        {
            return message;
        }

        details = details.Trim();
        if (details.Length > 500)
        {
            details = details[..500] + "...";
        }

        return $"{message} Detalhe: {details}";
    }

    private static FileInfo? FindNewestFile(string directory, string pattern)
    {
        return new DirectoryInfo(directory)
            .EnumerateFiles(pattern)
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .FirstOrDefault();
    }

    private static string GetAvailablePath(string directory, string filename)
    {
        string path = Path.Combine(directory, filename);
        if (!File.Exists(path))
        {
            return path;
        }

        string name = Path.GetFileNameWithoutExtension(filename);
        string extension = Path.GetExtension(filename);

        for (int index = 1; ; index++)
        {
            string candidate = Path.Combine(directory, $"{name} ({index}){extension}");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }

    private static void DeleteDirectory(string directory)
    {
        try
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
