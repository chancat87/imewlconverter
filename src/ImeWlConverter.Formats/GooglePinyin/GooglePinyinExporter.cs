namespace ImeWlConverter.Formats.GooglePinyin;

using System.Text;
using ImeWlConverter.Abstractions;
using ImeWlConverter.Abstractions.Models;
using ImeWlConverter.Formats.Shared;

/// <summary>Google Pinyin dictionary exporter (text format).</summary>
[FormatPlugin("ggpy", "谷歌拼音", 110)]
public sealed class GooglePinyinExporter : TextFormatExporter
{
    static GooglePinyinExporter()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    protected override Encoding FileEncoding => Encoding.GetEncoding("GBK");

    public override FormatMetadata Metadata { get; } = new(
        "ggpy", "谷歌拼音", 110, SupportsImport: false, SupportsExport: true);

    protected override string? FormatEntry(WordEntry entry)
    {
        var pinyin = entry.Code?.GetPrimaryCode(" ") ?? "";
        return $"{entry.Word}\t{entry.Rank}\t{pinyin}";
    }
}
