namespace Dalamud.Interface.Spannables.Styles;

/// <summary>Describe a word break mode.</summary>
public enum WordBreakType : byte
{
    /// <summary>Use the default line break rule.</summary>
    Normal,

    /// <summary>Insert word breaks between any two characters.</summary>
    BreakAll,

    /// <summary>Never break words.</summary>
    KeepAll,

    /// <summary>
    /// Insert word breaks between any two characters if the line still overflows under the default word break rule.
    /// </summary>
    BreakWord,
}
