namespace ImeWlConverter.Formats.BaiduPinyin;

using System.Text;
using ImeWlConverter.Abstractions;
using ImeWlConverter.Abstractions.Models;
using ImeWlConverter.Formats.Shared;

/// <summary>Baidu Pinyin dictionary exporter (text format). Supports both Chinese and English entries.</summary>
[FormatPlugin("bdpy", "百度拼音", 90)]
public sealed class BaiduPinyinExporter : TextFormatExporter
{
    protected override Encoding FileEncoding => Encoding.Unicode;

    public override FormatMetadata Metadata { get; } = new(
        "bdpy", "百度拼音", 90, SupportsImport: false, SupportsExport: true);

    protected override string? FormatEntry(WordEntry entry)
    {
        if (entry.IsEnglish)
            return $"{entry.Word}\t{entry.Rank}";

        var pinyin = entry.Code?.GetPrimaryCode("'") ?? "";
        if (string.IsNullOrEmpty(pinyin))
            return null;
        return $"{entry.Word}\t{pinyin}'\t{entry.Rank}";
    }
}
