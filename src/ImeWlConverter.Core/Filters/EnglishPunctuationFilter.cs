using System.Text.RegularExpressions;
using ImeWlConverter.Abstractions.Contracts;
using ImeWlConverter.Abstractions.Models;

namespace ImeWlConverter.Core.Filters;

public sealed partial class EnglishPunctuationFilter : IWordFilter
{
    [GeneratedRegex("[-,~.?:;'\"!`\\^]|(-{2})|(/.{3})|(/(/))|(/[/])|({})", RegexOptions.Compiled)]
    private static partial Regex EnglishPunctuationRegex();

    public bool ShouldKeep(WordEntry entry) =>
        !EnglishPunctuationRegex().IsMatch(entry.Word);
}

public sealed partial class EnglishPunctuationRemoveTransform : IWordTransform
{
    [GeneratedRegex("[-,~.?:;'\"!`\\^]|(-{2})|(/.{3})|(/(/))|(/[/])|({})", RegexOptions.Compiled)]
    private static partial Regex EnglishPunctuationRegex();

    public WordEntry? Transform(WordEntry entry)
    {
        var result = EnglishPunctuationRegex().Replace(entry.Word, "");
        return string.IsNullOrEmpty(result) ? null : entry with { Word = result };
    }
}
