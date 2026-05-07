namespace ImeWlConverter.Abstractions.Options;

/// <summary>Options for code generation during conversion.</summary>
public sealed class CodeGenerationOptions
{
    /// <summary>Keep English characters in generated code.</summary>
    public bool KeepEnglishInCode { get; init; }

    /// <summary>Keep numbers in generated code.</summary>
    public bool KeepNumberInCode { get; init; }

    /// <summary>Keep punctuation in generated code.</summary>
    public bool KeepPunctuationInCode { get; init; }

    /// <summary>Convert full-width characters to half-width.</summary>
    public bool ConvertFullWidth { get; init; }

    /// <summary>Translate numbers to Chinese characters in code.</summary>
    public bool TranslateNumbersToChinese { get; init; }

    /// <summary>Prefix English words with underscore in code.</summary>
    public bool PrefixEnglishWithUnderscore { get; init; }
}
