
## Project Overview

IME WL Converter (深蓝词库转换) is a cross-platform dictionary converter for Input Method Editors (IMEs). It converts dictionary files between 20+ different IME formats including Sougou Pinyin, QQ Pinyin, Rime, Google Pinyin, Baidu Pinyin, and many others.

**Language**: C# (.NET 10.0)
**License**: GPL-3.0
**Platforms**: Windows, Linux, macOS

## Common Commands

### Build Commands

```bash
# Restore NuGet packages
make restore

# Build all projects (Debug mode)
make build

# Build Release configuration
make build-release

# Build command-line tool only
make build-cmd

# Build macOS GUI version
make build-mac
```

### Testing

```bash
# Run unit tests
make test

# Run unit tests with verbose output
make test-verbose

# Run integration tests (requires CLI built first)
make integration-test

# Run integration tests with debug output
cd tests/integration
./run-tests.sh --all -v
```

### Running the Application

```bash
# Run CLI tool
make run-cmd

# Run macOS GUI
make run-mac

# Run CLI directly with dotnet
dotnet run --project src/ImeWlConverterCmd
```

### Code Quality

```bash
# Format code
make format

# Check code formatting (CI)
make lint
```

## High-Level Architecture

### Core Components

The codebase follows a plugin-style architecture with clear separation of concerns:

1. **ImeWlConverterCore** - Core library containing all conversion logic
2. **ImeWlConverterCmd** - Command-line interface
3. **ImeWlConverterMac** - macOS GUI application (Avalonia UI)
4. **IME WL Converter Win** - Windows GUI application (WinForms/WPF)
5. **ImeWlConverterCoreTest** - Unit tests

### Conversion Pipeline

```
Input File → Import → Filters → Code Generation → Export → Output File
```

**Key Classes:**

- `MainBody` - Orchestrates the entire conversion pipeline
- `ConsoleRun` - Handles CLI argument parsing and execution
- `IWordLibraryImport` - Interface for all input format parsers
- `IWordLibraryExport` - Interface for all output format generators
- `WordLibrary` - Core data structure representing a dictionary entry (word + code + rank)
- `WordLibraryList` - Collection of WordLibrary entries

### Directory Structure

```
src/ImeWlConverterCore/
├── IME/              # Input format parsers (20+ formats)
├── Generaters/       # Output format generators
├── Filters/          # Word filtering logic
├── Helpers/          # Utility functions
├── Entities/         # Data models (WordLibrary, CodeType, etc.)
├── MainBody.cs       # Core orchestration logic
└── ConsoleRun.cs     # CLI argument handling
```

### Adding New Format Support

To add support for a new IME format:

1. **For Import**: Create a new class in `src/ImeWlConverterCore/IME/` that implements `IWordLibraryImport`
   - Inherit from `BaseImport` or `BaseTextImport` for common functionality
   - Implement `Import(string path)` to parse the file format
   - Add the format to the import registry in `ConsoleRun.LoadImeList()`

2. **For Export**: Create a new class that implements `IWordLibraryExport`
   - Implement `Export(WordLibraryList)` to generate output
   - Implement `ExportLine(WordLibrary)` for line-by-line export
   - Add the format to the export registry in `ConsoleRun.LoadImeList()`

3. Add the format constant to `ConstantString.cs` with a short code (e.g., `SOUGOU_XIBAO_SCEL_C = "scel"`)

### New Architecture (refactor branch)

The codebase is being modernized with a clean layered architecture:

```
src/
├── ImeWlConverter.Abstractions/     # Zero-dependency interface layer
│   ├── Contracts/                   # IFormatImporter, IFormatExporter, IConversionPipeline, etc.
│   ├── Models/                      # WordEntry, WordCode, FormatMetadata (sealed records)
│   ├── Options/                     # ConversionOptions, FilterOptions, etc.
│   ├── Results/                     # Result<T>, ImportResult, ExportResult
│   └── Enums/                       # CodeType, SortType, PinyinType
├── ImeWlConverter.Core/             # Business logic services
│   ├── Pipeline/                    # ConversionPipeline, FilterPipeline
│   ├── CodeGeneration/              # CodeGenerationService
│   ├── Adapters/                    # Legacy ↔ New interface bridges
│   └── Utilities/                   # NumberToChineseConverter
├── ImeWlConverter.Formats/          # All IME format implementations (86 files)
│   ├── Shared/                      # TextFormatImporter/Exporter base classes
│   ├── GooglePinyin/                # GooglePinyinImporter + GooglePinyinExporter
│   ├── Rime/                        # RimeImporter + RimeExporter
│   └── ... (40+ format directories)
├── ImeWlConverter.SourceGenerators/ # Compile-time format registration
├── ImeWlConverterCore/              # Legacy core library (still in use)
├── ImeWlConverterCmd/               # CLI entry point
├── ImeWlConverterMac/               # macOS GUI (Avalonia)
└── ImeWlConverterCoreTest/          # xUnit tests
```

**Key Design Principles:**
- **AI-friendly**: Each file < 300 lines, explicit dependencies, sealed records
- **No runtime reflection**: CLI uses FormatRegistrar for explicit format registration
- **Source Generator**: [FormatPlugin] attribute enables compile-time format discovery
- **Adapter pattern**: Legacy code coexists with new interfaces via adapters

**Adding a new format (new architecture):**
1. Create `{Format}Importer.cs` extending `TextFormatImporter` or `BinaryFormatImporter`
2. Create `{Format}Exporter.cs` extending `TextFormatExporter`
3. Add `[FormatPlugin("code", "Display Name", sortOrder)]` attribute
4. Add the format to `FormatRegistrar.cs` in the CLI project
5. Source Generator auto-generates DI registration

## 项目约定

### 版本号管理

**重要**：项目使用自动化版本号生成机制，版本号从 Git tag 自动生成。

- **禁止手动修改**：不要手动修改以下位置的版本号：
  - `src/ImeWlConverterCore/ConstantString.cs` 中的 `VERSION` 字段
  - 任何 `.csproj` 文件中的 `<Version>` 标签
  
- **版本号来源**：版本号由 MinVer 从 Git tag 自动生成
  - 格式：`vX.Y.Z` → `X.Y.Z.0`
  - 配置文件：`src/Directory.Build.props`
  
- **发布新版本**：创建并推送 Git tag
  ```bash
  git tag v3.4.0
  git push origin v3.4.0
  ```

- **非 Git 环境构建**：在发行版打包系统等非 Git 环境中构建时
  ```bash
  export PACKAGE_VERSION=3.3.1
  dotnet build
  ```

详见 [RELEASING.md](docs/RELEASING.md) 了解完整的发布流程。

### 字符编码

代码库处理多种编码（UTF-8、GBK、GB2312、Big5）。必须注册 CodePagesEncodingProvider：

```csharp
Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
```

### 跨平台注意事项

- 使用 `Path.Combine()` 拼接文件路径，禁止硬编码路径分隔符
- CLI 工具为 framework-dependent（需要 .NET 运行时）
- macOS app bundle 包含完整 .NET 运行时（self-contained）
- 集成测试支持 Linux、macOS 和 Windows (Git Bash)

## Integration Tests

项目使用基于 shell 的集成测试框架，位于 `tests/integration/`。

**Test Framework**: xUnit 2.9.3 (migrated from NUnit in refactor branch)

### Test Structure

- `test-cases/` - 按格式或功能组织的测试用例定义
  - `1-imports/` - 各种格式导入到 CSV 的测试
  - `2-exports/` - CSV 导出到各种格式的测试
  - `3-advanced/` - 高级功能（过滤器、编码、大文件）
- 每个测试套件有一个 `test-config.yaml` 定义测试用例
- 测试数据来源于 `src/ImeWlConverterCoreTest/Test/`（与单元测试共享）

### Running Integration Tests

```bash
# Run all tests
cd tests/integration
./run-tests.sh --all

# Run specific suite
./run-tests.sh -s 1-imports

# Run with verbose output
./run-tests.sh -s 1-imports -v

# Keep output files for debugging
./run-tests.sh -s 1-imports --keep-output

# Generate JUnit XML report (for CI)
./run-tests.sh --all --xml
```

## CI/CD Workflow

GitHub Actions workflow (`.github/workflows/ci.yml`):

1. **Lint** - Code formatting check (fast-fail)
2. **Build and Test** - Build + unit tests on Ubuntu
3. **Platform Builds** - Parallel builds for Windows (x64/x86), Linux (x64/arm64), macOS (x64/arm64)
4. **Integration Tests** - Run on Linux and macOS builds
5. Artifacts retained for 7-30 day