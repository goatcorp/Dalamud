using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Configuration.Internal;
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
[SuppressMessage(
    "StyleCop.CSharp.LayoutRules",
    "SA1519:Braces should not be omitted from multi-line child statement",
    Justification = "Multiple fixed blocks")]
public sealed class SingleFontChooserDialog : IDisposable
{
    private const float MinFontSizePt = 1;

    private const float MaxFontSizePt = 127;

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
    private readonly TaskCompletionSource<SingleFontSpec> tcs = new();
    private readonly IFontAtlas atlas;

    private string popupImGuiName;
    private string title;

    private bool firstDraw = true;
    private bool firstDrawAfterRefresh;
    private int setFocusOn = -1;

    private bool useAdvancedOptions;
    private AdvancedOptionsUiState advUiState;

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
    private SingleFontSpec selectedFont;

    /// <summary>
    /// Initializes a new instance of the <see cref="SingleFontChooserDialog"/> class.
    /// </summary>
    /// <param name="newAsyncAtlas">A new instance of <see cref="IFontAtlas"/> created using
    /// <see cref="FontAtlasAutoRebuildMode.Async"/> as its auto-rebuild mode.</param>
    public SingleFontChooserDialog(IFontAtlas newAsyncAtlas)
    {
        this.counter = Interlocked.Increment(ref counterStatic);
        this.title = "Choose a font...";
        this.popupImGuiName = $"{this.title}##{nameof(SingleFontChooserDialog)}[{this.counter}]";
        this.atlas = newAsyncAtlas;
        this.selectedFont = new() { FontId = DalamudDefaultFontAndFamilyId.Instance };
        Encoding.UTF8.GetBytes("Font preview.\n0123456789!", this.fontPreviewText);
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
            this.popupImGuiName = $"{this.title}##{nameof(SingleFontChooserDialog)}[{this.counter}]";
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
    public Task<SingleFontSpec> ResultTask => this.tcs.Task;

    /// <summary>
    /// Gets or sets the selected family and font.
    /// </summary>
    public SingleFontSpec SelectedFont
    {
        get => this.selectedFont;
        set
        {
            this.selectedFont = value;

            var familyName = value.FontId.Family.ToString() ?? string.Empty;
            var fontName = value.FontId.ToString() ?? string.Empty;
            this.familySearch = this.ExtractName(value.FontId.Family);
            this.fontSearch = this.ExtractName(value.FontId);
            if (this.fontFamilies?.IsCompletedSuccessfully is true)
                this.UpdateSelectedFamilyAndFontIndices(this.fontFamilies.Result, familyName, fontName);
            this.fontSizeSearch = $"{value.SizePt:0.##}";
            this.advUiState = new(value);
            this.useAdvancedOptions |= Math.Abs(value.LineHeight - 1f) > 0.000001;
            this.useAdvancedOptions |= value.GlyphOffset != default;
            this.useAdvancedOptions |= value.GlyphExtraSpacing != default;
        }
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
    /// Creates a new instance of <see cref="SingleFontChooserDialog"/> that will automatically draw and dispose itself as
    /// needed.
    /// </summary>
    /// <param name="uiBuilder">An instance of <see cref="UiBuilder"/>.</param>
    /// <returns>The new instance of <see cref="SingleFontChooserDialog"/>.</returns>
    public static SingleFontChooserDialog CreateAuto(UiBuilder uiBuilder)
    {
        var fcd = new SingleFontChooserDialog(uiBuilder.CreateFontAtlas(FontAtlasAutoRebuildMode.Async));
        uiBuilder.Draw += fcd.Draw;
        fcd.tcs.Task.ContinueWith(
            r =>
            {
                _ = r.Exception;
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
    /// Cancels this dialog.
    /// </summary>
    public void Cancel()
    {
        this.tcs.SetCanceled();
        ImGui.GetIO().WantCaptureKeyboard = false;
        ImGui.GetIO().WantTextInput = false;
    }

    /// <summary>
    /// Draws this dialog.
    /// </summary>
    public void Draw()
    {
        if (this.firstDraw)
            ImGui.OpenPopup(this.popupImGuiName);

        ImGui.GetIO().WantCaptureKeyboard = true;
        ImGui.GetIO().WantTextInput = true;
        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            this.Cancel();
            return;
        }

        var open = true;
        ImGui.SetNextWindowSize(new(640, 480), ImGuiCond.Appearing);
        if (!ImGui.BeginPopupModal(this.popupImGuiName, ref open) || !open)
        {
            this.Cancel();
            return;
        }

        var framePad = ImGui.GetStyle().FramePadding;
        var windowPad = ImGui.GetStyle().WindowPadding;
        var baseOffset = ImGui.GetCursorPos() - windowPad;

        var actionSize = Vector2.Zero;
        actionSize = Vector2.Max(actionSize, ImGui.CalcTextSize("OK"));
        actionSize = Vector2.Max(actionSize, ImGui.CalcTextSize("Cancel"));
        actionSize = Vector2.Max(actionSize, ImGui.CalcTextSize("Refresh"));
        actionSize = Vector2.Max(actionSize, ImGui.CalcTextSize("Reset"));
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
                            Math.Max(lineHeight, this.selectedFont.LineHeightPx * 2);

        ImGui.Checkbox("Show advanced options", ref this.useAdvancedOptions);

        var advancedOptionsHeight =
            this.useAdvancedOptions
                ? ImGui.GetFrameHeightWithSpacing() * 3
                : 0;

        var tableSize = ImGui.GetContentRegionAvail() -
                        new Vector2(0, ImGui.GetStyle().WindowPadding.Y + previewHeight + advancedOptionsHeight);
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

            if (changed)
            {
                this.fontHandle?.Dispose();
                this.fontHandle = null;
            }

            ImGui.PopStyleVar();

            ImGui.EndTable();
        }

        ImGui.EndChild();

        ImGui.SetCursorPosY(
            ImGui.GetCursorPosY() + (ImGui.GetStyle().WindowPadding.Y - ImGui.GetStyle().ItemSpacing.Y));

        if (this.useAdvancedOptions)
        {
            if (this.DrawAdvancedOptions())
            {
                this.fontHandle?.Dispose();
                this.fontHandle = null;
            }
        }

        if (this.IgnorePreviewGlobalScale)
        {
            this.fontHandle ??= this.selectedFont.CreateFontHandle(
                this.atlas,
                tk =>
                    tk.OnPreBuild(e => e.IgnoreGlobalScale(e.Font))
                      .OnPostBuild(e => e.Font.AdjustGlyphMetrics(1f / e.Scale)));
        }
        else
        {
            this.fontHandle ??= this.selectedFont.CreateFontHandle(this.atlas);
        }

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
            using (this.fontHandle?.Push())
            {
                unsafe
                {
                    fixed (byte* buf = this.fontPreviewText)
                    fixed (byte* label = "##fontPreviewText"u8)
                    {
                        ImGuiNative.igInputTextMultiline(
                            label,
                            buf,
                            (uint)this.fontPreviewText.Length,
                            ImGui.GetContentRegionAvail(),
                            ImGuiInputTextFlags.None,
                            null,
                            null);
                    }
                }
            }
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
            this.selectedFont = this.selectedFont with { FontId = family.Fonts[this.selectedFontIndex] };
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
            this.selectedFont = this.selectedFont with { FontId = font };
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
                            this.selectedFont = this.selectedFont with
                            {
                                SizePt = Math.Min(MaxFontSizePt, MathF.Floor(this.selectedFont.SizePt) + 1),
                            };
                            changed = true;
                            break;
                        case ImGuiKey.UpArrow:
                            this.selectedFont = this.selectedFont with
                            {
                                SizePt = Math.Max(MinFontSizePt, MathF.Ceiling(this.selectedFont.SizePt) - 1),
                            };
                            changed = true;
                            break;
                    }

                    if (changed)
                        ImGuiHelpers.SetTextFromCallback(data, $"{this.selectedFont.SizePt:0.##}");

                    return 0;
                }))
        {
            if (float.TryParse(this.fontSizeSearch, out var fontSizePt1))
            {
                this.selectedFont = this.selectedFont with { SizePt = fontSizePt1 };
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

                    var selected = Equals(FontSizeList[i].Value, this.selectedFont.SizePt);
                    if (ImGui.Selectable(
                            FontSizeList[i].Name,
                            ref selected,
                            ImGuiSelectableFlags.DontClosePopups))
                    {
                        this.selectedFont = this.selectedFont with { SizePt = FontSizeList[i].Value };
                        this.setFocusOn = 2;
                        changed = true;
                    }
                }
            }

            clipper.Destroy();
        }

        ImGui.EndChild();

        if (this.selectedFont.SizePt < MinFontSizePt)
        {
            this.selectedFont = this.selectedFont with { SizePt = MinFontSizePt };
            changed = true;
        }

        if (this.selectedFont.SizePt > MaxFontSizePt)
        {
            this.selectedFont = this.selectedFont with { SizePt = MaxFontSizePt };
            changed = true;
        }

        if (changed)
            this.fontSizeSearch = $"{this.selectedFont.SizePt:0.##}";

        return changed;
    }

    private bool DrawAdvancedOptions()
    {
        var changed = false;
        if (!ImGui.BeginTable("##advancedOptionsTable", 4))
            return false;

        ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, Vector4.Zero);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, Vector4.Zero);
        ImGui.TableSetupColumn(
            "##legendsColumn",
            ImGuiTableColumnFlags.WidthStretch,
            0.1f);
        ImGui.TableSetupColumn(
            "Line Height:##lineHeightColumn",
            ImGuiTableColumnFlags.WidthStretch,
            0.3f);
        ImGui.TableSetupColumn(
            "Offset:##glyphOffsetColumn",
            ImGuiTableColumnFlags.WidthStretch,
            0.3f);
        ImGui.TableSetupColumn(
            "Spacing:##glyphExtraSpacingColumn",
            ImGuiTableColumnFlags.WidthStretch,
            0.3f);
        ImGui.TableHeadersRow();
        ImGui.PopStyleColor(3);

        ImGui.TableNextRow();

        var pad = (int)MathF.Round(8 * ImGuiHelpers.GlobalScale);
        ImGui.PushStyleVar(ImGuiStyleVar.CellPadding, new Vector2(pad));

        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("X");
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Y");

        ImGui.TableNextColumn();
        if (FloatInputText(
                "##lineHeightInput",
                ref this.advUiState.LineHeightText,
                this.selectedFont.LineHeight,
                0.05f,
                0.1f,
                3f) is { } newLineHeight)
        {
            changed = true;
            this.selectedFont = this.selectedFont with { LineHeight = newLineHeight };
        }

        ImGui.TableNextColumn();
        if (FloatInputText(
                "##glyphOffsetXInput",
                ref this.advUiState.OffsetXText,
                this.selectedFont.GlyphOffset.X) is { } newGlyphOffsetX)
        {
            changed = true;
            this.selectedFont = this.selectedFont with
            {
                GlyphOffset = this.selectedFont.GlyphOffset with { X = newGlyphOffsetX },
            };
        }

        if (FloatInputText(
                "##glyphOffsetYInput",
                ref this.advUiState.OffsetYText,
                this.selectedFont.GlyphOffset.Y) is { } newGlyphOffsetY)
        {
            changed = true;
            this.selectedFont = this.selectedFont with
            {
                GlyphOffset = this.selectedFont.GlyphOffset with { Y = newGlyphOffsetY },
            };
        }

        ImGui.TableNextColumn();
        if (FloatInputText(
                "##glyphExtraSpacingXInput",
                ref this.advUiState.ExtraSpacingXText,
                this.selectedFont.GlyphExtraSpacing.X) is { } newGlyphExtraSpacingX)
        {
            changed = true;
            this.selectedFont = this.selectedFont with
            {
                GlyphExtraSpacing = this.selectedFont.GlyphExtraSpacing with { X = newGlyphExtraSpacingX },
            };
        }

        if (FloatInputText(
                "##glyphExtraSpacingYInput",
                ref this.advUiState.ExtraSpacingYText,
                this.selectedFont.GlyphExtraSpacing.Y) is { } newGlyphExtraSpacingY)
        {
            changed = true;
            this.selectedFont = this.selectedFont with
            {
                GlyphExtraSpacing = this.selectedFont.GlyphExtraSpacing with { Y = newGlyphExtraSpacingY },
            };
        }

        ImGui.PopStyleVar();
        ImGui.EndTable();

        return changed;

        static unsafe float? FloatInputText(
            string label, ref string buf, float value, float step = 1f, float min = -127, float max = 127)
        {
            var stylePushed = value < min || value > max || !float.TryParse(buf, out _);
            if (stylePushed)
                ImGui.PushStyleColor(ImGuiCol.Text, ImGuiColors.DalamudRed);

            var changed2 = false;
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X);
            var changed1 = ImGui.InputText(
                label,
                ref buf,
                255,
                ImGuiInputTextFlags.AutoSelectAll | ImGuiInputTextFlags.CallbackHistory |
                ImGuiInputTextFlags.CharsDecimal,
                data =>
                {
                    switch (data->EventKey)
                    {
                        case ImGuiKey.DownArrow:
                            changed2 = true;
                            value = Math.Min(max, (MathF.Round(value / step) * step) + step);
                            ImGuiHelpers.SetTextFromCallback(data, $"{value:0.##}");
                            break;
                        case ImGuiKey.UpArrow:
                            changed2 = true;
                            value = Math.Max(min, (MathF.Round(value / step) * step) - step);
                            ImGuiHelpers.SetTextFromCallback(data, $"{value:0.##}");
                            break;
                    }

                    return 0;
                });
            
            if (stylePushed)
                ImGui.PopStyleColor();

            if (!changed1 && !changed2)
                return null;

            if (!float.TryParse(buf, out var parsed))
                return null;

            if (min > parsed || parsed > max)
                return null;

            return parsed;
        }
    }

    private void DrawActionButtons(Vector2 buttonSize)
    {
        if (this.fontHandle?.Available is not true
            || this.FontFamilyExcludeFilter?.Invoke(this.selectedFont.FontId.Family) is true)
        {
            ImGui.BeginDisabled();
            ImGui.Button("OK", buttonSize);
            ImGui.EndDisabled();
        }
        else if (ImGui.Button("OK", buttonSize))
        {
            this.tcs.SetResult(this.selectedFont);
        }

        if (ImGui.Button("Cancel", buttonSize))
        {
            this.Cancel();
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
                var familyName = this.selectedFont.FontId.Family.ToString() ?? string.Empty;
                var fontName = this.selectedFont.FontId.ToString() ?? string.Empty;

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

        if (this.useAdvancedOptions)
        {
            if (ImGui.Button("Reset", buttonSize))
            {
                this.selectedFont = this.selectedFont with
                {
                    LineHeight = 1f,
                    GlyphOffset = default,
                    GlyphExtraSpacing = default,
                };

                this.advUiState = new(this.selectedFont);
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
            this.selectedFont = this.selectedFont with
            {
                FontId = fonts[this.selectedFamilyIndex].Fonts[this.selectedFontIndex],
            };
        }
    }

    private string ExtractName(IObjectWithLocalizableName what) =>
        what.GetLocaleName(Service<DalamudConfiguration>.Get().EffectiveLanguage);
    // Note: EffectiveLanguage can be incorrect but close enough for now

    private bool TestName(IObjectWithLocalizableName what, string search) =>
        this.ExtractName(what).Contains(search, StringComparison.CurrentCultureIgnoreCase);

    private struct AdvancedOptionsUiState
    {
        public string OffsetXText;
        public string OffsetYText;
        public string ExtraSpacingXText;
        public string ExtraSpacingYText;
        public string LineHeightText;

        public AdvancedOptionsUiState(SingleFontSpec spec)
        {
            this.OffsetXText = $"{spec.GlyphOffset.X:0.##}";
            this.OffsetYText = $"{spec.GlyphOffset.Y:0.##}";
            this.ExtraSpacingXText = $"{spec.GlyphExtraSpacing.X:0.##}";
            this.ExtraSpacingYText = $"{spec.GlyphExtraSpacing.Y:0.##}";
            this.LineHeightText = $"{spec.LineHeight:0.##}";
        }
    }
}
