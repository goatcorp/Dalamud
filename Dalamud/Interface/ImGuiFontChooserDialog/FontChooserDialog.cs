using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Interface.Colors;
using Dalamud.Interface.FontIdentifier;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using ImGuiNET;

using TerraFX.Interop.DirectX;
using TerraFX.Interop.Windows;

namespace Dalamud.Interface.ImGuiFontChooserDialog;

/// <summary>
/// A dialog for choosing a font and its size.
/// </summary>
public sealed class FontChooserDialog : IDisposable
{
    private static readonly List<IFontId> EmptyIFontList = new();

    private static readonly (string Name, float Value)[] FontSizeList =
    {
        ("9.6", 9.6f),
        ("10", 10f),
        ("12", 12f),
        ("14", 14f),
        ("16", 16f),
        ("18", 18f),
        ("18.4", 18.4f),
        ("20", 20),
        ("23", 23),
        ("34", 34),
        ("36", 36),
        ("40", 40),
        ("45", 45),
        ("46", 46),
        ("68", 68),
        ("90", 90),
    };

    private static int counterStatic;

    private readonly int counter;
    private readonly byte[] fontPreviewText = new byte[2048];
    private readonly TaskCompletionSource<(IFontId Font, float SizePx)> tcs = new();
    private readonly IFontAtlas atlas;

    private string popupImGuiName;
    private string title;

    private bool firstDraw = true;
    private bool firstDrawAfterRefresh;
    private int setFocusOn = -1;

    private Task<List<IFontFamilyId>>? fontFamilies;
    private int selectedFamilyIndex = -1;
    private int selectedFontIndex = -1;
    private int selectedFontWeight = (int)DWRITE_FONT_WEIGHT.DWRITE_FONT_WEIGHT_NORMAL;
    private int selectedFontStretch = (int)DWRITE_FONT_STRETCH.DWRITE_FONT_STRETCH_NORMAL;
    private int selectedFontStyle = (int)DWRITE_FONT_STYLE.DWRITE_FONT_STYLE_NORMAL;

    private string familySearch = string.Empty;
    private string fontSearch = string.Empty;
    private string fontSizeSearch = "12";
    private IFontHandle? fontHandle;
    private IFontId? selectedFontId;
    private float fontSizePt = 12;

    /// <summary>
    /// Initializes a new instance of the <see cref="FontChooserDialog"/> class.
    /// </summary>
    /// <param name="newAsyncAtlas">A new instance of <see cref="IFontAtlas"/> created using
    /// <see cref="FontAtlasAutoRebuildMode.Async"/> as its auto-rebuild mode.</param>
    public FontChooserDialog(IFontAtlas newAsyncAtlas)
    {
        this.counter = Interlocked.Increment(ref counterStatic);
        this.title = "Choose a font...";
        this.popupImGuiName = $"{this.title}##{nameof(FontChooserDialog)}[{this.counter}]";
        this.atlas = newAsyncAtlas;
        Encoding.UTF8.GetBytes("Font preview. 0123456789", this.fontPreviewText);
    }

    /// <summary>
    /// Gets or sets the title of this font chooser dialog popup.
    /// </summary>
    public string Title
    {
        get => this.title;
        set
        {
            this.title = value;
            this.popupImGuiName = $"{this.title}##{nameof(FontChooserDialog)}[{this.counter}]";
        }
    }

    /// <summary>
    /// Gets or sets the preview text. A text too long may be truncated on assignment.
    /// </summary>
    public string PreviewText
    {
        get
        {
            var n = this.fontPreviewText.AsSpan().IndexOf((byte)0);
            return n < 0
                       ? Encoding.UTF8.GetString(this.fontPreviewText)
                       : Encoding.UTF8.GetString(this.fontPreviewText, 0, n);
        }
        set => Encoding.UTF8.GetBytes(value, this.fontPreviewText);
    }

    /// <summary>
    /// Gets the task that resolves upon choosing a font or cancellation.
    /// </summary>
    public Task<(IFontId Font, float SizePx)> ResultTask => this.tcs.Task;

    /// <summary>
    /// Gets or sets the selected family and font.
    /// </summary>
    public IFontId? SelectedFontId
    {
        get => this.selectedFontId;
        set
        {
            this.selectedFontId = value;

            var familyName = value?.Family.ToString() ?? string.Empty;
            var fontName = value?.ToString() ?? string.Empty;
            this.familySearch = value is null ? string.Empty : this.ExtractName(value.Family);
            this.fontSearch = value is null ? string.Empty : this.ExtractName(value);
            if (this.fontFamilies?.IsCompletedSuccessfully is true)
                this.UpdateSelectedFamilyAndFontIndices(this.fontFamilies.Result, familyName, fontName);
        }
    }

    /// <summary>
    /// Gets or sets the font size in points.
    /// </summary>
    public float FontSizePt
    {
        get => this.fontSizePt;
        set
        {
            this.fontSizePt = value;
            this.fontSizeSearch = $"{this.fontSizePt:##.###}";
        }
    }

    /// <summary>
    /// Gets or sets the font size in pixels.
    /// </summary>
    public float FontSizePx
    {
        get => (this.FontSizePt * 4) / 3;
        set => this.FontSizePt = (value * 3) / 4;
    }

    /// <summary>
    /// Gets or sets the font family exclusion filter predicate.
    /// </summary>
    public Predicate<IFontFamilyId>? FontFamilyExcludeFilter { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether to ignore the global scale on preview text input.
    /// </summary>
    public bool IgnorePreviewGlobalScale { get; set; }

    /// <summary>
    /// Creates a new instance of <see cref="FontChooserDialog"/> that will automatically draw and dispose itself as
    /// needed.
    /// </summary>
    /// <param name="uiBuilder">An instance of <see cref="UiBuilder"/>.</param>
    /// <returns>The new instance of <see cref="FontChooserDialog"/>.</returns>
    public static FontChooserDialog CreateAuto(UiBuilder uiBuilder)
    {
        var fcd = new FontChooserDialog(uiBuilder.CreateFontAtlas(FontAtlasAutoRebuildMode.Async));
        uiBuilder.Draw += fcd.Draw;
        fcd.tcs.Task.ContinueWith(
            _ =>
            {
                uiBuilder.Draw -= fcd.Draw;
                fcd.Dispose();
            });

        return fcd;
    }

    /// <inheritdoc/> 
    public void Dispose()
    {
        this.fontHandle?.Dispose();
        this.atlas.Dispose();
    }

    /// <summary>
    /// Draws this dialog.
    /// </summary>
    public void Draw()
    {
        if (this.firstDraw)
            ImGui.OpenPopup(this.popupImGuiName);

        ImGui.SetNextWindowSize(new(640, 480), ImGuiCond.Appearing);
        var open = true;
        if (!ImGui.BeginPopupModal(this.popupImGuiName, ref open) || !open)
        {
            this.tcs.SetCanceled();
            return;
        }

        var framePad = ImGui.GetStyle().FramePadding;
        var windowPad = ImGui.GetStyle().WindowPadding;
        var baseOffset = ImGui.GetCursorPos() - windowPad;

        var actionSize = Vector2.Zero;
        actionSize = Vector2.Max(actionSize, ImGui.CalcTextSize("OK"));
        actionSize = Vector2.Max(actionSize, ImGui.CalcTextSize("Cancel"));
        actionSize = Vector2.Max(actionSize, ImGui.CalcTextSize("Refresh"));
        actionSize += framePad * 2;

        var bodySize = ImGui.GetContentRegionAvail();
        ImGui.SetCursorPos(baseOffset + windowPad);
        if (ImGui.BeginChild(
                "##choicesBlock",
                bodySize with { X = bodySize.X - windowPad.X - actionSize.X },
                false,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
        {
            this.DrawChoices();
        }

        ImGui.EndChild();

        ImGui.SetCursorPos(baseOffset + windowPad + new Vector2(bodySize.X - actionSize.X, 0));

        if (ImGui.BeginChild("##actionsBlock", bodySize with { X = actionSize.X }))
        {
            this.DrawActionButtons(actionSize);
        }

        ImGui.EndChild();

        ImGui.EndPopup();

        this.firstDraw = false;
        this.firstDrawAfterRefresh = false;
    }

    private void DrawChoices()
    {
        var lineHeight = ImGui.GetTextLineHeight();
        var previewHeight = (ImGui.GetFrameHeightWithSpacing() - lineHeight) +
                            Math.Max(lineHeight, (this.FontSizePt * 4) / 3);

        var tableSize = ImGui.GetContentRegionAvail() -
                        new Vector2(0, ImGui.GetStyle().WindowPadding.Y + previewHeight);
        if (ImGui.BeginChild(
                "##tableContainer",
                tableSize,
                false,
                ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
            && ImGui.BeginTable("##table", 3, ImGuiTableFlags.None))
        {
            ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.HeaderHovered, Vector4.Zero);
            ImGui.PushStyleColor(ImGuiCol.HeaderActive, Vector4.Zero);
            ImGui.TableSetupColumn(
                "Font:##familyColumn",
                ImGuiTableColumnFlags.WidthStretch,
                0.4f);
            ImGui.TableSetupColumn(
                "Style:##fontColumn",
                ImGuiTableColumnFlags.WidthStretch,
                0.4f);
            ImGui.TableSetupColumn(
                "Size:##sizeColumn",
                ImGuiTableColumnFlags.WidthStretch,
                0.2f);
            ImGui.TableHeadersRow();
            ImGui.PopStyleColor(3);

            ImGui.TableNextRow();

            var pad = (int)MathF.Round(8 * ImGuiHelpers.GlobalScale);
            ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(pad));
            ImGui.TableNextColumn();
            var changed = this.DrawFamilyListColumn();

            ImGui.TableNextColumn();
            changed |= this.DrawFontListColumn(changed);

            ImGui.TableNextColumn();
            changed |= this.DrawSizeListColumn();

            if ((changed || this.fontHandle is null) && this.SelectedFontId is { } fontId)
            {
                this.fontHandle?.Dispose();
                this.fontHandle = this.atlas.NewDelegateFontHandle(
                    tk => tk.OnPreBuild(e => { e.Font = fontId.AddToBuildToolkit(e, this.FontSizePt * 4 / 3); }));
            }

            ImGui.PopStyleVar();

            ImGui.EndTable();
        }

        ImGui.EndChild();

        ImGui.SetCursorPosY(ImGui.GetCursorPosY() + ImGui.GetStyle().WindowPadding.Y - ImGui.GetStyle().ItemSpacing.Y);

        if (this.fontHandle is null)
        {
            ImGui.SetCursorPos(ImGui.GetCursorPos() + ImGui.GetStyle().FramePadding);
            ImGui.TextUnformatted("Select a font.");
        }
        else if (this.fontHandle.LoadException is { } loadException)
        {
            ImGui.SetCursorPos(ImGui.GetCursorPos() + ImGui.GetStyle().FramePadding);
            ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);
            ImGui.TextUnformatted(loadException.Message);
            ImGui.PopStyleColor();
        }
        else if (!this.fontHandle.Available)
        {
            ImGui.SetCursorPos(ImGui.GetCursorPos() + ImGui.GetStyle().FramePadding);
            ImGui.TextUnformatted("Loading font...");
        }
        else
        {
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            var pfgs = ImGui.GetIO().FontGlobalScale;
            if (this.IgnorePreviewGlobalScale)
                ImGui.GetIO().FontGlobalScale = 1;
            using (this.fontHandle?.Push())
                ImGui.InputText("##fontPreviewText", this.fontPreviewText, (uint)this.fontPreviewText.Length);
            ImGui.GetIO().FontGlobalScale = pfgs;
        }
    }

    private unsafe bool DrawFamilyListColumn()
    {
        if (this.fontFamilies?.IsCompleted is not true)
        {
            ImGui.SetScrollY(0);
            ImGui.TextUnformatted("Loading...");
            return false;
        }

        if (!this.fontFamilies.IsCompletedSuccessfully)
        {
            ImGui.SetScrollY(0);
            ImGui.TextUnformatted("Error: " + this.fontFamilies.Exception);
            return false;
        }

        var families = this.fontFamilies.Result;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

        if (this.setFocusOn == 0)
        {
            this.setFocusOn = -1;
            ImGui.SetKeyboardFocusHere();
        }

        var changed = false;
        if (ImGui.InputText(
                "##familySearch",
                ref this.familySearch,
                255,
                ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.CallbackHistory,
                data =>
                {
                    if (families.Count == 0)
                        return 0;

                    var baseIndex = this.selectedFamilyIndex;
                    if (data->SelectionStart == 0 && data->SelectionEnd == data->BufTextLen)
                    {
                        switch (data->EventKey)
                        {
                            case ImGuiKey.DownArrow:
                                this.selectedFamilyIndex = (this.selectedFamilyIndex + 1) % families.Count;
                                changed = true;
                                break;
                            case ImGuiKey.UpArrow:
                                this.selectedFamilyIndex =
                                    (this.selectedFamilyIndex + families.Count - 1) % families.Count;
                                changed = true;
                                break;
                        }

                        if (changed)
                        {
                            ImGuiHelpers.SetTextFromCallback(
                                data,
                                this.ExtractName(families[this.selectedFamilyIndex]));
                        }
                    }
                    else
                    {
                        switch (data->EventKey)
                        {
                            case ImGuiKey.DownArrow:
                                this.selectedFamilyIndex = families.FindIndex(
                                    baseIndex + 1,
                                    x => this.TestName(x, this.familySearch));
                                if (this.selectedFamilyIndex < 0)
                                {
                                    this.selectedFamilyIndex = families.FindIndex(
                                        0,
                                        baseIndex + 1,
                                        x => this.TestName(x, this.familySearch));
                                }

                                changed = true;
                                break;
                            case ImGuiKey.UpArrow:
                                if (baseIndex > 0)
                                {
                                    this.selectedFamilyIndex = families.FindLastIndex(
                                        baseIndex - 1,
                                        x => this.TestName(x, this.familySearch));
                                }

                                if (this.selectedFamilyIndex < 0)
                                {
                                    if (baseIndex < 0)
                                        baseIndex = 0;
                                    this.selectedFamilyIndex = families.FindLastIndex(
                                        families.Count - 1,
                                        families.Count - baseIndex,
                                        x => this.TestName(x, this.familySearch));
                                }

                                changed = true;
                                break;
                        }
                    }

                    return 0;
                }))
        {
            if (!string.IsNullOrWhiteSpace(this.familySearch) && !changed)
            {
                this.selectedFamilyIndex = families.FindIndex(x => this.TestName(x, this.familySearch));
                changed = true;
            }
        }

        if (ImGui.BeginChild("##familyList", ImGui.GetContentRegionAvail()))
        {
            var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
            var lineHeight = ImGui.GetTextLineHeightWithSpacing();

            if ((changed || this.firstDrawAfterRefresh) && this.selectedFamilyIndex != -1)
            {
                ImGui.SetScrollY(
                    (lineHeight * this.selectedFamilyIndex) -
                    ((ImGui.GetContentRegionAvail().Y - lineHeight) / 2));
            }

            clipper.Begin(families.Count, lineHeight);
            while (clipper.Step())
            {
                for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                {
                    if (i < 0)
                    {
                        ImGui.TextUnformatted(" ");
                        continue;
                    }

                    var selected = this.selectedFamilyIndex == i;
                    if (ImGui.Selectable(
                            this.ExtractName(families[i]),
                            ref selected,
                            ImGuiSelectableFlags.DontClosePopups))
                    {
                        this.selectedFamilyIndex = families.IndexOf(families[i]);
                        this.familySearch = this.ExtractName(families[i]);
                        this.setFocusOn = 0;
                        changed = true;
                    }
                }
            }

            clipper.Destroy();
        }

        if (changed && this.selectedFamilyIndex >= 0)
        {
            var family = families[this.selectedFamilyIndex];
            using var matchingFont = default(ComPtr<IDWriteFont>);
            this.selectedFontIndex = family.FindBestMatch(
                this.selectedFontWeight,
                this.selectedFontStretch,
                this.selectedFontStyle);
            this.selectedFontId = family.Fonts[this.selectedFontIndex];
        }

        ImGui.EndChild();
        return changed;
    }

    private unsafe bool DrawFontListColumn(bool changed)
    {
        if (this.fontFamilies?.IsCompleted is not true)
        {
            ImGui.TextUnformatted("Loading...");
            return changed;
        }

        if (!this.fontFamilies.IsCompletedSuccessfully)
        {
            ImGui.TextUnformatted("Error: " + this.fontFamilies.Exception);
            return changed;
        }

        var families = this.fontFamilies.Result;
        var family = this.selectedFamilyIndex >= 0
                     && this.selectedFamilyIndex < families.Count
                         ? families[this.selectedFamilyIndex]
                         : null;
        var fonts = family?.Fonts ?? EmptyIFontList;

        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

        if (this.setFocusOn == 1)
        {
            this.setFocusOn = -1;
            ImGui.SetKeyboardFocusHere();
        }

        if (ImGui.InputText(
                "##fontSearch",
                ref this.fontSearch,
                255,
                ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.CallbackHistory,
                data =>
                {
                    if (fonts.Count == 0)
                        return 0;

                    var baseIndex = this.selectedFontIndex;
                    if (data->SelectionStart == 0 && data->SelectionEnd == data->BufTextLen)
                    {
                        switch (data->EventKey)
                        {
                            case ImGuiKey.DownArrow:
                                this.selectedFontIndex = (this.selectedFontIndex + 1) % fonts.Count;
                                changed = true;
                                break;
                            case ImGuiKey.UpArrow:
                                this.selectedFontIndex = (this.selectedFontIndex + fonts.Count - 1) % fonts.Count;
                                changed = true;
                                break;
                        }

                        if (changed)
                        {
                            ImGuiHelpers.SetTextFromCallback(
                                data,
                                this.ExtractName(fonts[this.selectedFontIndex]));
                        }
                    }
                    else
                    {
                        switch (data->EventKey)
                        {
                            case ImGuiKey.DownArrow:
                                this.selectedFontIndex = fonts.FindIndex(
                                    baseIndex + 1,
                                    x => this.TestName(x, this.fontSearch));
                                if (this.selectedFontIndex < 0)
                                {
                                    this.selectedFontIndex = fonts.FindIndex(
                                        0,
                                        baseIndex + 1,
                                        x => this.TestName(x, this.fontSearch));
                                }

                                changed = true;
                                break;
                            case ImGuiKey.UpArrow:
                                if (baseIndex > 0)
                                {
                                    this.selectedFontIndex = fonts.FindLastIndex(
                                        baseIndex - 1,
                                        x => this.TestName(x, this.fontSearch));
                                }

                                if (this.selectedFontIndex < 0)
                                {
                                    if (baseIndex < 0)
                                        baseIndex = 0;
                                    this.selectedFontIndex = fonts.FindLastIndex(
                                        fonts.Count - 1,
                                        fonts.Count - baseIndex,
                                        x => this.TestName(x, this.fontSearch));
                                }

                                changed = true;
                                break;
                        }
                    }

                    return 0;
                }))
        {
            if (!string.IsNullOrWhiteSpace(this.fontSearch) && !changed)
            {
                this.selectedFontIndex = fonts.FindIndex(x => this.TestName(x, this.fontSearch));
                changed = true;
            }
        }

        if (ImGui.BeginChild("##fontList"))
        {
            var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
            var lineHeight = ImGui.GetTextLineHeightWithSpacing();

            if ((changed || this.firstDrawAfterRefresh) && this.selectedFontIndex != -1)
            {
                ImGui.SetScrollY(
                    (lineHeight * this.selectedFontIndex) -
                    ((ImGui.GetContentRegionAvail().Y - lineHeight) / 2));
            }

            clipper.Begin(fonts.Count, lineHeight);
            while (clipper.Step())
            {
                for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                {
                    if (i < 0)
                    {
                        ImGui.TextUnformatted(" ");
                        continue;
                    }

                    var selected = this.selectedFontIndex == i;
                    if (ImGui.Selectable(
                            this.ExtractName(fonts[i]),
                            ref selected,
                            ImGuiSelectableFlags.DontClosePopups))
                    {
                        this.selectedFontIndex = fonts.IndexOf(fonts[i]);
                        this.fontSearch = this.ExtractName(fonts[i]);
                        this.setFocusOn = 1;
                        changed = true;
                    }
                }
            }

            clipper.Destroy();
        }

        ImGui.EndChild();

        if (changed && family is not null && this.selectedFontIndex >= 0)
        {
            var font = family.Fonts[this.selectedFontIndex];
            this.selectedFontWeight = font.Weight;
            this.selectedFontStretch = font.Stretch;
            this.selectedFontStyle = font.Style;
            this.selectedFontId = font;
        }

        return changed;
    }

    private unsafe bool DrawSizeListColumn()
    {
        var changed = false;
        ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);

        if (this.setFocusOn == 2)
        {
            this.setFocusOn = -1;
            ImGui.SetKeyboardFocusHere();
        }

        if (ImGui.InputText(
                "##fontSizeSearch",
                ref this.fontSizeSearch,
                255,
                ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.CallbackHistory |
                ImGuiInputTextFlags.CharsDecimal,
                data =>
                {
                    switch (data->EventKey)
                    {
                        case ImGuiKey.DownArrow:
                            this.fontSizePt = Math.Min(127, MathF.Floor(this.FontSizePt) + 1);
                            changed = true;
                            break;
                        case ImGuiKey.UpArrow:
                            this.fontSizePt = Math.Max(1, MathF.Ceiling(this.FontSizePt) - 1);
                            changed = true;
                            break;
                    }

                    if (changed)
                        ImGuiHelpers.SetTextFromCallback(data, $"{this.FontSizePt:##.###}");

                    return 0;
                }))
        {
            if (float.TryParse(this.fontSizeSearch, out var fontSizePt1))
            {
                this.fontSizePt = fontSizePt1;
                changed = true;
            }
        }

        if (ImGui.BeginChild("##fontSizeList"))
        {
            var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
            var lineHeight = ImGui.GetTextLineHeightWithSpacing();

            if (changed && this.selectedFontIndex != -1)
            {
                ImGui.SetScrollY(
                    (lineHeight * this.selectedFontIndex) -
                    ((ImGui.GetContentRegionAvail().Y - lineHeight) / 2));
            }

            clipper.Begin(FontSizeList.Length, lineHeight);
            while (clipper.Step())
            {
                for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                {
                    if (i < 0)
                    {
                        ImGui.TextUnformatted(" ");
                        continue;
                    }

                    var selected = Equals(FontSizeList[i].Value, this.FontSizePt);
                    if (ImGui.Selectable(
                            FontSizeList[i].Name,
                            ref selected,
                            ImGuiSelectableFlags.DontClosePopups))
                    {
                        this.fontSizePt = FontSizeList[i].Value;
                        this.setFocusOn = 2;
                        changed = true;
                    }
                }
            }

            clipper.Destroy();
        }

        ImGui.EndChild();

        if (this.FontSizePt < 1)
        {
            this.fontSizePt = 1;
            changed = true;
        }

        if (this.FontSizePt > 127)
        {
            this.fontSizePt = 127;
            changed = true;
        }

        if (changed)
            this.fontSizeSearch = $"{this.FontSizePt:##.###}";

        return changed;
    }

    private void DrawActionButtons(Vector2 buttonSize)
    {
        if (this.fontHandle?.Available is not true || this.SelectedFontId is null)
        {
            ImGui.BeginDisabled();
            ImGui.Button("OK", buttonSize);
            ImGui.EndDisabled();
        }
        else if (ImGui.Button("OK", buttonSize))
        {
            this.tcs.SetResult((this.SelectedFontId, this.FontSizePt * 4 / 3));
        }

        if (ImGui.Button("Cancel", buttonSize))
        {
            this.tcs.SetCanceled();
        }

        var doRefresh = false;
        var isFirst = false;
        if (this.fontFamilies?.IsCompleted is not true)
        {
            isFirst = doRefresh = this.fontFamilies is null;
            ImGui.BeginDisabled();
            ImGui.Button("Refresh", buttonSize);
            ImGui.EndDisabled();
        }
        else if (ImGui.Button("Refresh", buttonSize))
        {
            doRefresh = true;
        }

        if (doRefresh)
        {
            this.fontFamilies =
                this.fontFamilies?.ContinueWith(_ => RefreshBody())
                ?? Task.Run(RefreshBody);
            this.fontFamilies.ContinueWith(_ => this.firstDrawAfterRefresh = true);

            List<IFontFamilyId> RefreshBody()
            {
                var familyName = this.SelectedFontId?.Family.ToString() ?? string.Empty;
                var fontName = this.SelectedFontId?.ToString() ?? string.Empty;

                var newFonts = new List<IFontFamilyId> { DalamudDefaultFontAndFamilyId.Instance };
                newFonts.AddRange(IFontFamilyId.ListDalamudFonts());
                newFonts.AddRange(IFontFamilyId.ListGameFonts());
                var systemFonts = IFontFamilyId.ListSystemFonts(!isFirst);
                systemFonts.Sort(
                    (a, b) => string.Compare(
                        this.ExtractName(a),
                        this.ExtractName(b),
                        StringComparison.CurrentCultureIgnoreCase));
                newFonts.AddRange(systemFonts);
                if (this.FontFamilyExcludeFilter is not null)
                    newFonts.RemoveAll(this.FontFamilyExcludeFilter);

                this.UpdateSelectedFamilyAndFontIndices(newFonts, familyName, fontName);
                return newFonts;
            }
        }
    }

    private void UpdateSelectedFamilyAndFontIndices(
        IReadOnlyList<IFontFamilyId> fonts,
        string familyName,
        string fontName)
    {
        this.selectedFamilyIndex = fonts.FindIndex(x => x.ToString() == familyName);
        if (this.selectedFamilyIndex == -1)
        {
            this.selectedFontIndex = -1;
            this.selectedFontId = null;
        }
        else
        {
            this.selectedFontIndex = -1;
            var family = fonts[this.selectedFamilyIndex];
            for (var i = 0; i < family.Fonts.Count; i++)
            {
                if (family.Fonts[i].ToString() == fontName)
                {
                    this.selectedFontIndex = i;
                    break;
                }
            }

            if (this.selectedFontIndex == -1)
                this.selectedFontIndex = 0;
            this.selectedFontId = fonts[this.selectedFamilyIndex].Fonts[this.selectedFontIndex];
        }
    }

    private string ExtractName(IObjectWithLocalizableName what) => what.LocalizedName;

    private bool TestName(IObjectWithLocalizableName what, string search) =>
        this.ExtractName(what).Contains(search, StringComparison.CurrentCultureIgnoreCase);
}
