using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Fonts.Internal;

namespace Dalamud.Interface.Fonts;

/// <summary>The font of a text style, loaded or being loaded, with automatic lifecycle management.</summary>
/// <remarks>Holding a strong reference to this will keep the font alive. An <see cref="ImFontPtr"/> is valid in the
/// frame it was got in only.</remarks>
public sealed class ImFontRef
{
    /// <summary>Initializes a new instance of the <see cref="ImFontRef"/> class.</summary>
    /// <param name="impl">The font.</param>
    internal ImFontRef(FontImpl impl) => this.Impl = impl;

    /// <summary>Gets the style of the font. Equal styles share one font.</summary>
    public TextStyle Style => this.Impl.Style;

    /// <summary>Gets a <see cref="Task"/> representing the load state of the font family.</summary>
    /// <remarks>Fonts of the family that are not found or fail to load are left out, and listed in
    /// <see cref="LoadErrors"/>. The task faults with <see cref="FontLoadException"/> only if none of them loaded.
    /// </remarks>
    public Task LoadTask => this.Impl.Entry.Load;

    /// <summary>Gets why fonts of the family were left out, as of the last load that completed.</summary>
    /// <value>One exception per font left out, with <see cref="FontLoadException.Family"/> the part of the family it
    /// is for. If the whole font failed for another reason, that alone. Empty if nothing was left out.</value>
    /// <remarks>The default font a family falls back to is not listed.</remarks>
    public IReadOnlyList<FontLoadException> LoadErrors => this.Impl.Entry.LoadErrors;

    /// <summary>Gets a value indicating whether the font has loaded, fully or with some fonts of the family left out.
    /// </summary>
    public bool IsLoaded => this.Impl.Entry.Load.IsCompletedSuccessfully;

    /// <summary>Gets the font.</summary>
    internal FontImpl Impl { get; }

    /// <summary>Converts to <see cref="ImFontPtr"/>, valid in this frame, without waiting for the load.</summary>
    /// <param name="font">The font.</param>
    /// <returns>The font. It draws with the default font until it has loaded, or if it could not be loaded.
    /// </returns>
    public static unsafe implicit operator ImFontPtr(ImFontRef font) => font.Impl.Native;

    /// <inheritdoc cref="TextStyle.Push"/>
    public ImPopFontDisposable Push() => this.Impl.Resolver.Push(this.Impl);

    /// <inheritdoc cref="TextStyle.CalcTextSize"/>
    public Vector2 CalcTextSize(ImU8String text, float wrapWidth = -1) => this.Style.CalcTextSize(text, wrapWidth);

    /// <inheritdoc/>
    public override string ToString() => $"{nameof(ImFontRef)}({this.Impl.Style})";
}
