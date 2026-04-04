using System;

using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.ManagedFontAtlas.Internals;
using Dalamud.Utility;

namespace Dalamud.CorePlugin.PluginInstallerV2.Controllers;

/// <summary>
/// Manages the fonts for the Plugin Installer Window.
/// </summary>
internal class FontManager : IDisposable
{
    private readonly DisposeSafety.ScopedFinalizer scopedFinalizer = new();
    private readonly IFontAtlas privateAtlas;

    /// <summary>
    /// Initializes a new instance of the <see cref="FontManager"/> class.
    /// </summary>
    public FontManager()
    {
        var fontAtlasFactory = Service<FontAtlasFactory>.Get();

        this.privateAtlas = fontAtlasFactory.CreateFontAtlas("PluginInstallerFontAtlas", FontAtlasAutoRebuildMode.Async);

        this.LargerFontHandle = new Lazy<IFontHandle>(
            () => this.scopedFinalizer.Add(
                this.privateAtlas.NewDelegateFontHandle(
                    e => e.OnPreBuild(
                        toolkit => toolkit.AddDalamudDefaultFont(32.0f)))));

        this.LargerIconFontHandle = new Lazy<IFontHandle>(
            () => this.scopedFinalizer.Add(
                this.privateAtlas.NewDelegateFontHandle(
                    e => e.OnPreBuild(
                        toolkit => toolkit.AddFontAwesomeIconFont(new SafeFontConfig
                        {
                            SizePx = 32.0f,
                        })))));
    }

    /// <summary>
    /// Gets a reference to a larger font handle for displaying things like titles or headers.
    /// </summary>
    public Lazy<IFontHandle> LargerFontHandle { get; private set; }

    /// <summary>
    /// Gets a reference to a larger icon font handle for displaying sharper icons.
    /// </summary>
    public Lazy<IFontHandle> LargerIconFontHandle { get; private set; }

    /// <inheritdoc/>
    public void Dispose()
    {
        this.scopedFinalizer.Dispose();
    }
}
