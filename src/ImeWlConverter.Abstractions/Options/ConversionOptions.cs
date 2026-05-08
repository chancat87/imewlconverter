using ImeWlConverter.Abstractions.Enums;

namespace ImeWlConverter.Abstractions.Options;

/// <summary>Options for the entire conversion pipeline.</summary>
public sealed class ConversionOptions
{
    /// <summary>Import options.</summary>
    public ImportOptions Import { get; init; } = new();

    /// <summary>Export options.</summary>
    public ExportOptions Export { get; init; } = new();

    /// <summary>Filter options.</summary>
    public FilterOptions Filter { get; init; } = new();

    /// <summary>Code generation options.</summary>
    public CodeGenerationOptions CodeGeneration { get; init; } = new();

    /// <summary>Chinese simplified/traditional conversion mode.</summary>
    public ChineseConversionMode ChineseConversion { get; init; } = ChineseConversionMode.None;
}

/// <summary>Chinese conversion modes.</summary>
public enum ChineseConversionMode
{
    /// <summary>No conversion.</summary>
    None = 0,

    /// <summary>Convert simplified Chinese to traditional.</summary>
    SimplifiedToTraditional,

    /// <summary>Convert traditional Chinese to simplified.</summary>
    TraditionalToSimplified
}
