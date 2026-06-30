using System.Diagnostics;

namespace ConversorJ.Core;

public sealed class WhisperRunner : IWhisperRunner
{
    private readonly string executablePath;

    public WhisperRunner(string executablePath)
    {
        this.executablePath = string.IsNullOrWhiteSpace(executablePath) ? "whisper-cli" : executablePath;
    }

    public async Task<CommandResult> RunAsync(IEnumerable<string> arguments, CancellationToken cancellationToken)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = executablePath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
        };

        foreach (string argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        using var process = new Process { StartInfo = startInfo };

        try
        {
            process.Start();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            throw new ConversionException(
                "missing_whisper_binary",
                "Nao foi possivel iniciar o whisper-cli. Verifique se os binarios estao instalados.",
                ex);
        }

        Task<string> stdoutTask = process.StandardOutput.ReadToEndAsync(cancellationToken);
        Task<string> stderrTask = process.StandardError.ReadToEndAsync(cancellationToken);

        try
        {
            await process.WaitForExitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            KillProcess(process);
            throw;
        }

        string stdout = await stdoutTask;
        string stderr = await stderrTask;
        return new CommandResult(process.ExitCode, stdout, stderr);
    }

    private static void KillProcess(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }
}
