namespace ImeWlConverter.Formats.Wubi;

using System.Text;
using ImeWlConverter.Abstractions;
using ImeWlConverter.Abstractions.Models;
using ImeWlConverter.Formats.Shared;

/// <summary>WubiNewAge (五笔新世纪) dictionary exporter. Format: "code word".</summary>
[FormatPlugin("wbnewage", "五笔新世纪", 221)]
public sealed class WubiNewAgeExporter : TextFormatExporter
{
    protected override Encoding FileEncoding => Encoding.Unicode;

    public override FormatMetadata Metadata { get; } = new(
        "wbnewage", "五笔新世纪", 221, SupportsImport: false, SupportsExport: true);

    protected override string? FormatEntry(WordEntry entry)
    {
        var code = entry.Code?.GetPrimaryCode("") ?? "";
        if (string.IsNullOrEmpty(code))
            return null;

        return $"{code} {entry.Word}";
    }
}
