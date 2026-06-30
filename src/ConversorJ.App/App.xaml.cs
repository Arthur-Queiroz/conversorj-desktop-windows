using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;

namespace ConversorJ.App;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
        DispatcherUnhandledException += OnDispatcherUnhandledException;

        try
        {
            var window = new MainWindow();
            MainWindow = window;
            window.Show();
        }
        catch (Exception ex)
        {
            ReportFatalError(ex);
            Shutdown(1);
        }
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        if (e.ExceptionObject is Exception ex)
        {
            ReportFatalError(ex);
        }
    }

    private static void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        ReportFatalError(e.Exception);
        e.Handled = true;
        System.Windows.Application.Current.Shutdown(1);
    }

    private static void ReportFatalError(Exception exception)
    {
        string logPath = WriteCrashLog(exception);
        System.Windows.MessageBox.Show(
            $"O ConversorJ encontrou um erro ao iniciar.\n\nLog: {logPath}",
            "ConversorJ",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
    }

    private static string WriteCrashLog(Exception exception)
    {
        string logDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ConversorJ",
            "logs");
        Directory.CreateDirectory(logDirectory);

        string logPath = Path.Combine(logDirectory, "startup-error.log");
        var content = new StringBuilder()
            .AppendLine(DateTimeOffset.Now.ToString("O"))
            .AppendLine(exception.ToString())
            .ToString();

        File.WriteAllText(logPath, content);
        return logPath;
    }
}
