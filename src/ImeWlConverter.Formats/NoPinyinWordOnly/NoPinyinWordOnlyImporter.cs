namespace ImeWlConverter.Formats.NoPinyinWordOnly;

using System.Text;
using ImeWlConverter.Abstractions;
using ImeWlConverter.Abstractions.Enums;
using ImeWlConverter.Abstractions.Models;
using ImeWlConverter.Formats.Shared;

/// <summary>Plain word list importer (no pinyin, one word per line).</summary>
[FormatPlugin("word", "无拼音纯汉字", 2010)]
public sealed class NoPinyinWordOnlyImporter : TextFormatImporter
{
    protected override Encoding FileEncoding => Encoding.UTF8;

    public override FormatMetadata Metadata { get; } = new(
        "word", "无拼音纯汉字", 2010, SupportsImport: true, SupportsExport: false);

    protected override IEnumerable<WordEntry> ParseLine(string line)
    {
        yield return new WordEntry
        {
            Word = line,
            Rank = 0,
            CodeType = CodeType.NoCode
        };
    }
}
