namespace ConversorJ.Core;

public enum TranscriptionModel
{
    Tiny,
    Base,
    Small,
    Large,
}

public static class TranscriptionModels
{
    public static string FileName(TranscriptionModel model)
    {
        return model switch
        {
            TranscriptionModel.Tiny => "ggml-tiny-q5_1.bin",
            TranscriptionModel.Base => "ggml-base-q5_1.bin",
            TranscriptionModel.Small => "ggml-small-q5_1.bin",
            TranscriptionModel.Large => "ggml-large-v3-turbo-q5_0.bin",
            _ => throw new ArgumentOutOfRangeException(nameof(model), model, "Modelo de transcricao desconhecido."),
        };
    }

    // Lists the models whose .bin file is present, so the UI only offers what can actually run.
    public static IReadOnlyList<TranscriptionModel> Installed(string modelsDirectory)
    {
        return Enum.GetValues<TranscriptionModel>()
            .Where(model => File.Exists(Path.Combine(modelsDirectory, FileName(model))))
            .ToList();
    }
}
