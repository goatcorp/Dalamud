namespace Dalamud.Utility;

/// <summary>
/// Utility class for marking something to be changed for API 10, for ease of lookup.
/// </summary>
[AttributeUsage(AttributeTargets.All, Inherited = false)]
internal sealed class Api10ToDoAttribute : Attribute
{
    /// <summary>
    /// Marks that this exists purely for making API 9 plugins work.
    /// </summary>
    public const string DeleteCompatBehavior = "Delete. This is for making API 9 plugins work.";

    /// <summary>
    /// Initializes a new instance of the <see cref="Api10ToDoAttribute"/> class.
    /// </summary>
    /// <param name="what">The explanation.</param>
    public Api10ToDoAttribute(string what) => _ = what;
}
