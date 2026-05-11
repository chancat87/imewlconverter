using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ImeWlConverter.Abstractions.Contracts;
using ImeWlConverter.Abstractions.Models;
using ImeWlConverter.Abstractions.Options;
using ImeWlConverter.Core;
using ImeWlConverter.Core.Pipeline;
using ImeWlConverter.Formats;
using Microsoft.Extensions.DependencyInjection;

namespace ImeWlConverterMac.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private readonly IDictionary<string, IFormatImporter> _importers = new Dictionary<string, IFormatImporter>();
    private readonly IDictionary<string, IFormatExporter> _exporters = new Dictionary<string, IFormatExporter>();

    private string _filePath = "";
    private string _resultText = "";
    private string _statusMessage = "欢迎使用深蓝词库转换工具";
    private double _progress = 0;
    private bool _isConverting = false;
    private bool _showLess = true;
    private bool _exportDirectly = false;
    private bool _streamExport = false;
    private bool _mergeToOneFile = true;

    private IFormatImporter? _selectedImporter;
    private IFormatExporter? _selectedExporter;
    private ChineseConversionMode _chineseConversion = ChineseConversionMode.None;
    private ServiceProvider? _serviceProvider;
    private string? _lastTempFile;

    public MainWindowViewModel()
    {
        LoadImeList();

        // 初始化命令
        OpenFileCommand = new RelayCommand(async () => await OpenFileAsync());
        ConvertCommand = new RelayCommand(async () => await ConvertAsync(), () => CanConvert());
        FilterConfigCommand = new RelayCommand(() => ShowFilterConfig());
        RankGenerateCommand = new RelayCommand(() => ShowRankGenerate());
        ChineseTransConfigCommand = new RelayCommand(() => ShowChineseTransConfig());
        DonateCommand = new RelayCommand(() => ShowDonate());
        HelpCommand = new RelayCommand(() => ShowHelp());
        AboutCommand = new RelayCommand(() => ShowAbout());
        AccessWebSiteCommand = new RelayCommand(() => AccessWebSite());
        SplitFileCommand = new RelayCommand(() => ShowSplitFile());
        MergeWLCommand = new RelayCommand(() => ShowMergeWL());

        // 切换命令
        ToggleShowLessCommand = new RelayCommand(() => ShowLess = !ShowLess);
        ToggleExportDirectlyCommand = new RelayCommand(() => ExportDirectly = !ExportDirectly);
        ToggleStreamExportCommand = new RelayCommand(() => StreamExport = !StreamExport);
        ToggleMergeToOneFileCommand = new RelayCommand(() => MergeToOneFile = !MergeToOneFile);
    }

    #region Properties

    public string FilePath
    {
        get => _filePath;
        set
        {
            if (SetField(ref _filePath, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    public string ResultText
    {
        get => _resultText;
        set => SetField(ref _resultText, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetField(ref _statusMessage, value);
    }

    public double Progress
    {
        get => _progress;
        set => SetField(ref _progress, value);
    }

    public bool IsConverting
    {
        get => _isConverting;
        set
        {
            if (SetField(ref _isConverting, value))
            {
                RaiseCanExecuteChanged();
            }
        }
    }

    public bool ShowLess
    {
        get => _showLess;
        set => SetField(ref _showLess, value);
    }

    public bool ExportDirectly
    {
        get => _exportDirectly;
        set => SetField(ref _exportDirectly, value);
    }

    public bool StreamExport
    {
        get => _streamExport;
        set => SetField(ref _streamExport, value);
    }

    public bool MergeToOneFile
    {
        get => _mergeToOneFile;
        set => SetField(ref _mergeToOneFile, value);
    }

    public ObservableCollection<string> ImportTypes { get; } = new();
    public ObservableCollection<string> ExportTypes { get; } = new();

    private string? _selectedImportType;
    public string? SelectedImportType
    {
        get => _selectedImportType;
        set
        {
            if (SetField(ref _selectedImportType, value))
            {
                if (value != null)
                {
                    _selectedImporter = _importers.TryGetValue(value, out var imp) ? imp : null;
                }
                RaiseCanExecuteChanged();
            }
        }
    }

    private string? _selectedExportType;
    public string? SelectedExportType
    {
        get => _selectedExportType;
        set
        {
            if (SetField(ref _selectedExportType, value))
            {
                if (value != null)
                {
                    _selectedExporter = _exporters.TryGetValue(value, out var exp) ? exp : null;
                }
                RaiseCanExecuteChanged();
            }
        }
    }

    #endregion

    #region Commands

    public ICommand OpenFileCommand { get; }
    public ICommand ConvertCommand { get; }
    public ICommand FilterConfigCommand { get; }
    public ICommand RankGenerateCommand { get; }
    public ICommand ChineseTransConfigCommand { get; }
    public ICommand DonateCommand { get; }
    public ICommand HelpCommand { get; }
    public ICommand AboutCommand { get; }
    public ICommand AccessWebSiteCommand { get; }
    public ICommand SplitFileCommand { get; }
    public ICommand MergeWLCommand { get; }

    // 切换命令
    public ICommand ToggleShowLessCommand { get; }
    public ICommand ToggleExportDirectlyCommand { get; }
    public ICommand ToggleStreamExportCommand { get; }
    public ICommand ToggleMergeToOneFileCommand { get; }

    #endregion

    #region Methods

    private void LoadImeList()
    {
        var services = new ServiceCollection();
        services.AddAllFormats();
        services.AddImeWlConverterCore();
        _serviceProvider = services.BuildServiceProvider();

        var importers = _serviceProvider.GetServices<IFormatImporter>()
            .OrderBy(i => i.Metadata.SortOrder).ToList();
        var exporters = _serviceProvider.GetServices<IFormatExporter>()
            .OrderBy(e => e.Metadata.SortOrder).ToList();

        foreach (var imp in importers)
            _importers[imp.Metadata.DisplayName] = imp;
        foreach (var exp in exporters)
            _exporters[exp.Metadata.DisplayName] = exp;

        ImportTypes.Clear();
        foreach (var imp in importers)
            ImportTypes.Add(imp.Metadata.DisplayName);
        ExportTypes.Clear();
        foreach (var exp in exporters)
            ExportTypes.Add(exp.Metadata.DisplayName);
    }

    private async Task OpenFileAsync()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null);
            if (topLevel != null)
            {
                var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
                {
                    Title = "选择词库文件",
                    AllowMultiple = true,
                    FileTypeFilter = new[]
                    {
                        FilePickerFileTypes.All,
                        FilePickerFileTypes.TextPlain
                    }
                });

                if (files.Count > 0)
                {
                    var filePaths = files.Select(f => f.Path.LocalPath).ToArray();
                    FilePath = string.Join(" | ", filePaths);

                    if (filePaths.Length == 1)
                    {
                        var autoType = AutoMatchImportType(filePaths[0]);
                        if (autoType != null && ImportTypes.Contains(autoType))
                            SelectedImportType = autoType;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开文件失败: {ex.Message}";
        }
    }

    public void HandleFileDrop(string[] filePaths)
    {
        FilePath = string.Join(" | ", filePaths);

        if (filePaths.Length == 1)
        {
            var autoType = AutoMatchImportType(filePaths[0]);
            if (autoType != null && ImportTypes.Contains(autoType))
                SelectedImportType = autoType;
        }
    }

    private string? AutoMatchImportType(string filePath)
    {
        var ext = Path.GetExtension(filePath)?.ToLowerInvariant();
        if (string.IsNullOrEmpty(ext)) return null;

        // 按文件扩展名匹配导入格式
        foreach (var kvp in _importers)
        {
            var id = kvp.Value.Metadata.Id.ToLowerInvariant();
            // 常见扩展名映射
            if (ext == ".scel" && id == "scel") return kvp.Key;
            if (ext == ".qcel" && id == "qcel") return kvp.Key;
            if (ext == ".qpyd" && id == "qpyd") return kvp.Key;
            if (ext == ".bcd" && id == "baiduBcd") return kvp.Key;
            if (ext == ".bdict" && id == "baiduBdict") return kvp.Key;
            if (ext == ".ld2" && id == "lingoesLd2") return kvp.Key;
            if (ext == ".uwl" && id == "ziguangUwl") return kvp.Key;
            if (ext == ".bin" && id == "sougouBin") return kvp.Key;
            if (ext == ".plist" && id == "macPlist") return kvp.Key;
        }

        return null;
    }

    private bool CanConvert()
    {
        return _selectedImporter != null && _selectedExporter != null && !string.IsNullOrEmpty(FilePath) && !IsConverting;
    }

    private async Task ConvertAsync()
    {
        if (!CanConvert()) return;

        try
        {
            IsConverting = true;
            Progress = 0;
            ResultText = "";
            StatusMessage = "开始转换...";

            await Task.Run(() => PerformConversion());

            // 转换完成后处理保存逻辑
            await HandleConversionCompleted();
        }
        catch (Exception ex)
        {
            StatusMessage = $"转换失败: {ex.Message}";
            ResultText = $"错误: {ex.Message}\n{ex.StackTrace}";
        }
        finally
        {
            IsConverting = false;
            Progress = 100;
        }
    }

    private void PerformConversion()
    {
        var files = FilePath.Split(new[] { " | " }, StringSplitOptions.RemoveEmptyEntries).ToList();

        var pipeline = _serviceProvider!.GetRequiredService<ConversionPipeline>();

        // 用临时文件做输出
        var tempFile = Path.GetTempFileName();

        var request = new ConversionRequest
        {
            InputFormatId = _selectedImporter!.Metadata.Id,
            OutputFormatId = _selectedExporter!.Metadata.Id,
            InputPaths = files,
            OutputPath = tempFile,
            Options = new ConversionOptions
            {
                ChineseConversion = _chineseConversion
            }
        };

        var result = pipeline.ExecuteAsync(request).GetAwaiter().GetResult();

        if (result.IsSuccess)
        {
            var content = File.ReadAllText(tempFile);

            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                if (ShowLess && content.Length > 200000)
                {
                    ResultText = "为避免输出时卡死，本文本框中不显示转换后的全部结果。\n\n" +
                               content[..100000] + "\n\n\n...\n\n\n" +
                               content[^100000..];
                }
                else
                {
                    ResultText = content;
                }
                StatusMessage = $"转换完成，导入 {result.Value.ImportedCount} 条，导出 {result.Value.ExportedCount} 条";
            });

            _lastTempFile = tempFile;
        }
        else
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                StatusMessage = $"转换失败: {result.Error}";
                ResultText = result.Error ?? "未知错误";
            });
        }
    }

    #endregion

    #region Command Implementations

    private async void ShowFilterConfig()
    {
        try
        {
            // FilterConfig 窗口暂时使用简化版（不应用过滤）
            StatusMessage = "过滤配置功能正在升级中，暂时使用默认配置";
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开过滤配置失败: {ex.Message}";
        }
    }

    private async void ShowRankGenerate()
    {
        try
        {
            // 词频生成功能暂时使用默认配置
            StatusMessage = "词频生成配置功能正在升级中，暂时使用默认配置";
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开词频生成配置失败: {ex.Message}";
        }
    }

    private async void ShowChineseTransConfig()
    {
        try
        {
            var window = new ImeWlConverterMac.Views.ChineseConverterSelectWindow(_chineseConversion);
            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                var result = await window.ShowDialog<bool?>(mainWindow);
                if (result == true)
                {
                    _chineseConversion = window.SelectedConversionMode;
                    StatusMessage = "简繁体转换配置已更新";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开简繁体转换配置失败: {ex.Message}";
        }
    }

    private async void ShowDonate()
    {
        try
        {
            var window = new ImeWlConverterMac.Views.DonateWindow();
            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                await window.ShowDialog(mainWindow);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开捐赠窗口失败: {ex.Message}";
        }
    }

    private async void ShowHelp()
    {
        try
        {
            var window = new ImeWlConverterMac.Views.HelpWindow();
            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                await window.ShowDialog(mainWindow);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开帮助窗口失败: {ex.Message}";
        }
    }

    private async void ShowAbout()
    {
        try
        {
            var window = new ImeWlConverterMac.Views.AboutWindow();
            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                await window.ShowDialog(mainWindow);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开关于窗口失败: {ex.Message}";
        }
    }

    private void AccessWebSite()
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "https://github.com/studyzy/imewlconverter/releases",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            StatusMessage = $"无法打开网站: {ex.Message}";
        }
    }

    private async void ShowSplitFile()
    {
        try
        {
            var window = new ImeWlConverterMac.Views.SplitFileWindow();
            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                await window.ShowDialog(mainWindow);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开文件分割窗口失败: {ex.Message}";
        }
    }

    private async void ShowMergeWL()
    {
        try
        {
            var window = new ImeWlConverterMac.Views.MergeWLWindow();
            var mainWindow = GetMainWindow();
            if (mainWindow != null)
            {
                await window.ShowDialog(mainWindow);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"打开词库合并窗口失败: {ex.Message}";
        }
    }

    private Window? GetMainWindow()
    {
        if (App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            return desktop.MainWindow;
        }
        return null;
    }

    private async Task HandleConversionCompleted()
    {
        if (string.IsNullOrEmpty(_lastTempFile)) return;
        if (ResultText.Length > 0)
            await ShowSaveDialog();
    }

    private async Task ShowSaveDialog()
    {
        try
        {
            var topLevel = TopLevel.GetTopLevel(App.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop ? desktop.MainWindow : null);
            if (topLevel != null)
            {
                // 确定文件扩展名和过滤器
                string defaultExt = ".txt";
                var fileTypes = new List<FilePickerFileType>
                {
                    new("文本文件") { Patterns = new[] { "*.txt" } }
                };

                if (_selectedExporter?.Metadata.Id.Contains("msPinyin") == true)
                {
                    defaultExt = ".dctx";
                    fileTypes.Insert(0, new("微软拼音") { Patterns = new[] { "*.dctx" } });
                }

                fileTypes.Add(FilePickerFileTypes.All);

                var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "保存转换结果",
                    FileTypeChoices = fileTypes,
                    DefaultExtension = defaultExt,
                    SuggestedFileName = $"转换结果{defaultExt}"
                });

                if (file != null)
                {
                    var filePath = file.Path.LocalPath;
                    File.Copy(_lastTempFile!, filePath, overwrite: true);
                    StatusMessage = $"保存成功，词库路径：{filePath}";
                }
                else
                {
                    StatusMessage = "用户取消了保存操作";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"保存失败: {ex.Message}";
        }
    }

    private void RaiseCanExecuteChanged()
    {
        if (ConvertCommand is RelayCommand convertCmd)
        {
            convertCmd.RaiseCanExecuteChanged();
        }
    }

    #endregion
}

// 简单的命令实现
public class RelayCommand : ICommand
{
    private readonly Action _execute;
    private readonly Func<bool>? _canExecute;

    public RelayCommand(Action execute, Func<bool>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;

    public void Execute(object? parameter) => _execute();

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
