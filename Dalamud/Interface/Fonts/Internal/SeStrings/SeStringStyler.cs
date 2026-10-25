using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal.Layout;
using Dalamud.Interface.Fonts.Internal.Markup;
using Dalamud.Interface.Fonts.Internal.SeStrings.Formats;
using Dalamud.Logging.Internal;

namespace Dalamud.Interface.Fonts.Internal.SeStrings;

/// <summary>Reads SeStrings.</summary>
/// <remarks>Layouts draw their icons and entities, and make their links react.</remarks>
internal class SeStringStyler : TextStyler
{
    private static readonly ModuleLog Log = new(typeof(SeStringStyler));

    private readonly uint[,] colorTypes;
    private readonly Func<ImTextureRef> gfdTexture;
    private readonly SeStringParser parser;

    // The theme of the last key, which the parse of the same callbacks takes.
    private int keyTheme;

    /// <summary>Initializes a new instance of the <see cref="SeStringStyler"/> class, with the game's icons and
    /// colors.</summary>
    /// <seealso cref="SeStringGameData"/>
    public SeStringStyler()
        : this(SeStringGameData.Gfd, SeStringGameData.ColorTypes, SeStringGameData.GetGfdTexture)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="SeStringStyler"/> class.</summary>
    /// <param name="gfd">The bitmap font icon table.</param>
    /// <param name="colorTypes">Colors of each color type and theme.</param>
    /// <param name="gfdTexture">Gets the texture of the icons, or default if there is none yet.</param>
    public SeStringStyler(GfdFileView gfd, uint[,] colorTypes, Func<ImTextureRef> gfdTexture)
    {
        this.Gfd = gfd;
        this.colorTypes = colorTypes;
        this.gfdTexture = gfdTexture;
        this.parser = new(gfd, colorTypes, DrawIcon);
    }

    /// <inheritdoc/>
    public override TextMarkup Markup => TextMarkup.SeString;

    /// <summary>Gets the bitmap font icon table.</summary>
    public GfdFileView Gfd { get; }

    /// <inheritdoc/>
    protected override int ExtraKeyCount => 2;

    /// <summary>Gets the texture of the icons of <see cref="Gfd"/>.</summary>
    /// <returns>The texture, or default if there is none yet.</returns>
    /// <remarks>It is asked whenever an icon is drawn, as the texture may not exist when the layout is made.</remarks>
    public ImTextureRef GetGfdTexture() => this.gfdTexture();

    /// <inheritdoc/>
    protected override void Parse(
        FontImpl font, in TextLayoutDesc desc, ReadOnlySpan<byte> text, ITextMarkupCallback[] callbacks, bool nested,
        StyledText output) =>
        this.ParseSeString(font, desc, [.. text], callbacks, nested, output);

    /// <inheritdoc/>
    /// <remarks>The theme the callbacks ask for, and the gamepad icons. Global numbers are not, as they may change
    /// often. So a text with <c>&lt;color(gnum1)&gt;</c> keeps the color of when it was laid out, while its layout is
    /// cached.</remarks>
    protected override void GetExtraKeys(ReadOnlySpan<ITextMarkupCallback> callbacks, Span<long> keys)
    {
        keys[0] = this.keyTheme = ThemeIndexOf(callbacks);
        keys[1] = SeStringGameData.IconRemapKey();
    }

    /// <summary>Reads a SeString.</summary>
    /// <param name="font">The font, whose style the text starts with.</param>
    /// <param name="desc">Layout parameters.</param>
    /// <param name="seString">The SeString, which becomes <see cref="StyledText.Source"/>.</param>
    /// <param name="callbacks">The callbacks, in order.</param>
    /// <param name="nested">Whether the parser of this styler is in use.</param>
    /// <param name="output">Receives the styled text, cleared.</param>
    protected void ParseSeString(
        FontImpl font, in TextLayoutDesc desc, byte[] seString, ITextMarkupCallback[] callbacks, bool nested,
        StyledText output)
    {
        // Without callbacks, as after a failed parse, the theme is the game's.
        var theme = callbacks.Length == 0 ? SeStringGameData.GameThemeIndex() : this.keyTheme;
        var reader = nested ? new(this.Gfd, this.colorTypes, DrawIcon) : this.parser;
        reader.Parse(seString, this.Markup, font, desc, callbacks, theme, output);
    }

    // The theme the callbacks ask for, in order, or that the game is set to. A callback that throws has no say.
    private static int ThemeIndexOf(ReadOnlySpan<ITextMarkupCallback> callbacks)
    {
        foreach (var callback in callbacks)
        {
            if (callback is ISeStringCallback seStringCallback && AskThemeIndex(seStringCallback) is { } theme)
                return theme;
        }

        return SeStringGameData.GameThemeIndex();
    }

    // The entity data is the ID of the icon's entry.
    private static unsafe void DrawIcon(ImDrawListPtr drawList, Vector2 topLeft, in TextEntityContext context)
    {
        if (TextStylers.Get(context.Markup) is not SeStringStyler styler ||
            !styler.Gfd.TryGetEntry((uint)context.Data, out var entry))
        {
            return;
        }

        var texture = styler.GetGfdTexture();
        if (texture.TexData is null && texture.TexID.IsNull)
            return;
        entry.CalculateScaledSize(context.FontSize, out var hq);
        var tint = ColorHelpers.ApplyOpacity(uint.MaxValue, (context.Color >> 24) / 255f);
        drawList.AddImage(
            texture,
            topLeft,
            topLeft + context.Size,
            hq ? entry.HqUv0 : entry.Uv0,
            hq ? entry.HqUv1 : entry.Uv1,
            tint);
    }

    // Its own method, so that a draw without such callbacks has no try block.
    private static int? AskThemeIndex(ISeStringCallback callback)
    {
        try
        {
            return callback.ThemeIndex;
        }
        catch (Exception e)
        {
            var type = callback.GetType();
            Log.Error(e, "A text callback of {Type} failed to give a theme", type);
            return null;
        }
    }
}
