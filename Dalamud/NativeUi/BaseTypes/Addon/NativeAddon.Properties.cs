using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Logging.Internal;

using Lumina.Text.ReadOnly;

namespace Dalamud.NativeUi.BaseTypes.Addon;

/// <summary>
/// .
/// </summary>
internal unsafe partial class NativeAddon
{
    /// <summary>
    /// Gets or inits the addons internal name.
    /// </summary>
    /// <remarks>
    /// Names are limited to 31 characters.
    /// </remarks>
    public required string InternalName
    {
        get;
        init => field = new string(value.Replace(" ", string.Empty).Take(31).ToArray());
    }

    /// <summary>
    /// Gets or sets the addons main title string.
    /// </summary>
    public required ReadOnlySeString Title { get; set; }

    /// <summary>
    /// Gets or sets the addons subtitle string.
    /// </summary>
    public ReadOnlySeString? Subtitle { get; set; }

    /// <summary>
    /// Gets or sets sound effect to play when opening or closing this addon.
    /// </summary>
    public int OpenWindowSoundEffectId { get; set; } = 23;

    /// <summary>
    /// Gets or sets this addons size, defaults to 400px by 400px.
    /// </summary>
    public Vector2 Size
    {
        get;
        set
        {
            field = value;

            if (value == Vector2.Zero)
            {
                field = new Vector2(400.0f, 400.0f);
            }
        }
    }

    = new(400.0f, 400.0f);

    /// <summary>
    /// Gets the position of the content body start.
    /// </summary>
    /// <remarks>
    /// This is the bottom left of the header node plus some <see cref="ContentPadding"/>.
    /// </remarks>
    public Vector2 ContentStartPosition
        => (this.WindowNode?.ContentStartPosition ?? Vector2.Zero) + new Vector2(this.ContentPadding.X, 0.0f);

    /// <summary>
    /// Gets the size of the body of the window.
    /// </summary>
    /// <remarks>
    /// This is the size of the window minus the size of the header, minus 2x <see cref="ContentPadding"/>.
    /// </remarks>
    public Vector2 ContentSize
        => (this.WindowNode?.ContentSize ?? Vector2.Zero) - new Vector2(this.ContentPadding.X * 2.0f, this.ContentPadding.Y);

    /// <summary>
    /// Gets or sets the padding used for the content area.
    /// </summary>
    public Vector2 ContentPadding { get; set; } = new(8.0f, 8.0f);

    /// <summary>
    /// Gets the depth layer this window will open on.
    /// </summary>
    public int DepthLayer { get; init; } = 5;

    /// <summary>
    /// Gets a value indicating whether this window is open and visible.
    /// </summary>
    public bool IsOpen
        => this.InternalAddon is not null && this.InternalAddon->IsVisible;

    /// <summary>
    /// Gets this addons ID.
    /// </summary>
    public int AddonId
        => this.InternalAddon is null ? 0 : this.InternalAddon->Id;

    // Omitted for now, need a way to save and load addon position in a dalamud-y way.
    // /// <summary>
    // /// Gets or sets a value indicating whether this addon should remove its close position.
    // /// </summary>
    // public bool RememberClosePosition { get; set; } = true;

    /// <summary>
    /// Gets a value indicating whether this addon should be forced into the viewable area when opening.
    /// </summary>
    public bool OpenInBounds { get; init; } = true;

    /// <summary>
    /// Gets a value indicating whether this addon is intended to be used as an overlay addon.
    /// </summary>
    internal bool IsOverlayAddon { get; init; }

    /// <summary>
    /// Gets the list of all created addons.
    /// </summary>
    internal List<NativeAddon> CreatedAddons { get; } = [];

    /// <summary>
    /// Gets the logger for Native Addons.
    /// </summary>
    protected ModuleLog Log { get; } = new("NativeAddon");
}
