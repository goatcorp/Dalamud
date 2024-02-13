using Dalamud.Interface.ManagedFontAtlas;

using ImGuiNET;

namespace Dalamud.Interface.FontIdentifier;

/// <summary>
/// Represents a font identifier.<br />
/// Not intended for plugins to implement.
/// </summary>
public interface IFontId : IObjectWithLocalizableName
{
    /// <summary>
    /// Gets the associated font family.
    /// </summary>
    IFontFamilyId Family { get; }

    /// <summary>
    /// Gets the font weight, ranging from 1 to 999.
    /// </summary>
    int Weight { get; }

    /// <summary>
    /// Gets the font stretch, ranging from 1 to 9.
    /// </summary>
    int Stretch { get; }

    /// <summary>
    /// Gets the font style. Treat as an opaque value.
    /// </summary>
    int Style { get; }

    /// <summary>
    /// Adds this font to the given font build toolkit.
    /// </summary>
    /// <param name="tk">The font build toolkit.</param>
    /// <param name="config">The font configuration. Some parameters may be ignored.</param>
    /// <returns>The added font.</returns>
    ImFontPtr AddToBuildToolkit(IFontAtlasBuildToolkitPreBuild tk, in SafeFontConfig config);
}
