using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Senice.App.Themes;
using Senice.Core.Abstractions;
using Senice.Core.Encodings;
using Senice.Core.Models;
using Senice.Core.Processing;
using Senice.App.Views;

namespace Senice.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ProcessingService _processing;
    private readonly ILanguageResolver _resolver;
    private readonly IFileSystem _fileSystem;
    private readonly IEncodingService _encoding;
    private readonly IDiffEngine _diffEngine;
    private CancellationTokenSource? _cancellation;

    [ObservableProperty]
    private string _targetPath = string.Empty;

    [ObservableProperty]
    private bool _isDirectoryTarget;

    [ObservableProperty]
    private string _detectedLanguage = string.Empty;

    [ObservableProperty]
    private bool _removeComments = true;

    [ObservableProperty]
    private bool _removeEmojisInComments = true;

    [ObservableProperty]
    private bool _removeEmojisInStrings;

    [ObservableProperty]
    private bool _preserveShebang = true;

    [ObservableProperty]
    private bool _backupBeforeWrite = true;

    [ObservableProperty]
    private bool _skipUncertainFiles = true;

    [ObservableProperty]
    private bool _includeHiddenFiles;

    [ObservableProperty]
    private int _maxDepth = 64;

    [ObservableProperty]
    private bool _isRunning;

    [ObservableProperty]
    private int _processed;

    [ObservableProperty]
    private int _total;

    [ObservableProperty]
    private string _currentFile = string.Empty;

    [ObservableProperty]
    private string _summaryText = string.Empty;

    [ObservableProperty]
    private bool _hasResults;

    [ObservableProperty]
    private ObservableCollection<FileProcessingResult> _results = new();

    [ObservableProperty]
    private bool _isDarkTheme = true;

    public MainViewModel(
        ProcessingService processing,
        ILanguageResolver resolver,
        IFileSystem fileSystem,
        IEncodingService encoding,
        IDiffEngine diffEngine)
    {
        _processing = processing;
        _resolver = resolver;
        _fileSystem = fileSystem;
        _encoding = encoding;
        _diffEngine = diffEngine;
        _results.CollectionChanged += (_, _) => HasResults = _results.Count > 0;
    }

    partial void OnIsDarkThemeChanged(bool value) => ThemeService.Apply(value ? AppTheme.Dark : AppTheme.Light);

    partial void OnTargetPathChanged(string value) => RefreshDetection();

    partial void OnIsDirectoryTargetChanged(bool value) => RefreshDetection();

    private void RefreshDetection()
    {
        if (IsDirectoryTarget || string.IsNullOrEmpty(TargetPath))
        {
            DetectedLanguage = string.Empty;
            return;
        }

        DetectedLanguage = _resolver.Resolve(TargetPath)?.Name ?? "Unknown language";
    }

    public void SetTarget(string path)
    {
        IsDirectoryTarget = _fileSystem.IsDirectory(path);
        TargetPath = path;
    }

    [RelayCommand]
    private void BrowseFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = "Source files (*.cs;*.cpp;*.c;*.py;*.java;*.js;*.ts;*.go;*.rs;*.php;*.rb;*.sh;*.html;*.css;*.xml;*.json)|*.cs;*.cpp;*.c;*.py;*.java;*.js;*.ts;*.go;*.rs;*.php;*.rb;*.sh;*.html;*.css;*.xml;*.json|All files (*.*)|*.*",
            CheckFileExists = true,
        };

        if (dialog.ShowDialog() == true)
            SetTarget(dialog.FileName);
    }

    [RelayCommand]
    private void BrowseFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Multiselect = false,
        };

        if (dialog.ShowDialog() == true)
            SetTarget(dialog.FolderName);
    }

    [RelayCommand]
    private async Task RunAsync()
    {
        if (string.IsNullOrWhiteSpace(TargetPath))
            return;

        if (!_fileSystem.FileExists(TargetPath) && !_fileSystem.IsDirectory(TargetPath))
        {
            SummaryText = "The selected path does not exist.";
            return;
        }

        var options = new ProcessingOptions
        {
            RemoveComments = RemoveComments,
            RemoveEmojisInComments = RemoveEmojisInComments,
            RemoveEmojisInStrings = RemoveEmojisInStrings,
            PreserveShebang = PreserveShebang,
            BackupBeforeWrite = BackupBeforeWrite,
            SkipUncertainFiles = SkipUncertainFiles,
            IncludeHiddenFiles = IncludeHiddenFiles,
            MaxDepth = Math.Max(0, MaxDepth),
            MaxDegreeOfParallelism = Math.Max(1, Environment.ProcessorCount),
        };

        _cancellation = new CancellationTokenSource();
        IsRunning = true;
        Processed = 0;
        Total = 0;
        CurrentFile = string.Empty;
        SummaryText = string.Empty;
        Results.Clear();

        var progress = new Progress<ProcessingProgress>(p =>
        {
            Processed = p.Processed;
            Total = p.Total;
            CurrentFile = p.CurrentFile;
        });

        try
        {
            var summary = await _processing.ProcessFilesAsync(
                new[] { TargetPath },
                options,
                progress,
                cancellationToken: _cancellation.Token);

            SummaryText = BuildSummary(summary);
            foreach (var result in summary.Results.OrderBy(r => r.Path, StringComparer.OrdinalIgnoreCase))
                Results.Add(result);
        }
        catch (OperationCanceledException)
        {
            SummaryText = $"Cancelled after {Processed} of {Total} files.";
        }
        finally
        {
            IsRunning = false;
            _cancellation?.Dispose();
            _cancellation = null;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        _cancellation?.Cancel();
    }

    [RelayCommand]
    private async Task ShowDiffAsync(FileProcessingResult? result)
    {
        if (result is null)
            return;

        var viewModel = await DiffWindowViewModel.CreateAsync(result, _fileSystem, _encoding, _diffEngine);
        if (viewModel is null)
            return;

        var window = new DiffWindow(viewModel) { Owner = System.Windows.Application.Current.MainWindow };
        window.ShowDialog();
    }

    [RelayCommand]
    private void ClearResults()
    {
        Results.Clear();
        SummaryText = string.Empty;
    }

    private static string BuildSummary(ProcessingSummary summary)
    {
        var parts = new List<string>
        {
            $"{summary.Total} file(s)",
            $"{summary.Changed} changed",
            $"{summary.Unchanged} unchanged",
            $"{summary.Skipped} skipped",
            $"{summary.Errors} errors",
        };

        if (summary.BytesSaved > 0)
            parts.Add($"{summary.BytesSaved} bytes saved");

        return string.Join(", ", parts) + $" in {summary.Elapsed.TotalSeconds:F1}s";
    }
}
