namespace Dalamud.Utility;

/// <summary>
/// Utility class for marking something to be changed for API 11, for ease of lookup.
/// </summary>
[AttributeUsage(AttributeTargets.All, Inherited = false)]
internal sealed class Api11ToDoAttribute : Attribute
{
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
