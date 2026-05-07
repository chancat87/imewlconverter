namespace ImeWlConverter.Abstractions.Contracts;

/// <summary>
/// Provides pinyin lookup for Chinese characters.
/// </summary>
public interface IPinyinDictionary
{
    /// <summary>Get all possible pinyin readings for a character.</summary>
    IReadOnlyList<string> GetPinyin(char character);

    /// <summary>Check if a character has pinyin data available.</summary>
    bool HasPinyin(char character);
}
