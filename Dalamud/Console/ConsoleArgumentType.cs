namespace Dalamud.Console;

/// <summary>
/// Possible console argument types.
/// </summary>
internal enum ConsoleArgumentType
{
    /// <summary>
    /// A regular string.
    /// </summary>
    String,
    
    /// <summary>
    /// A signed integer.
    /// </summary>
    Integer,
    
    /// <summary>
    /// A floating point value.
    /// </summary>
    Float,
    
    /// <summary>
    /// A boolean value.
    /// </summary>
    Bool,
}
