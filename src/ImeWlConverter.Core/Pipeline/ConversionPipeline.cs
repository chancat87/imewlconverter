using ImeWlConverter.Abstractions.Contracts;
using ImeWlConverter.Abstractions.Enums;
using ImeWlConverter.Abstractions.Models;
using ImeWlConverter.Abstractions.Options;
using ImeWlConverter.Abstractions.Results;
using ImeWlConverter.Core.CodeGeneration;

namespace ImeWlConverter.Core.Pipeline;

/// <summary>
/// Orchestrates the complete conversion pipeline:
/// Import → Filter → ChineseConvert → WordRank → CodeGen → RemoveEmpty → Export.
/// </summary>
public sealed class ConversionPipeline : IConversionPipeline
{
    private readonly IEnumerable<IFormatImporter> _importers;
    private readonly IEnumerable<IFormatExporter> _exporters;
    private readonly IProgress<ProgressInfo>? _progress;
    private readonly FilterPipeline? _filterPipeline;
    private readonly IChineseConverter? _chineseConverter;
    private readonly IWordRankGenerator? _wordRankGenerator;
    private readonly CodeGenerationService? _codeGenerationService;

    public ConversionPipeline(
        IEnumerable<IFormatImporter> importers,
        IEnumerable<IFormatExporter> exporters,
        IProgress<ProgressInfo>? progress = null,
        FilterPipeline? filterPipeline = null,
        IChineseConverter? chineseConverter = null,
        IWordRankGenerator? wordRankGenerator = null,
        CodeGenerationService? codeGenerationService = null)
    {
        _importers = importers;
        _exporters = exporters;
        _progress = progress;
        _filterPipeline = filterPipeline;
        _chineseConverter = chineseConverter;
        _wordRankGenerator = wordRankGenerator;
        _codeGenerationService = codeGenerationService;
    }

    /// <inheritdoc/>
    public async Task<Result<ConversionResult>> ExecuteAsync(
        ConversionRequest request,
        CancellationToken ct = default)
    {
        // 1. Find importer/exporter by format ID
        var importer = _importers.FirstOrDefault(i => i.Metadata.Id == request.InputFormatId);
        if (importer is null)
            return Result<ConversionResult>.Failure($"Unknown input format: {request.InputFormatId}");

        var exporter = _exporters.FirstOrDefault(e => e.Metadata.Id == request.OutputFormatId);
        if (exporter is null)
            return Result<ConversionResult>.Failure($"Unknown output format: {request.OutputFormatId}");

        // 2. Import all input files
        var allEntries = new List<WordEntry>();
        foreach (var inputPath in request.InputPaths)
        {
            ct.ThrowIfCancellationRequested();
            _progress?.Report(new ProgressInfo(0, 0, $"Importing {Path.GetFileName(inputPath)}..."));

            using var stream = File.OpenRead(inputPath);
            var importResult = await importer.ImportAsync(stream, request.Options.Import, ct);
            allEntries.AddRange(importResult.Entries);
        }

        var importedCount = allEntries.Count;

        // 3. Filter (before code generation)
        _progress?.Report(new ProgressInfo(0, 0, "Filtering..."));
        IReadOnlyList<WordEntry> entries = _filterPipeline is not null
            ? _filterPipeline.Apply(allEntries)
            : allEntries;

        // 4. Chinese conversion (simplified ↔ traditional)
        entries = ApplyChineseConversion(entries, request.Options.ChineseConversion);

        // 5. Word rank generation
        entries = await ApplyWordRankGenerationAsync(entries, ct);

        // 6. Code generation (target encoding)
        entries = ApplyCodeGeneration(entries, request.Options.CodeGeneration);

        // 7. Remove entries with empty code (when code generation was requested)
        if (_codeGenerationService is not null && request.Options.CodeGeneration.TargetCodeType != CodeType.NoCode)
        {
            entries = entries.Where(e => e.Code is not null && e.Code.Segments.Count > 0).ToList();
        }

        var exportedCount = entries.Count;
        var filteredCount = importedCount - exportedCount;

        // 8. Export
        _progress?.Report(new ProgressInfo(0, 0, "Exporting..."));
        using var outputStream = File.Create(request.OutputPath);
        await exporter.ExportAsync(entries, outputStream, request.Options.Export, ct);

        return Result<ConversionResult>.Success(new ConversionResult
        {
            ImportedCount = importedCount,
            ExportedCount = exportedCount,
            FilteredCount = filteredCount
        });
    }

    private IReadOnlyList<WordEntry> ApplyChineseConversion(
        IReadOnlyList<WordEntry> entries, ChineseConversionMode mode)
    {
        if (_chineseConverter is null || mode == ChineseConversionMode.None)
            return entries;

        var result = new List<WordEntry>(entries.Count);
        foreach (var entry in entries)
        {
            var converted = mode switch
            {
                ChineseConversionMode.SimplifiedToTraditional =>
                    entry with { Word = _chineseConverter.ToTraditional(entry.Word) },
                ChineseConversionMode.TraditionalToSimplified =>
                    entry with { Word = _chineseConverter.ToSimplified(entry.Word) },
                _ => entry
            };
            result.Add(converted);
        }

        return result;
    }

    private async Task<IReadOnlyList<WordEntry>> ApplyWordRankGenerationAsync(
        IReadOnlyList<WordEntry> entries, CancellationToken ct)
    {
        if (_wordRankGenerator is null)
            return entries;

        return await _wordRankGenerator.GenerateRanksAsync(entries, ct);
    }

    private IReadOnlyList<WordEntry> ApplyCodeGeneration(
        IReadOnlyList<WordEntry> entries, CodeGenerationOptions options)
    {
        if (_codeGenerationService is null || options.TargetCodeType == CodeType.NoCode)
            return entries;

        _progress?.Report(new ProgressInfo(0, entries.Count, "Generating codes..."));
        return _codeGenerationService.GenerateCodes(entries, options.TargetCodeType, _progress);
    }
}
