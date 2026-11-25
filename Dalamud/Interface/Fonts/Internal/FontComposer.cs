using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Runtime.InteropServices;

using Dalamud.Interface.Fonts.Internal.Sources;
using Dalamud.Logging.Internal;

namespace Dalamud.Interface.Fonts.Internal;

/// <summary>Collects <see cref="CompositeFont.Element"/> entries while building a family's <see cref="CompositeFont"/>.</summary>
/// <param name="factory">The factory that serves the fonts of faces and the default font.</param>
/// <param name="locale">The locale.</param>
/// <param name="skipDefault">Whether to leave out the default font, for making the default font itself.</param>
/// <param name="report">Receives the fonts that were loaded and left out, or null.</param>
internal sealed class FontComposer(
    FontFactory factory,
    string locale,
    bool skipDefault,
    FontFactory.LoadReport? report)
{
    private static readonly ModuleLog Log = ModuleLog.Create<FontComposer>();

    // Keyed by IFontFamilyId.DiagnosticKey, so that no ids or their data are kept alive.
    private static readonly ConcurrentDictionary<string, bool> WarnedFonts = new();

    private enum Kind
    {
        Face,
        Default,
        Composite,
    }

    /// <summary>Gets the elements.</summary>
    public List<CompositeFont.Element> Elements { get; } = [];

    /// <summary>Adds the font of the face of a family that best matches the default style.</summary>
    /// <param name="family">The family.</param>
    public void AddFace(IFontFamilyId family) =>
        this.Add(family, Kind.Face, () => this.GetSourceFont(family.GetFace(default)));

    /// <summary>Adds the default font, unless it is left out.</summary>
    /// <param name="family">The default font family.</param>
    public void AddDefault(IFontFamilyId family)
    {
        if (!skipDefault)
            this.Add(family, Kind.Default, () => factory.GetDefaultChain(locale));
    }

    /// <summary>Appends a composite element built from <paramref name="members"/>.</summary>
    /// <param name="family">The owning composite family.</param>
    /// <param name="members">The member families, in priority order.</param>
    /// <param name="adjustments">Adjustments applied to the composite.</param>
    /// <remarks>Does nothing if no members loaded.</remarks>
    public void AddComposite(IFontFamilyId family, ImmutableArray<IFontFamilyId> members, FontAdjustments adjustments)
    {
        var inner = new FontComposer(factory, locale, skipDefault, report);
        foreach (var m in members)
            m.AppendTo(inner);
        if (inner.Elements.Count == 0)
            return;

        this.Add(
            family,
            Kind.Composite,
            () =>
            {
                Font font = new CompositeFont(family.ToString()!, CollectionsMarshal.AsSpan(inner.Elements));
                return adjustments == default
                           ? font
                           : new CompositeFont(family.ToString()!, [CreateElement(font, adjustments, false)]);
            });
    }

    /// <summary>Records a missing or failed member family, logging load errors once per <see cref="IFontFamilyId.DiagnosticKey"/>.</summary>
    /// <param name="family">The family.</param>
    /// <param name="error">Why it failed, or null if it was not found.</param>
    public void ReportFailure(IFontFamilyId family, Exception? error)
    {
        if (error is not null && WarnedFonts.TryAdd(family.DiagnosticKey, true))
            Log.Warning(error, "Font {Id} failed to load. It is left out", family);

        report?.Errors.Add(new(family, error ?? new FileNotFoundException($"Font {family} was not found.")));
    }

    private static CompositeFont.Element CreateElement(Font font, in FontAdjustments adjustments, bool terminal)
    {
        var offset = adjustments.GlyphOffsetUnit == StyleUnit.Px;
        var spacing = adjustments.LetterSpacing;
        return new(
            font.WithStyle(new() { Features = adjustments.Features }),
            adjustments.CodepointRanges.IsEmpty ? null : CompositeFont.ToRanges(adjustments.CodepointRanges.AsSpan()),
            adjustments.SizeScale,
            offset ? default : adjustments.GlyphOffset,
            offset ? adjustments.GlyphOffset : default,
            spacing.Unit == StyleUnit.Em ? spacing.Value : 0,
            spacing.Unit == StyleUnit.Px ? spacing.Value : 0,
            adjustments.GlyphMinAdvanceX,
            adjustments.GlyphMaxAdvanceX,
            FitToSquare: adjustments.FitToSquare,
            Terminal: terminal);
    }

    // Fall back to AXIS for GameSymbolsId when the Lodestone symbol font is unavailable, matching DefaultFontChain.
    private Font? GetSourceFont(IFontFaceId id) =>
        factory.GetServedFont(id, locale)
        ?? (id.Equals(DefaultFontChain.GameSymbolsId)
                ? factory.GetServedFont(DefaultFontChain.GameSymbolsStandInId, locale)
                : null);

    private void Add(IFontFamilyId family, Kind kind, Func<Font?> make)
    {
        Font? font;
        Exception? error = null;
        try
        {
            font = make();
        }
        catch (Exception e)
        {
            font = null;
            error = e;
        }

        if (font is null)
        {
            this.ReportFailure(family, error);
            return;
        }

        if (kind == Kind.Face && report is not null)
            report.Loaded++;
        this.Elements.Add(CreateElement(font, default, kind == Kind.Default));
    }
}
