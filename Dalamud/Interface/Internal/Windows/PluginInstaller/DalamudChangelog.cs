using System.Collections.Generic;

namespace Dalamud.Interface.Internal.Windows.PluginInstaller;

/// <summary>
/// Class representing a Dalamud changelog.
/// </summary>
internal class DalamudChangelog
{
    /// <summary>
    /// Gets the date of the version.
    /// </summary>
    public DateTime Date { get; init; }

    /// <summary>
    /// Gets the relevant version number.
    /// </summary>
    public string Version { get; init; }

    /// <summary>
    /// Gets the list of changes.
    /// </summary>
    public List<DalamudChangelogChange> Changes { get; init; }

    /// <summary>
    /// Class representing the relevant changes.
    /// </summary>
    public class DalamudChangelogChange
    {
        /// <summary>
        /// Gets the commit message.
        /// </summary>
        public string Message { get; init; }

        /// <summary>
        /// Gets the commit author.
        /// </summary>
        public string Author { get; init; }

        /// <summary>
        /// Gets the commit reference SHA.
        /// </summary>
        public string Sha { get; init; }

        /// <summary>
        /// Gets the commit datetime.
        /// </summary>
        public DateTime Date { get; init; }
    }
}
