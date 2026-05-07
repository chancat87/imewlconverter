namespace ImeWlConverter.Formats.SinaPinyin;

using System.Text;
using ImeWlConverter.Abstractions;
using ImeWlConverter.Abstractions.Models;
using ImeWlConverter.Formats.Shared;

/// <summary>Sina Pinyin dictionary exporter (text format). Format: pinyin\tword</summary>
[FormatPlugin("xlpy", "新浪拼音", 180)]
public sealed class SinaPinyinExporter : TextFormatExporter
{
    static SinaPinyinExporter()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    protected override Encoding FileEncoding => Encoding.GetEncoding("GBK");

    protected override string LineEnding => "\n";

    public override FormatMetadata Metadata { get; } = new(
        "xlpy", "新浪拼音", 180, SupportsImport: false, SupportsExport: true);

    protected override string? FormatEntry(WordEntry entry)
    {
        var pinyin = entry.Code?.GetPrimaryCode("'") ?? "";
        if (string.IsNullOrEmpty(pinyin))
            return null;
        return $"{pinyin}\t{entry.Word}";
    }
}
