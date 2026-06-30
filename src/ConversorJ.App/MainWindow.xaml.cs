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
        var durationChecker = new DurationChecker(runner);
        var mediaConverter = new MediaConverter(runner);
        var conversionService = new ConversionService(durationChecker, mediaConverter);

        return new MainViewModel(conversionService);
    }
}
