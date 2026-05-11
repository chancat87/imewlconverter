using ImeWlConverter.Abstractions.Contracts;
using ImeWlConverter.Abstractions.Models;

namespace ImeWlConverter.Core.Filters;

public sealed class EmojiReplacer : IWordTransform
{
    private readonly Dictionary<string, string> _mapping;

    // TODO: 当 Helper 迁移完成后，可从外部文件加载映射
    public EmojiReplacer(Dictionary<string, string> mapping)
    {
        _mapping = mapping;
    }

    public WordEntry? Transform(WordEntry entry)
    {
        return _mapping.TryGetValue(entry.Word, out var emoji)
            ? entry with { Word = emoji }
            : entry;
    }
}
