namespace ConversorJ.Core;

public sealed class ConversionService
{
    private readonly DurationChecker durationChecker;
    private readonly MediaConverter mediaConverter;

    public ConversionService(DurationChecker durationChecker, MediaConverter mediaConverter)
    {
        this.durationChecker = durationChecker;
        this.mediaConverter = mediaConverter;
    }

    public async Task<ConversionResult> ConvertAsync(
        Platform platform,
        string url,
        OutputFormat format,
        VideoResolution videoResolution,
        string outputDirectory,
        CancellationToken cancellationToken)
    {
        if (!UrlValidator.IsValid(platform, url))
        {
            throw new ConversionException("invalid_url", $"O link informado nao e valido para a plataforma {GetPlatformName(platform)}.");
        }

        await durationChecker.EnsureWithinLimitAsync(url, cancellationToken);
        return await mediaConverter.ConvertAsync(url, format, videoResolution, outputDirectory, cancellationToken);
    }

    private static string GetPlatformName(Platform platform)
    {
        return platform switch
        {
            Platform.YouTube => "YouTube",
            Platform.X => "X",
            _ => "selecionada",
        };
    }
}
