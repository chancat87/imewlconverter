namespace ImeWlConverter.Formats.Gboard;

using System.Text;
using ImeWlConverter.Abstractions;
using ImeWlConverter.Abstractions.Enums;
using ImeWlConverter.Abstractions.Models;
using ImeWlConverter.Formats.Shared;

/// <summary>Gboard dictionary importer (tab-separated text in zip).</summary>
[FormatPlugin("gboard", "Gboard", 111)]
public sealed class GboardImporter : TextFormatImporter
{
    protected override Encoding FileEncoding => new UTF8Encoding(false);

    public override FormatMetadata Metadata { get; } = new(
        "gboard", "Gboard", 111, SupportsImport: true, SupportsExport: false);

    protected override bool IsContentLine(string line)
        => !string.IsNullOrWhiteSpace(line) && !line.StartsWith("#");

    protected override IEnumerable<WordEntry> ParseLine(string line)
    {
        var parts = line.Split('\t');
        if (parts.Length < 2)
            yield break;

        yield return new WordEntry
        {
            Word = parts[1],
            CodeType = CodeType.UserDefine,
            Code = WordCode.FromSingle(new[] { parts[0] })
        };
    }
}
