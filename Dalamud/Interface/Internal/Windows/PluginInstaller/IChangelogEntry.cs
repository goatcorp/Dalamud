namespace Dalamud.Interface.Internal.Windows.PluginInstaller;

/// <summary>
/// Class representing a changelog entry.
/// </summary>
internal interface IChangelogEntry
{
    /// <summary>
    /// Gets the title of the entry.
    /// </summary>
    string Title { get; }

    /// <summary>
    /// Gets the version this entry applies to.
    /// </summary>
    string Version { get; }

    /// <summary>
    /// Gets the text of the entry.
    /// </summary>
    string Text { get; }

    /// <summary>
    /// Gets the author of the changelog.
    /// </summary>
    string? Author { get; }

    /// <summary>
    /// Gets the date of the entry.
    /// </summary>
    DateTime Date { get; }
}
