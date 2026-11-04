using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using Dalamud.Interface.Fonts.Internal.Glyphs;

namespace Dalamud.Interface.Fonts.Internal.Sources;

/// <summary>A font that stands for another, which can be replaced.</summary>
/// <remarks><ul>
/// <li>It exists before the font it stands for is loaded, and stays the same when <see cref="Target"/> changes, such
/// as when the default font changes.</li>
/// <li>Using it before the load completes waits for the load.</li>
/// <li>Layouts take <see cref="Target"/> when they are made, so a layout keeps drawing the glyphs of the font it was
/// made with.</li>
/// </ul></remarks>
/// <seealso cref="Font.Resolve"/>
internal sealed class SlotFont : Font
{
    private volatile CompositeFont? target;
    private volatile Task? load;

    /// <summary>Initializes a new instance of the <see cref="SlotFont"/> class.</summary>
    /// <param name="name">Name for debugging.</param>
    /// <param name="target">The font to stand for, or null to set it when <see cref="Load"/> completes.</param>
    public SlotFont(string name, CompositeFont? target = null)
        : base(name) =>
        this.target = target;

    /// <summary>Gets or sets the load that sets <see cref="Target"/>.</summary>
    public Task? Load
    {
        get => this.load;
        set => this.load = value;
    }

    /// <summary>Gets the font this one stands for, or null if it has not loaded.</summary>
    /// <remarks>Does not wait.</remarks>
    public CompositeFont? LoadedTarget => this.target;

    /// <summary>Gets or sets the font this one stands for.</summary>
    /// <remarks>Getting it waits for <see cref="Load"/> if needed. Change it only on the main thread before ImGui
    /// starts a frame, so that a frame uses one font throughout.</remarks>
    public CompositeFont Target
    {
        get => this.target ?? this.Join();
        set => this.target = value;
    }

    /// <inheritdoc/>
    public override Font Resolve() => this.Target;

    /// <inheritdoc/>
    public override Font GetPrimary(ref float sizeRatio) => this.target?.GetPrimary(ref sizeRatio) ?? this;

    /// <inheritdoc/>
    public override FontMetrics GetMetrics(float size, float density) => this.Target.GetMetrics(size, density);

    /// <inheritdoc/>
    public override void GetGlyphIndices(ReadOnlySpan<uint> codepoints, Span<uint> glyphs) =>
        this.Target.GetGlyphIndices(codepoints, glyphs);

    /// <inheritdoc/>
    public override void GetGlyphMetrics(
        ReadOnlySpan<uint> glyphs, float size, float density, Span<GlyphMetrics> metrics) =>
        this.Target.GetGlyphMetrics(glyphs, size, density, metrics);

    /// <inheritdoc/>
    public override void Shape(ShapeRun run, ShapedGlyphList output) => this.Target.Shape(run, output);

    /// <inheritdoc/>
    public override void GetGlyphImages(
        ReadOnlySpan<uint> glyphs, ReadOnlySpan<byte> subpixelSteps, float size, float density, float embolden,
        bool subpixel, Span<GlyphImage> images) =>
        this.Target.GetGlyphImages(glyphs, subpixelSteps, size, density, embolden, subpixel, images);

    // Waits for the load, on first use of a font that is still loading.
    [MethodImpl(MethodImplOptions.NoInlining)]
    private CompositeFont Join()
    {
        var task = this.load;
        task?.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing).GetAwaiter().GetResult();
        return this.target
               ?? throw new InvalidOperationException(
                      $"{this} has no font to stand for.",
                      task?.Exception?.InnerException);
    }
}
