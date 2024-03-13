using System.Threading.Tasks;

using Dalamud.Interface.GameFonts;
using Dalamud.Interface.Utility;

using ImGuiNET;

namespace Dalamud.Interface.ManagedFontAtlas;

/// <summary>
/// Wrapper for <see cref="ImFontAtlasPtr"/>.<br />
/// Not intended for plugins to implement.
/// </summary>
public interface IFontAtlas : IDisposable
{
    /// <summary>
    /// Event to be called on build step changes.<br />
    /// <see cref="IFontAtlasBuildToolkit.Font"/> is meaningless for this event.
    /// </summary>
    event FontAtlasBuildStepDelegate? BuildStepChange;

    /// <summary>
    /// Event fired when a font rebuild operation is recommended.<br />
    /// This event will be invoked from the main thread.<br />
    /// <br />
    /// Reasons for the event include changes in <see cref="ImGuiHelpers.GlobalScale"/> and
    /// initialization of new associated font handles.
    /// </summary>
    /// <remarks>
    /// You should call <see cref="BuildFontsAsync"/> or <see cref="BuildFontsOnNextFrame"/>
    /// if <see cref="AutoRebuildMode"/> is not set to <c>true</c>.<br />
    /// Avoid calling <see cref="BuildFontsImmediately"/> here; it will block the main thread.
    /// </remarks>
    event Action? RebuildRecommend;

    /// <summary>
    /// Gets the name of the atlas. For logging and debugging purposes.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets a value how the atlas should be rebuilt when the relevant Dalamud Configuration changes. 
    /// </summary>
    FontAtlasAutoRebuildMode AutoRebuildMode { get; }

    /// <summary>
    /// Gets the font atlas. Might be empty.
    /// </summary>
    ImFontAtlasPtr ImAtlas { get; }

    /// <summary>
    /// Gets the task that represents the current font rebuild state.
    /// </summary>
    Task BuildTask { get; }

    /// <summary>
    /// Gets a value indicating whether there exists any built atlas, regardless of <see cref="BuildTask"/>.
    /// </summary>
    bool HasBuiltAtlas { get; }

    /// <summary>
    /// Gets a value indicating whether this font atlas is under the effect of global scale.
    /// </summary>
    bool IsGlobalScaled { get; }

    /// <summary>
    /// Suppresses automatically rebuilding fonts for the scope.
    /// </summary>
    /// <returns>An instance of <see cref="IDisposable"/> that will release the suppression.</returns>
    /// <remarks>
    /// Use when you will be creating multiple new handles, and want rebuild to trigger only when you're done doing so.
    /// This function will effectively do nothing, if <see cref="AutoRebuildMode"/> is set to
    /// <see cref="FontAtlasAutoRebuildMode.Disable"/>.
    /// </remarks>
    /// <example>
    /// <code>
    /// using (atlas.SuppressBuild()) {
    ///     this.font1 = atlas.NewGameFontHandle(...);
    ///     this.font2 = atlas.NewDelegateFontHandle(...);
    /// }
    /// </code>
    /// </example>
    public IDisposable SuppressAutoRebuild();

    /// <summary>
    /// Creates a new <see cref="IFontHandle"/> from game's built-in fonts.
    /// </summary>
    /// <param name="style">Font to use.</param>
    /// <returns>Handle to a font that may or may not be ready yet.</returns>
    /// <remarks>
    /// <para>Do not call this function during <see cref="BuildStepChange"/>, <see cref="UiBuilder.BuildFonts"/>,
    /// <see cref="UiBuilder.AfterBuildFonts"/>, and alike. If it works, it is accidental.</para>
    /// <para>It is safe to call this function from the plugin constructor.</para>
    /// </remarks>
    public IFontHandle NewGameFontHandle(GameFontStyle style);

    /// <summary>
    /// Creates a new IFontHandle using your own callbacks.
    /// </summary>
    /// <param name="buildStepDelegate">Callback for <see cref="IFontAtlas.BuildStepChange"/>.</param>
    /// <returns>Handle to a font that may or may not be ready yet.</returns>
    /// <remarks>
    /// <para>Consider calling <see cref="IFontAtlasBuildToolkitPreBuild.AttachExtraGlyphsForDalamudLanguage"/> to
    /// support glyphs that are not supplied by the game by default; this mostly affects Chinese and Korean language
    /// users.</para>
    /// <para>Do not call this function during <see cref="BuildStepChange"/>, <see cref="UiBuilder.BuildFonts"/>,
    /// <see cref="UiBuilder.AfterBuildFonts"/>, and alike. If it works, it is accidental.</para>
    /// <para>It is safe to call this function from the plugin constructor.</para>
    /// </remarks>
    /// <example>
    /// <b>On initialization</b>:
    /// <code>
    /// this.fontHandle = atlas.NewDelegateFontHandle(e => e.OnPreBuild(tk => {
    ///     var config = new SafeFontConfig { SizePx = UiBuilder.DefaultFontSizePx };
    ///     config.MergeFont = tk.AddFontFromFile(@"C:\Windows\Fonts\comic.ttf", config);
    ///     tk.AddGameSymbol(config);
    ///     tk.AddExtraGlyphsForDalamudLanguage(config);
    ///     // optionally do the following if you have to add more than one font here,
    ///     // to specify which font added during this delegate is the final font to use.
    ///     tk.Font = config.MergeFont;
    /// }));
    /// // or
    /// this.fontHandle = atlas.NewDelegateFontHandle(e => e.OnPreBuild(tk => tk.AddDalamudDefaultFont(36)));
    /// </code>
    /// <br />
    /// <b>On use</b>:
    /// <code>
    /// using (this.fontHandle.Push())
    ///     ImGui.TextUnformatted("Example");
    /// </code>
    /// </example>
    public IFontHandle NewDelegateFontHandle(FontAtlasBuildStepDelegate buildStepDelegate);

    /// <summary>
    /// Queues rebuilding fonts, on the main thread.<br />
    /// Note that <see cref="BuildTask"/> would not necessarily get changed from calling this function.
    /// </summary>
    /// <exception cref="InvalidOperationException">If <see cref="AutoRebuildMode"/> is <see cref="FontAtlasAutoRebuildMode.Async"/>.</exception>
    /// <remarks>
    /// Using this method will block the main thread on rebuilding fonts, effectively calling
    /// <see cref="BuildFontsImmediately"/> from the main thread. Consider migrating to <see cref="BuildFontsAsync"/>.
    /// </remarks>
    void BuildFontsOnNextFrame();

    /// <summary>
    /// Rebuilds fonts immediately, on the current thread.
    /// </summary>
    /// <exception cref="InvalidOperationException">If <see cref="AutoRebuildMode"/> is <see cref="FontAtlasAutoRebuildMode.Async"/>.</exception>
    void BuildFontsImmediately();

    /// <summary>
    /// Rebuilds fonts asynchronously, on any thread. 
    /// </summary>
    /// <returns>The task.</returns>
    /// <exception cref="InvalidOperationException">If <see cref="AutoRebuildMode"/> is <see cref="FontAtlasAutoRebuildMode.OnNewFrame"/>.</exception>
    Task BuildFontsAsync();
}
