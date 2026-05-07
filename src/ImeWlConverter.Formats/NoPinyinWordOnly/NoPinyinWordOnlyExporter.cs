namespace ImeWlConverter.Formats.NoPinyinWordOnly;

using System.Text;
using ImeWlConverter.Abstractions;
using ImeWlConverter.Abstractions.Models;
using ImeWlConverter.Formats.Shared;

/// <summary>Plain word list exporter (no pinyin, one word per line).</summary>
[FormatPlugin("word", "无拼音纯汉字", 2010)]
public sealed class NoPinyinWordOnlyExporter : TextFormatExporter
{
    protected override Encoding FileEncoding => Encoding.UTF8;

    public override FormatMetadata Metadata { get; } = new(
        "word", "无拼音纯汉字", 2010, SupportsImport: false, SupportsExport: true);

    protected override string? FormatEntry(WordEntry entry)
    {
        return entry.Word;
    }
}
