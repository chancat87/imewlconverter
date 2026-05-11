namespace ImeWlConverter.Formats.SougouPinyinDict;

using ImeWlConverter.Abstractions;
using ImeWlConverter.Abstractions.Models;
using ImeWlConverter.Formats.Shared;
using ImeWlConverter.Formats.SougouBin;

/// <summary>Sougou Pinyin backup dictionary importer (binary).</summary>
/// <remarks>
/// Uses the same binary format as SougouBinImporter.
/// Both formats share the SougouBinParser implementation.
/// </remarks>
[FormatPlugin("sgpydict", "搜狗拼音备份词典", 30)]
public sealed partial class SougouPinyinDictImporter : BinaryFormatImporter
{
    protected override IReadOnlyList<WordEntry> ParseBinary(Stream input, CancellationToken ct)
    {
        return SougouBinParser.Parse(input);
    }
}
