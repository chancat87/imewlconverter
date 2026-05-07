using ImeWlConverter.Abstractions.Models;

namespace ImeWlConverter.Abstractions.Contracts;

/// <summary>
/// Generates frequency ranks for words.
/// </summary>
public interface IWordRankGenerator
{
    /// <summary>Whether this generator should override existing ranks.</summary>
    bool ForceOverride { get; }

    /// <summary>Generate rank for a single word.</summary>
    Task<int> GenerateRankAsync(string word, CancellationToken ct = default);

    /// <summary>Generate ranks for a batch of entries.</summary>
    Task<IReadOnlyList<WordEntry>> GenerateRanksAsync(
        IReadOnlyList<WordEntry> entries,
        IProgress<ProgressInfo>? progress = null,
        CancellationToken ct = default);
}
