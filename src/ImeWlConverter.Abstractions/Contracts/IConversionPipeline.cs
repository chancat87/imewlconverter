using ImeWlConverter.Abstractions.Options;
using ImeWlConverter.Abstractions.Results;

namespace ImeWlConverter.Abstractions.Contracts;

/// <summary>
/// Orchestrates the full conversion pipeline: Import → Filter → Transform → Export.
/// </summary>
public interface IConversionPipeline
{
    /// <summary>Execute the conversion pipeline.</summary>
    Task<Result<ConversionResult>> ExecuteAsync(
        ConversionRequest request,
        CancellationToken ct = default);
}

/// <summary>A conversion request specifying input/output and options.</summary>
public sealed record ConversionRequest
{
    /// <summary>Input format identifier (e.g., "scel", "ggpy").</summary>
    public required string InputFormatId { get; init; }

    /// <summary>Output format identifier.</summary>
    public required string OutputFormatId { get; init; }

    /// <summary>Input file paths.</summary>
    public required IReadOnlyList<string> InputPaths { get; init; }

    /// <summary>Output file path.</summary>
    public required string OutputPath { get; init; }

    /// <summary>Conversion options.</summary>
    public ConversionOptions Options { get; init; } = new();
}

/// <summary>Result of a complete conversion.</summary>
public sealed record ConversionResult
{
    /// <summary>Total entries imported.</summary>
    public int ImportedCount { get; init; }

    /// <summary>Total entries exported after filtering.</summary>
    public int ExportedCount { get; init; }

    /// <summary>Entries filtered out.</summary>
    public int FilteredCount { get; init; }
}
