namespace Dalamud.Utility;

/// <summary>
/// Utility class for marking something to be changed for API 13, for ease of lookup.
/// </summary>
[AttributeUsage(AttributeTargets.All, Inherited = false)]
internal sealed class Api14ToDoAttribute : Attribute
{
    /// <summary>
    /// Marks that this should be made internal.
    /// </summary>
    public const string MakeInternal = "Make internal.";

    /// <summary>
    /// Marks that this should be removed entirely.
    /// </summary>
    public const string Remove = "Remove.";

    /// <summary>
    /// Initializes a new instance of the <see cref="Api14ToDoAttribute"/> class.
    /// </summary>
    /// <param name="what">The explanation.</param>
    /// <param name="what2">The explanation 2.</param>
    public Api14ToDoAttribute(string what, string what2 = "")
    {
        _ = what;
        _ = what2;
    }
}
