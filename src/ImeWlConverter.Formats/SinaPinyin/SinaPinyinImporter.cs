namespace ImeWlConverter.Formats.SinaPinyin;

using System.Text;
using ImeWlConverter.Abstractions;
using ImeWlConverter.Abstractions.Enums;
using ImeWlConverter.Abstractions.Models;
using ImeWlConverter.Formats.Shared;

/// <summary>Sina Pinyin dictionary importer (text format). Format: pinyin word</summary>
[FormatPlugin("xlpy", "新浪拼音", 180)]
public sealed class SinaPinyinImporter : TextFormatImporter
{
    static SinaPinyinImporter()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    protected override Encoding FileEncoding => Encoding.GetEncoding("GBK");

    public override FormatMetadata Metadata { get; } = new(
        "xlpy", "新浪拼音", 180, SupportsImport: true, SupportsExport: false);

    protected override IEnumerable<WordEntry> ParseLine(string line)
    {
        var parts = line.Split(' ');
        if (parts.Length < 2)
            yield break;

        var py = parts[0];
        var word = parts[1];
        var pinyinParts = py.Split(new[] { '\'' }, StringSplitOptions.RemoveEmptyEntries);

        yield return new WordEntry
        {
            Word = word,
            Rank = 1,
            CodeType = CodeType.Pinyin,
            Code = WordCode.FromSingle(pinyinParts)
        };
    }
}
