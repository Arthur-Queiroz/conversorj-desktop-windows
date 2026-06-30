using ConversorJ.Core;

var tests = new (string Name, Func<Task> Run)[]
{
    ("UrlValidator accepts YouTube URLs", TestYouTubeUrls),
    ("UrlValidator accepts X URLs", TestXUrls),
    ("UrlValidator rejects cross-platform URLs", TestCrossPlatformUrls),
    ("MediaConverter builds MP3 arguments", TestMp3Arguments),
    ("MediaConverter builds MP4 arguments", TestMp4Arguments),
    ("MediaConverter builds MP4 resolution arguments", TestMp4ResolutionArguments),
    ("TranscriptionService builds audio arguments", TestTranscriptionAudioArguments),
    ("TranscriptionService builds whisper arguments", TestWhisperArguments),
    ("TranscriptionModels map tiers to ggml files", TestTranscriptionModelFiles),
    ("TranscriptionModels.FileName rejects unknown model", TestUnknownModelFile),
    ("TranscriptionModels.Installed lists present files in tier order", TestInstalledModels),
    ("MediaConverter builds all MP4 resolution selectors", TestMp4AllResolutions),
    ("DurationChecker reads duration", TestDuration),
    ("DurationChecker rejects long videos", TestDurationExceeded),
    ("DurationChecker allows exactly the limit", TestDurationAtLimit),
    ("DurationChecker fails on yt-dlp error exit", TestDurationExtractionFailed),
    ("DurationChecker fails on invalid metadata JSON", TestDurationParseError),
    ("DurationChecker fails when duration missing", TestDurationMissing),
    ("ConversionService validates before yt-dlp", TestServiceValidation),
    ("ConversionService routes TXT to transcription", TestServiceRoutesToTranscription),
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

static Task TestTranscriptionAudioArguments()
{
    IReadOnlyList<string> args = TranscriptionService.BuildAudioArgs("https://youtu.be/abc", "C:\\Temp");

    AssertSequence(
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
            "C:\\Temp",
            "-o",
            "%(title).80B.%(ext)s",
            "https://youtu.be/abc",
        ],
        args);

    return Task.CompletedTask;
}

static Task TestWhisperArguments()
{
    IReadOnlyList<string> args = TranscriptionService.BuildWhisperArgs(
        "C:\\App\\bin\\models\\ggml-base-q5_1.bin",
        "C:\\Temp\\audio.wav",
        "C:\\Temp\\audio");

    AssertSequence(
        [
            "-m",
            "C:\\App\\bin\\models\\ggml-base-q5_1.bin",
            "-f",
            "C:\\Temp\\audio.wav",
            "-otxt",
            "-of",
            "C:\\Temp\\audio",
            "-l",
            "auto",
        ],
        args);

    return Task.CompletedTask;
}

static Task TestTranscriptionModelFiles()
{
    AssertEqual("ggml-tiny-q5_1.bin", TranscriptionModels.FileName(TranscriptionModel.Tiny));
    AssertEqual("ggml-base-q5_1.bin", TranscriptionModels.FileName(TranscriptionModel.Base));
    AssertEqual("ggml-small-q5_1.bin", TranscriptionModels.FileName(TranscriptionModel.Small));
    AssertEqual("ggml-large-v3-turbo-q5_0.bin", TranscriptionModels.FileName(TranscriptionModel.Large));

    return Task.CompletedTask;
}

static Task TestUnknownModelFile()
{
    AssertThrows<ArgumentOutOfRangeException>(() => TranscriptionModels.FileName((TranscriptionModel)999));

    return Task.CompletedTask;
}

static Task TestInstalledModels()
{
    string withModels = CreateTempDir();
    string empty = CreateTempDir();
    try
    {
        // Only Base and Large present; Tiny and Small absent.
        File.WriteAllText(Path.Combine(withModels, TranscriptionModels.FileName(TranscriptionModel.Base)), "");
        File.WriteAllText(Path.Combine(withModels, TranscriptionModels.FileName(TranscriptionModel.Large)), "");

        IReadOnlyList<TranscriptionModel> installed = TranscriptionModels.Installed(withModels);

        // Order matters: the UI defaults to the last (best) installed model.
        AssertSequenceEqual([TranscriptionModel.Base, TranscriptionModel.Large], installed);
        AssertEqual(0, TranscriptionModels.Installed(empty).Count);
    }
    finally
    {
        DeleteTempDir(withModels);
        DeleteTempDir(empty);
    }

    return Task.CompletedTask;
}

static Task TestMp4AllResolutions()
{
    AssertEqual(
        "bestvideo[height<=1080][ext=mp4]+bestaudio[ext=m4a]/best[height<=1080][ext=mp4]/best[height<=1080]/best",
        Mp4SelectorFor(VideoResolution.P1080));
    AssertEqual(
        "bestvideo[height<=480][ext=mp4]+bestaudio[ext=m4a]/best[height<=480][ext=mp4]/best[height<=480]/best",
        Mp4SelectorFor(VideoResolution.P480));
    AssertEqual(
        "bestvideo[height<=360][ext=mp4]+bestaudio[ext=m4a]/best[height<=360][ext=mp4]/best[height<=360]/best",
        Mp4SelectorFor(VideoResolution.P360));

    return Task.CompletedTask;
}

static string Mp4SelectorFor(VideoResolution resolution)
{
    IReadOnlyList<string> args = MediaConverter.BuildArgs("https://youtu.be/abc", OutputFormat.Mp4, "C:\\Downloads", resolution);
    int index = args.ToList().IndexOf("-f");
    return args[index + 1];
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

static async Task TestDurationAtLimit()
{
    // Exactly at the 20-minute limit must pass; the check uses a strict greater-than.
    var runner = new FakeRunner(new CommandResult(0, """{"duration":1200}""", ""));
    var checker = new DurationChecker(runner);

    await checker.EnsureWithinLimitAsync("https://youtu.be/abc", CancellationToken.None);
}

static async Task TestDurationExtractionFailed()
{
    var runner = new FakeRunner(new CommandResult(1, "", "boom"));
    var checker = new DurationChecker(runner);

    ConversionException exception = await AssertThrowsAsync<ConversionException>(
        () => checker.GetDurationAsync("https://youtu.be/abc", CancellationToken.None));

    AssertEqual("extraction_failed", exception.Code);
}

static async Task TestDurationParseError()
{
    var runner = new FakeRunner(new CommandResult(0, "not json", ""));
    var checker = new DurationChecker(runner);

    ConversionException exception = await AssertThrowsAsync<ConversionException>(
        () => checker.GetDurationAsync("https://youtu.be/abc", CancellationToken.None));

    AssertEqual("parse_metadata", exception.Code);
}

static async Task TestDurationMissing()
{
    var runner = new FakeRunner(new CommandResult(0, "{}", ""));
    var checker = new DurationChecker(runner);

    ConversionException exception = await AssertThrowsAsync<ConversionException>(
        () => checker.GetDurationAsync("https://youtu.be/abc", CancellationToken.None));

    AssertEqual("extraction_failed", exception.Code);
}

static async Task TestServiceValidation()
{
    var runner = new FakeRunner(new CommandResult(0, """{"duration":60}""", ""));
    var service = new ConversionService(
        new DurationChecker(runner),
        new MediaConverter(runner),
        new TranscriptionService(runner, new FakeWhisperRunner(), "C:\\App\\bin\\models"));

    ConversionException exception = await AssertThrowsAsync<ConversionException>(
        () => service.ConvertAsync(
            Platform.YouTube,
            "https://x.com/user/status/123",
            OutputFormat.Mp3,
            VideoResolution.Best,
            TranscriptionModel.Base,
            "C:\\Downloads",
            CancellationToken.None));

    AssertEqual("invalid_url", exception.Code);
    AssertEqual(0, runner.Calls.Count);
}

static async Task TestServiceRoutesToTranscription()
{
    var runner = new FakeRunner(new CommandResult(0, """{"duration":60}""", ""));
    string emptyModels = CreateTempDir();
    string output = CreateTempDir();
    try
    {
        var service = new ConversionService(
            new DurationChecker(runner),
            new MediaConverter(runner),
            new TranscriptionService(runner, new FakeWhisperRunner(), emptyModels));

        // No model file present, so the TXT path bails with missing_whisper_model.
        // That can only happen if the service routed to the transcription branch.
        ConversionException exception = await AssertThrowsAsync<ConversionException>(
            () => service.ConvertAsync(
                Platform.YouTube,
                "https://youtu.be/abc",
                OutputFormat.Txt,
                VideoResolution.Best,
                TranscriptionModel.Base,
                output,
                CancellationToken.None));

        AssertEqual("missing_whisper_model", exception.Code);

        // Only the duration check ran; transcription bailed before invoking yt-dlp for audio.
        AssertEqual(1, runner.Calls.Count);
    }
    finally
    {
        DeleteTempDir(emptyModels);
        DeleteTempDir(output);
    }
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
    AssertSequenceEqual(expected, actual);
}

static void AssertSequenceEqual<T>(IReadOnlyList<T> expected, IReadOnlyList<T> actual)
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

static TException AssertThrows<TException>(Action action)
    where TException : Exception
{
    try
    {
        action();
    }
    catch (TException exception)
    {
        return exception;
    }

    throw new InvalidOperationException($"expected exception {typeof(TException).Name}");
}

static string CreateTempDir()
{
    string directory = Path.Combine(Path.GetTempPath(), "ConversorJ.Tests", Guid.NewGuid().ToString("N"));
    Directory.CreateDirectory(directory);
    return directory;
}

static void DeleteTempDir(string directory)
{
    if (Directory.Exists(directory))
    {
        Directory.Delete(directory, recursive: true);
    }
}

sealed class FakeWhisperRunner : IWhisperRunner
{
    public Task<CommandResult> RunAsync(IEnumerable<string> arguments, CancellationToken cancellationToken)
    {
        return Task.FromResult(new CommandResult(0, "", ""));
    }
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
