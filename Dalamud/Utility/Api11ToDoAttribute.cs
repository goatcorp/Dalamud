namespace Dalamud.Utility;

/// <summary>
/// Utility class for marking something to be changed for API 10, for ease of lookup.
/// </summary>
[AttributeUsage(AttributeTargets.All, Inherited = false)]
internal sealed class Api11ToDoAttribute : Attribute
{
    /// <summary>
    /// Marks that this exists purely for making API 9 plugins work.
    /// </summary>
    public const string DeleteCompatBehavior = "Delete. This is for making API 9 plugins work.";

    /// <summary>
    /// Marks that this should be moved to another namespace.
    /// </summary>
    public const string MoveNamespace = "Move to another namespace.";

    /// <summary>
    /// Marks that this should be made internal.
    /// </summary>
    public const string MakeInternal = "Make internal.";

    /// <summary>
    /// Initializes a new instance of the <see cref="Api11ToDoAttribute"/> class.
    /// </summary>
    /// <param name="what">The explanation.</param>
    /// <param name="what2">The explanation 2.</param>
    public Api11ToDoAttribute(string what, string what2 = "")
    {
        _ = what;
        _ = what2;
    }
}
