namespace ImeWlConverter.Formats.BaiduPinyinBackup;

using System.Text;
using ImeWlConverter.Abstractions;
using ImeWlConverter.Abstractions.Enums;
using ImeWlConverter.Abstractions.Models;
using ImeWlConverter.Formats.Shared;
using Studyzy.IMEWLConverter.IME;

/// <summary>Baidu Pinyin backup file importer (binary, delegates to legacy).</summary>
[FormatPlugin("bdpybin", "百度拼音备份", 20)]
public sealed class BaiduPinyinBackupImporter : BinaryFormatImporter
{
    static BaiduPinyinBackupImporter()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public override FormatMetadata Metadata { get; } = new(
        "bdpybin", "百度拼音备份", 20, SupportsImport: true, SupportsExport: false, IsBinary: true);

    protected override IReadOnlyList<WordEntry> ParseBinary(Stream input, CancellationToken ct)
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            using (var fs = File.Create(tempFile))
                input.CopyTo(fs);

            var legacy = new BaiduPinyinBackup();
            var legacyResult = legacy.Import(tempFile);

            return legacyResult.Select(wl => new WordEntry
            {
                Word = wl.Word,
                Rank = wl.Rank,
                CodeType = CodeType.Pinyin,
                Code = wl.PinYin?.Length > 0
                    ? WordCode.FromSingle(wl.PinYin)
                    : null
            }).ToList();
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
