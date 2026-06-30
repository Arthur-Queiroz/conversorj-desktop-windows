using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using ConversorJ.Core;
using Forms = System.Windows.Forms;

namespace ConversorJ.App.ViewModels;

public sealed class MainViewModel : INotifyPropertyChanged
{
    private static readonly TimeSpan ConversionTimeout = TimeSpan.FromMinutes(10);
    private static readonly TimeSpan TranscriptionTimeout = TimeSpan.FromMinutes(30);

    private readonly ConversionService conversionService;
    private Platform selectedPlatform = Platform.YouTube;
    private OutputFormat selectedFormat = OutputFormat.Mp3;
    private VideoResolution selectedVideoResolution = VideoResolution.Best;
    private TranscriptionModel selectedTranscriptionModel = TranscriptionModel.Base;
    private string url = string.Empty;
    private string outputDirectory;
    private bool isBusy;
    private ConversionResult? result;
    private string statusTitle = "Pronto para converter";
    private string statusMessage = "Cole o link, escolha o formato e inicie a conversao.";

    public MainViewModel(ConversionService conversionService, IReadOnlyList<TranscriptionModel> availableTranscriptionModels)
    {
        this.conversionService = conversionService;
        outputDirectory = GetDefaultOutputDirectory();
        OutputFilename = "Nenhum arquivo gerado ainda.";
        VideoResolutionChoices =
        [
            new VideoResolutionChoice("Melhor disponivel", VideoResolution.Best),
            new VideoResolutionChoice("1080p", VideoResolution.P1080),
            new VideoResolutionChoice("720p", VideoResolution.P720),
            new VideoResolutionChoice("480p", VideoResolution.P480),
            new VideoResolutionChoice("360p", VideoResolution.P360),
        ];

        TranscriptionModelChoices = availableTranscriptionModels
            .Select(model => new TranscriptionModelChoice(GetTranscriptionModelLabel(model), model))
            .ToList();

        // Default to the most capable model that is actually installed.
        if (TranscriptionModelChoices.Count > 0)
        {
            selectedTranscriptionModel = TranscriptionModelChoices[^1].Value;
        }

        ConvertCommand = new AsyncRelayCommand(ConvertAsync, () => !IsBusy);
        ChooseOutputDirectoryCommand = new RelayCommand(ChooseOutputDirectory, () => !IsBusy);
        OpenFileCommand = new RelayCommand(OpenFile, () => Result is not null);
        OpenFolderCommand = new RelayCommand(OpenFolder, () => Directory.Exists(OutputDirectory));
        NewConversionCommand = new RelayCommand(Reset, () => !IsBusy);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public AsyncRelayCommand ConvertCommand { get; }

    public RelayCommand ChooseOutputDirectoryCommand { get; }

    public RelayCommand OpenFileCommand { get; }

    public RelayCommand OpenFolderCommand { get; }

    public RelayCommand NewConversionCommand { get; }

    public IReadOnlyList<VideoResolutionChoice> VideoResolutionChoices { get; }

    public IReadOnlyList<TranscriptionModelChoice> TranscriptionModelChoices { get; }

    public bool IsYouTube
    {
        get => SelectedPlatform == Platform.YouTube;
        set
        {
            if (value)
            {
                SelectedPlatform = Platform.YouTube;
            }
        }
    }

    public bool IsX
    {
        get => SelectedPlatform == Platform.X;
        set
        {
            if (value)
            {
                SelectedPlatform = Platform.X;
            }
        }
    }

    public bool IsMp3
    {
        get => SelectedFormat == OutputFormat.Mp3;
        set
        {
            if (value)
            {
                SelectedFormat = OutputFormat.Mp3;
            }
        }
    }

    public bool IsMp4
    {
        get => SelectedFormat == OutputFormat.Mp4;
        set
        {
            if (value)
            {
                SelectedFormat = OutputFormat.Mp4;
            }
        }
    }

    public bool IsTxt
    {
        get => SelectedFormat == OutputFormat.Txt;
        set
        {
            if (value)
            {
                SelectedFormat = OutputFormat.Txt;
            }
        }
    }

    public string Url
    {
        get => url;
        set => SetField(ref url, value);
    }

    public string OutputDirectory
    {
        get => outputDirectory;
        private set
        {
            if (SetField(ref outputDirectory, value))
            {
                OpenFolderCommand.RaiseCanExecuteChanged();
            }
        }
    }

    public string OutputFilename { get; private set; }

    public VideoResolution SelectedVideoResolution
    {
        get => selectedVideoResolution;
        set => SetField(ref selectedVideoResolution, value);
    }

    public TranscriptionModel SelectedTranscriptionModel
    {
        get => selectedTranscriptionModel;
        set => SetField(ref selectedTranscriptionModel, value);
    }

    public string StatusTitle
    {
        get => statusTitle;
        private set => SetField(ref statusTitle, value);
    }

    public string StatusMessage
    {
        get => statusMessage;
        private set => SetField(ref statusMessage, value);
    }

    public bool IsBusy
    {
        get => isBusy;
        private set
        {
            if (SetField(ref isBusy, value))
            {
                OnPropertyChanged(nameof(CanEdit));
                OnPropertyChanged(nameof(ProgressVisibility));
                OnPropertyChanged(nameof(ConvertButtonText));
                RaiseCommandStates();
            }
        }
    }

    public bool CanEdit => !IsBusy;

    public Visibility VideoResolutionVisibility =>
        SelectedFormat == OutputFormat.Mp4 ? Visibility.Visible : Visibility.Collapsed;

    public Visibility TranscriptionModelVisibility =>
        SelectedFormat == OutputFormat.Txt ? Visibility.Visible : Visibility.Collapsed;

    public Visibility ProgressVisibility => IsBusy ? Visibility.Visible : Visibility.Collapsed;

    public string ConvertButtonText => IsBusy ? GetBusyText() : GetActionText();

    private Platform SelectedPlatform
    {
        get => selectedPlatform;
        set
        {
            if (selectedPlatform == value)
            {
                return;
            }

            selectedPlatform = value;
            OnPropertyChanged(nameof(IsYouTube));
            OnPropertyChanged(nameof(IsX));
        }
    }

    private OutputFormat SelectedFormat
    {
        get => selectedFormat;
        set
        {
            if (selectedFormat == value)
            {
                return;
            }

            selectedFormat = value;
            OnPropertyChanged(nameof(IsMp3));
            OnPropertyChanged(nameof(IsMp4));
            OnPropertyChanged(nameof(IsTxt));
            OnPropertyChanged(nameof(VideoResolutionVisibility));
            OnPropertyChanged(nameof(TranscriptionModelVisibility));
            OnPropertyChanged(nameof(ConvertButtonText));
        }
    }

    private ConversionResult? Result
    {
        get => result;
        set
        {
            result = value;
            RaiseCommandStates();
        }
    }

    private async Task ConvertAsync()
    {
        if (string.IsNullOrWhiteSpace(Url))
        {
            ShowError("Link obrigatorio", "Informe um link do YouTube ou X antes de converter.");
            return;
        }

        IsBusy = true;
        Result = null;
        SetOutputFilename("Nenhum arquivo gerado ainda.");
        StatusTitle = GetBusyTitle();
        StatusMessage = GetBusyMessage();

        try
        {
            using var timeout = new CancellationTokenSource(GetTimeout());
            ConversionResult conversionResult = await conversionService.ConvertAsync(
                SelectedPlatform,
                Url.Trim(),
                SelectedFormat,
                SelectedVideoResolution,
                SelectedTranscriptionModel,
                OutputDirectory,
                timeout.Token);

            Result = conversionResult;
            SetOutputFilename(conversionResult.Filename);
            StatusTitle = SelectedFormat == OutputFormat.Txt ? "Transcricao concluida" : "Conversao concluida";
            StatusMessage = "Seu arquivo esta pronto na pasta de saida.";
        }
        catch (OperationCanceledException)
        {
            ShowError("Tempo limite atingido", "A operacao demorou demais e foi cancelada.");
        }
        catch (ConversionException ex)
        {
            ShowError(GetUserTitle(ex.Code), GetUserMessage(ex));
        }
        catch (Exception)
        {
            ShowError("Erro ao converter", "Nao foi possivel concluir a operacao. Tente novamente.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void Reset()
    {
        Url = string.Empty;
        Result = null;
        SetOutputFilename("Nenhum arquivo gerado ainda.");
        StatusTitle = "Pronto para converter";
        StatusMessage = "Cole o link, escolha o formato e inicie a conversao.";
    }

    private void ChooseOutputDirectory()
    {
        using var dialog = new Forms.FolderBrowserDialog
        {
            Description = "Escolha a pasta onde os arquivos convertidos serao salvos.",
            SelectedPath = OutputDirectory,
            UseDescriptionForTitle = true,
        };

        if (dialog.ShowDialog() == Forms.DialogResult.OK && Directory.Exists(dialog.SelectedPath))
        {
            OutputDirectory = dialog.SelectedPath;
        }
    }

    private void OpenFile()
    {
        ConversionResult? currentResult = Result;
        if (currentResult is not null && File.Exists(currentResult.FilePath))
        {
            OpenPath(currentResult.FilePath);
        }
    }

    private void OpenFolder()
    {
        if (Directory.Exists(OutputDirectory))
        {
            OpenPath(OutputDirectory);
        }
    }

    private static void OpenPath(string path)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = path,
            UseShellExecute = true,
        });
    }

    private void ShowError(string title, string message)
    {
        StatusTitle = title;
        StatusMessage = message;
    }

    private void SetOutputFilename(string filename)
    {
        OutputFilename = filename;
        OnPropertyChanged(nameof(OutputFilename));
    }

    private string GetActionText()
    {
        return SelectedFormat switch
        {
            OutputFormat.Mp3 => "Converter em MP3",
            OutputFormat.Mp4 => "Converter em MP4",
            OutputFormat.Txt => "Transcrever em TXT",
            _ => "Converter",
        };
    }

    private string GetBusyText()
    {
        return SelectedFormat == OutputFormat.Txt ? "Transcrevendo..." : "Convertendo...";
    }

    private string GetBusyTitle()
    {
        return SelectedFormat == OutputFormat.Txt ? "Transcrevendo..." : "Convertendo...";
    }

    private string GetBusyMessage()
    {
        return SelectedFormat switch
        {
            OutputFormat.Mp3 => "Extraindo audio e preparando o MP3.",
            OutputFormat.Mp4 => "Baixando video e mesclando audio com ffmpeg.",
            OutputFormat.Txt => "Preparando o audio e transcrevendo com whisper.cpp.",
            _ => "Processando o arquivo.",
        };
    }

    private TimeSpan GetTimeout()
    {
        return SelectedFormat == OutputFormat.Txt ? TranscriptionTimeout : ConversionTimeout;
    }

    private static string GetUserTitle(string code)
    {
        return code switch
        {
            "invalid_url" => "Link invalido",
            "duration_exceeded" => "Video muito longo",
            "missing_binary" => "Binarios nao encontrados",
            "missing_whisper_binary" => "Whisper nao encontrado",
            "missing_whisper_model" => "Modelo nao encontrado",
            "extraction_failed" => "Falha ao ler o video",
            "transcription_audio_failed" => "Falha ao preparar audio",
            "transcription_audio_not_found" => "Audio nao encontrado",
            "transcription_failed" => "Falha ao transcrever",
            "transcription_output_not_found" => "TXT nao encontrado",
            _ => "Erro ao converter",
        };
    }

    private static string GetUserMessage(ConversionException exception)
    {
        return exception.Code switch
        {
            "invalid_url" => exception.Message,
            "duration_exceeded" => "Mais de 20 minutos. Tente um trecho mais curto.",
            "missing_binary" => "Coloque yt-dlp.exe e ffmpeg.exe na pasta bin do aplicativo, ou instale o yt-dlp no PATH.",
            "missing_whisper_binary" => "Coloque whisper-cli.exe na pasta bin do aplicativo, ou instale o whisper-cli no PATH.",
            "missing_whisper_model" => exception.Message,
            "extraction_failed" => "Nao foi possivel obter informacoes do video. Verifique o link e tente novamente.",
            "transcription_audio_failed" => exception.Message,
            "transcription_audio_not_found" => "O audio preparado para transcricao nao foi encontrado.",
            "transcription_failed" => exception.Message,
            "transcription_output_not_found" => "A transcricao terminou, mas o arquivo TXT nao foi encontrado.",
            _ => "Erro ao converter o video. Tente novamente.",
        };
    }

    private static string GetTranscriptionModelLabel(TranscriptionModel model)
    {
        return model switch
        {
            TranscriptionModel.Tiny => "Muito rapida",
            TranscriptionModel.Base => "Transcricao rapida",
            TranscriptionModel.Small => "Equilibrada",
            TranscriptionModel.Large => "Melhor transcricao possivel",
            _ => model.ToString(),
        };
    }

    private static string GetDefaultOutputDirectory()
    {
        string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        string downloads = Path.Combine(profile, "Downloads");
        Directory.CreateDirectory(downloads);
        return downloads;
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private void RaiseCommandStates()
    {
        ConvertCommand.RaiseCanExecuteChanged();
        ChooseOutputDirectoryCommand.RaiseCanExecuteChanged();
        OpenFileCommand.RaiseCanExecuteChanged();
        OpenFolderCommand.RaiseCanExecuteChanged();
        NewConversionCommand.RaiseCanExecuteChanged();
    }
}

public sealed record VideoResolutionChoice(string Label, VideoResolution Value);

public sealed record TranscriptionModelChoice(string Label, TranscriptionModel Value);
