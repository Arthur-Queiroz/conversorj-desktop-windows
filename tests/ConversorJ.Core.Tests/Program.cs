using ConversorJ.Core;

var tests = new (string Name, Func<Task> Run)[]
{
    ("UrlValidator accepts YouTube URLs", TestYouTubeUrls),
    ("UrlValidator accepts X URLs", TestXUrls),
    ("UrlValidator rejects cross-platform URLs", TestCrossPlatformUrls),
    ("MediaConverter builds MP3 arguments", TestMp3Arguments),
    ("MediaConverter builds MP4 arguments", TestMp4Arguments),
    ("MediaConverter builds MP4 resolution arguments", TestMp4ResolutionArguments),
    ("DurationChecker reads duration", TestDuration),
    ("DurationChecker rejects long videos", TestDurationExceeded),
    ("ConversionService validates before yt-dlp", TestServiceValidation),
};

foreach ((string name, Func<Task> run) in tests)
{
    await run();
    Console.WriteLine($"ok - {name}");
}

Console.WriteLine($"{tests.Length} tests passed.");

static Task TestYouTubeUrls()
{
    string[] valid =
    [
        "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
        "https://youtube.com/watch?v=dQw4w9WgXcQ",
        "https://youtu.be/dQw4w9WgXcQ",
        "https://www.youtube.com/shorts/abc123def",
        "https://youtube.com/watch?v=abc&t=10s",
    ];

    foreach (string url in valid)
    {
        AssertTrue(UrlValidator.IsValid(Platform.YouTube, url), $"expected valid YouTube URL: {url}");
    }

    return Task.CompletedTask;
}

static Task TestXUrls()
{
    string[] valid =
    [
        "https://x.com/natgeo/status/1789452200145",
        "https://twitter.com/user/status/9876543210",
        "https://www.x.com/user/status/1234567890",
    ];

    foreach (string url in valid)
    {
        AssertTrue(UrlValidator.IsValid(Platform.X, url), $"expected valid X URL: {url}");
    }

    return Task.CompletedTask;
}

static Task TestCrossPlatformUrls()
{
    string[] invalidYouTube =
    [
        "https://vimeo.com/123456",
        "https://youtube.com/channel/UCabc",
        "https://youtube.com/playlist?list=PLabc",
        "not-a-url",
        "",
        "https://x.com/user/status/123",
    ];

    foreach (string url in invalidYouTube)
    {
        AssertFalse(UrlValidator.IsValid(Platform.YouTube, url), $"expected invalid YouTube URL: {url}");
    }

    string[] invalidX =
    [
        "https://x.com/user",
        "https://twitter.com/user",
        "https://youtube.com/watch?v=abc",
        "",
    ];

    foreach (string url in invalidX)
    {
        AssertFalse(UrlValidator.IsValid(Platform.X, url), $"expected invalid X URL: {url}");
    }

    return Task.CompletedTask;
}

static Task TestMp3Arguments()
{
    IReadOnlyList<string> args = MediaConverter.BuildArgs("https://youtu.be/abc", OutputFormat.Mp3, "C:\\Downloads");

    AssertSequence(
        [
            "-x",
            "--audio-format",
            "mp3",
            "--audio-quality",
            "0",
            "--no-playlist",
            "--no-warnings",
            "--no-part",
            "-P",
            "C:\\Downloads",
            "-o",
            "%(title).80B.%(ext)s",
            "https://youtu.be/abc",
        ],
        args);

    return Task.CompletedTask;
}

static Task TestMp4Arguments()
{
    IReadOnlyList<string> args = MediaConverter.BuildArgs("https://youtu.be/abc", OutputFormat.Mp4, "C:\\Downloads");

    AssertSequence(
        [
            "-f",
            "bestvideo[ext=mp4]+bestaudio[ext=m4a]/best[ext=mp4]/best",
            "--merge-output-format",
            "mp4",
            "--no-playlist",
            "--no-warnings",
            "--no-part",
            "-P",
            "C:\\Downloads",
            "-o",
            "%(title).80B.%(ext)s",
            "https://youtu.be/abc",
        ],
        args);

    return Task.CompletedTask;
}

static Task TestMp4ResolutionArguments()
{
    IReadOnlyList<string> args = MediaConverter.BuildArgs(
        "https://youtu.be/abc",
        OutputFormat.Mp4,
        "C:\\Downloads",
        VideoResolution.P720);

    AssertSequence(
        [
            "-f",
            "bestvideo[height<=720][ext=mp4]+bestaudio[ext=m4a]/best[height<=720][ext=mp4]/best[height<=720]/best",
            "--merge-output-format",
            "mp4",
            "--no-playlist",
            "--no-warnings",
            "--no-part",
            "-P",
            "C:\\Downloads",
            "-o",
            "%(title).80B.%(ext)s",
            "https://youtu.be/abc",
        ],
        args);

    return Task.CompletedTask;
}

static async Task TestDuration()
{
    var runner = new FakeRunner(new CommandResult(0, """{"duration":1199.5}""", ""));
    var checker = new DurationChecker(runner);

    double duration = await checker.GetDurationAsync("https://youtu.be/abc", CancellationToken.None);

    AssertEqual(1199.5, duration);
    AssertSequence(
        [
            "--dump-json",
            "--no-playlist",
            "--no-warnings",
            "--skip-download",
            "https://youtu.be/abc",
        ],
        runner.Calls.Single());
}

static async Task TestDurationExceeded()
{
    var runner = new FakeRunner(new CommandResult(0, """{"duration":1201}""", ""));
    var checker = new DurationChecker(runner);

    ConversionException exception = await AssertThrowsAsync<ConversionException>(
        () => checker.EnsureWithinLimitAsync("https://youtu.be/abc", CancellationToken.None));

    AssertEqual("duration_exceeded", exception.Code);
}

static async Task TestServiceValidation()
{
    var runner = new FakeRunner(new CommandResult(0, """{"duration":60}""", ""));
    var service = new ConversionService(new DurationChecker(runner), new MediaConverter(runner));

    ConversionException exception = await AssertThrowsAsync<ConversionException>(
        () => service.ConvertAsync(
            Platform.YouTube,
            "https://x.com/user/status/123",
            OutputFormat.Mp3,
            VideoResolution.Best,
            "C:\\Downloads",
            CancellationToken.None));

    AssertEqual("invalid_url", exception.Code);
    AssertEqual(0, runner.Calls.Count);
}

static void AssertTrue(bool condition, string message)
{
    if (!condition)
    {
        throw new InvalidOperationException(message);
    }
}

static void AssertFalse(bool condition, string message)
{
    AssertTrue(!condition, message);
}

static void AssertEqual<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
    {
        throw new InvalidOperationException($"expected '{expected}', got '{actual}'");
    }
}

static void AssertSequence(IReadOnlyList<string> expected, IReadOnlyList<string> actual)
{
    AssertEqual(expected.Count, actual.Count);

    for (int i = 0; i < expected.Count; i++)
    {
        AssertEqual(expected[i], actual[i]);
    }
}

static async Task<TException> AssertThrowsAsync<TException>(Func<Task> action)
    where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException exception)
    {
        return exception;
    }

    throw new InvalidOperationException($"expected exception {typeof(TException).Name}");
}

sealed class FakeRunner : IYtDlpRunner
{
    private readonly Queue<CommandResult> results;

    public FakeRunner(params CommandResult[] results)
    {
        this.results = new Queue<CommandResult>(results);
    }

    public List<IReadOnlyList<string>> Calls { get; } = [];

    public Task<CommandResult> RunAsync(IEnumerable<string> arguments, CancellationToken cancellationToken)
    {
        Calls.Add(arguments.ToArray());
        return Task.FromResult(results.Dequeue());
    }
}
