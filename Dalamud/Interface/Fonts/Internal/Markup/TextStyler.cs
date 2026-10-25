using System.Linq;
using System.Runtime.InteropServices;

using Dalamud.Interface.Fonts.Internal.Layout;
using Dalamud.Logging.Internal;
using Dalamud.Utility;

namespace Dalamud.Interface.Fonts.Internal.Markup;

/// <summary>Reads text of a markup into styled text, and lays it out.</summary>
/// <seealso cref="TextStylers"/>
internal abstract class TextStyler
{
    // How deep callbacks may lay out text of the same markup. A callback that lays out the text it is asked about would
    // recurse without end.
    private const int MaxDepth = 8;

    private static readonly ModuleLog Log = new(typeof(TextStyler));

    private readonly StyledText output = new();
    private int depth;

    /// <summary>Gets the markup it reads.</summary>
    public abstract TextMarkup Markup { get; }

    /// <summary>Gets the number of values <see cref="GetExtraKeys"/> writes.</summary>
    protected virtual int ExtraKeyCount => 0;

    /// <summary>Lays out text, with the callbacks pushed now. Equal arguments share a layout.</summary>
    /// <param name="font">The font, whose style the text starts with.</param>
    /// <param name="desc">Layout parameters.</param>
    /// <param name="text">The text as drawn.</param>
    /// <returns>The layout, whose offsets are into <paramref name="text"/>. Null outside the main thread.</returns>
    public TextLayout? CreateLayout(FontImpl font, in TextLayoutDesc desc, ReadOnlySpan<byte> text)
    {
        if (!ThreadSafety.IsMainThread)
            return null;

        var chain = TextCallbackStack.Snapshot();
        var keyCount = (chain.Length * 2) + this.ExtraKeyCount;
        var pooledKeys = new PooledSpan<long>(keyCount <= 32 ? stackalloc long[keyCount] : default, keyCount);
        var keys = pooledKeys.Span;
        TextCallbackStack.GetKeys(chain, keys);
        this.GetExtraKeys(chain, keys[(chain.Length * 2)..]);
        var found = TextLayouter.TryGetCached(font, desc, text, keys, out var cached, out var key);
        pooledKeys.Recycle();
        return found ? cached : this.CreateUncachedLayout(font, desc, text, chain, key!);
    }

    /// <summary>Reads text.</summary>
    /// <param name="font">The font, whose style the text starts with.</param>
    /// <param name="desc">Layout parameters.</param>
    /// <param name="text">The text as drawn.</param>
    /// <param name="callbacks">The callbacks, in order.</param>
    /// <param name="nested">Whether this styler is already reading another text, from a callback. Buffers kept for
    /// reuse are in use then.</param>
    /// <param name="output">Receives the styled text, cleared.</param>
    protected abstract void Parse(
        FontImpl font, in TextLayoutDesc desc, ReadOnlySpan<byte> text, ITextMarkupCallback[] callbacks, bool nested,
        StyledText output);

    /// <summary>Gets what layouts depend on, besides the callbacks and the text.</summary>
    /// <param name="callbacks">The callbacks, in order.</param>
    /// <param name="keys">Receives <see cref="ExtraKeyCount"/> values.</param>
    protected virtual void GetExtraKeys(ReadOnlySpan<ITextMarkupCallback> callbacks, Span<long> keys)
    {
    }

    // CreateLayout of what is not cached. Its own method, so that the cached path has no try block.
    private TextLayout CreateUncachedLayout(
        FontImpl font, in TextLayoutDesc desc, ReadOnlySpan<byte> text, ITextMarkupCallback[] chain, byte[] key)
    {
        // Callbacks may lay out text of this markup themselves.
        var nested = this.depth++ > 0;
        var start = FontProfiler.Begin(FontProfiler.Section.Layout);
        try
        {
            if (this.depth > MaxDepth)
                chain = this.TooDeep(chain);

            // What fails is laid out without the callbacks, or as plain text if that fails too. It is cached as the
            // layout of the text, so that it does not fail again every frame.
            var styled = nested ? new() : this.output;
            if (!this.TryParse(font, desc, text, chain, nested, styled))
            {
                chain = [];
                if (!this.TryParse(font, desc, text, chain, nested, styled))
                {
                    var plain = TextLayout.Rent(
                        font.WithMarkup(TextMarkup.Plain), desc, text, 1, [], []);
                    TextLayouter.AddCached(key, plain);
                    return plain;
                }
            }

            var layout = TextLayout.Rent(
                styled.DefaultFont ?? font,
                desc,
                styled.Text,
                1,
                CollectionsMarshal.AsSpan(styled.Runs),
                CollectionsMarshal.AsSpan(styled.Objects),
                styled.GetDrawnOffsets());
            layout.RenderHook = new(this.Markup, styled, chain, desc.EmSize);
            TextLayouter.AddCached(key, layout);
            return layout;
        }
        finally
        {
            FontProfiler.End(FontProfiler.Section.Layout, start, text.Length);

            // The layout has copied what it needs. Entities from plugin callbacks must not stay reachable from here.
            if (!nested)
                this.output.Clear();
            this.depth--;
        }
    }

    private ITextMarkupCallback[] TooDeep(ITextMarkupCallback[] callbacks)
    {
        var callbackTypes = string.Join(", ", callbacks.Select(c => c.GetType().FullName));
        Log.Error(
            "Callbacks [{Callbacks}] nested text of markup {Markup} over {Depth} deep. It is laid out without them",
            callbackTypes,
            this.Markup,
            MaxDepth);
        return [];
    }

    // Failures are logged for each styler and set of callback types.
    private bool TryParse(
        FontImpl font, in TextLayoutDesc desc, ReadOnlySpan<byte> text, ITextMarkupCallback[] callbacks, bool nested,
        StyledText styled)
    {
        styled.Clear();
        try
        {
            this.Parse(font, desc, text, callbacks, nested, styled);
            return true;
        }
        catch (Exception e)
        {
            var callbackTypes = string.Join(", ", callbacks.Select(c => c.GetType().FullName));
            Log.Error(
                e,
                "Text of markup {Markup} failed to lay out with callbacks [{Callbacks}]. It is laid out {Fallback}",
                this.Markup,
                callbackTypes,
                callbacks.Length > 0 ? "without them." : "as plain text.");
            return false;
        }
    }
}
