using ImeWlConverter.Abstractions.Models;

namespace ImeWlConverter.Abstractions.Contracts;

/// <summary>
/// Reports progress of long-running operations.
/// </summary>
public interface IProgressReporter
{
    /// <summary>Report a progress update.</summary>
    void Report(ProgressInfo info);
}
