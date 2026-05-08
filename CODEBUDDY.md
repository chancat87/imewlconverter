
## 快速定位

> 这是你进入项目时最先需要知道的信息。根据任务类型，跳转到对应章节。

| 你想做什么 | 入口文件/目录 |
|-----------|-------------|
| 添加新的输入法格式支持 | `src/ImeWlConverter.Formats/{Format}/` → 创建 Importer + Exporter |
| 修改转换管道逻辑 | `src/ImeWlConverterCore/MainBody.cs` (旧) 或 `src/ImeWlConverter.Core/Pipeline/` (新) |
| 修改 CLI 参数/行为 | `src/ImeWlConverterCmd/CommandBuilder.cs` |
| 修改编码生成（拼音/五笔等） | `src/ImeWlConverterCore/Generaters/` |
| 修改过滤器 | `src/ImeWlConverterCore/Filters/` |
| 修改 macOS GUI | `src/ImeWlConverterMac/ViewModels/MainWindowViewModel.cs` |
| 添加/修改单元测试 | `src/ImeWlConverterCoreTest/` |
| 修改 CI 流程 | `.github/workflows/ci.yml` |
| 发布新版本 | `docs/RELEASING.md` |

## 项目一句话描述

深蓝词库转换（IME WL Converter）是一个跨平台的输入法词库格式转换工具，支持 50+ 种输入法格式之间的相互转换。

- **语言**: C# / .NET 10.0
- **测试框架**: xUnit 2.9.3
- **构建系统**: Makefile + dotnet CLI
- **版本管理**: MinVer（从 Git tag 自动生成）
- **平台**: Windows / Linux / macOS

## 日常命令

```bash
make build              # 构建所有项目
make test               # 运行单元测试 (92个)
make integration-test   # 运行集成测试 (24个，需先 make build-cmd)
make lint               # 检查代码格式
make format             # 自动格式化代码
make run-cmd            # 运行 CLI 工具
make run-mac            # 运行 macOS GUI
```

## 架构概览

```
src/
├── ImeWlConverter.Abstractions/     # 接口层（零依赖）
├── ImeWlConverter.Core/             # 业务服务层
├── ImeWlConverter.Formats/          # 格式实现层（86个文件，50+种格式）
├── ImeWlConverter.SourceGenerators/ # Source Generator（编译时格式注册）
├── ImeWlConverterCore/              # 遗留核心库（转换主逻辑仍在此）
├── ImeWlConverterCmd/               # CLI 入口
├── ImeWlConverterMac/               # macOS GUI (Avalonia 11.2.3)
└── ImeWlConverterCoreTest/          # xUnit 单元测试
```

### 数据流

```
CLI 参数 → CommandBuilder → ConsoleRun → MainBody.Convert()
                                              │
                              Import ─→ Filter ─→ CodeGen ─→ Export
                              (旧接口)                        (旧接口)
```

**当前状态**：CLI 通过 `FormatRegistrar`（无反射）注册所有格式，调用 `ConsoleRun.Execute()` → `MainBody.Convert()`。新架构的 `IConversionPipeline` 就绪但尚未接管主路径。

---

## 深入：项目层次

以下按从外到内的顺序描述各层，你只需要读到与任务相关的层即可。

### Layer 1: CLI 入口 (`src/ImeWlConverterCmd/`)

| 文件 | 职责 |
|------|------|
| `Program.cs` | 入口，旧参数格式检测，调用 CommandBuilder |
| `CommandBuilder.cs` | System.CommandLine 定义所有 CLI 选项，调用 `ExecuteConversion` |
| `FormatRegistrar.cs` | 显式注册所有 50+ 种格式（替代反射），返回 imports/exports 字典 |

### Layer 2: 转换协调 (`src/ImeWlConverterCore/`)

| 文件 | 职责 | 行数 |
|------|------|------|
| `ConsoleRun.cs` | 接受 CLI 选项 → 配置 importer/exporter/filter → 调用 MainBody | ~430 |
| `MainBody.cs` | 转换管道主体：Import → Filter → ChineseConvert → CodeGen → Export | ~840 |
| `CommandLineOptions.cs` | CLI 选项 DTO |
| `ConstantString.cs` | 所有格式的 ID 常量和显示名 |

### Layer 3: 格式实现

**旧架构**（仍在使用）：`src/ImeWlConverterCore/IME/` — 每个格式一个类，同时实现 Import + Export

**新架构**（已建好）：`src/ImeWlConverter.Formats/` — 每个格式拆分为独立的 Importer 和 Exporter

| 基类 | 用途 | 位置 |
|------|------|------|
| `TextFormatImporter` | 文本格式导入（逐行解析） | `Formats/Shared/` |
| `TextFormatExporter` | 文本格式导出（逐行生成） | `Formats/Shared/` |
| `BinaryFormatImporter` | 二进制格式导入 | `Formats/Shared/` |

### Layer 4: 核心基础设施

| 目录 | 内容 |
|------|------|
| `Generaters/` | 编码生成器（拼音、五笔86/98、郑码、仓颉、注音等） |
| `Filters/` | ISingleFilter / IBatchFilter / IReplaceFilter 实现 |
| `Helpers/` | 文件操作、编码检测、拼音字典、HTTP |
| `Entities/` | WordLibrary, Code, ParsePattern, FilterConfig |
| `Language/` | 简繁转换 |
| `Resources/` | 嵌入式拼音/五笔/注音码表 |

### Layer 5: 新抽象层 (`src/ImeWlConverter.Abstractions/`)

纯接口和 DTO，零依赖。供新架构的所有层引用。

| 子目录 | 关键类型 |
|--------|---------|
| `Contracts/` | IFormatImporter, IFormatExporter, IConversionPipeline, ICodeGenerator, IWordFilter, ILlmClient |
| `Models/` | WordEntry (sealed record), WordCode, FormatMetadata, ProgressInfo |
| `Options/` | ConversionOptions, FilterOptions, CodeGenerationOptions |
| `Results/` | Result\<T\>, ImportResult, ExportResult |
| `Enums/` | CodeType, SortType, PinyinType |

---

## 常见任务操作指南

### 添加新的输入法格式

```bash
# 1. 在 Formats 项目中创建目录和文件
mkdir src/ImeWlConverter.Formats/MyFormat/
```

```csharp
// 2. 创建 Importer
[FormatPlugin("myf", "我的格式", 500)]
public sealed class MyFormatImporter : TextFormatImporter
{
    protected override Encoding FileEncoding => new UTF8Encoding(false);
    public override FormatMetadata Metadata { get; } = new("myf", "我的格式", 500, true, false);
    protected override IEnumerable<WordEntry> ParseLine(string line) { /* 解析逻辑 */ }
}
```

```csharp
// 3. 创建 Exporter
[FormatPlugin("myf", "我的格式", 500)]
public sealed class MyFormatExporter : TextFormatExporter
{
    protected override Encoding FileEncoding => new UTF8Encoding(false);
    public override FormatMetadata Metadata { get; } = new("myf", "我的格式", 500, false, true);
    protected override string? FormatEntry(WordEntry entry) { /* 导出逻辑 */ }
}
```

```csharp
// 4. 在 FormatRegistrar.cs 中注册（CLI 路径）
Register(imports, exports, "myf", new MyLegacyClass()); // 如果有旧实现
```

```csharp
// 5. 在旧 IME/ 目录中也创建对应类（当前 CLI 通过旧接口转换）
[ComboBoxShow(ConstantString.MY_FORMAT, ConstantString.MY_FORMAT_C, 500)]
public class MyFormat : BaseTextImport, IWordLibraryExport, IWordLibraryTextImport { ... }
```

### 修改过滤逻辑

过滤器位于 `src/ImeWlConverterCore/Filters/`，实现 `ISingleFilter` 接口：

```csharp
public interface ISingleFilter { bool IsKeep(WordLibrary wl); }
```

CLI 通过 `--filter` 参数启用过滤：`-f "len:2-10|rm:eng|rm:num"`

### 修改编码生成

编码生成器位于 `src/ImeWlConverterCore/Generaters/`，实现 `IWordCodeGenerater` 接口。由 `CodeTypeHelper.GetGenerater(CodeType)` 工厂方法获取实例。

---

## 项目约定

### 禁止操作

- **禁止手动修改版本号**（由 MinVer 从 Git tag 生成，配置在 `src/Directory.Build.props`）
- **禁止使用运行时反射注册格式**（CLI 已改用 `FormatRegistrar` 显式注册）
- **禁止硬编码路径分隔符**（用 `Path.Combine()`）

### 编码处理

项目处理多种字符编码（UTF-8、GBK、GB2312、Big5、Unicode）。.NET 10 已内置 CodePages 支持，但部分旧代码仍调用：
```csharp
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
```

### 跨平台注意事项

- CLI 工具为 framework-dependent（需 .NET 运行时）
- macOS app bundle 为 self-contained（包含运行时）
- 集成测试支持 Linux、macOS、Windows (Git Bash)

### 测试

- **单元测试**: xUnit 2.9.3，位于 `src/ImeWlConverterCoreTest/`
- **集成测试**: shell 脚本框架，位于 `tests/integration/`
- 测试串行执行（`xunit.runner.json` 中 `parallelizeTestCollections: false`）
- `[Fact(Skip = "...")]` 标记按需运行的慢速测试

### CI/CD

GitHub Actions (`.github/workflows/ci.yml`)：
1. Lint（格式检查，快速失败）
2. Build + 单元测试 (Ubuntu)
3. 多平台构建 (Win x64/x86, Linux x64/arm64, macOS x64/arm64)
4. 集成测试 (Linux + macOS)

---

## 关键决策记录

| 决策 | 原因 |
|------|------|
| 保留旧 MainBody 转换路径 | 50+ 种格式的精确行为需要集成测试全部通过才能切换 |
| CLI 用 FormatRegistrar 显式注册 | 消除反射，支持 AOT/Trimming |
| Source Generator 注册新格式 | AI 添加格式只需加 `[FormatPlugin]` 属性 |
| Adapter 模式新旧共存 | 渐进式迁移，每步可验证 |
| 测试串行执行 | 编码生成器有静态字典状态，并行会竞态 |
| sealed record 实体 | 不可变性保证，AI 不会意外修改中间状态 |
