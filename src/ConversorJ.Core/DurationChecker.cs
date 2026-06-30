using System.Text.Json;
using System.Text.Json.Serialization;

namespace ConversorJ.Core;

public sealed class DurationChecker
{
    public const int MaxDurationSeconds = 20 * 60;

    private readonly IYtDlpRunner runner;

    public DurationChecker(IYtDlpRunner runner)
    {
        this.runner = runner;
    }

    public async Task<double> GetDurationAsync(string url, CancellationToken cancellationToken)
    {
        string[] arguments =
        [
            "--dump-json",
            "--no-playlist",
            "--no-warnings",
            "--skip-download",
            url,
        ];

        CommandResult result = await runner.RunAsync(arguments, cancellationToken);
        if (result.ExitCode != 0)
        {
            throw new ConversionException("extraction_failed", "Nao foi possivel obter informacoes do video.");
        }

        try
        {
            Metadata? metadata = JsonSerializer.Deserialize<Metadata>(result.StandardOutput);
            if (metadata?.Duration is null)
            {
                throw new ConversionException("extraction_failed", "Nao foi possivel obter a duracao do video.");
            }

            return metadata.Duration.Value;
        }
        catch (JsonException ex)
        {
            throw new ConversionException("parse_metadata", "Nao foi possivel ler os metadados do video.", ex);
        }
    }

    public async Task EnsureWithinLimitAsync(string url, CancellationToken cancellationToken)
    {
        double duration = await GetDurationAsync(url, cancellationToken);
        if (duration > MaxDurationSeconds)
        {
            throw new ConversionException(
                "duration_exceeded",
                "Video muito longo. Mais de 20 minutos. Tente um trecho mais curto.");
        }
    }

    private sealed record Metadata([property: JsonPropertyName("duration")] double? Duration);
}
