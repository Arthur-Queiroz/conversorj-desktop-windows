using System.Text.RegularExpressions;

namespace ConversorJ.Core;

public static class UrlValidator
{
    private static readonly Regex[] YouTubePatterns =
    [
        new(@"^https?://(www\.)?youtube\.com/watch\?.*v=[\w-]+", RegexOptions.IgnoreCase),
        new(@"^https?://youtu\.be/[\w-]+", RegexOptions.IgnoreCase),
        new(@"^https?://(www\.)?youtube\.com/shorts/[\w-]+", RegexOptions.IgnoreCase),
    ];

    private static readonly Regex[] XPatterns =
    [
        new(@"^https?://(www\.)?(x\.com|twitter\.com)/\w+/status/\d+", RegexOptions.IgnoreCase),
    ];

    public static bool IsValid(Platform platform, string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        Regex[] patterns = platform switch
        {
            Platform.YouTube => YouTubePatterns,
            Platform.X => XPatterns,
            _ => [],
        };

        return patterns.Any(pattern => pattern.IsMatch(url));
    }
}
