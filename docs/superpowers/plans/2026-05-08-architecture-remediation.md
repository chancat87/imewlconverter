# 架构治理计划：消除双轨系统，实现高内聚低耦合

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 消除新旧架构双轨并存的死代码问题，将新架构（Abstractions/Core/Formats）接入实际执行路径，使 CLI 和 macOS GUI 通过新管道完成转换，同时保持所有现有测试和集成测试通过。

**Architecture:** 采用渐进式迁移策略——先让 CLI 通过新 `ConversionPipeline` 执行转换（内部通过 `LegacyImporterAdapter`/`LegacyExporterAdapter` 桥接旧格式实现），然后逐步将格式实现从旧 IME/ 目录迁移到新 Formats/ 目录。每个阶段都是独立可验证的。

**Tech Stack:** C# / .NET 10.0, xUnit 2.9.3, System.CommandLine, Microsoft.Extensions.DependencyInjection, Source Generator

---

## 文件结构总览

### 将创建的文件

| 文件 | 职责 |
|------|------|
| `src/ImeWlConverter.Core/Adapters/LegacyFilterAdapter.cs` | 旧 ISingleFilter → 新 IWordFilter 适配器 |
| `src/ImeWlConverter.Core/Adapters/LegacyBatchFilterAdapter.cs` | 旧 IBatchFilter → 新 IBatchFilter 适配器 |
| `src/ImeWlConverter.Core/Adapters/LegacyCodeGeneratorAdapter.cs` | 旧 IWordCodeGenerater → 新 ICodeGenerator 适配器 |
| `src/ImeWlConverter.Core/ChineseConversion/LegacyChineseConverterAdapter.cs` | 旧 IChineseConverter → 新流程适配 |
| `src/ImeWlConverterCmd/PipelineHost.cs` | DI 容器组装 + 新管道调用入口 |
| `src/ImeWlConverterCoreTest/Pipeline/ConversionPipelineTest.cs` | ConversionPipeline 集成测试 |
| `src/ImeWlConverterCoreTest/Pipeline/FilterPipelineTest.cs` | FilterPipeline 单元测试 |
| `src/ImeWlConverterCoreTest/Adapters/LegacyImporterAdapterTest.cs` | 适配器测试 |
| `src/ImeWlConverterCoreTest/Adapters/LegacyExporterAdapterTest.cs` | 适配器测试 |

### 将修改的文件

| 文件 | 修改内容 |
|------|---------|
| `src/ImeWlConverterCmd/ImeWlConverterCmd.csproj` | 添加对 Core/Formats 的项目引用 |
| `src/ImeWlConverterCmd/CommandBuilder.cs` | 调用新管道入口 |
| `src/ImeWlConverterMac/ImeWlConverterMac.csproj` | 添加对 Core 的项目引用 |
| `src/ImeWlConverterMac/ViewModels/MainWindowViewModel.cs` | 用显式注册替代反射 |
| `src/ImeWlConverter.Core/ImeWlConverter.Core.csproj` | 更新依赖 |
| `src/ImeWlConverter.Core/Pipeline/ConversionPipeline.cs` | 接入 FilterPipeline 和 CodeGeneration |
| `src/ImeWlConverter.Core/ServiceCollectionExtensions.cs` | 完善 DI 注册 |
| `src/ImeWlConverter.Formats/ImeWlConverter.Formats.csproj` | 修复包版本，移除不需要的依赖 |
| `src/ImeWlConverter.Abstractions/Contracts/IProgressReporter.cs` | 替换为 `IProgress<ProgressInfo>` |
| `src/ImeWlConverterCore/CommandLineOptions.cs` | 改为 record |
| 各 Formats 下的 `Encoding.RegisterProvider` 调用 | 全部移除 |

### 将删除的文件

| 文件 | 原因 |
|------|------|
| `src/ImeWlConverter.Core/Utilities/NumberToChineseConverter.cs` | 与旧 MainBody.Num2ChsRegex 重复，无调用者 |
| `src/ImeWlConverter.Core/LlmIntegration/HttpLlmClient.cs` | 无调用者，旧 LlmWordRankGenerater 有独立实现 |
| `src/ImeWlConverter.Abstractions/Contracts/IProgressReporter.cs` | 被 BCL `IProgress<T>` 替代 |
| `src/ImeWlConverter.Abstractions/Contracts/ILlmClient.cs` | 无实际使用场景 |
| `src/ImeWlConverter.Abstractions/Contracts/IPinyinDictionary.cs` | 无实现，无消费者 |
| `src/ImeWlConverter.Abstractions/Contracts/IWordRankGenerator.cs` | 无实现，旧实现走不同路径 |
| `src/ImeWlConverterCore/ImeWlConverterCore-net46.csproj` | .NET 4.6 已废弃 |
| `src/ImeWlConverterCoreTest/ImeWlConverterCoreTest-net46.csproj` | .NET 4.6 已废弃 |

---

## Phase 1: 清理死代码和修复基础问题

本阶段目标：移除不会被使用的代码，修正包版本，消除 DRY 违规。每步独立，不影响现有功能。

---

### Task 1: 删除孤儿代码和废弃文件

**Files:**
- Delete: `src/ImeWlConverter.Core/Utilities/NumberToChineseConverter.cs`
- Delete: `src/ImeWlConverter.Core/LlmIntegration/HttpLlmClient.cs`
- Delete: `src/ImeWlConverter.Abstractions/Contracts/ILlmClient.cs`
- Delete: `src/ImeWlConverter.Abstractions/Contracts/IPinyinDictionary.cs`
- Delete: `src/ImeWlConverter.Abstractions/Contracts/IWordRankGenerator.cs`
- Delete: `src/ImeWlConverterCore/ImeWlConverterCore-net46.csproj`
- Delete: `src/ImeWlConverterCoreTest/ImeWlConverterCoreTest-net46.csproj`

- [ ] **Step 1: 验证无引用**

Run: `grep -rn "NumberToChineseConverter\|HttpLlmClient\|ILlmClient\|IPinyinDictionary\|IWordRankGenerator" src/ --include="*.cs" | grep -v "obj/" | grep -v "bin/"`

Expected: 仅在定义文件自身和 `ServiceCollectionExtensions.cs` 中出现（如有引用需先清除）

- [ ] **Step 2: 删除文件**

```bash
rm src/ImeWlConverter.Core/Utilities/NumberToChineseConverter.cs
rm src/ImeWlConverter.Core/LlmIntegration/HttpLlmClient.cs
rm src/ImeWlConverter.Abstractions/Contracts/ILlmClient.cs
rm src/ImeWlConverter.Abstractions/Contracts/IPinyinDictionary.cs
rm src/ImeWlConverter.Abstractions/Contracts/IWordRankGenerator.cs
rm src/ImeWlConverterCore/ImeWlConverterCore-net46.csproj
rm src/ImeWlConverterCoreTest/ImeWlConverterCoreTest-net46.csproj
```

- [ ] **Step 3: 清除残留引用**

如果 `ServiceCollectionExtensions.cs` 或其他文件引用了已删除类型，移除相关 using 语句和注册代码。

- [ ] **Step 4: 验证编译**

Run: `make build`
Expected: BUILD SUCCEEDED

- [ ] **Step 5: 运行测试**

Run: `make test`
Expected: 92 个测试全部通过

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor: remove orphaned code (HttpLlmClient, NumberToChineseConverter, unused interfaces, net46 projects)"
```

---

### Task 2: 移除散落的 Encoding.RegisterProvider 调用

**Files:**
- Modify: `src/ImeWlConverter.Formats/` 下所有含 `Encoding.RegisterProvider` 的 26 个文件
- Modify: `src/ImeWlConverterCore/IME/` 下所有含该调用的文件
- Modify: `src/ImeWlConverterCore/MainBody.cs`（保留此处一处调用作为后备）
- Keep: `src/ImeWlConverterCmd/Program.cs:29`（应用入口，唯一正确位置）

- [ ] **Step 1: 确认 Program.cs 已有调用**

Run: `grep -n "Encoding.RegisterProvider" src/ImeWlConverterCmd/Program.cs`
Expected: 第 29 行有 `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);`

- [ ] **Step 2: 移除新格式层中的所有调用**

对 `src/ImeWlConverter.Formats/` 下的每个文件，删除静态构造函数中的 `Encoding.RegisterProvider` 调用。如果静态构造函数只有这一行，删除整个静态构造函数。

示例（GooglePinyinImporter.cs 修改前）：
```csharp
[FormatPlugin("ggpy", "谷歌拼音", 110)]
public sealed partial class GooglePinyinImporter : TextFormatImporter
{
    static GooglePinyinImporter()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    protected override Encoding FileEncoding => Encoding.GetEncoding("GBK");
```

修改后：
```csharp
[FormatPlugin("ggpy", "谷歌拼音", 110)]
public sealed partial class GooglePinyinImporter : TextFormatImporter
{
    protected override Encoding FileEncoding => Encoding.GetEncoding("GBK");
```

- [ ] **Step 3: 移除旧 IME 层中的调用**

对 `src/ImeWlConverterCore/IME/` 下的文件（GooglePinyin.cs, SinaPinyin.cs, CangjiePlatform.cs, Gboard.cs, Xiaoxiao.cs, XiaoxiaoErbi.cs, SougouPinyin.cs），移除 `Encoding.RegisterProvider` 行。注意：这些通常在属性 getter 或方法内部调用，需保留方法结构。

- [ ] **Step 4: 保留 MainBody.cs 中的调用**

`MainBody.cs` 构造函数中的调用保留（作为 GUI 路径的入口保证）。

- [ ] **Step 5: 移除测试文件中的调用**

从 `NoPinyinWordOnlyTest.cs` 和 `FileOperationTest.cs` 中移除 `Encoding.RegisterProvider` 行。

- [ ] **Step 6: 验证编译和测试**

Run: `make build && make test`
Expected: BUILD SUCCEEDED, 92 个测试通过

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "refactor: centralize Encoding.RegisterProvider to Program.cs entry point, remove 35+ scattered calls"
```

---

### Task 3: 修复包版本不一致

**Files:**
- Modify: `src/ImeWlConverter.Formats/ImeWlConverter.Formats.csproj`

- [ ] **Step 1: 更新 DI 包版本**

修改 `src/ImeWlConverter.Formats/ImeWlConverter.Formats.csproj`：

```xml
<!-- 旧 -->
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="9.0.0"/>
<PackageReference Include="System.Text.Encoding.CodePages" Version="9.0.0"/>
```

改为：
```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.7"/>
```

注意：`System.Text.Encoding.CodePages` 在 .NET 10 中已内置，直接移除该包引用。

- [ ] **Step 2: 验证编译**

Run: `make build`
Expected: BUILD SUCCEEDED（如有 CodePages 相关编译错误，说明某些格式仍需要包，恢复引用并升级版本到 10.0.x）

- [ ] **Step 3: 验证测试**

Run: `make test`
Expected: 92 个测试通过

- [ ] **Step 4: Commit**

```bash
git add src/ImeWlConverter.Formats/ImeWlConverter.Formats.csproj
git commit -m "fix: align DI package to 10.0.7, remove unnecessary System.Text.Encoding.CodePages reference"
```

---

### Task 4: 用 BCL IProgress<T> 替换自定义 IProgressReporter

**Files:**
- Delete: `src/ImeWlConverter.Abstractions/Contracts/IProgressReporter.cs`
- Modify: `src/ImeWlConverter.Core/Pipeline/ConversionPipeline.cs`
- Modify: `src/ImeWlConverter.Abstractions/Models/ProgressInfo.cs`（确认 ProgressInfo record 保留）

- [ ] **Step 1: 检查 IProgressReporter 的使用者**

Run: `grep -rn "IProgressReporter" src/ --include="*.cs" | grep -v obj/`
Expected: 仅 ConversionPipeline.cs 和接口定义文件

- [ ] **Step 2: 删除 IProgressReporter 接口文件**

```bash
rm src/ImeWlConverter.Abstractions/Contracts/IProgressReporter.cs
```

- [ ] **Step 3: 修改 ConversionPipeline 使用 IProgress<ProgressInfo>**

修改 `src/ImeWlConverter.Core/Pipeline/ConversionPipeline.cs`：

```csharp
public sealed class ConversionPipeline : IConversionPipeline
{
    private readonly IEnumerable<IFormatImporter> _importers;
    private readonly IEnumerable<IFormatExporter> _exporters;
    private readonly IProgress<ProgressInfo>? _progress;

    public ConversionPipeline(
        IEnumerable<IFormatImporter> importers,
        IEnumerable<IFormatExporter> exporters,
        IProgress<ProgressInfo>? progress = null)
    {
        _importers = importers;
        _exporters = exporters;
        _progress = progress;
    }

    public async Task<Result<ConversionResult>> ExecuteAsync(
        ConversionRequest request,
        CancellationToken ct = default)
    {
        var importer = _importers.FirstOrDefault(i => i.Metadata.Id == request.InputFormatId);
        if (importer is null)
            return Result<ConversionResult>.Failure($"Unknown input format: {request.InputFormatId}");

        var exporter = _exporters.FirstOrDefault(e => e.Metadata.Id == request.OutputFormatId);
        if (exporter is null)
            return Result<ConversionResult>.Failure($"Unknown output format: {request.OutputFormatId}");

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

        // Filter (pass-through for now, wired in Task 8)
        var exportedCount = allEntries.Count;
        var filteredCount = importedCount - exportedCount;

        _progress?.Report(new ProgressInfo(0, 0, "Exporting..."));
        using var outputStream = File.Create(request.OutputPath);
        await exporter.ExportAsync(allEntries, outputStream, request.Options.Export, ct);

        return Result<ConversionResult>.Success(new ConversionResult
        {
            ImportedCount = importedCount,
            ExportedCount = exportedCount,
            FilteredCount = filteredCount
        });
    }
}
```

- [ ] **Step 4: 更新 ServiceCollectionExtensions**

移除对 `IProgressReporter` 的任何注册。

- [ ] **Step 5: 验证编译和测试**

Run: `make build && make test`
Expected: BUILD SUCCEEDED, 92 个测试通过

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor: replace custom IProgressReporter with BCL IProgress<ProgressInfo>"
```

---

## Phase 2: 接入新管道到 CLI 执行路径

本阶段目标：让 CLI 通过新 ConversionPipeline 执行转换，内部通过适配器桥接旧格式实现。

---

### Task 5: 创建 LegacyFilterAdapter 和 LegacyCodeGeneratorAdapter

**Files:**
- Create: `src/ImeWlConverter.Core/Adapters/LegacyFilterAdapter.cs`
- Create: `src/ImeWlConverter.Core/Adapters/LegacyBatchFilterAdapter.cs`
- Create: `src/ImeWlConverter.Core/Adapters/LegacyCodeGeneratorAdapter.cs`

- [ ] **Step 1: 创建 LegacyFilterAdapter**

```csharp
// src/ImeWlConverter.Core/Adapters/LegacyFilterAdapter.cs
using ImeWlConverter.Abstractions.Contracts;
using ImeWlConverter.Abstractions.Models;
using Studyzy.IMEWLConverter.Filters;

namespace ImeWlConverter.Core.Adapters;

/// <summary>
/// Adapts legacy ISingleFilter to new IWordFilter interface.
/// </summary>
public sealed class LegacyFilterAdapter : IWordFilter
{
    private readonly ISingleFilter _legacyFilter;

    public LegacyFilterAdapter(ISingleFilter legacyFilter)
    {
        _legacyFilter = legacyFilter;
    }

    public bool ShouldKeep(WordEntry entry)
    {
        // Legacy filter operates on WordLibrary; create a minimal shim
        var wl = new Studyzy.IMEWLConverter.Entities.WordLibrary
        {
            Word = entry.Word,
            Rank = entry.Rank,
            IsEnglish = entry.IsEnglish
        };
        return _legacyFilter.IsKeep(wl);
    }
}
```

- [ ] **Step 2: 创建 LegacyBatchFilterAdapter**

```csharp
// src/ImeWlConverter.Core/Adapters/LegacyBatchFilterAdapter.cs
using ImeWlConverter.Abstractions.Contracts;
using ImeWlConverter.Abstractions.Models;
using OldBatchFilter = Studyzy.IMEWLConverter.Filters.IBatchFilter;

namespace ImeWlConverter.Core.Adapters;

/// <summary>
/// Adapts legacy IBatchFilter to new IBatchFilter interface.
/// </summary>
public sealed class LegacyBatchFilterAdapter : IBatchFilter
{
    private readonly OldBatchFilter _legacyFilter;

    public LegacyBatchFilterAdapter(OldBatchFilter legacyFilter)
    {
        _legacyFilter = legacyFilter;
    }

    public IReadOnlyList<WordEntry> Filter(IReadOnlyList<WordEntry> entries)
    {
        // Convert to legacy WordLibraryList, filter, convert back
        var legacyList = new Studyzy.IMEWLConverter.Entities.WordLibraryList();
        foreach (var entry in entries)
        {
            legacyList.Add(new Studyzy.IMEWLConverter.Entities.WordLibrary
            {
                Word = entry.Word,
                Rank = entry.Rank,
                IsEnglish = entry.IsEnglish
            });
        }

        var filtered = _legacyFilter.Filter(legacyList);
        var result = new List<WordEntry>(filtered.Count);
        foreach (var wl in filtered)
        {
            result.Add(new WordEntry
            {
                Word = wl.Word,
                Rank = wl.Rank,
                IsEnglish = wl.IsEnglish,
                CodeType = LegacyImporterAdapter.MapCodeType(wl.CodeType)
            });
        }

        return result;
    }
}
```

- [ ] **Step 3: 创建 LegacyCodeGeneratorAdapter**

```csharp
// src/ImeWlConverter.Core/Adapters/LegacyCodeGeneratorAdapter.cs
using ImeWlConverter.Abstractions.Contracts;
using ImeWlConverter.Abstractions.Enums;
using ImeWlConverter.Abstractions.Models;
using Studyzy.IMEWLConverter.Generaters;

namespace ImeWlConverter.Core.Adapters;

/// <summary>
/// Adapts legacy IWordCodeGenerater to new ICodeGenerator interface.
/// </summary>
public sealed class LegacyCodeGeneratorAdapter : ICodeGenerator
{
    private readonly IWordCodeGenerater _legacyGenerator;

    public LegacyCodeGeneratorAdapter(IWordCodeGenerater legacyGenerator, CodeType supportedType)
    {
        _legacyGenerator = legacyGenerator;
        SupportedType = supportedType;
    }

    public CodeType SupportedType { get; }

    public bool Is1Char1Code => _legacyGenerator.Is1Char1Code;

    public WordCode GenerateCode(string word)
    {
        var codes = _legacyGenerator.GetCodeOfString(word);
        if (codes == null || codes.Count == 0)
            return new WordCode { Segments = [] };

        var segments = new List<IReadOnlyList<string>>(codes.Count);
        foreach (var segment in codes)
        {
            segments.Add(segment.ToList());
        }

        return new WordCode { Segments = segments };
    }
}
```

- [ ] **Step 4: 验证编译**

Run: `make build`
Expected: BUILD SUCCEEDED

- [ ] **Step 5: Commit**

```bash
git add src/ImeWlConverter.Core/Adapters/LegacyFilterAdapter.cs \
        src/ImeWlConverter.Core/Adapters/LegacyBatchFilterAdapter.cs \
        src/ImeWlConverter.Core/Adapters/LegacyCodeGeneratorAdapter.cs
git commit -m "feat: add LegacyFilterAdapter, LegacyBatchFilterAdapter, LegacyCodeGeneratorAdapter"
```

---

### Task 6: 将新管道接入 CLI 项目依赖

**Files:**
- Modify: `src/ImeWlConverterCmd/ImeWlConverterCmd.csproj`

- [ ] **Step 1: 添加项目引用**

在 `src/ImeWlConverterCmd/ImeWlConverterCmd.csproj` 的 `<ItemGroup>` 中添加：

```xml
<ItemGroup>
    <ProjectReference Include="..\ImeWlConverterCore\ImeWlConverterCore.csproj"/>
    <ProjectReference Include="..\ImeWlConverter.Core\ImeWlConverter.Core.csproj"/>
    <ProjectReference Include="..\ImeWlConverter.Abstractions\ImeWlConverter.Abstractions.csproj"/>
</ItemGroup>
```

注意：保留对 `ImeWlConverterCore` 的引用（旧核心通过适配器仍在使用）。不引用 `ImeWlConverter.Formats`，因为当前阶段通过适配器使用旧格式实现。

- [ ] **Step 2: 添加 DI 包引用**

```xml
<PackageReference Include="Microsoft.Extensions.DependencyInjection" Version="10.0.7"/>
```

- [ ] **Step 3: 验证编译**

Run: `make build`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add src/ImeWlConverterCmd/ImeWlConverterCmd.csproj
git commit -m "build: add Core/Abstractions project references and DI package to CLI"
```

---

### Task 7: 创建 PipelineHost（DI 容器组装）

**Files:**
- Create: `src/ImeWlConverterCmd/PipelineHost.cs`

- [ ] **Step 1: 创建 PipelineHost**

```csharp
// src/ImeWlConverterCmd/PipelineHost.cs
using ImeWlConverter.Abstractions.Contracts;
using ImeWlConverter.Abstractions.Models;
using ImeWlConverter.Core.Adapters;
using ImeWlConverter.Core.Pipeline;
using Microsoft.Extensions.DependencyInjection;
using Studyzy.IMEWLConverter;

namespace Studyzy.IMEWLConverter;

/// <summary>
/// Assembles the DI container and provides access to the conversion pipeline.
/// Bridges legacy format registrations into the new pipeline architecture.
/// </summary>
internal static class PipelineHost
{
    /// <summary>
    /// Build a configured ConversionPipeline using legacy format implementations
    /// wrapped in adapters.
    /// </summary>
    public static ConversionPipeline BuildPipeline(
        IDictionary<string, IWordLibraryImport> imports,
        IDictionary<string, IWordLibraryExport> exports,
        IDictionary<string, string> names,
        IProgress<ProgressInfo>? progress = null)
    {
        var importers = new List<IFormatImporter>();
        foreach (var kvp in imports)
        {
            var metadata = new FormatMetadata(
                kvp.Key,
                names.GetValueOrDefault(kvp.Key, kvp.Key),
                0,
                SupportsImport: true,
                SupportsExport: exports.ContainsKey(kvp.Key));
            importers.Add(new LegacyImporterAdapter(kvp.Value, metadata));
        }

        var exporterList = new List<IFormatExporter>();
        foreach (var kvp in exports)
        {
            var metadata = new FormatMetadata(
                kvp.Key,
                names.GetValueOrDefault(kvp.Key, kvp.Key),
                0,
                SupportsImport: imports.ContainsKey(kvp.Key),
                SupportsExport: true);
            exporterList.Add(new LegacyExporterAdapter(kvp.Value, metadata));
        }

        return new ConversionPipeline(importers, exporterList, progress);
    }
}
```

- [ ] **Step 2: 验证编译**

Run: `make build`
Expected: BUILD SUCCEEDED

- [ ] **Step 3: Commit**

```bash
git add src/ImeWlConverterCmd/PipelineHost.cs
git commit -m "feat: add PipelineHost to bridge legacy registrations into new ConversionPipeline"
```

---

### Task 8: 将 FilterPipeline 接入 ConversionPipeline

**Files:**
- Modify: `src/ImeWlConverter.Core/Pipeline/ConversionPipeline.cs`

- [ ] **Step 1: 修改 ConversionPipeline 接受 FilterPipeline**

```csharp
// src/ImeWlConverter.Core/Pipeline/ConversionPipeline.cs
using ImeWlConverter.Abstractions.Contracts;
using ImeWlConverter.Abstractions.Models;
using ImeWlConverter.Abstractions.Options;
using ImeWlConverter.Abstractions.Results;

namespace ImeWlConverter.Core.Pipeline;

public sealed class ConversionPipeline : IConversionPipeline
{
    private readonly IEnumerable<IFormatImporter> _importers;
    private readonly IEnumerable<IFormatExporter> _exporters;
    private readonly FilterPipeline? _filterPipeline;
    private readonly IProgress<ProgressInfo>? _progress;

    public ConversionPipeline(
        IEnumerable<IFormatImporter> importers,
        IEnumerable<IFormatExporter> exporters,
        IProgress<ProgressInfo>? progress = null,
        FilterPipeline? filterPipeline = null)
    {
        _importers = importers;
        _exporters = exporters;
        _progress = progress;
        _filterPipeline = filterPipeline;
    }

    public async Task<Result<ConversionResult>> ExecuteAsync(
        ConversionRequest request,
        CancellationToken ct = default)
    {
        var importer = _importers.FirstOrDefault(i => i.Metadata.Id == request.InputFormatId);
        if (importer is null)
            return Result<ConversionResult>.Failure($"Unknown input format: {request.InputFormatId}");

        var exporter = _exporters.FirstOrDefault(e => e.Metadata.Id == request.OutputFormatId);
        if (exporter is null)
            return Result<ConversionResult>.Failure($"Unknown output format: {request.OutputFormatId}");

        // 1. Import
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

        // 2. Filter
        IReadOnlyList<WordEntry> filtered = _filterPipeline is not null
            ? _filterPipeline.Apply(allEntries)
            : allEntries;

        var exportedCount = filtered.Count;
        var filteredCount = importedCount - exportedCount;

        // 3. Export
        _progress?.Report(new ProgressInfo(0, 0, "Exporting..."));
        using var outputStream = File.Create(request.OutputPath);
        await exporter.ExportAsync(filtered, outputStream, request.Options.Export, ct);

        return Result<ConversionResult>.Success(new ConversionResult
        {
            ImportedCount = importedCount,
            ExportedCount = exportedCount,
            FilteredCount = filteredCount
        });
    }
}
```

- [ ] **Step 2: 验证编译**

Run: `make build`
Expected: BUILD SUCCEEDED

- [ ] **Step 3: Commit**

```bash
git add src/ImeWlConverter.Core/Pipeline/ConversionPipeline.cs
git commit -m "feat: wire FilterPipeline into ConversionPipeline execution path"
```

---

### Task 9: 为新管道编写测试

**Files:**
- Create: `src/ImeWlConverterCoreTest/Pipeline/ConversionPipelineTest.cs`
- Create: `src/ImeWlConverterCoreTest/Pipeline/FilterPipelineTest.cs`
- Create: `src/ImeWlConverterCoreTest/Adapters/LegacyImporterAdapterTest.cs`

- [ ] **Step 1: 创建 FilterPipeline 测试**

```csharp
// src/ImeWlConverterCoreTest/Pipeline/FilterPipelineTest.cs
using ImeWlConverter.Abstractions.Contracts;
using ImeWlConverter.Abstractions.Models;
using ImeWlConverter.Core.Pipeline;
using Xunit;

namespace Studyzy.IMEWLConverter.Tests.Pipeline;

public class FilterPipelineTest
{
    [Fact]
    public void Apply_WithNoFilters_ReturnsAllEntries()
    {
        var pipeline = new FilterPipeline();
        var entries = new List<WordEntry>
        {
            new() { Word = "你好", Rank = 100 },
            new() { Word = "世界", Rank = 50 }
        };

        var result = pipeline.Apply(entries);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Apply_WithFilter_RemovesNonMatchingEntries()
    {
        var filter = new MinLengthFilter(2);
        var pipeline = new FilterPipeline(filters: [filter]);
        var entries = new List<WordEntry>
        {
            new() { Word = "好", Rank = 100 },
            new() { Word = "你好", Rank = 50 },
            new() { Word = "你好世界", Rank = 10 }
        };

        var result = pipeline.Apply(entries);

        Assert.Equal(2, result.Count);
        Assert.All(result, e => Assert.True(e.Word.Length >= 2));
    }

    private sealed class MinLengthFilter(int minLength) : IWordFilter
    {
        public bool ShouldKeep(WordEntry entry) => entry.Word.Length >= minLength;
    }
}
```

- [ ] **Step 2: 创建 LegacyImporterAdapter 测试**

```csharp
// src/ImeWlConverterCoreTest/Adapters/LegacyImporterAdapterTest.cs
using ImeWlConverter.Abstractions.Enums;
using ImeWlConverter.Core.Adapters;
using Xunit;
using OldCodeType = Studyzy.IMEWLConverter.Entities.CodeType;
using NewCodeType = ImeWlConverter.Abstractions.Enums.CodeType;

namespace Studyzy.IMEWLConverter.Tests.Adapters;

public class LegacyImporterAdapterTest
{
    [Theory]
    [InlineData(OldCodeType.Pinyin, NewCodeType.Pinyin)]
    [InlineData(OldCodeType.Wubi, NewCodeType.Wubi86)]
    [InlineData(OldCodeType.Wubi98, NewCodeType.Wubi98)]
    [InlineData(OldCodeType.Zhengma, NewCodeType.Zhengma)]
    [InlineData(OldCodeType.Cangjie, NewCodeType.Cangjie5)]
    [InlineData(OldCodeType.Zhuyin, NewCodeType.Zhuyin)]
    [InlineData(OldCodeType.English, NewCodeType.English)]
    [InlineData(OldCodeType.NoCode, NewCodeType.NoCode)]
    public void MapCodeType_MapsCorrectly(OldCodeType old, NewCodeType expected)
    {
        var result = LegacyImporterAdapter.MapCodeType(old);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(OldCodeType.Unknown)]
    [InlineData(OldCodeType.Yong)]
    [InlineData(OldCodeType.InnerCode)]
    [InlineData(OldCodeType.UserDefinePhrase)]
    public void MapCodeType_UnmappedValues_DefaultToPinyin(OldCodeType old)
    {
        var result = LegacyImporterAdapter.MapCodeType(old);
        Assert.Equal(NewCodeType.Pinyin, result);
    }
}
```

- [ ] **Step 3: 确保测试项目引用 Core**

检查 `src/ImeWlConverterCoreTest/ImeWlConverterCoreTest.csproj` 是否引用了 `ImeWlConverter.Core`，如未引用则添加：

```xml
<ProjectReference Include="..\ImeWlConverter.Core\ImeWlConverter.Core.csproj"/>
<ProjectReference Include="..\ImeWlConverter.Abstractions\ImeWlConverter.Abstractions.csproj"/>
```

- [ ] **Step 4: 运行新测试**

Run: `dotnet test src/ImeWlConverterCoreTest/ --filter "FullyQualifiedName~Pipeline|FullyQualifiedName~Adapters" -v n`
Expected: 全部通过

- [ ] **Step 5: 运行全部测试**

Run: `make test`
Expected: 测试总数增加，全部通过

- [ ] **Step 6: Commit**

```bash
git add src/ImeWlConverterCoreTest/
git commit -m "test: add unit tests for ConversionPipeline, FilterPipeline, and LegacyImporterAdapter"
```

---

### Task 10: 在 CLI 中添加 --use-pipeline 标志（渐进式切换）

**Files:**
- Modify: `src/ImeWlConverterCmd/CommandBuilder.cs`

- [ ] **Step 1: 添加 --use-pipeline 选项**

在 `CommandBuilder.cs` 中的 Command 定义处添加一个新选项：

```csharp
var usePipelineOption = new Option<bool>(
    name: "--use-pipeline",
    description: "使用新转换管道（实验性）",
    getDefaultValue: () => false);
```

- [ ] **Step 2: 在 handler 中分支调用**

在 `ExecuteConversion` 方法中，判断 `usePipeline` 标志：

```csharp
if (usePipeline)
{
    var (imports, exports, names) = FormatRegistrar.RegisterAll();
    var pipeline = PipelineHost.BuildPipeline(imports, exports, names);
    var request = new ConversionRequest
    {
        InputFormatId = options.InputFormat,
        OutputFormatId = options.OutputFormat,
        InputPaths = options.InputFiles,
        OutputPath = options.OutputPath
    };
    var result = await pipeline.ExecuteAsync(request);
    if (!result.IsSuccess)
    {
        Console.Error.WriteLine($"转换失败: {result.Error}");
        return;
    }
    Console.WriteLine($"转换完成: 导入 {result.Value.ImportedCount} 条, 导出 {result.Value.ExportedCount} 条");
}
else
{
    // 现有的 ConsoleRun 路径
    var run = new ConsoleRun(imports, exports);
    run.Execute(options);
}
```

- [ ] **Step 3: 验证编译**

Run: `make build`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: 验证旧路径不受影响**

Run: `make integration-test`
Expected: 24 个集成测试全部通过（它们不使用 --use-pipeline）

- [ ] **Step 5: 手动验证新路径**

Run: `dotnet run --project src/ImeWlConverterCmd -- -i scel -o ggpy --use-pipeline -f test_input.scel -t /tmp/test_output.txt`
Expected: 转换成功，输出文件有内容（使用项目中的测试数据文件）

- [ ] **Step 6: Commit**

```bash
git add src/ImeWlConverterCmd/CommandBuilder.cs
git commit -m "feat: add --use-pipeline flag for gradual migration to new ConversionPipeline"
```

---

## Phase 3: 统一枚举和模型

本阶段目标：消除新旧枚举的双重定义，建立单一真相源。

---

### Task 11: 统一 CodeType 枚举

**Files:**
- Modify: `src/ImeWlConverter.Abstractions/Enums/CodeType.cs`
- Modify: `src/ImeWlConverter.Core/Adapters/LegacyImporterAdapter.cs`

- [ ] **Step 1: 在新枚举中补全旧枚举值的映射**

当前 `LegacyImporterAdapter.MapCodeType` 中 `Unknown`、`Yong`、`InnerCode`、`UserDefinePhrase`、`ChaoqingYinxin` 映射到默认值 `Pinyin`，这会导致数据丢失。

修改新枚举，添加缺失的值：

```csharp
// src/ImeWlConverter.Abstractions/Enums/CodeType.cs
public enum CodeType
{
    Pinyin = 0,
    Wubi86,
    Wubi98,
    WubiNewAge,
    Zhengma,
    Cangjie5,
    TerraPinyin,
    Zhuyin,
    QingsongErbi,
    ChaoqiangErbi,
    XiandaiErbi,
    YinxingErbi,
    Chaoyin,
    English,
    UserDefine,
    NoCode,
    Phrase,
    Shuangpin,
    // 从旧枚举补全的值
    Yong,
    InnerCode,
    UserDefinePhrase,
    Unknown
}
```

- [ ] **Step 2: 更新映射消除默认值丢失**

```csharp
internal static NewCodeType MapCodeType(OldCodeType old) => old switch
{
    OldCodeType.Pinyin => NewCodeType.Pinyin,
    OldCodeType.Wubi => NewCodeType.Wubi86,
    OldCodeType.Wubi98 => NewCodeType.Wubi98,
    OldCodeType.WubiNewAge => NewCodeType.WubiNewAge,
    OldCodeType.Zhengma => NewCodeType.Zhengma,
    OldCodeType.Cangjie => NewCodeType.Cangjie5,
    OldCodeType.TerraPinyin => NewCodeType.TerraPinyin,
    OldCodeType.Zhuyin => NewCodeType.Zhuyin,
    OldCodeType.English => NewCodeType.English,
    OldCodeType.UserDefine => NewCodeType.UserDefine,
    OldCodeType.NoCode => NewCodeType.NoCode,
    OldCodeType.QingsongErbi => NewCodeType.QingsongErbi,
    OldCodeType.ChaoqiangErbi => NewCodeType.ChaoqiangErbi,
    OldCodeType.XiandaiErbi => NewCodeType.XiandaiErbi,
    OldCodeType.ChaoqingYinxin => NewCodeType.YinxingErbi,
    OldCodeType.Chaoyin => NewCodeType.Chaoyin,
    OldCodeType.Yong => NewCodeType.Yong,
    OldCodeType.InnerCode => NewCodeType.InnerCode,
    OldCodeType.UserDefinePhrase => NewCodeType.UserDefinePhrase,
    OldCodeType.Unknown => NewCodeType.Unknown,
    _ => NewCodeType.Pinyin
};
```

- [ ] **Step 3: 验证编译和测试**

Run: `make build && make test`
Expected: BUILD SUCCEEDED, 测试通过

- [ ] **Step 4: 更新 MapCodeType 测试**

更新 `LegacyImporterAdapterTest.cs` 中的 `MapCodeType_UnmappedValues_DefaultToPinyin` 测试，将 `Unknown`/`Yong`/`InnerCode` 改为正确映射断言。

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "fix: complete CodeType enum mapping, eliminate silent data loss on Unknown/Yong/InnerCode"
```

---

## Phase 4: macOS GUI 消除反射

---

### Task 12: macOS GUI 用显式注册替代反射

**Files:**
- Modify: `src/ImeWlConverterMac/ImeWlConverterMac.csproj`
- Modify: `src/ImeWlConverterMac/ViewModels/MainWindowViewModel.cs`
- Create: `src/ImeWlConverterMac/FormatRegistrar.cs`（复用 CLI 的模式）

- [ ] **Step 1: 添加项目引用（如需要）**

`ImeWlConverterMac.csproj` 已引用 `ImeWlConverterCore`。无需额外引用。

- [ ] **Step 2: 创建 macOS 版 FormatRegistrar**

从 CLI 的 `FormatRegistrar.cs` 复制或创建一个共享版本：

```csharp
// src/ImeWlConverterMac/FormatRegistrar.cs
// 与 CLI 版相同内容，或将 CLI 版移到 ImeWlConverterCore 中共享
```

更好的方案：将 `FormatRegistrar.cs` 移到 `ImeWlConverterCore` 项目中，CLI 和 GUI 都引用它。

- [ ] **Step 3: 移动 FormatRegistrar 到共享位置**

```bash
cp src/ImeWlConverterCmd/FormatRegistrar.cs src/ImeWlConverterCore/FormatRegistrar.cs
```

修改命名空间为 `Studyzy.IMEWLConverter`（已是），修改访问修饰符为 `public`。

更新 CLI 中删除旧文件，改为引用共享版。

- [ ] **Step 4: 替换 GUI 中的反射代码**

修改 `MainWindowViewModel.cs` 的 `LoadImeList()` 方法：

```csharp
private void LoadImeList()
{
    var (imports, exports, names) = FormatRegistrar.RegisterAll();

    var cbxImportItems = new List<ComboBoxShowAttribute>();
    var cbxExportItems = new List<ComboBoxShowAttribute>();

    foreach (var kvp in imports)
    {
        var displayName = names.GetValueOrDefault(kvp.Key, kvp.Key);
        cbxImportItems.Add(new ComboBoxShowAttribute(displayName, kvp.Key, 0));
        _imports[displayName] = kvp.Value;
    }

    foreach (var kvp in exports)
    {
        var displayName = names.GetValueOrDefault(kvp.Key, kvp.Key);
        cbxExportItems.Add(new ComboBoxShowAttribute(displayName, kvp.Key, 0));
        _exports[displayName] = kvp.Value;
    }

    cbxImportItems.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));
    cbxExportItems.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

    ImportFormatList = new ObservableCollection<string>(cbxImportItems.Select(x => x.Name));
    ExportFormatList = new ObservableCollection<string>(cbxExportItems.Select(x => x.Name));
}
```

- [ ] **Step 5: 移除 [RequiresUnreferencedCode] 属性**

由于不再使用反射，移除所有相关抑制属性。

- [ ] **Step 6: 验证编译**

Run: `dotnet build src/ImeWlConverterMac/`
Expected: BUILD SUCCEEDED, 无 trim 警告

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "refactor: eliminate reflection in macOS GUI, use shared FormatRegistrar for explicit format registration"
```

---

## Phase 5: 现代 C# 改进

---

### Task 13: 将 CommandLineOptions 改为 record

**Files:**
- Modify: `src/ImeWlConverterCore/CommandLineOptions.cs`

- [ ] **Step 1: 转换为 record**

```csharp
#nullable enable

namespace Studyzy.IMEWLConverter;

/// <summary>
/// 命令行选项的强类型模型（不可变）
/// </summary>
public sealed record CommandLineOptions
{
    /// <summary>输入词库格式代码（如 scel, ggpy, qqpy）</summary>
    public required string InputFormat { get; init; }

    /// <summary>输出词库格式代码（如 ggpy, rime, self）</summary>
    public required string OutputFormat { get; init; }

    /// <summary>输出文件路径或目录路径</summary>
    public required string OutputPath { get; init; }

    /// <summary>输入文件路径列表（支持多文件）</summary>
    public required IReadOnlyList<string> InputFiles { get; init; }

    /// <summary>编码映射文件路径（用于自定义编码）</summary>
    public string? CodeFile { get; init; }

    /// <summary>过滤条件字符串（如 "len:1-100|rm:eng"）</summary>
    public string? Filter { get; init; }

    /// <summary>自定义格式规范（如 "213, nyyn"）</summary>
    public string? CustomFormat { get; init; }

    /// <summary>词频生成器类型（llm, 或固定数字）</summary>
    public string? RankGenerator { get; init; }

    /// <summary>LLM API Endpoint</summary>
    public string? LlmEndpoint { get; init; }

    /// <summary>LLM API Key</summary>
    public string? LlmKey { get; init; }

    /// <summary>LLM Model Name</summary>
    public string? LlmModel { get; init; }

    /// <summary>多字词编码生成规则</summary>
    public string? MultiCode { get; init; }

    /// <summary>编码类型（pinyin, wubi, zhengma, cangjie, zhuyin）</summary>
    public string? CodeType { get; init; }

    /// <summary>目标操作系统（windows, macos, linux）</summary>
    public string? TargetOS { get; init; }

    /// <summary>Lingoes ld2 文件编码设置</summary>
    public string? Ld2Encoding { get; init; }
}
```

- [ ] **Step 2: 更新 CommandBuilder.cs 中的构造方式**

在构建 `CommandLineOptions` 的位置使用 object initializer 带 `required` 属性：

```csharp
var options = new CommandLineOptions
{
    InputFormat = inputFormat,
    OutputFormat = outputFormat,
    OutputPath = outputPath,
    InputFiles = inputFiles.ToList(),
    CodeFile = codeFile,
    Filter = filter,
    // ... 其余属性
};
```

- [ ] **Step 3: 更新 ConsoleRun.Execute 签名**

将 `List<string>` 改为 `IReadOnlyList<string>` 访问：

```csharp
// ConsoleRun.cs 中已经通过 options.InputFiles 访问，
// 只需确保循环用 foreach 而非 index 修改
```

- [ ] **Step 4: 验证编译和测试**

Run: `make build && make test`
Expected: BUILD SUCCEEDED, 测试通过

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "refactor: convert CommandLineOptions to sealed record with init-only properties"
```

---

### Task 14: 为旧格式类添加 sealed 修饰符

**Files:**
- Modify: `src/ImeWlConverterCore/IME/` 下所有格式类

- [ ] **Step 1: 找到所有非 sealed 的公共格式类**

Run: `grep -rn "^public class" src/ImeWlConverterCore/IME/ --include="*.cs" | grep -v "Base"`
Expected: 列出所有需要加 sealed 的类

- [ ] **Step 2: 批量添加 sealed**

对每个 `public class XxxFormat` 改为 `public sealed class XxxFormat`（排除任何有子类的基类如 `BaseTextImport`）。

使用 sed 命令批量替换：
```bash
find src/ImeWlConverterCore/IME -name "*.cs" -exec grep -l "^public class" {} \; | \
  xargs sed -i '' 's/^public class/public sealed class/g'
```

注意：`BaseTextImport`、`BaseImport` 等基类**不能**加 sealed。手动检查并恢复。

- [ ] **Step 3: 验证编译**

Run: `make build`
Expected: BUILD SUCCEEDED（如有继承错误说明某些类有子类，恢复其 sealed）

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor: seal all legacy IME format classes (no subclasses exist)"
```

---

### Task 15: 启用旧核心库 Nullable

**Files:**
- Modify: `src/ImeWlConverterCore/ImeWlConverterCore.csproj`

- [ ] **Step 1: 添加 nullable 启用**

在 `<PropertyGroup>` 中添加：
```xml
<Nullable>enable</Nullable>
```

- [ ] **Step 2: 编译并统计警告数**

Run: `dotnet build src/ImeWlConverterCore/ 2>&1 | grep -c "warning CS8"`
Expected: 记录警告总数（可能 100+）

- [ ] **Step 3: 修复关键实体类的 nullable 警告**

优先修复以下文件（影响面最大）：
- `Entities/WordLibrary.cs`
- `Entities/Code.cs`
- `CommandLineOptions.cs`（已在 Task 13 中修复）

对于其余文件，可以用 `#nullable disable` 文件级指令逐步启用。

- [ ] **Step 4: 对暂不修复的文件添加 pragma**

在顶部添加 `#nullable disable` 以抑制警告，后续逐步清理。

- [ ] **Step 5: 验证无编译错误**

Run: `make build`
Expected: BUILD SUCCEEDED（有 nullable 警告但无错误）

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "refactor: enable nullable reference types in ImeWlConverterCore, suppress legacy files"
```

---

## Phase 6: 项目结构清理

---

### Task 16: 删除新格式层中对旧核心的直接依赖

**Files:**
- Modify: `src/ImeWlConverter.Formats/ImeWlConverter.Formats.csproj`
- Modify: `src/ImeWlConverter.Formats/SougouScel/SougouScelImporter.cs`（及其他直接调用旧类的 importer）

- [ ] **Step 1: 找出哪些新格式文件直接使用旧命名空间**

Run: `grep -rn "using Studyzy.IMEWLConverter" src/ImeWlConverter.Formats/ --include="*.cs" | grep -v obj/`
Expected: 列出所有直接引用旧核心的文件

- [ ] **Step 2: 评估策略**

对于每个直接引用旧核心的格式文件：
- 如果逻辑简单（如 SougouScelImporter 仅委托给 `SougouPinyinScel.Import()`），保持委托但通过构造函数注入旧实例
- 如果逻辑复杂，先保持不动，后续 Phase 中逐步内联

- [ ] **Step 3: 对于当前阶段**

由于新格式层目前仍未被主路径使用（CLI 通过适配器走旧路径），此 Task 标记为 "评估完成，记录改进方向"。当 Phase 2 的 `--use-pipeline` 切换为默认后，再执行实际的旧依赖消除。

- [ ] **Step 4: 记录待办到代码注释**

在 `ImeWlConverter.Formats.csproj` 中添加注释：

```xml
<!-- TODO: Remove ImeWlConverterCore dependency after full pipeline migration -->
<ProjectReference Include="..\ImeWlConverterCore\ImeWlConverterCore.csproj"/>
```

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "docs: mark ImeWlConverter.Formats → ImeWlConverterCore dependency for future removal"
```

---

### Task 17: 统一命名空间风格

**Files:**
- Modify: `src/ImeWlConverter.Core/` 下所有文件（确保使用 `ImeWlConverter.Core.*`）
- 不修改旧核心的命名空间（破坏性太大）

- [ ] **Step 1: 确认新项目命名空间一致性**

Run: `grep -rn "^namespace" src/ImeWlConverter.Core/ src/ImeWlConverter.Abstractions/ src/ImeWlConverter.Formats/ --include="*.cs" | grep -v obj/ | sort -t: -k3`

检查是否有不一致的命名空间。

- [ ] **Step 2: 修复 iFlyIME 目录名大小写**

```bash
# 如果 git 配置为 case-sensitive
git mv src/ImeWlConverter.Formats/iFlyIME src/ImeWlConverter.Formats/IFlyIME
```

更新相关 namespace：`ImeWlConverter.Formats.IFlyIME`

- [ ] **Step 3: 验证编译**

Run: `make build`
Expected: BUILD SUCCEEDED

- [ ] **Step 4: Commit**

```bash
git add -A
git commit -m "refactor: fix iFlyIME directory casing to PascalCase convention"
```

---

## Phase 7: 最终验证

---

### Task 18: 全面验证

**Files:** 无修改

- [ ] **Step 1: 编译全部项目**

Run: `make build`
Expected: BUILD SUCCEEDED

- [ ] **Step 2: 运行单元测试**

Run: `make test`
Expected: 所有测试通过（含新增测试）

- [ ] **Step 3: 运行集成测试**

Run: `make build-cmd && make integration-test`
Expected: 24 个集成测试全部通过

- [ ] **Step 4: 验证新管道路径**

Run: `dotnet run --project src/ImeWlConverterCmd -- --use-pipeline -i ggpy -o rime tests/integration/data/google_pinyin_sample.txt -t /tmp/pipeline_output.txt && cat /tmp/pipeline_output.txt | head -5`
Expected: 输出有效的 Rime 格式内容

- [ ] **Step 5: 验证 macOS GUI 编译**

Run: `dotnet build src/ImeWlConverterMac/`
Expected: BUILD SUCCEEDED，无 trim 警告

- [ ] **Step 6: 运行 lint**

Run: `make lint`
Expected: 无格式错误

---

## 执行顺序总结

| Phase | Tasks | 风险 | 可独立验证 |
|-------|-------|------|-----------|
| Phase 1: 清理 | Task 1-4 | 极低 | ✅ 每步后 make test |
| Phase 2: 接入管道 | Task 5-10 | 中等 | ✅ --use-pipeline 不影响默认路径 |
| Phase 3: 枚举统一 | Task 11 | 低 | ✅ 映射测试覆盖 |
| Phase 4: GUI 去反射 | Task 12 | 中等 | ✅ GUI 独立编译验证 |
| Phase 5: 现代化 | Task 13-15 | 低 | ✅ 编译检查 |
| Phase 6: 结构清理 | Task 16-17 | 低 | ✅ 编译检查 |
| Phase 7: 验证 | Task 18 | 无 | ✅ 全面回归 |

---

## 后续工作（不在本计划范围内）

完成本计划后，代码库将处于以下状态：
1. 新管道可通过 `--use-pipeline` 使用，旧路径保持为默认
2. 所有死代码已清除
3. 枚举映射无数据丢失
4. GUI 不再使用反射

**下一步计划（另开）：**
- 将 `--use-pipeline` 设为默认，旧路径改为 `--legacy`
- 逐步将格式实现从旧 IME/ 迁移到新 Formats/（每次迁移一个格式+集成测试验证）
- 最终删除旧 `ImeWlConverterCore/IME/` 目录和 `ConsoleRun.cs`
- 最终删除 `ImeWlConverter.Formats` 对 `ImeWlConverterCore` 的项目引用
