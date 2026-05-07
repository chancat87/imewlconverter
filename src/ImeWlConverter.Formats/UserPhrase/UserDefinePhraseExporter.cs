namespace ImeWlConverter.Formats.UserPhrase;

using System.Text;
using ImeWlConverter.Abstractions;
using ImeWlConverter.Abstractions.Models;
using ImeWlConverter.Formats.Shared;

/// <summary>User-defined phrase exporter. Export only, default format: "{code},{rank}={word}".</summary>
[FormatPlugin("dy", "自定义短语", 110)]
public sealed class UserDefinePhraseExporter : TextFormatExporter
{
    protected override Encoding FileEncoding => Encoding.UTF8;

    public override FormatMetadata Metadata { get; } = new(
        "dy", "自定义短语", 110, SupportsImport: false, SupportsExport: true);

    protected override string? FormatEntry(WordEntry entry)
    {
        var code = entry.Code?.GetPrimaryCode("") ?? "";
        if (string.IsNullOrEmpty(code))
            return null;

        var rank = entry.Rank == 0 ? 1 : entry.Rank;
        return $"{code},{rank}={entry.Word}";
    }
}
