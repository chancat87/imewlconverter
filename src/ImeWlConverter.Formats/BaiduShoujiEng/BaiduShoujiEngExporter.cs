namespace ImeWlConverter.Formats.BaiduShoujiEng;

using System.Text;
using ImeWlConverter.Abstractions;
using ImeWlConverter.Abstractions.Models;
using ImeWlConverter.Formats.Shared;

/// <summary>Baidu Mobile English dictionary exporter. Format: word\t(54999+rank)</summary>
[FormatPlugin("bdsje", "百度手机英文", 1010)]
public sealed class BaiduShoujiEngExporter : TextFormatExporter
{
    protected override Encoding FileEncoding => Encoding.ASCII;

    public override FormatMetadata Metadata { get; } = new(
        "bdsje", "百度手机英文", 1010, SupportsImport: false, SupportsExport: true);

    protected override string? FormatEntry(WordEntry entry)
    {
        return $"{entry.Word}\t{54999 + entry.Rank}";
    }
}
