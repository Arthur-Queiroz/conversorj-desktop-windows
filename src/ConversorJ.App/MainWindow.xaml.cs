using System.Windows;
using ConversorJ.App.ViewModels;
using ConversorJ.Core;

namespace ConversorJ.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = CreateViewModel();
    }

    private static MainViewModel CreateViewModel()
    {
        BinaryLocation binaries = BinaryLocator.Locate();
        var runner = new YtDlpRunner(binaries.YtDlpPath, binaries.FfmpegDirectory);
        var whisperRunner = new WhisperRunner(binaries.WhisperPath);
        var durationChecker = new DurationChecker(runner);
        var mediaConverter = new MediaConverter(runner);
        var transcriptionService = new TranscriptionService(runner, whisperRunner, binaries.WhisperModelsDirectory);
        var conversionService = new ConversionService(durationChecker, mediaConverter, transcriptionService);

        IReadOnlyList<TranscriptionModel> installedModels = TranscriptionModels.Installed(binaries.WhisperModelsDirectory);

        return new MainViewModel(conversionService, installedModels);
    }
}
