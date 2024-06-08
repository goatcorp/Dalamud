using Dalamud.Game.Text.SeStringHandling;

namespace Dalamud.Game.Gui.Nameplates.Model;

/// <summary>
/// Represents a nameplate by providing some information about the visual look.
/// </summary>
public class NameplateInfo
{
    /// <summary>
    /// Gets or sets the title text of the Nameplate.
    /// </summary>
    public SeString Title { get; set; }

    /// <summary>
    /// Gets or sets the name text of the Nameplate.
    /// </summary>
    public SeString Name { get; set; }

    /// <summary>
    /// Gets or sets the free company text.
    /// </summary>
    public SeString FreeCompany { get; set; }

    /// <summary>
    /// Gets or sets the prefix text. Mostly used for the job name shortcuts or some status icons that can be shown within a text.
    /// </summary>
    public SeString Prefix { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the title is shown above the name.
    /// <br/>If the value is true, the title is shown above the name. Otherwise the title is shown below the name.
    /// </summary>
    public bool IsTitleAboveName { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the title is visible.
    /// <br/>If the title value is true, the title is visible. Otherwise the title text is not visible and ignored.
    /// </summary>
    public bool IsTitleVisible { get; set; }

    /// <summary>
    /// Gets or sets the icon shown on the nameplate. Mostly used for the status icon.
    /// </summary>
    public StatusIcons IconID { get; set; }
}
