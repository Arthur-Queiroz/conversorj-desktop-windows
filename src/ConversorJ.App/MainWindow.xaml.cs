using System.IO;
using System.Windows;
using ConversorJ.App.ViewModels;
using ConversorJ.Core;

namespace ConversorJ.App;

public partial class MainWindow : Window
{
    private const string DarkThemePreference = "dark";
    private const string LightThemePreference = "light";

    public MainWindow()
    {
        InitializeComponent();
        DataContext = CreateViewModel();

        bool isDarkMode = LoadThemePreference();
        ThemeToggle.IsChecked = isDarkMode;
        ApplyTheme(isDarkMode);
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

    private void ThemeToggleChanged(object sender, RoutedEventArgs e)
    {
        bool isDarkMode = ThemeToggle.IsChecked == true;
        ApplyTheme(isDarkMode);
        SaveThemePreference(isDarkMode);
    }

    private void ApplyTheme(bool isDarkMode)
    {
        ThemeToggleText.Text = isDarkMode ? "Modo escuro" : "Modo claro";

        if (isDarkMode)
        {
            SetThemeBrush("AppBackgroundBrush", "#08090D");
            SetThemeBrush("CardBackgroundBrush", "#18191F");
            SetThemeBrush("PanelBackgroundBrush", "#1F2028");
            SetThemeBrush("NestedPanelBackgroundBrush", "#111218");
            SetThemeBrush("InputBackgroundBrush", "#0D0E13");
            SetThemeBrush("PrimaryTextBrush", "#F5F3FF");
            SetThemeBrush("SecondaryTextBrush", "#A5A1B8");
            SetThemeBrush("LabelTextBrush", "#D9D6E8");
            SetThemeBrush("CardBorderBrush", "#2B2C36");
            SetThemeBrush("PanelBorderBrush", "#30313C");
            SetThemeBrush("SubtleBorderBrush", "#393A46");
            SetThemeBrush("ButtonBackgroundBrush", "#242630");
            SetThemeBrush("ButtonBorderBrush", "#3A3C48");
            SetThemeBrush("PrimaryButtonBackgroundBrush", "#8B5CF6");
            SetThemeBrush("PrimaryButtonForegroundBrush", "#FFFFFF");
            SetThemeBrush("ToggleTrackBrush", "#30313C");
            SetThemeBrush("ToggleThumbBrush", "#F5F3FF");
            SetThemeBrush("AccentBrush", "#8B5CF6");
            return;
        }

        SetThemeBrush("AppBackgroundBrush", "#FFFFFF");
        SetThemeBrush("CardBackgroundBrush", "#FFFFFF");
        SetThemeBrush("PanelBackgroundBrush", "#FAFAFD");
        SetThemeBrush("NestedPanelBackgroundBrush", "#FFFFFF");
        SetThemeBrush("InputBackgroundBrush", "#FFFFFF");
        SetThemeBrush("PrimaryTextBrush", "#252333");
        SetThemeBrush("SecondaryTextBrush", "#908AA3");
        SetThemeBrush("LabelTextBrush", "#363247");
        SetThemeBrush("CardBorderBrush", "#E4E0EC");
        SetThemeBrush("PanelBorderBrush", "#ECE8F3");
        SetThemeBrush("SubtleBorderBrush", "#E6E1EF");
        SetThemeBrush("ButtonBackgroundBrush", "#F4F1FA");
        SetThemeBrush("ButtonBorderBrush", "#D8D0E7");
        SetThemeBrush("PrimaryButtonBackgroundBrush", "#8B5CF6");
        SetThemeBrush("PrimaryButtonForegroundBrush", "#FFFFFF");
        SetThemeBrush("ToggleTrackBrush", "#E7E1F2");
        SetThemeBrush("ToggleThumbBrush", "#FFFFFF");
        SetThemeBrush("AccentBrush", "#8B5CF6");
    }

    private void SetThemeBrush(string resourceKey, string color)
    {
        Resources[resourceKey] = new System.Windows.Media.SolidColorBrush(
            (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(color));
    }

    private static bool LoadThemePreference()
    {
        try
        {
            return File.Exists(GetThemePreferencePath()) &&
                File.ReadAllText(GetThemePreferencePath()).Trim() == DarkThemePreference;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void SaveThemePreference(bool isDarkMode)
    {
        try
        {
            string path = GetThemePreferencePath();
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, isDarkMode ? DarkThemePreference : LightThemePreference);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private static string GetThemePreferencePath()
    {
        string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        return Path.Combine(appData, "ConversorJ", "theme.txt");
    }
}
