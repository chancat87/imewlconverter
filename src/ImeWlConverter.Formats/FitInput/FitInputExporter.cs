namespace ImeWlConverter.Formats.FitInput;

using System.Text;
using ImeWlConverter.Abstractions;
using ImeWlConverter.Abstractions.Models;
using ImeWlConverter.Formats.Shared;

/// <summary>FIT Input dictionary exporter (text format). Format: pinyin,word</summary>
[FormatPlugin("fit", "FIT", 140)]
public sealed class FitInputExporter : TextFormatExporter
{
    protected override Encoding FileEncoding => new UTF8Encoding(false);

    protected override string LineEnding => "\n";

    public override FormatMetadata Metadata { get; } = new(
        "fit", "FIT", 140, SupportsImport: false, SupportsExport: true);

    protected override string? FormatEntry(WordEntry entry)
    {
        var pinyin = entry.Code?.GetPrimaryCode("'") ?? "";
        if (string.IsNullOrEmpty(pinyin))
            return null;
        return $"{pinyin},{entry.Word}";
    }
}
