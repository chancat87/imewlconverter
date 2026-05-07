namespace ImeWlConverter.Formats.Shared;

using System.Text;
using ImeWlConverter.Abstractions.Contracts;
using ImeWlConverter.Abstractions.Models;
using ImeWlConverter.Abstractions.Options;
using ImeWlConverter.Abstractions.Results;

/// <summary>
/// Base class for text-based format exporters.
/// Handles line-by-line export with configurable encoding and line endings.
/// </summary>
public abstract class TextFormatExporter : IFormatExporter
{
    /// <summary>The text encoding for output.</summary>
    protected abstract Encoding FileEncoding { get; }

    /// <summary>Line ending string (default: \r\n).</summary>
    protected virtual string LineEnding => "\r\n";

    public abstract FormatMetadata Metadata { get; }

    /// <summary>Convert a word entry to its text representation.</summary>
    protected abstract string? FormatEntry(WordEntry entry);

    /// <summary>Optional header to write before entries.</summary>
    protected virtual string? GetHeader() => null;

    public Task<ExportResult> ExportAsync(
        IReadOnlyList<WordEntry> entries, Stream output,
        ExportOptions? options = null, CancellationToken ct = default)
    {
        using var writer = new StreamWriter(output, FileEncoding, leaveOpen: true);
        var count = 0;
        var errorCount = 0;

        var header = GetHeader();
        if (header != null)
            writer.Write(header + LineEnding);

        foreach (var entry in entries)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var line = FormatEntry(entry);
                if (line != null)
                {
                    writer.Write(line + LineEnding);
                    count++;
                }
            }
            catch
            {
                errorCount++;
            }
        }

        writer.Flush();
        return Task.FromResult(new ExportResult
        {
            EntryCount = count,
            ErrorCount = errorCount
        });
    }
}
