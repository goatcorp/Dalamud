using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Fonts.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Utility;

using Serilog;

namespace Dalamud.Interface.Fonts;

/// <summary>A dialog for choosing a font.</summary>
public sealed partial class FontChooserDialog : IDisposable
{
    private const float MinSizePx = 1;
    private const float MaxSizePx = 200;
    private const float MinSizeEm = 0.1f;
    private const float MaxSizeEm = 10;

    private const FontChooserFields DetailFields =
        FontChooserFields.LetterSpacing | FontChooserFields.LineHeight | FontChooserFields.Locale;

    private static readonly List<IFontFaceId> EmptyFaceList = [];

    private static readonly float[] SizeList = [10, 12, 13, 14, 16, 18, 20, 24, 28, 32, 40, 48];

    private static readonly string[] SizeLabels = Array.ConvertAll(SizeList, x => $"{x}");

    // Mirrors the list of FontSamplesImGuiWidget.
    private static readonly string[] Locales = ["ja-JP", "zh-CN", "zh-TW", "zh-HK", "ko-KR"];

    private static int counterStatic;

    private readonly int counter;
    private readonly byte[] previewText = new byte[2048];
    private readonly FontPreviews previews = new();
    private readonly Dictionary<IFontFamilyId, IFontFaceId?> previewFaces = [];
    private readonly TaskCompletionSource<TextStyle> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);

    private string windowName = string.Empty;
    private string title = string.Empty;
    private Predicate<IFontFamilyId>? familyFilter;

    private bool firstDraw = true;
    private bool placementChanged;
    private Vector2 popupPosition = new(float.NaN);
    private Vector2 popupSize = new(float.NaN);

    private Task<List<FamilyRow>>? familiesTask;
    private List<FamilyRow>? appliedFamilies;
    private bool rowsDirty;
    private List<FamilyRow> rows = [];

    private TextStyle style;
    private IFontFamilyId family = DalamudDefaultFontId.Instance;
    private IReadOnlyList<IFontFaceId> faces = EmptyFaceList;
    private int familyIndex = -1;
    private int faceIndex = -1;
    private string familySearch = string.Empty;
    private string faceSearch = string.Empty;
    private bool scrollFamily;
    private bool scrollFace;
    private bool scrollSize;
    private int focusColumn = -1;
    private bool detailsOpen;

    private ImFontRef? requestedFont;
    private ImFontRef? shownFont;

    /// <summary>Initializes a new instance of the <see cref="FontChooserDialog"/> class.</summary>
    /// <param name="initial">The style to start with.</param>
    /// <remarks>Call <see cref="Draw"/> every frame, and <see cref="Dispose"/> once done, or use
    /// <see cref="CreateAuto"/>, which does both.</remarks>
    public FontChooserDialog(TextStyle initial = default)
    {
        this.counter = Interlocked.Increment(ref counterStatic);
        this.Title = "Choose a font";
        this.PreviewText = "Font preview.\n0123456789!\n遍角次亮采之门，门上插刀、直字拐弯、天上平板、船顶漏雨。\n다람쥐 헌 쳇바퀴에 타고파";
        this.Style = initial;
    }

    /// <summary>Called when the style has changed.</summary>
    /// <remarks>It is raised from <see cref="Draw"/>, at the first call and after each change.</remarks>
    public event Action<TextStyle>? StyleChanged;

    /// <summary>Gets the task that resolves upon choosing a font or cancellation.</summary>
    /// <remarks>The task is canceled on cancel.</remarks>
    public Task<TextStyle> ResultTask => this.tcs.Task;

    /// <summary>Gets or sets the style being edited.</summary>
    /// <remarks><ul>
    /// <li>The dialog edits the first family of <see cref="TextStyle.FamilyId"/>, and keeps the rest of a
    /// <see cref="ChainFamilyId"/>.</li>
    /// <li>It edits the face, <see cref="TextStyle.Size"/>, <see cref="TextStyle.LetterSpacing"/>,
    /// <see cref="TextStyle.LineHeight"/> and <see cref="TextStyle.Locale"/>, and keeps the rest.</li>
    /// <li>A size keeps its unit, until a size is picked from the list, which is in pixels.</li>
    /// </ul></remarks>
    public TextStyle Style
    {
        get => this.style;
        set
        {
            this.style = value;
            this.detailsOpen |=
                value.LetterSpacing != default || value.LineHeight != default || value.Locale is not null;
            this.SyncSelection();

            // The next Draw loads it, and raises StyleChanged.
            this.requestedFont = null;
        }
    }

    /// <summary>Gets or sets the title of this dialog.</summary>
    public string Title
    {
        get => this.title;
        set
        {
            this.title = value;
            this.windowName = $"{value}###FontChooser{this.counter}";
        }
    }

    /// <summary>Gets or sets the preview text. A text too long may be truncated on assignment.</summary>
    public string PreviewText
    {
        get
        {
            var n = this.previewText.AsSpan().IndexOf((byte)0);
            return n < 0
                       ? Encoding.UTF8.GetString(this.previewText)
                       : Encoding.UTF8.GetString(this.previewText, 0, n);
        }

        set
        {
            // The last byte stays 0. The encoder writes whole characters only.
            Encoding.UTF8.GetEncoder().Convert(
                value,
                this.previewText.AsSpan(0, this.previewText.Length - 1),
                true,
                out _,
                out var length,
                out _);
            this.previewText.AsSpan(length).Clear();
        }
    }

    /// <summary>Gets or sets the predicate that tells which families to list.</summary>
    /// <remarks>A family is listed if it returns <see langword="true"/>. A style whose first family it rejects cannot
    /// be chosen.</remarks>
    public Predicate<IFontFamilyId>? FamilyFilter
    {
        get => this.familyFilter;
        set
        {
            this.familyFilter = value;
            this.rowsDirty = true;
        }
    }

    /// <summary>Gets or sets which parts of the style the user can edit, besides the family and the face.</summary>
    public FontChooserFields Fields { get; set; } = FontChooserFields.Default;

    /// <summary>Gets or sets a value indicating whether this dialog is modal, blocking everything behind from being
    /// interacted with.</summary>
    /// <remarks>If <see langword="true"/>, then
    /// <see cref="ImGui.BeginPopupModal(ImU8String, ref bool, ImGuiWindowFlags)"/> is used. Otherwise,
    /// <see cref="ImGui.Begin(ImU8String, ref bool, ImGuiWindowFlags)"/> is used.</remarks>
    public bool IsModal { get; set; } = true;

    /// <summary>Gets or sets the window flags.</summary>
    public ImGuiWindowFlags WindowFlags { get; set; }

    /// <summary>Gets or sets the position of the top left corner of the dialog.</summary>
    /// <remarks>It is applied on the next <see cref="Draw"/>. If any of the coordinates are
    /// <see cref="float.NaN"/>, the dialog is centered on the main viewport.</remarks>
    public Vector2 PopupPosition
    {
        get => this.popupPosition;
        set
        {
            this.placementChanged = true;
            this.popupPosition = value;
        }
    }

    /// <summary>Gets or sets the size of the dialog.</summary>
    /// <remarks>It is applied on the next <see cref="Draw"/>. If any of the coordinates are
    /// <see cref="float.NaN"/>, the default size is used.</remarks>
    public Vector2 PopupSize
    {
        get => this.popupSize;
        set
        {
            this.placementChanged = true;
            this.popupSize = value;
        }
    }

    private static Vector2 MinPopupSize => new Vector2(320, 240) * ImGuiHelpers.GlobalScale;

    private static Vector2 DefaultPopupSize => new Vector2(40, 30) * ImGui.GetTextLineHeight();

    // The size in pixels before global scaling. An em is taken at the default font size.
    private float SizePx => this.style.Size.Unit switch
    {
        StyleUnit.Px => this.style.Size.Value,
        StyleUnit.Em => this.style.Size.Value * Service<FontFactory>.Get().DefaultSizePx,
        _ => Service<FontFactory>.Get().DefaultSizePx,
    };

    private float LineHeightRatio => this.style.LineHeight.Unit switch
    {
        StyleUnit.Em => this.style.LineHeight.Value,
        StyleUnit.Px => this.style.LineHeight.Value / this.SizePx,
        _ => 1f,
    };

    /// <summary>Creates a new instance of <see cref="FontChooserDialog"/> that draws itself, and disposes itself
    /// once closed.</summary>
    /// <param name="uiBuilder">An instance of <see cref="IUiBuilder"/>.</param>
    /// <param name="initial">The style to start with.</param>
    /// <returns>The new instance of <see cref="FontChooserDialog"/>.</returns>
    public static FontChooserDialog CreateAuto(IUiBuilder uiBuilder, TextStyle initial = default)
    {
        var fcd = new FontChooserDialog(initial);
        uiBuilder.Draw += DrawAndDispose;
        return fcd;

        // Disposed in the frame it closed in, so that it is never drawn while disposed.
        void DrawAndDispose()
        {
            fcd.Draw();
            if (!fcd.tcs.Task.IsCompleted)
                return;
            uiBuilder.Draw -= DrawAndDispose;
            fcd.Dispose();
        }
    }

    /// <inheritdoc/>
    /// <remarks>Call it on the main thread, after the last <see cref="Draw"/>. A dialog still open is canceled.
    /// </remarks>
    public void Dispose()
    {
        this.tcs.TrySetCanceled();
        this.requestedFont = null;
        this.shownFont = null;
        this.previews.Clear();
    }

    /// <summary>Cancels this dialog.</summary>
    /// <remarks>It does nothing to a dialog that is already closed.</remarks>
    public void Cancel()
    {
        this.tcs.TrySetCanceled();
        ImGui.GetIO().WantCaptureKeyboard = false;
        ImGui.GetIO().WantTextInput = false;
    }

    /// <summary>Sets <see cref="PopupSize"/> and <see cref="PopupPosition"/> to center the dialog on the window
    /// being drawn.</summary>
    /// <param name="size">The size, or <see langword="null"/> for the default size.</param>
    /// <remarks>The dialog is kept within the work area of the viewport of the window.</remarks>
    public void CenterOnCurrentWindow(Vector2? size = null)
    {
        ThreadSafety.AssertMainThread();
        var viewport = ImGui.GetWindowViewport();
        var s = Vector2.Min(size ?? DefaultPopupSize, viewport.WorkSize);
        var pos = ImGui.GetWindowPos() + ((ImGui.GetWindowSize() - s) / 2);
        this.PopupSize = s;
        this.PopupPosition = Vector2.Clamp(pos, viewport.WorkPos, viewport.WorkPos + viewport.WorkSize - s);
    }

    /// <summary>Draws this dialog.</summary>
    /// <remarks>A closed dialog draws nothing.</remarks>
    public void Draw()
    {
        if (this.tcs.Task.IsCompleted)
            return;

        this.familiesTask ??= Task.Run(() => this.ListFamilies(false));
        if (this.familiesTask is { IsCompletedSuccessfully: true, Result: var all }
            && (all != this.appliedFamilies || this.rowsDirty))
        {
            this.ApplyFamilies(all);
        }

        var modal = this.IsModal;
        if (this.firstDraw && modal)
            ImGui.OpenPopup(this.windowName);

        if (this.firstDraw || this.placementChanged)
        {
            var size = float.IsNaN(this.popupSize.X) || float.IsNaN(this.popupSize.Y)
                           ? Vector2.Min(DefaultPopupSize, ImGui.GetMainViewport().WorkSize)
                           : this.popupSize;
            ImGui.SetNextWindowSize(Vector2.Max(size, MinPopupSize), ImGuiCond.Always);
            if (float.IsNaN(this.popupPosition.X) || float.IsNaN(this.popupPosition.Y))
            {
                var viewport = ImGui.GetMainViewport();
                ImGui.SetNextWindowPos(viewport.WorkPos + (viewport.WorkSize / 2), ImGuiCond.Always, new(0.5f));
            }
            else
            {
                ImGui.SetNextWindowPos(this.popupPosition, ImGuiCond.Always);
            }

            this.placementChanged = false;
        }

        this.firstDraw = false;
        ImGui.SetNextWindowSizeConstraints(MinPopupSize, new(float.MaxValue));

        var open = true;
        if (modal)
        {
            if (!ImGui.BeginPopupModal(this.windowName, ref open, this.WindowFlags))
            {
                this.Cancel();
                return;
            }
        }
        else
        {
            var visible = ImGui.Begin(this.windowName, ref open, this.WindowFlags);
            if (!open || !visible)
            {
                ImGui.End();

                // Begin returns false for a collapsed window too, which stays open.
                if (!open)
                    this.Cancel();
                return;
            }
        }

        if (ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
        {
            if (ImGui.IsKeyPressed(ImGuiKey.Escape))
            {
                this.Cancel();
                End(modal);
                return;
            }

            ImGui.GetIO().WantCaptureKeyboard = true;
            ImGui.GetIO().WantTextInput = true;
        }

        this.DrawBody();

        this.popupPosition = ImGui.GetWindowPos();
        this.popupSize = ImGui.GetWindowSize();
        End(modal);

        static void End(bool modal)
        {
            if (modal)
                ImGui.EndPopup();
            else
                ImGui.End();
        }
    }

    // The faces of a family, or none if they cannot be read, as for a family uninstalled since it was listed.
    private static IReadOnlyList<IFontFaceId> FacesOf(IFontFamilyId family)
    {
        try
        {
            return family.Fonts;
        }
        catch (Exception e)
        {
            Log.Warning(e, "Failed to list the fonts of {Family}", family);
            return EmptyFaceList;
        }
    }

    private static IFontFamilyId FirstFamily(IFontFamilyId? family) =>
        family is ChainFamilyId { Elements: [var first, ..] }
            ? FirstFamily(first.Family)
            : family ?? DalamudDefaultFontId.Instance;

    private static IFontFamilyId WithFirstFamily(IFontFamilyId? family, IFontFamilyId first) =>
        family is ChainFamilyId { Elements: [var e, ..] } chain
            ? new ChainFamilyId(chain.Elements.SetItem(0, e with { Family = WithFirstFamily(e.Family, first) }))
            : first;

    private static string ExtractName(LocalizedName name) => name.Get(Service<FontFactory>.Get().InferredLocale);

    private static bool Matches(string name, string search) =>
        name.Contains(search, StringComparison.CurrentCultureIgnoreCase);

    // Finds the next item after from that passes the test, going down if dir is 1 or up if it is -1, wrapping
    // around. Returns -1 if none passes.
    private static int Step<T>(IReadOnlyList<T> items, int from, int dir, Func<T, bool> test)
    {
        var n = items.Count;
        if (from < 0)
            from = dir > 0 ? -1 : 0;
        for (var k = 1; k <= n; k++)
        {
            var i = (((from + (dir * k)) % n) + n) % n;
            if (test(items[i]))
                return i;
        }

        return -1;
    }

    // Draws only the visible rows of a list, and clears scroll once it scrolled to the selected row. Returns the
    // index clicked, or -1.
    private static int DrawList<T>(
        ImU8String id,
        IReadOnlyList<T> items,
        int selected,
        ref bool scroll,
        Func<int, T, bool, bool> drawRow)
    {
        using var child = ImRaii.Child(id, ImGui.GetContentRegionAvail());
        if (!child)
            return -1;

        var lineHeight = ImGui.GetTextLineHeightWithSpacing();
        if (scroll && selected >= 0)
            ImGui.SetScrollY((lineHeight * selected) - ((ImGui.GetContentRegionAvail().Y - lineHeight) / 2));
        scroll = false;

        var clicked = -1;
        var clipper = ImGui.ImGuiListClipper();
        clipper.Begin(items.Count, lineHeight);
        while (clipper.Step())
        {
            for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
            {
                if (drawRow(i, items[i], i == selected))
                    clicked = i;
            }
        }

        clipper.Destroy();
        return clicked;
    }

    // Draws a search box that selects items by name. Items without a name cannot be selected. Returns whether the
    // selection changed.
    private static bool DrawSearch<T>(
        ImU8String id,
        ref string search,
        IReadOnlyList<T> items,
        ref int selected,
        bool focus,
        Func<T, string?> nameOf)
    {
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        if (focus)
            ImGui.SetKeyboardFocusHere();

        var text = search;
        var index = selected;
        var changed = false;
        var edited = ImGui.InputText(
            id,
            ref text,
            255,
            ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.CallbackHistory,
            (ref data) =>
            {
                var dir = data.EventKey switch
                {
                    ImGuiKey.DownArrow => 1,
                    ImGuiKey.UpArrow => -1,
                    _ => 0,
                };
                if (dir == 0)
                    return 0;

                // A fully selected text is the name of the selection, not a search.
                var all = data.SelectionStart == 0 && data.SelectionEnd == data.BufTextLen;
                var next = Step(items, index, dir, x => nameOf(x) is { } name && (all || Matches(name, text)));
                if (next < 0)
                    return 0;

                index = next;
                changed = true;
                if (all)
                    ImGuiHelpers.SetTextFromCallback(ref data, nameOf(items[next])!);
                return 0;
            });

        if (edited && !changed && !string.IsNullOrWhiteSpace(text))
        {
            var found = Step(items, -1, 1, x => nameOf(x) is { } name && Matches(name, text));
            if (found >= 0)
            {
                index = found;
                changed = true;
            }
        }

        search = text;
        selected = index;
        return changed;
    }

    private List<FamilyRow> ListFamilies(bool refresh)
    {
        var list = new List<FamilyRow> { new(null, "Dalamud"), Row(DalamudDefaultFontId.Instance) };
        foreach (var f in IFontFamilyId.ListDalamudFonts())
            list.Add(Row(f));

        list.Add(new(null, "Game"));
        foreach (var f in IFontFamilyId.ListGameFonts())
            list.Add(Row(f));

        list.Add(new(null, "System"));
        var system = IFontFamilyId.ListSystemFonts(refresh).ConvertAll(Row);
        system.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.CurrentCultureIgnoreCase));
        list.AddRange(system);
        return list;

        static FamilyRow Row(IFontFamilyId f) => new(f, ExtractName(f.Name));
    }

    // Applies a list of families to the rows and the selection, on the main thread.
    private void ApplyFamilies(List<FamilyRow> all)
    {
        if (all != this.appliedFamilies)
        {
            this.appliedFamilies = all;
            this.previewFaces.Clear();
        }

        this.rowsDirty = false;
        this.rows = [];
        var header = -1;
        foreach (var row in all)
        {
            if (row.Family is null)
            {
                if (header >= 0 && header == this.rows.Count - 1)
                    this.rows.RemoveAt(header);
                header = this.rows.Count;
                this.rows.Add(row);
            }
            else if (this.familyFilter?.Invoke(row.Family) is not false)
            {
                this.rows.Add(row);
            }
        }

        if (header >= 0 && header == this.rows.Count - 1)
            this.rows.RemoveAt(header);

        this.SyncSelection();
    }

    // Finds the family and the face of the style in the lists.
    private void SyncSelection()
    {
        var first = FirstFamily(this.style.FamilyId);
        this.familyIndex = this.rows.FindIndex(x => Equals(x.Family, first));
        this.family = this.familyIndex >= 0 ? this.rows[this.familyIndex].Family! : first;
        this.faces = FacesOf(this.family);

        var wanted = this.style.On(default);
        this.faceIndex = -1;
        for (var i = 0; i < this.faces.Count && this.faceIndex < 0; i++)
        {
            if (this.faces[i].Style == wanted)
                this.faceIndex = i;
        }

        if (this.faceIndex < 0 && this.faces.Count > 0)
            this.faceIndex = this.family.FindBestMatch(wanted);

        this.familySearch = ExtractName(this.family.Name);
        this.faceSearch = this.faceIndex >= 0 ? ExtractName(this.faces[this.faceIndex].Name) : string.Empty;
        this.scrollFamily = this.scrollFace = this.scrollSize = true;
    }

    private void SelectFamily(IFontFamilyId f)
    {
        this.family = f;
        this.faces = FacesOf(f);
        this.faceIndex = this.faces.Count > 0 ? f.FindBestMatch(this.style.On(default)) : -1;
        this.style = this.style with { FamilyId = WithFirstFamily(this.style.FamilyId, f) };
        if (this.faceIndex >= 0)
        {
            this.style = this.style.WithFace(this.faces[this.faceIndex].Style);
            this.faceSearch = ExtractName(this.faces[this.faceIndex].Name);
        }
        else
        {
            this.faceSearch = string.Empty;
        }

        this.scrollFace = true;
        this.requestedFont = null;
    }

    // The face a family row previews in. Kept, as finding it lists and sorts the faces.
    private IFontFaceId? PreviewFaceOf(IFontFamilyId f)
    {
        ref var face = ref CollectionsMarshal.GetValueRefOrAddDefault(this.previewFaces, f, out var exists);
        if (!exists)
            face = FacesOf(f) is { Count: > 0 } list ? list[f.FindBestMatch(FontStyle.Default)] : null;
        return face;
    }

    private bool TakeFocus(int column)
    {
        if (this.focusColumn != column)
            return false;
        this.focusColumn = -1;
        return true;
    }

    private void DrawBody()
    {
        var imStyle = ImGui.GetStyle();
        var frameHeight = ImGui.GetFrameHeightWithSpacing();
        var details = this.Fields & DetailFields;
        var twoDetailLines =
            this.detailsOpen
            && (details & FontChooserFields.Locale) != 0
            && (details & (FontChooserFields.LetterSpacing | FontChooserFields.LineHeight)) != 0;
        var detailsHeight = details == 0 ? 0 : frameHeight * (twoDetailLines ? 2 : 1);
        var previewHeight = (ImGui.GetTextLineHeight() * 4) + (imStyle.FramePadding.Y * 2);
        var listsHeight = ImGui.GetContentRegionAvail().Y
                          - detailsHeight
                          - previewHeight
                          - imStyle.ItemSpacing.Y
                          - frameHeight;

        using (var child = ImRaii.Child(
                   "##lists"u8,
                   new(0, Math.Max(listsHeight, frameHeight * 3)),
                   false,
                   ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            if (child)
                this.DrawLists();
        }

        if (details != 0)
            this.DrawDetails(details, twoDetailLines);

        if (this.requestedFont is null)
        {
            this.requestedFont = this.style.GetFont();
            this.StyleChanged?.InvokeSafely(this.style);
        }

        // The font shown stays until the next one is loaded, so that sliders do not make the preview flicker.
        if (this.requestedFont.LoadTask.IsCompleted)
            this.shownFont = this.requestedFont;

        var previewSize = new Vector2(ImGui.GetContentRegionAvail().X, previewHeight);
        if (this.shownFont is null)
        {
            ImGui.InputTextMultiline("##preview"u8, this.previewText, previewSize);
        }
        else
        {
            using (this.shownFont.Push())
                ImGui.InputTextMultiline("##preview"u8, this.previewText, previewSize);
        }

        this.DrawStatusAndButtons();
    }

    private void DrawLists()
    {
        var showSize = (this.Fields & FontChooserFields.Size) != 0;
        using var table = ImRaii.Table("##lists"u8, showSize ? 3 : 2);
        if (!table)
            return;

        var imStyle = ImGui.GetStyle();
        ImGui.TableSetupColumn("##family"u8, ImGuiTableColumnFlags.WidthStretch, 45);
        ImGui.TableSetupColumn("##face"u8, ImGuiTableColumnFlags.WidthStretch, 40);
        if (showSize)
        {
            ImGui.TableSetupColumn(
                "##size"u8,
                ImGuiTableColumnFlags.WidthFixed,
                ImGui.CalcTextSize("000.00 em"u8).X
                + (imStyle.FramePadding.X * 2)
                + ((ImGui.GetFrameHeight() + imStyle.ItemInnerSpacing.X) * 2));
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        this.DrawFamilyColumn();
        ImGui.TableNextColumn();
        this.DrawFaceColumn();
        if (showSize)
        {
            ImGui.TableNextColumn();
            this.DrawSizeColumn();
        }
    }

    private void DrawFamilyColumn()
    {
        ImGui.Text("Family"u8);
        var index = this.familyIndex;
        var changed = DrawSearch(
            "##familySearch"u8,
            ref this.familySearch,
            this.rows,
            ref index,
            this.TakeFocus(0),
            static r => r.Family is null ? null : r.Name);
        this.scrollFamily |= changed;

        var clicked = DrawList(
            "##familyList"u8,
            this.rows,
            index,
            ref this.scrollFamily,
            (i, row, selected) =>
            {
                if (row.Family is null)
                {
                    ImGui.TextDisabled(row.Name);
                    return false;
                }

                ImGui.Indent();
                var r = this.previews.Selectable(i, this.PreviewFaceOf(row.Family), row.Name, selected);
                ImGui.Unindent();
                return r;
            });
        if (clicked >= 0)
        {
            index = clicked;
            this.familySearch = this.rows[clicked].Name;
            this.focusColumn = 0;
            changed = true;
        }

        if (changed && this.rows[index].Family is { } f)
        {
            this.familyIndex = index;
            this.SelectFamily(f);
        }
    }

    private void DrawFaceColumn()
    {
        ImGui.Text("Face"u8);
        var index = this.faceIndex;
        var changed = DrawSearch(
            "##faceSearch"u8,
            ref this.faceSearch,
            this.faces,
            ref index,
            this.TakeFocus(1),
            static f => ExtractName(f.Name));
        this.scrollFace |= changed;

        var clicked = DrawList(
            "##faceList"u8,
            this.faces,
            index,
            ref this.scrollFace,
            (i, face, selected) => this.previews.Selectable(i, face, ExtractName(face.Name), selected));
        if (clicked >= 0)
        {
            index = clicked;
            this.faceSearch = ExtractName(this.faces[clicked].Name);
            this.focusColumn = 1;
            changed = true;
        }

        if (changed)
        {
            this.faceIndex = index;
            this.style = this.style.WithFace(this.faces[index].Style);
            this.requestedFont = null;
        }
    }

    private void DrawSizeColumn()
    {
        var inEm = this.style.Size.Unit == StyleUnit.Em;
        ImGui.Text(inEm ? "Size (em)"u8 : "Size (px)"u8);

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
        var value = this.style.Size.Unit is StyleUnit.Px or StyleUnit.Em ? this.style.Size.Value : this.SizePx;
        if (inEm)
        {
            if (ImGui.InputFloat("##size"u8, ref value, 0.05f, 0.25f, "%.2f em"u8))
            {
                this.style = this.style with { Size = StyleLength.Em(Math.Clamp(value, MinSizeEm, MaxSizeEm)) };
                this.requestedFont = null;
            }
        }
        else if (ImGui.InputFloat("##size"u8, ref value, 1, 4, "%.1f"u8))
        {
            this.style = this.style with { Size = StyleLength.Px(Math.Clamp(value, MinSizePx, MaxSizePx)) };
            this.requestedFont = null;
            this.scrollSize = true;
        }

        var selected = this.style.Size.Unit == StyleUnit.Px ? Array.IndexOf(SizeList, this.style.Size.Value) : -1;
        var clicked = DrawList(
            "##sizeList"u8,
            SizeLabels,
            selected,
            ref this.scrollSize,
            (i, label, isSelected) => this.previews.Selectable(i, null, label, isSelected));
        if (clicked >= 0)
        {
            this.style = this.style with { Size = StyleLength.Px(SizeList[clicked]) };
            this.requestedFont = null;
        }
    }

    private void DrawDetails(FontChooserFields details, bool twoLines)
    {
        if (ImGui.ArrowButton("##details"u8, this.detailsOpen ? ImGuiDir.Down : ImGuiDir.Right))
            this.detailsOpen = !this.detailsOpen;
        ImGui.SameLine();
        ImGui.AlignTextToFramePadding();
        ImGui.Text("Details"u8);
        if (ImGui.IsItemClicked())
            this.detailsOpen = !this.detailsOpen;
        if (!this.detailsOpen)
            return;

        var imStyle = ImGui.GetStyle();
        var inputWidth = ImGui.CalcTextSize("-000.00 px"u8).X + (imStyle.FramePadding.X * 2);
        var em = this.SizePx;
        var old = this.style;

        ImGui.SameLine();
        var indentX = ImGui.GetCursorPosX();
        var sameLine = false;
        if ((details & FontChooserFields.LetterSpacing) != 0)
        {
            Label("Spacing"u8, ref sameLine);
            var spacingInEm = this.style.LetterSpacing.Unit == StyleUnit.Em;
            var spacing = StyleLength.Resolve(this.style.LetterSpacing, em, 1, 0);
            ImGui.SetNextItemWidth(inputWidth);
            if (ImGui.SliderFloat("##letterSpacing"u8, ref spacing, -0.25f * em, 0.5f * em, "%.1f px"u8))
            {
                this.style = this.style with
                {
                    LetterSpacing = spacingInEm ? StyleLength.Em(spacing / em) : StyleLength.Px(spacing),
                };
            }
        }

        if ((details & FontChooserFields.LineHeight) != 0)
        {
            Label("Line height"u8, ref sameLine);
            var ratio = this.LineHeightRatio;
            ImGui.SetNextItemWidth(inputWidth);
            if (ImGui.SliderFloat("##lineHeight"u8, ref ratio, 0.8f, 2f, "%.2fx"u8))
            {
                ratio = Math.Clamp(ratio, 0.5f, 3f);
                this.style = this.style with
                {
                    LineHeight = this.style.LineHeight.Unit == StyleUnit.Px
                                     ? StyleLength.Px(ratio * em)
                                     : StyleLength.Em(ratio),
                };
            }
        }

        if ((details & FontChooserFields.Locale) != 0)
        {
            if (twoLines)
            {
                ImGui.SetCursorPosX(indentX);
                sameLine = false;
            }

            Label("Locale"u8, ref sameLine);
            ImGui.SetNextItemWidth(
                ImGui.CalcTextSize("zh-HK"u8).X + ImGui.GetFrameHeight() + (imStyle.FramePadding.X * 2));
            using var combo = ImRaii.Combo("##locale"u8, this.style.Locale ?? "Auto");
            if (combo)
            {
                if (ImGui.Selectable("Auto"u8, this.style.Locale is null))
                    this.style = this.style with { Locale = null };
                foreach (var locale in Locales)
                {
                    if (ImGui.Selectable(locale, this.style.Locale == locale))
                        this.style = this.style with { Locale = locale };
                }
            }
        }

        var reset = this.style;
        if ((details & FontChooserFields.LetterSpacing) != 0)
            reset.LetterSpacing = default;
        if ((details & FontChooserFields.LineHeight) != 0)
            reset.LineHeight = default;
        if ((details & FontChooserFields.Locale) != 0)
            reset.Locale = null;

        ImGui.SameLine();
        var resetWidth = ImGui.CalcTextSize("Reset"u8).X + (imStyle.FramePadding.X * 2);
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0, ImGui.GetContentRegionAvail().X - resetWidth));
        using (ImRaii.Disabled(reset == this.style))
        {
            if (ImGui.Button("Reset"u8))
                this.style = reset;
        }

        if (this.style != old)
            this.requestedFont = null;

        static void Label(ImU8String text, ref bool sameLine)
        {
            if (sameLine)
                ImGui.SameLine();
            ImGui.AlignTextToFramePadding();
            ImGui.Text(text);
            ImGui.SameLine();
            sameLine = true;
        }
    }

    private void DrawStatusAndButtons()
    {
        ImGui.AlignTextToFramePadding();
        if (this.familiesTask is { IsFaulted: true, Exception: { } listException })
        {
            Error("Failed to list the fonts."u8, listException.InnerExceptions);
        }
        else if (this.requestedFont is { LoadTask.IsCompleted: false })
        {
            ImGui.TextDisabled("Loading the font..."u8);
        }
        else if (this.familiesTask?.IsCompleted is not true)
        {
            ImGui.TextDisabled("Listing the fonts..."u8);
        }
        else if (this.shownFont is { LoadTask: { IsFaulted: true, Exception: { } loadException } })
        {
            Error("Failed to load the font."u8, loadException.InnerExceptions);
        }
        else if (this.shownFont is { LoadErrors: { Count: > 0 } errors })
        {
            Error(
                errors.Count == 1 ? "1 font failed to load." : $"{errors.Count} fonts failed to load.",
                errors);
        }
        else
        {
            ImGui.Text(string.Empty);
        }

        var imStyle = ImGui.GetStyle();
        var buttonsWidth = ImGui.CalcTextSize("Refresh"u8).X
                           + ImGui.CalcTextSize("OK"u8).X
                           + ImGui.CalcTextSize("Cancel"u8).X
                           + (imStyle.FramePadding.X * 6)
                           + (imStyle.ItemSpacing.X * 2);
        ImGui.SameLine();
        ImGui.SetCursorPosX(ImGui.GetCursorPosX() + Math.Max(0, ImGui.GetContentRegionAvail().X - buttonsWidth));

        using (ImRaii.Disabled(this.familiesTask?.IsCompleted is not true))
        {
            if (ImGui.Button("Refresh"u8))
                this.familiesTask = Task.Run(() => this.ListFamilies(true));
        }

        ImGui.SameLine();
        using (ImRaii.Disabled(
                   this.requestedFont is not { LoadTask.IsCompletedSuccessfully: true }
                   || this.familyFilter?.Invoke(this.family) is false))
        {
            if (ImGui.Button("OK"u8))
            {
                this.tcs.TrySetResult(this.style);
                ImGui.GetIO().WantCaptureKeyboard = false;
                ImGui.GetIO().WantTextInput = false;
            }
        }

        ImGui.SameLine();
        if (ImGui.Button("Cancel"u8))
            this.Cancel();

        static void Error(ImU8String text, IEnumerable<Exception> exceptions)
        {
            ImGui.TextColored(ImGuiColors.ErrorForeground, text);
            if (!ImGui.IsItemHovered())
                return;

            using var tooltip = ImRaii.Tooltip();
            foreach (var e in exceptions)
            {
                ImGui.Text(
                    e is FontLoadException fle
                        ? $"{fle.Family}: {fle.InnerException?.Message ?? fle.Message}"
                        : e.Message);
            }
        }
    }

    // A row of the family list.
    private readonly record struct FamilyRow(IFontFamilyId? Family, string Name);
}
