namespace Dalamud.Plugin.SelfTest.Internal;

/// <summary>
/// Represents a self-test group with its loaded/unloaded state.
/// </summary>
internal class SelfTestGroup
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SelfTestGroup"/> class.
    /// </summary>
    /// <param name="name">The name of the test group.</param>
    /// <param name="loaded">Whether the group is currently loaded.</param>
    public SelfTestGroup(string name, bool loaded = true)
    {
        this.Name = name;
        this.Loaded = loaded;
    }

    /// <summary>
    /// Gets the name of the test group.
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// Gets or sets a value indicating whether this test group is currently loaded.
    /// </summary>
    public bool Loaded { get; set; }
}
