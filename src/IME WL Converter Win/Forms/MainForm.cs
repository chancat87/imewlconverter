/*
 *   Copyright © 2009-2020 studyzy(深蓝,曾毅)
 *
 *   This program "IME WL Converter(深蓝词库转换)" is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   This program is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU General Public License for more details.
 *
 *   You should have received a copy of the GNU General Public License
 *   along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using ImeWlConverter.Abstractions.Contracts;
using ImeWlConverter.Abstractions.Enums;
using ImeWlConverter.Abstractions.Models;
using ImeWlConverter.Abstractions.Options;
using ImeWlConverter.Core;
using ImeWlConverter.Core.Filters;
using ImeWlConverter.Core.Helpers;
using ImeWlConverter.Core.Pipeline;
using ImeWlConverter.Core.WordRank;
using ImeWlConverter.Formats;
using Microsoft.Extensions.DependencyInjection;

namespace Studyzy.IMEWLConverter;

public partial class MainForm : Form
{
    private ServiceProvider _serviceProvider = null!;
    private readonly IDictionary<string, IFormatImporter> _importers = new Dictionary<string, IFormatImporter>();
    private readonly IDictionary<string, IFormatExporter> _exporters = new Dictionary<string, IFormatExporter>();

    private IFormatImporter? _selectedImporter;
    private IFormatExporter? _selectedExporter;

    private ChineseConversionMode _chineseConversionMode = ChineseConversionMode.None;

    private FilterConfig filterConfig = new();
    private IWordRankGenerator _wordRankGenerator;

    private string exportPath = "";
    private string outputDir = "";
    private string errorMessages = "";
    private int _convertedCount;

    // Conversion result storage
    private IReadOnlyList<string>? _exportContents;

    private bool exportDirectly => toolStripMenuItemExportDirectly.Checked;
    private bool mergeTo1File => toolStripMenuItemMergeToOneFile.Checked;
    private bool streamExport => toolStripMenuItemStreamExport.Checked;

    public MainForm()
    {
        InitializeComponent();
        LoadTitle();

        // Initialize DI
        var services = new ServiceCollection();
        services.AddAllFormats();
        services.AddImeWlConverterCore();
        _serviceProvider = services.BuildServiceProvider();

        _wordRankGenerator = new DefaultWordRankGenerator();
    }

    private void LoadTitle()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var infoVersion = assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "3.3.1";
        // Remove Git commit info
        if (infoVersion.Contains("+"))
            infoVersion = infoVersion.Split('+')[0];
        if (infoVersion.Contains("-"))
            infoVersion = infoVersion.Split('-')[0];

        Text = "深蓝词库转换" + infoVersion;
    }

    private void LoadImeList()
    {
        var importers = _serviceProvider.GetServices<IFormatImporter>()
            .OrderBy(i => i.Metadata.SortOrder).ToList();
        var exporters = _serviceProvider.GetServices<IFormatExporter>()
            .OrderBy(e => e.Metadata.SortOrder).ToList();

        _importers.Clear();
        _exporters.Clear();

        foreach (var imp in importers)
            _importers[imp.Metadata.DisplayName] = imp;
        foreach (var exp in exporters)
            _exporters[exp.Metadata.DisplayName] = exp;

        cbxFrom.Items.Clear();
        foreach (var imp in importers)
            cbxFrom.Items.Add(imp.Metadata.DisplayName);

        cbxTo.Items.Clear();
        foreach (var exp in exporters)
            cbxTo.Items.Add(exp.Metadata.DisplayName);
    }

    private void MainForm_Load(object sender, EventArgs e)
    {
        LoadImeList();
        InitOpenFileDialogFilter("");
    }

    private void InitOpenFileDialogFilter(string select)
    {
        var types = new[]
        {
            "文本文件|*.txt",
            "细胞词库|*.scel",
            "QQ分类词库|*.qpyd",
            "百度分类词库|*.bdict",
            "百度分类词库|*.bcd",
            "搜狗备份词库|*.bin",
            "紫光分类词库|*.uwl",
            "微软拼音词库|*.dat",
            "Gboard词库|*.zip",
            "灵格斯词库|*.ld2",
            "所有文件|*.*"
        };
        var idx = 0;
        for (var i = 0; i < types.Length; i++)
            if (!string.IsNullOrEmpty(select) && types[i].Contains(select))
                idx = i;
        openFileDialog1.Filter = string.Join("|", types);
        openFileDialog1.FilterIndex = idx;
    }

    #region 选择格式

    private void cbxFrom_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_importers.TryGetValue(cbxFrom.Text, out var imp))
        {
            _selectedImporter = imp;
            var form = new CoreWinFormMapping().GetConfigForm(imp.Metadata.Id);
            if (form != null) form.ShowDialog();
        }
    }

    private void cbxTo_SelectedIndexChanged(object sender, EventArgs e)
    {
        if (_exporters.TryGetValue(cbxTo.Text, out var exp))
        {
            _selectedExporter = exp;
            var form = new CoreWinFormMapping().GetConfigForm(exp.Metadata.Id);
            if (form != null)
            {
                if (form is SelfDefiningConfigForm selfDefForm)
                {
                    selfDefForm.ShowDialog();
                }
                else
                {
                    form.ShowDialog();
                }
            }
        }
    }

    #endregion

    #region 文件操作

    private void btnOpenFileDialog_Click(object sender, EventArgs e)
    {
        if (openFileDialog1.ShowDialog() == DialogResult.OK)
        {
            var files = "";
            foreach (var file in openFileDialog1.FileNames) files += file + " | ";
            txbWLPath.Text = files.Remove(files.Length - 3);
            if (_selectedImporter?.Metadata.Id != "self")
            {
                var autoType = AutoMatchImportType(openFileDialog1.FileName);
                if (autoType != null) cbxFrom.Text = autoType;
            }
        }
    }

    private void MainForm_DragEnter(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
            e.Effect = DragDropEffects.Link;
        else
            e.Effect = DragDropEffects.None;
    }

    private void MainForm_DragDrop(object sender, DragEventArgs e)
    {
        var array = (Array)e.Data.GetData(DataFormats.FileDrop);
        var files = "";

        foreach (var a in array)
        {
            var path = a.ToString();
            files += path + " | ";
        }

        txbWLPath.Text = files.Remove(files.Length - 3);
        if (array.Length == 1)
        {
            var autoType = AutoMatchImportType(array.GetValue(0).ToString());
            if (autoType != null) cbxFrom.Text = autoType;
        }
    }

    /// <summary>
    /// Auto-detect import format based on file extension.
    /// Returns the DisplayName of the matching importer, or null.
    /// </summary>
    private string? AutoMatchImportType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLower();
        // Map common extensions to format IDs
        var extToId = new Dictionary<string, string>
        {
            { ".scel", "scel" },
            { ".qcel", "qcel" },
            { ".uwl", "uwl" },
            { ".bin", "sougou_bin" },
            { ".dat", "win10mspy" },
            { ".bcd", "baidu_bcd" },
            { ".bdict", "bdict" },
            { ".qpyd", "qpyd" },
            { ".ld2", "ld2" },
            { ".zip", "gboard" },
            { ".mb", "jidian_mb" },
        };

        if (extToId.TryGetValue(ext, out var formatId))
        {
            var match = _importers.Values.FirstOrDefault(i => i.Metadata.Id == formatId);
            if (match != null) return match.Metadata.DisplayName;
        }
        return null;
    }

    #endregion

    #region 转换

    private bool CheckCanRun()
    {
        if (_selectedImporter == null || _selectedExporter == null)
        {
            MessageBox.Show(
                "请先选择导入词库类型和导出词库类型",
                "深蓝词库转换",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return false;
        }

        if (txbWLPath.Text == "")
        {
            MessageBox.Show(
                "请先选择源词库文件",
                "深蓝词库转换",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
            return false;
        }

        return true;
    }

    private void btnConvert_Click(object sender, EventArgs e)
    {
        if (!CheckCanRun()) return;
        richTextBox1.Clear();
        errorMessages = "";
        _exportContents = null;
        try
        {
            if (streamExport)
            {
                if (saveFileDialog1.ShowDialog() == DialogResult.OK)
                {
                    exportPath = saveFileDialog1.FileName;
                }
                else
                {
                    ShowStatusMessage("请选择词库保存的路径，否则将无法进行词库导出", true);
                    return;
                }
            }

            if (!mergeTo1File)
            {
                if (folderBrowserDialog1.ShowDialog() == DialogResult.OK)
                {
                    outputDir = folderBrowserDialog1.SelectedPath;
                }
                else
                {
                    ShowStatusMessage("请选择词库保存的路径，否则将无法进行词库导出", true);
                    return;
                }
            }

            timer1.Enabled = true;
            backgroundWorker1.RunWorkerAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
    {
        var files = FileOperationHelper.GetFilesPath(txbWLPath.Text);
        var importer = _selectedImporter!;
        var exporter = _selectedExporter!;

        // Build filter pipeline
        var filters = BuildFilters();
        var transforms = BuildTransforms();
        var batchFilters = BuildBatchFilters();
        var filterPipeline = new FilterPipeline(filters, transforms, batchFilters);

        // Import all files
        RichTextBoxShow("正在导入...");
        var allEntries = new List<WordEntry>();
        foreach (var file in files)
        {
            try
            {
                using var stream = File.OpenRead(file);
                var importResult = importer.ImportAsync(stream, new ImportOptions(), CancellationToken.None).GetAwaiter().GetResult();
                allEntries.AddRange(importResult.Entries);
                RichTextBoxShow($"已导入 {file}，{importResult.Entries.Count} 条");
            }
            catch (Exception ex)
            {
                WriteErrorMessage($"导入 {file} 失败: {ex.Message}");
            }
        }

        // Filter
        RichTextBoxShow("正在过滤...");
        IReadOnlyList<WordEntry> entries = filterPipeline.Apply(allEntries);

        // Chinese conversion
        if (_chineseConversionMode != ChineseConversionMode.None)
        {
            var converter = _serviceProvider.GetService<IChineseConverter>();
            if (converter != null)
            {
                entries = ApplyChineseConversion(entries, converter);
            }
        }

        // Word rank generation
        entries = _wordRankGenerator.GenerateRanksAsync(entries, CancellationToken.None).GetAwaiter().GetResult();

        // Code generation (let the exporter handle its own code type needs)
        var codeGenService = _serviceProvider.GetService<ImeWlConverter.Core.CodeGeneration.CodeGenerationService>();
        if (codeGenService != null)
        {
            var targetCodeType = CodeType.Pinyin; // Default; exporter determines actual need
            entries = codeGenService.GenerateCodes(entries, targetCodeType, null);
        }

        _convertedCount = entries.Count;

        // Export
        RichTextBoxShow($"正在导出 {entries.Count} 条词条...");
        if (mergeTo1File)
        {
            using var outputStream = new MemoryStream();
            exporter.ExportAsync(entries, outputStream, new ExportOptions(), CancellationToken.None).GetAwaiter().GetResult();
            outputStream.Position = 0;
            using var reader = new StreamReader(outputStream);
            var content = reader.ReadToEnd();
            _exportContents = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
        }
        else
        {
            // Export each file separately
            foreach (var file in files)
            {
                var outputFile = Path.Combine(outputDir, Path.GetFileNameWithoutExtension(file) + ".txt");
                using var stream = File.OpenRead(file);
                var importResult = importer.ImportAsync(stream, new ImportOptions(), CancellationToken.None).GetAwaiter().GetResult();
                IReadOnlyList<WordEntry> fileEntries = filterPipeline.Apply(importResult.Entries.ToList());

                using var outStream = File.Create(outputFile);
                exporter.ExportAsync(fileEntries, outStream, new ExportOptions(), CancellationToken.None).GetAwaiter().GetResult();
                RichTextBoxShow($"已导出: {outputFile}");
            }
        }

        timer1.Enabled = false;
    }

    private IReadOnlyList<WordEntry> ApplyChineseConversion(
        IReadOnlyList<WordEntry> entries, IChineseConverter converter)
    {
        var result = new List<WordEntry>(entries.Count);
        foreach (var entry in entries)
        {
            var converted = _chineseConversionMode switch
            {
                ChineseConversionMode.SimplifiedToTraditional =>
                    entry with { Word = converter.ToTraditional(entry.Word) },
                ChineseConversionMode.TraditionalToSimplified =>
                    entry with { Word = converter.ToSimplified(entry.Word) },
                _ => entry
            };
            result.Add(converted);
        }
        return result;
    }

    #endregion

    #region 过滤器构建

    private IList<IWordFilter> BuildFilters()
    {
        var filters = new List<IWordFilter>();
        if (filterConfig.NoFilter) return filters;
        if (filterConfig.IgnoreEnglish) filters.Add(new EnglishFilter());
        if (filterConfig.IgnoreFirstCJK) filters.Add(new FirstCJKFilter());

        if (filterConfig.WordLengthFrom > 1 || filterConfig.WordLengthTo < 9999)
            filters.Add(new LengthFilter { MinLength = filterConfig.WordLengthFrom, MaxLength = filterConfig.WordLengthTo });

        if (filterConfig.WordRankFrom > 1 || filterConfig.WordRankTo < 999999)
            filters.Add(new RankFilter { MinRank = filterConfig.WordRankFrom, MaxRank = filterConfig.WordRankTo });

        if (filterConfig.IgnoreSpace) filters.Add(new SpaceFilter());
        if (filterConfig.IgnorePunctuation)
        {
            filters.Add(new ChinesePunctuationFilter());
            filters.Add(new EnglishPunctuationFilter());
        }
        if (filterConfig.IgnoreNumber) filters.Add(new NumberFilter());
        if (filterConfig.IgnoreNoAlphabetCode) filters.Add(new NoAlphabetCodeFilter());
        return filters;
    }

    private IList<IWordTransform> BuildTransforms()
    {
        var transforms = new List<IWordTransform>();
        if (filterConfig.NoFilter) return transforms;
        if (filterConfig.ReplaceEnglish) transforms.Add(new EnglishRemoveTransform());
        if (filterConfig.ReplacePunctuation)
        {
            transforms.Add(new EnglishPunctuationRemoveTransform());
            transforms.Add(new ChinesePunctuationRemoveTransform());
        }
        if (filterConfig.ReplaceSpace) transforms.Add(new SpaceRemoveTransform());
        if (filterConfig.ReplaceNumber) transforms.Add(new NumberRemoveTransform());
        return transforms;
    }

    private IList<IBatchFilter> BuildBatchFilters()
    {
        var filters = new List<IBatchFilter>();
        if (filterConfig.NoFilter) return filters;
        if (filterConfig.WordRankPercentage < 100)
        {
            filters.Add(new RankPercentageFilter { Percentage = filterConfig.WordRankPercentage });
        }
        return filters;
    }

    #endregion

    #region UI 显示

    private void ShowStatusMessage(string statusMessage, bool showMessageBox)
    {
        toolStripStatusLabel1.Text = statusMessage;
        if (showMessageBox)
            MessageBox.Show(
                statusMessage,
                "深蓝词库转换",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
    }

    private void timer1_Tick(object sender, EventArgs e)
    {
        ShowStatusMessage("转换中...", false);
    }

    private void RichTextBoxShow(string msg)
    {
        if (richTextBox1.InvokeRequired)
            richTextBox1.Invoke(() => richTextBox1.AppendText(msg + "\r\n"));
        else
            richTextBox1.AppendText(msg + "\r\n");
    }

    private void WriteErrorMessage(string msg)
    {
        errorMessages += msg + "\r\n";
    }

    private void backgroundWorker1_RunWorkerCompleted(
        object sender,
        RunWorkerCompletedEventArgs e
    )
    {
        timer1.Enabled = false;
        toolStripProgressBar1.Value = toolStripProgressBar1.Maximum;
        ShowStatusMessage("转换完成", false);
        if (errorMessages.Length > 0)
        {
            var errForm = new ErrorLogForm(errorMessages);
            errForm.ShowDialog();
        }

        if (e.Error != null)
        {
            MessageBox.Show(
                "不好意思，发生了错误：" + e.Error.Message,
                "出错",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error
            );
            if (e.Error.InnerException != null) RichTextBoxShow(e.Error.InnerException.ToString());
            return;
        }

        if (!mergeTo1File)
        {
            MessageBox.Show(
                "转换完成!",
                "深蓝词库转换",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information
            );
            return;
        }

        if (exportDirectly)
        {
            richTextBox1.Text =
                "为提高处理速度，"高级设置"中选中了"不显示结果，直接导出"，本文本框中不显示转换后的结果，若要查看转换后的结果再确定是否保存请取消该设置。";
        }
        else if (_exportContents != null)
        {
            var dataText = string.Join("\r\n", _exportContents);
            if (toolStripMenuItemShowLess.Checked && dataText.Length > 200000)
                richTextBox1.Text =
                    "为避免输出时卡死，"高级设置"中选中了"结果只显示首、末10万字"，本文本框中不显示转换后的全部结果，若要查看转换后的结果再确定是否保存请取消该设置。\n\n"
                    + dataText.Substring(0, 100000)
                    + "\n\n\n...\n\n\n"
                    + dataText.Substring(dataText.Length - 100000);
            else if (dataText.Length > 0) richTextBox1.Text = dataText;
        }

        if (_convertedCount > 0)
        {
            if (
                MessageBox.Show(
                    "是否将导入的" + _convertedCount + "条词库保存到本地硬盘上？",
                    "是否保存",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question
                ) == DialogResult.No
            )
                return;

            saveFileDialog1.DefaultExt = ".txt";
            saveFileDialog1.Filter = "文本文件|*.txt";

            if (saveFileDialog1.ShowDialog() == DialogResult.OK)
            {
                if (_exportContents != null)
                {
                    File.WriteAllText(saveFileDialog1.FileName, string.Join("\r\n", _exportContents));
                }

                ShowStatusMessage("保存成功，词库路径：" + saveFileDialog1.FileName, true);
            }
        }
        else
        {
            MessageBox.Show(
                "转换失败，没有找到词条",
                "深蓝词库转换",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning
            );
        }
    }

    #endregion

    #region 菜单操作

    private void ToolStripMenuItemSplitFile_Click(object sender, EventArgs e)
    {
        new SplitFileForm().ShowDialog();
    }

    private void ToolStripMenuItemChineseTransConfig_Click(object sender, EventArgs e)
    {
        var form = new ChineseConverterSelectForm();
        if (form.ShowDialog() == DialogResult.OK)
        {
            _chineseConversionMode = form.SelectedConversionMode;
        }
    }

    private void ToolStripMenuItemAccessWebSite_Click(object sender, EventArgs e)
    {
        Process.Start(
            new ProcessStartInfo("https://github.com/studyzy/imewlconverter/releases")
            {
                UseShellExecute = true
            }
        );
    }

    private void ToolStripMenuItemDonate_Click(object sender, EventArgs e)
    {
        new DonateForm().ShowDialog();
    }

    private void btnAbout_Click(object sender, EventArgs e)
    {
        new AboutBox().ShowDialog();
    }

    private void ToolStripMenuItemHelp_Click(object sender, EventArgs e)
    {
        new HelpForm().ShowDialog();
    }

    private void toolStripMenuItemFilterConfig_Click(object sender, EventArgs e)
    {
        var form = new FilterConfigForm();

        if (form.ShowDialog() == DialogResult.OK) filterConfig = form.FilterConfig;
    }

    private void ToolStripMenuItemMergeWL_Click(object sender, EventArgs e)
    {
        new MergeWLForm().ShowDialog();
    }

    private void ToolStripMenuItemRankGenerate_Click(object sender, EventArgs e)
    {
        var form = new WordRankGenerateForm();
        if (form.ShowDialog() == DialogResult.OK) _wordRankGenerator = form.SelectedWordRankGenerator;
    }

    #endregion
}
