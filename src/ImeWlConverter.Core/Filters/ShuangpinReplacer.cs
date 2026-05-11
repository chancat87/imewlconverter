using ImeWlConverter.Abstractions.Contracts;
using ImeWlConverter.Abstractions.Enums;
using ImeWlConverter.Abstractions.Models;

namespace ImeWlConverter.Core.Filters;

public sealed class ShuangpinReplacer : IWordTransform
{
    private readonly Dictionary<string, string> _mapping;

    // TODO: 当 Helper 迁移完成后，可从资源文件 Shuangpin.txt 加载映射
    public ShuangpinReplacer(Dictionary<string, string> mapping)
    {
        _mapping = mapping;
    }

    public WordEntry? Transform(WordEntry entry)
    {
        if (entry.CodeType != CodeType.Pinyin || entry.Code is null)
            return entry;

        var newSegments = new List<IReadOnlyList<string>>();
        foreach (var segment in entry.Code.Segments)
        {
            var newCodes = new List<string>();
            foreach (var code in segment)
            {
                newCodes.Add(_mapping.TryGetValue(code, out var mapped) ? mapped : code);
            }
            newSegments.Add(newCodes);
        }

        var newCode = new WordCode { Segments = newSegments };
        return entry with { Code = newCode };
    }
}
