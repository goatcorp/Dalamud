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
using Dalamud.Interface.ManagedFontAtlas.Internals;
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

    private bool popupPositionChanged;
    private bool popupSizeChanged;
    private Vector2 popupPosition = new(float.NaN);
    private Vector2 popupSize = new(float.NaN);

    /// <summary>Initializes a new instance of the <see cref="SingleFontChooserDialog"/> class.</summary>
    /// <param name="newAsyncAtlas">A new instance of <see cref="IFontAtlas"/> created using
    /// <see cref="FontAtlasAutoRebuildMode.Async"/> as its auto-rebuild mode.</param>
    /// <remarks>The passed instance of <paramref see="newAsyncAtlas"/> will be disposed after use. If you pass an atlas
    /// that is already being used, then all the font handles under the passed atlas will be invalidated upon disposing
    /// this font chooser. Consider using <see cref="SingleFontChooserDialog(UiBuilder, bool, string?)"/> for automatic
    /// handling of font atlas derived from a <see cref="UiBuilder"/>, or even <see cref="CreateAuto"/> for automatic
    /// registration and unregistration of <see cref="Draw"/> event handler in addition to automatic disposal of this
    /// class and the temporary font atlas for this font chooser dialog.</remarks>
    [Obsolete("See remarks, and use the other constructor.", false)]
    [Api10ToDo("Make private.")]
    public SingleFontChooserDialog(IFontAtlas newAsyncAtlas)
    {
        this.counter = Interlocked.Increment(ref counterStatic);
        this.title = "Choose a font...";
        this.popupImGuiName = $"{this.title}##{nameof(SingleFontChooserDialog)}[{this.counter}]";
        this.atlas = newAsyncAtlas;
        this.selectedFont = new() { FontId = DalamudDefaultFontAndFamilyId.Instance };
        Encoding.UTF8.GetBytes("Font preview.\n0123456789!", this.fontPreviewText);
    }

#pragma warning disable CS0618 // Type or member is obsolete
    // TODO: Api10ToDo; Remove this pragma warning disable line
    
    /// <summary>Initializes a new instance of the <see cref="SingleFontChooserDialog"/> class.</summary>
    /// <param name="uiBuilder">The relevant instance of UiBuilder.</param>
    /// <param name="isGlobalScaled">Whether the fonts in the atlas is global scaled.</param>
    /// <param name="debugAtlasName">Atlas name for debugging purposes.</param>
    /// <remarks>
    /// <para>The passed <see cref="UiBuilder"/> is only used for creating a temporary font atlas. It will not
    /// automatically register a hander for <see cref="UiBuilder.Draw"/>.</para>
    /// <para>Consider using <see cref="CreateAuto"/> for automatic registration and unregistration of
    /// <see cref="Draw"/> event handler in addition to automatic disposal of this class and the temporary font atlas
    /// for this font chooser dialog.</para>
    /// </remarks>
    public SingleFontChooserDialog(UiBuilder uiBuilder, bool isGlobalScaled = true, string? debugAtlasName = null)
        : this(uiBuilder.CreateFontAtlas(FontAtlasAutoRebuildMode.Async, isGlobalScaled, debugAtlasName))
    {
    }

    /// <summary>Initializes a new instance of the <see cref="SingleFontChooserDialog"/> class.</summary>
    /// <param name="factory">An instance of <see cref="FontAtlasFactory"/>.</param>
    /// <param name="debugAtlasName">The temporary atlas name.</param>
    internal SingleFontChooserDialog(FontAtlasFactory factory, string debugAtlasName)
        : this(factory.CreateFontAtlas(debugAtlasName, FontAtlasAutoRebuildMode.Async))
    {
    }
    
#pragma warning restore CS0618 // Type or member is obsolete
    // TODO: Api10ToDo; Remove this pragma warning restore line

    /// <summary>Called when the selected font spec has changed.</summary>
    public event Action<SingleFontSpec>? SelectedFontSpecChanged;

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
            this.useAdvancedOptions |= value.LetterSpacing != 0f;

            this.SelectedFontSpecChanged?.Invoke(this.selectedFont);
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

    /// <summary>Gets or sets a value indicating whether this popup should be modal, blocking everything behind from
    /// being interacted.</summary>
    /// <remarks>If <c>true</c>, then <see cref="ImGui.BeginPopupModal(string, ref bool, ImGuiWindowFlags)"/> will be
    /// used. Otherwise, <see cref="ImGui.Begin(string, ref bool, ImGuiWindowFlags)"/> will be used.</remarks>
    public bool IsModal { get; set; } = true;

    /// <summary>Gets or sets the window flags.</summary>
    public ImGuiWindowFlags WindowFlags { get; set; }

    /// <summary>Gets or sets the popup window position.</summary>
    /// <remarks>
    /// <para>Setting the position only works before the first call to <see cref="Draw"/>.</para>
    /// <para>If any of the coordinates are <see cref="float.NaN"/>, default position will be used.</para>
    /// <para>The position will be clamped into the work area of the selected monitor.</para>
    /// </remarks>
    public Vector2 PopupPosition
    {
        get => this.popupPosition;
        set
        {
            this.popupPositionChanged = true;
            this.popupPosition = value;
        }
    }

    /// <summary>Gets or sets the popup window size.</summary>
    /// <remarks>
    /// <para>Setting the size only works before the first call to <see cref="Draw"/>.</para>
    /// <para>If any of the coordinates are <see cref="float.NaN"/>, default size will be used.</para>
    /// <para>The size will be clamped into the work area of the selected monitor.</para>
    /// </remarks>
    public Vector2 PopupSize
    {
        get => this.popupSize;
        set
        {
            this.popupSizeChanged = true;
            this.popupSize = value;
        }
    }

    /// <summary>Creates a new instance of <see cref="SingleFontChooserDialog"/> that will automatically draw and
    /// dispose itself as needed; calling <see cref="Draw"/> and <see cref="Dispose"/> are handled automatically.
    /// </summary>
    /// <param name="uiBuilder">An instance of <see cref="UiBuilder"/>.</param>
    /// <returns>The new instance of <see cref="SingleFontChooserDialog"/>.</returns>
    public static SingleFontChooserDialog CreateAuto(UiBuilder uiBuilder)
    {
        var fcd = new SingleFontChooserDialog(uiBuilder);
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

    /// <summary>Gets the default popup size before clamping to monitor work area.</summary>
    /// <returns>The default popup size.</returns>
    public static Vector2 GetDefaultPopupSizeNonClamped()
    {
        ThreadSafety.AssertMainThread();
        return new Vector2(40, 30) * ImGui.GetTextLineHeight();
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

    /// <summary>Sets <see cref="PopupSize"/> and <see cref="PopupPosition"/> to be at the center of the current window
    /// being drawn.</summary>
    /// <param name="preferredPopupSize">The preferred popup size.</param>
    public void SetPopupPositionAndSizeToCurrentWindowCenter(Vector2 preferredPopupSize)
    {
        ThreadSafety.AssertMainThread();
        this.PopupSize = preferredPopupSize;
        this.PopupPosition = ImGui.GetWindowPos() + ((ImGui.GetWindowSize() - preferredPopupSize) / 2);
    }

    /// <summary>Sets <see cref="PopupSize"/> and <see cref="PopupPosition"/> to be at the center of the current window
    /// being drawn.</summary>
    public void SetPopupPositionAndSizeToCurrentWindowCenter() =>
        this.SetPopupPositionAndSizeToCurrentWindowCenter(GetDefaultPopupSizeNonClamped());

    /// <summary>
    /// Draws this dialog.
    /// </summary>
    public void Draw()
    {
        const float popupMinWidth = 320;
        const float popupMinHeight = 240;

        ImGui.GetIO().WantCaptureKeyboard = true;
        ImGui.GetIO().WantTextInput = true;
        if (ImGui.IsKeyPressed(ImGuiKey.Escape))
        {
            this.Cancel();
            return;
        }

        if (this.firstDraw)
        {
            if (this.IsModal)
                ImGui.OpenPopup(this.popupImGuiName);
        }

        if (this.firstDraw || this.popupPositionChanged || this.popupSizeChanged)
        {
            var preferProvidedSize = !float.IsNaN(this.popupSize.X) && !float.IsNaN(this.popupSize.Y);
            var size = preferProvidedSize ? this.popupSize : GetDefaultPopupSizeNonClamped();
            size.X = Math.Max(size.X, popupMinWidth);
            size.Y = Math.Max(size.Y, popupMinHeight);

            var preferProvidedPos = !float.IsNaN(this.popupPosition.X) && !float.IsNaN(this.popupPosition.Y);
            var monitorLocatorPos = preferProvidedPos ? this.popupPosition + (size / 2) : ImGui.GetMousePos();

            var monitors = ImGui.GetPlatformIO().Monitors;
            var preferredMonitor = 0;
            var preferredDistance = GetDistanceFromMonitor(monitorLocatorPos, monitors[0]);
            for (var i = 1; i < monitors.Size; i++)
            {
                var distance = GetDistanceFromMonitor(monitorLocatorPos, monitors[i]);
                if (distance < preferredDistance)
                {
                    preferredMonitor = i;
                    preferredDistance = distance;
                }
            }

            var lt = monitors[preferredMonitor].WorkPos;
            var workSize = monitors[preferredMonitor].WorkSize;
            size.X = Math.Min(size.X, workSize.X);
            size.Y = Math.Min(size.Y, workSize.Y);
            var rb = (lt + workSize) - size;

            var pos =
                preferProvidedPos
                    ? new(Math.Clamp(this.PopupPosition.X, lt.X, rb.X), Math.Clamp(this.PopupPosition.Y, lt.Y, rb.Y))
                    : (lt + rb) / 2;

            ImGui.SetNextWindowSize(size, ImGuiCond.Always);
            ImGui.SetNextWindowPos(pos, ImGuiCond.Always);
            this.popupPositionChanged = this.popupSizeChanged = false;
        }

        ImGui.SetNextWindowSizeConstraints(new(popupMinWidth, popupMinHeight), new(float.MaxValue));
        if (this.IsModal)
        {
            var open = true;
            if (!ImGui.BeginPopupModal(this.popupImGuiName, ref open, this.WindowFlags) || !open)
            {
                this.Cancel();
                return;
            }
        }
        else
        {
            var open = true;
            if (!ImGui.Begin(this.popupImGuiName, ref open, this.WindowFlags) || !open)
            {
                ImGui.End();
                this.Cancel();
                return;
            }
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

        this.popupPosition = ImGui.GetWindowPos();
        this.popupSize = ImGui.GetWindowSize();
        if (this.IsModal)
            ImGui.EndPopup();
        else
            ImGui.End();

        this.firstDraw = false;
        this.firstDrawAfterRefresh = false;
    }

    private static float GetDistanceFromMonitor(Vector2 point, ImGuiPlatformMonitorPtr monitor)
    {
        var lt = monitor.MainPos;
        var rb = monitor.MainPos + monitor.MainSize;
        var xoff =
            point.X < lt.X
                ? lt.X - point.X
                : point.X > rb.X
                    ? point.X - rb.X
                    : 0;
        var yoff =
            point.Y < lt.Y
                ? lt.Y - point.Y
                : point.Y > rb.Y
                    ? point.Y - rb.Y
                    : 0;
        return MathF.Sqrt((xoff * xoff) + (yoff * yoff));
    }

    private void DrawChoices()
    {
        var lineHeight = ImGui.GetTextLineHeight();
        var previewHeight = (ImGui.GetFrameHeightWithSpacing() - lineHeight) +
                            Math.Max(lineHeight, this.selectedFont.LineHeightPx * 2);

        var advancedOptionsHeight = ImGui.GetFrameHeightWithSpacing() * (this.useAdvancedOptions ? 4 : 1);

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

        ImGui.Checkbox("Show advanced options", ref this.useAdvancedOptions);
        if (this.useAdvancedOptions)
        {
            if (this.DrawAdvancedOptions())
            {
                this.fontHandle?.Dispose();
                this.fontHandle = null;
            }
        }

        if (this.fontHandle is null)
        {
            if (this.IgnorePreviewGlobalScale)
            {
                this.fontHandle = this.selectedFont.CreateFontHandle(
                    this.atlas,
                    tk => tk.OnPreBuild(e => e.SetFontScaleMode(e.Font, FontScaleMode.UndoGlobalScale)));
            }
            else
            {
                this.fontHandle = this.selectedFont.CreateFontHandle(this.atlas);
            }

            this.SelectedFontSpecChanged?.InvokeSafely(this.selectedFont);
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

        if (!ImGui.BeginTable("##advancedOptions", 4))
            return false;

        var labelWidth = ImGui.CalcTextSize("Letter Spacing:").X;
        labelWidth = Math.Max(labelWidth, ImGui.CalcTextSize("Offset:").X);
        labelWidth = Math.Max(labelWidth, ImGui.CalcTextSize("Line Height:").X);
        labelWidth += ImGui.GetStyle().FramePadding.X;

        var inputWidth = ImGui.CalcTextSize("000.000").X + (ImGui.GetStyle().FramePadding.X * 2);
        ImGui.TableSetupColumn(
            "##inputLabelColumn",
            ImGuiTableColumnFlags.WidthFixed,
            labelWidth);
        ImGui.TableSetupColumn(
            "##input1Column",
            ImGuiTableColumnFlags.WidthFixed,
            inputWidth);
        ImGui.TableSetupColumn(
            "##input2Column",
            ImGuiTableColumnFlags.WidthFixed,
            inputWidth);
        ImGui.TableSetupColumn(
            "##fillerColumn",
            ImGuiTableColumnFlags.WidthStretch,
            1f);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Offset:");

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

        ImGui.TableNextColumn();
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

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Letter Spacing:");

        ImGui.TableNextColumn();
        if (FloatInputText(
                "##letterSpacingXInput",
                ref this.advUiState.LetterSpacingText,
                this.selectedFont.LetterSpacing) is { } newLetterSpacing)
        {
            changed = true;
            this.selectedFont = this.selectedFont with { LetterSpacing = newLetterSpacing };
        }

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.AlignTextToFramePadding();
        ImGui.TextUnformatted("Line Height:");

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
                    LetterSpacing = default,
                };

                this.advUiState = new(this.selectedFont);
                this.fontHandle?.Dispose();
                this.fontHandle = null;
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
        what.GetLocalizedName(Service<DalamudConfiguration>.Get().EffectiveLanguage);
    // Note: EffectiveLanguage can be incorrect but close enough for now

    private bool TestName(IObjectWithLocalizableName what, string search) =>
        this.ExtractName(what).Contains(search, StringComparison.CurrentCultureIgnoreCase);

    private struct AdvancedOptionsUiState
    {
        public string OffsetXText;
        public string OffsetYText;
        public string LetterSpacingText;
        public string LineHeightText;

        public AdvancedOptionsUiState(SingleFontSpec spec)
        {
            this.OffsetXText = $"{spec.GlyphOffset.X:0.##}";
            this.OffsetYText = $"{spec.GlyphOffset.Y:0.##}";
            this.LetterSpacingText = $"{spec.LetterSpacing:0.##}";
            this.LineHeightText = $"{spec.LineHeight:0.##}";
        }
    }
}
