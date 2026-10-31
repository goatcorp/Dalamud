using System.Collections.Concurrent;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Bindings.ImGui;
using Dalamud.Data;
using Dalamud.Excel.Sheets;
using Dalamud.Game.Gui;
using Dalamud.Interface.Fonts;
using Dalamud.Interface.Fonts.Internal.SeStrings;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.Internal;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Internal;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Plugin.Services;
using Dalamud.Storage.Assets;
using Dalamud.Utility;

using Lumina.Text;
using Lumina.Text.Parse;
using Lumina.Text.Payloads;
using Lumina.Text.ReadOnly;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>Shows samples of SeString text drawn with ImGui.</summary>
internal class FontSamplesSeStringWidget : IDataWindowWidget, ISeStringCallback
{
    // The game's color themes and their ids. There is no theme 4.
    private static readonly string[] ThemeNames =
        ["Dark", "Light", "Classic FF", "Clear Blue", "Clear White", "Clear Green", "Clear Grey", "Clear Pink"];

    private static readonly int[] ThemeIds = [0, 1, 2, 3, 5, 6, 7, 8];

    // Words in the text that show an asset instead.
    private static readonly (byte[] Name, DalamudAsset Asset)[] AssetEntities =
    [
        ("Dalamud"u8.ToArray(), DalamudAsset.LogoSmall),
        ("White"u8.ToArray(), DalamudAsset.White4X4),
        ("DefaultIcon"u8.ToArray(), DalamudAsset.DefaultIcon),
        ("DisabledIcon"u8.ToArray(), DalamudAsset.DisabledIcon),
        ("OutdatedInstallableIcon"u8.ToArray(), DalamudAsset.OutdatedInstallableIcon),
        ("TroubleIcon"u8.ToArray(), DalamudAsset.TroubleIcon),
        ("DevPluginIcon"u8.ToArray(), DalamudAsset.DevPluginIcon),
        ("UpdateIcon"u8.ToArray(), DalamudAsset.UpdateIcon),
        ("ThirdIcon"u8.ToArray(), DalamudAsset.ThirdIcon),
        ("ThirdInstalledIcon"u8.ToArray(), DalamudAsset.ThirdInstalledIcon),
        ("ChangelogApiBumpIcon"u8.ToArray(), DalamudAsset.ChangelogApiBumpIcon),
        ("InstalledIcon"u8.ToArray(), DalamudAsset.InstalledIcon),
    ];

    // Textures that a layout waits for. Each is rented once, however often the layout is made again.
    private readonly ConcurrentDictionary<ISharedImmediateTexture, byte> loadingTextures = new();

    private ImVectorWrapper<byte> testStringBuffer;
    private ReadOnlySeString? logkind;
    private TextStyle textStyle;
    private float edgeWidth;
    private float edgeSoftness;
    private Vector2 shadowOffset;
    private bool bold;
    private bool italic;
    private uint linkHoverColor;
    private uint linkActiveColor;
    private float opacity;
    private bool wrap;
    private uint edgeColor;
    private uint shadowColor;
    private bool edge;
    private bool shadow;
    private bool forceEdgeColor;
    private int? themeIndex;
    private bool showInteraction;
    private bool useEntity;
    private bool alignToFramePadding;

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "Font: Samples(SeString)";

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; }

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    int? ISeStringCallback.ThemeIndex => this.themeIndex;

    /// <inheritdoc/>
    bool? ISeStringCallback.ShouldHandle(ReadOnlySePayloadSpan payload) =>
        this.forceEdgeColor && payload.MacroCode is MacroCode.EdgeColor or MacroCode.EdgeColorType ? false : null;

    /// <inheritdoc/>
    bool ITextMarkupCallback.TryGetEntity(in TextEntityContext context, out TextEntity entity)
    {
        entity = this.GetEntity(context);
        return entity.Length != 0;
    }

    /// <inheritdoc/>
    bool ITextMarkupCallback.TryGetLinkStyle(in TextLinkContext link, in TextStyle style, out TextStyle result)
    {
        result = style with { Background = link.Held ? this.linkActiveColor : this.linkHoverColor };
        return link.State != TextLinkState.Normal;
    }

    /// <inheritdoc/>
    public void Load()
    {
        this.linkHoverColor = ImGui.GetColorU32(ImGuiCol.ButtonHovered);
        this.linkActiveColor = ImGui.GetColorU32(ImGuiCol.ButtonActive);
        this.opacity = 1;
        this.wrap = true;
        this.forceEdgeColor = false;
        this.themeIndex = null;
        this.textStyle = default;
        this.edgeWidth = TextEdge.DefaultWidth;
        this.edgeSoftness = 0;
        this.shadowOffset = TextShadow.DefaultOffset;
        this.bold = this.italic = false;
        this.edgeColor = this.shadowColor = 0xFF000000;
        this.edge = this.shadow = false;
        this.logkind = null;
        this.showInteraction = this.useEntity = true;
        this.alignToFramePadding = false;
        this.Ready = true;

        // The answers to the callback changed.
        ImGui.InvalidateTextCallback(this);
    }

    /// <inheritdoc/>
    public unsafe void Draw()
    {
        var color = this.textStyle.Color != 0 ? this.textStyle.Color.Value : ImGui.GetColorU32(ImGuiCol.Text);
        if (ImGui.ColorEdit4("Color"u8, ref color))
            this.textStyle.Color = color;

        ImGui.ColorEdit4("Edge Color"u8, ref this.edgeColor);

        ImGui.SameLine();
        if (ImGui.Checkbox("Forced"u8, ref this.forceEdgeColor))
            ImGui.InvalidateTextCallback(this);

        ImGui.ColorEdit4("Shadow Color"u8, ref this.shadowColor);
        ImGui.ColorEdit4("Link Hover Color"u8, ref this.linkHoverColor);
        ImGui.ColorEdit4("Link Active Color"u8, ref this.linkActiveColor);

        ImGui.DragFloat("Opacity"u8, ref this.opacity, 0.005f, 0f, 1f, "%.02f");

        ImGui.DragFloat("Edge Width (em)"u8, ref this.edgeWidth, 0.001f, 0f, 0.5f, "%.03f");
        ImGui.DragFloat("Edge Softness"u8, ref this.edgeSoftness, 0.01f, 0f, 2f, "%.02f");
        ImGui.DragFloat2("Shadow Offset (em)"u8, ref this.shadowOffset, 0.001f, -0.5f, 0.5f, "%.03f");

        ImGui.Checkbox("Edge"u8, ref this.edge);
        ImGui.SameLine();
        ImGui.Checkbox("Bold"u8, ref this.bold);
        ImGui.SameLine();
        ImGui.Checkbox("Italic"u8, ref this.italic);
        ImGui.SameLine();
        ImGui.Checkbox("Shadow"u8, ref this.shadow);
        this.textStyle.Bold = this.bold;
        this.textStyle.Italic = this.italic;
        this.textStyle.Edge = new(this.edge ? this.edgeColor : 0, StyleLength.Em(this.edgeWidth))
        {
            Softness = this.edgeSoftness,
        };
        this.textStyle.Shadow = new(this.shadow ? this.shadowColor : 0, this.shadowOffset);

        ImGui.SameLine();
        var themeItem = Array.IndexOf(ThemeIds, this.themeIndex ?? SeStringGameData.GameThemeIndex());
        using (ImRaii.ItemWidth(ImGui.CalcTextSize("WWWWWWWWWWWWWW"u8).X))
        {
            if (ImGui.Combo("##theme"u8, ref themeItem, ThemeNames))
            {
                this.themeIndex = ThemeIds[themeItem];
                ImGui.InvalidateTextCallback(this);
            }
        }

        ImGui.SameLine();
        ImGui.Checkbox("Word Wrap"u8, ref this.wrap);

        ImGui.Checkbox("Show Interaction"u8, ref this.showInteraction);

        ImGui.SameLine();
        if (ImGui.Checkbox("Use Entity Replacements"u8, ref this.useEntity))
            ImGui.InvalidateTextCallback(this);

        ImGui.SameLine();
        ImGui.Checkbox("Align to Frame Padding"u8, ref this.alignToFramePadding);

        if (ImGui.CollapsingHeader("LogKind Preview"u8))
        {
            if (this.logkind is null)
            {
                var tt = new SeStringBuilder();
                foreach (var uc in Service<DataManager>.Get().GetExcelSheet<LogKind>())
                {
                    var ucsp = uc.Format.AsSpan();
                    if (ucsp.IsEmpty)
                        continue;

                    tt.Append($"#{uc.RowId}: ");
                    foreach (var p in ucsp.GetOffsetEnumerator())
                    {
                        if (p.Payload.Type == ReadOnlySePayloadType.Macro && p.Payload.MacroCode == MacroCode.String)
                        {
                            tt.Append("Text"u8);
                            continue;
                        }

                        tt.Append(new ReadOnlySeStringSpan(ucsp.Data.Slice(p.Offset, p.Payload.EnvelopeByteLength)));
                    }

                    tt.BeginMacro(MacroCode.NewLine).EndMacro();
                }

                this.logkind = tt.ToReadOnlySeString();
            }

            this.DrawSeString(this.logkind.Value.Data.Span, TextMarkup.SeString);
        }

        if (ImGui.CollapsingHeader("Draw into drawlist"u8))
        {
            ImGuiHelpers.ScaledDummy(100);
            var clipMin = ImGui.GetItemRectMin() + ImGui.GetStyle().FramePadding;
            var clipMax = ImGui.GetItemRectMax() - ImGui.GetStyle().FramePadding;
            clipMin.Y = MathF.Max(clipMin.Y, ImGui.GetWindowPos().Y);
            clipMax.Y = MathF.Min(clipMax.Y, ImGui.GetWindowPos().Y + ImGui.GetWindowHeight());

            var dl = ImGui.GetWindowDrawList();
            dl.PushClipRect(clipMin, clipMax);
            using (ImGui.PushTextCallback(this))
            {
                dl.AddText(
                    this.FontFor(TextMarkup.MacroString),
                    ImGui.GetFontSize(),
                    clipMin,
                    ImGui.GetColorU32(ImGuiCol.Text),
                    "<icon(1)>Test test<icon(1)>"u8);
            }

            dl.PopClipRect();
        }

        if (ImGui.CollapsingHeader("Addon Table"u8))
        {
            using var table = ImRaii.Table("Addon Sheet"u8, 3);
            if (table.Success)
            {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn("Row ID"u8, ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("0000000"u8).X);
                ImGui.TableSetupColumn("Text"u8, ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(
                    "Misc"u8,
                    ImGuiTableColumnFlags.WidthFixed,
                    ImGui.CalcTextSize("AAAAAAAAAAAAAAAAA"u8).X);
                ImGui.TableHeadersRow();

                var addon = Service<DataManager>.GetNullable()?.GetExcelSheet<Addon>() ??
                            throw new InvalidOperationException("Addon sheet not loaded.");

                var clipper = ImGui.ImGuiListClipper();
                clipper.Begin(addon.Count);
                while (clipper.Step())
                {
                    for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                    {
                        var row = addon.GetRowAt(i);

                        ImGui.TableNextRow();
                        using var pushedId = ImRaii.PushId(i);

                        ImGui.TableNextColumn();
                        ImGui.AlignTextToFramePadding();
                        ImGui.Text($"{row.RowId}");

                        ImGui.TableNextColumn();
                        ImGui.AlignTextToFramePadding();
                        this.DrawSeString(row.Text.Data.Span, TextMarkup.SeString);

                        ImGui.TableNextColumn();
                        if (ImGui.Button("Print to Chat"u8))
                            Service<ChatGui>.Get().Print(row.Text);
                    }
                }

                clipper.Destroy();
            }
        }

        if (ImGui.Button("Reset Text"u8) || this.testStringBuffer.IsDisposed)
        {
            this.testStringBuffer.Dispose();
            this.testStringBuffer = ImVectorWrapper.CreateFromSpan(
                "<icon(1)><icon(2)><icon(3)><icon(4)><icon(5)><icon(6)><icon(7)><icon(8)><icon(9)><icon(10)>"u8 +
                "<icon(11)><icon(12)><icon(13)><icon(14)><icon(15)><icon(16)><icon(17)><icon(18)><icon(19)>"u8 +
                "<icon(20)><icon(21)><icon(22)><icon(23)><icon(24)><icon(25)>\n\n<icon(56)>"u8 +
                "Lorem ipsum dolor <colortype(504)><edgecolortype(505)>sit<colortype(0)><edgecolortype(0)>"u8 +
                " <italic(1)>amet,<italic(0)> <colortype(500)><edgecolortype(501)>conse<->ctetur<colortype(0)>"u8 +
                "<edgecolortype(0)> <colortype(500)><edgecolortype(501)><italic(1)>adipi<-><colortype(504)>"u8 +
                "<edgecolortype(505)>scing<colortype(0)><edgecolortype(0)><italic(0)><colortype(0)>"u8 +
                "<edgecolortype(0)> elit. <colortype(502)><edgecolortype(503)>Maece<->nas<colortype(0)>"u8 +
                "<edgecolortype(0)> <colortype(500)><edgecolortype(501)>digni<-><colortype(504)>"u8 +
                "<edgecolortype(505)>ssim<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)>"u8 +
                " <colortype(504)><edgecolortype(505)>sem<colortype(0)><edgecolortype(0)> <italic(1)>at<italic(0)>"u8 +
                " inter<->dum <colortype(500)><edgecolortype(501)>ferme<->ntum.<colortype(0)><edgecolortype(0)>"u8 +
                " Praes<->ent <colortype(500)><edgecolortype(501)>ferme<->ntum<colortype(0)><edgecolortype(0)>"u8 +
                " <colortype(500)><edgecolortype(501)>conva<->llis<colortype(0)><edgecolortype(0)>"u8 +
                " velit <colortype(504)><edgecolortype(505)>sit<colortype(0)><edgecolortype(0)> <italic(1)>"u8 +
                "amet<italic(0)> <colortype(500)><edgecolortype(501)>hendr<->erit.<colortype(0)><edgecolortype(0)>"u8 +
                " <colortype(504)><edgecolortype(505)>Sed<colortype(0)><edgecolortype(0)> eu nibh <colortype(502)>"u8 +
                "<edgecolortype(503)>magna.<colortype(0)><edgecolortype(0)> Integ<->"u8 +
                "er nec lacus in velit porta euism<->od <colortype(504)><edgecolortype(505)>sed<colortype(0)>"u8 +
                "<edgecolortype(0)> et lacus. <colortype(504)><edgecolortype(505)>Sed<colortype(0)>"u8 +
                "<edgecolortype(0)> non <colortype(502)><edgecolortype(503)>mauri<->s<colortype(0)>"u8 +
                "<edgecolortype(0)> <colortype(500)><edgecolortype(501)>venen<-><italic(1)>atis,<colortype(0)>"u8 +
                "<edgecolortype(0)><italic(0)> <colortype(502)><edgecolortype(503)>matti<->s<colortype(0)>"u8 +
                "<edgecolortype(0)> <colortype(502)><edgecolortype(503)>metus<colortype(0)><edgecolortype(0)>"u8 +
                " in, <italic(1)>aliqu<->et<italic(0)> dolor. <italic(1)>Aliqu<->am<italic(0)>"u8 +
                " erat <colortype(500)><edgecolortype(501)>volut<->pat.<colortype(0)><edgecolortype(0)>"u8 +
                " Nulla <colortype(500)><edgecolortype(501)>venen<-><italic(1)>atis<colortype(0)><edgecolortype(0)>"u8 +
                "<italic(0)> velit <italic(1)>ac<italic(0)> <colortype(504)><edgecolortype(505)><colortype(516)>"u8 +
                "<edgecolortype(517)>sus<colortype(0)><edgecolortype(0)>ci<->pit<colortype(0)><edgecolortype(0)>"u8 +
                " euism<->od. <colortype(500)><edgecolortype(501)><colortype(504)><edgecolortype(505)>"u8 +
                "<colortype(516)><edgecolortype(517)>sus<colortype(0)><edgecolortype(0)>pe<->ndisse<colortype(0)>"u8 +
                "<edgecolortype(0)><colortype(0)><edgecolortype(0)> <colortype(502)><edgecolortype(503)>maxim<->"u8 +
                "us<colortype(0)><edgecolortype(0)> viver<->ra dui id dapib<->us. Nam torto<->"u8 +
                "r dolor, <colortype(500)><edgecolortype(501)>eleme<->ntum<colortype(0)><edgecolortype(0)>"u8 +
                " quis orci id, pulvi<->nar <colortype(500)><edgecolortype(501)>fring<->illa<colortype(0)>"u8 +
                "<edgecolortype(0)> quam. <colortype(500)><edgecolortype(501)>Pelle<->ntesque<colortype(0)>"u8 +
                "<edgecolortype(0)> laore<->et viver<->ra torto<->r eget <colortype(502)><edgecolortype(503)>"u8 +
                "matti<-><colortype(504)><edgecolortype(505)>s.<colortype(0)><edgecolortype(0)><colortype(0)>"u8 +
                "<edgecolortype(0)> <colortype(500)><edgecolortype(501)>Vesti<-><bold(1)>bulum<colortype(0)>"u8 +
                "<edgecolortype(0)><bold(0)> eget porta <italic(1)>ante,<italic(0)> a <colortype(502)>"u8 +
                "<edgecolortype(503)>molli<->s<colortype(0)><edgecolortype(0)> nulla. <colortype(500)>"u8 +
                "<edgecolortype(501)>Curab<->itur<colortype(0)><edgecolortype(0)> a ligul<->a leo. <italic(1)>"u8 +
                "Aliqu<->am<italic(0)> volut<->pat <colortype(504)><edgecolortype(505)>sagit<->tis<colortype(0)>"u8 +
                "<edgecolortype(0)> dapib<->us.\n\n<icon(57)>Fusce iacul<->is <italic(1)>aliqu<->am<italic(0)>"u8 +
                " <colortype(502)><edgecolortype(503)>mi,<colortype(0)><edgecolortype(0)> eget <colortype(500)>"u8 +
                "<edgecolortype(501)>portt<->itor<colortype(0)><edgecolortype(0)> <italic(1)>arcu<italic(0)>"u8 +
                " <colortype(500)><edgecolortype(501)><colortype(504)><edgecolortype(505)>solli<->"u8 +
                "citudin<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)> <colortype(500)>"u8 +
                "<edgecolortype(501)>conse<->ctetur.<colortype(0)><edgecolortype(0)> <colortype(500)>"u8 +
                "<edgecolortype(501)><colortype(504)><edgecolortype(505)><colortype(516)><edgecolortype(517)>"u8 +
                "sus<colortype(0)><edgecolortype(0)>pe<->ndisse<colortype(0)><edgecolortype(0)><colortype(0)>"u8 +
                "<edgecolortype(0)> <italic(1)>aliqu<->am<italic(0)> commo<->do <colortype(500)>"u8 +
                "<edgecolortype(501)>tinci<->dunt.<colortype(0)><edgecolortype(0)> Duis <colortype(504)>"u8 +
                "<edgecolortype(505)>sed<colortype(0)><edgecolortype(0)> posue<->re tellu<-><colortype(504)>"u8 +
                "<edgecolortype(505)>s.<colortype(0)><edgecolortype(0)> <colortype(504)><edgecolortype(505)>"u8 +
                "Sed<colortype(0)><edgecolortype(0)> phare<->tra ex vel torto<->r <colortype(500)>"u8 +
                "<edgecolortype(501)>pelle<->ntesque,<colortype(0)><edgecolortype(0)> inter<->"u8 +
                "dum porta <colortype(504)><edgecolortype(505)>sapie<->n<colortype(0)><edgecolortype(0)>"u8 +
                " <colortype(500)><edgecolortype(501)>digni<-><colortype(504)><edgecolortype(505)>"u8 +
                "ssim.<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)> Queue Dun <colortype(504)>"u8 +
                "<edgecolortype(505)>Scait<->h.<colortype(0)><edgecolortype(0)> Cras <italic(1)>aliqu<->"u8 +
                "et<italic(0)> <italic(1)>at<italic(0)> nulla quis <colortype(500)><edgecolortype(501)>"u8 +
                "<colortype(502)><edgecolortype(503)>moles<->tie.<colortype(0)><edgecolortype(0)><colortype(0)>"u8 +
                "<edgecolortype(0)> <colortype(500)><edgecolortype(501)>Vesti<-><bold(1)>bulum<colortype(0)>"u8 +
                "<edgecolortype(0)><bold(0)> eu ligul<->a <colortype(504)><edgecolortype(505)>sapie<->"u8 +
                "n.<colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)>Curab<->itur<colortype(0)>"u8 +
                "<edgecolortype(0)> <colortype(500)><edgecolortype(501)>digni<-><colortype(504)>"u8 +
                "<edgecolortype(505)>ssim<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)> feugi<->"u8 +
                "<italic(1)>at<italic(0)> <colortype(500)><edgecolortype(501)>volut<->pat.<colortype(0)>"u8 +
                "<edgecolortype(0)>\n\n<icon(58)><colortype(500)><edgecolortype(501)>Vesti<-><bold(1)>"u8 +
                "bulum<colortype(0)><edgecolortype(0)><bold(0)> <colortype(500)><edgecolortype(501)>condi<->"u8 +
                "<colortype(502)><edgecolortype(503)>mentum<colortype(0)><edgecolortype(0)><colortype(0)>"u8 +
                "<edgecolortype(0)> laore<->et rhonc<->us. Vivam<->us et <italic(1)>accum<-><colortype(504)>"u8 +
                "<edgecolortype(505)>san<italic(0)><colortype(0)><edgecolortype(0)> purus. <colortype(500)>"u8 +
                "<edgecolortype(501)>Curab<->itur<colortype(0)><edgecolortype(0)> inter<->dum vel ligul<->"u8 +
                "a <italic(1)>ac<italic(0)> euism<->od. Donec <colortype(504)><edgecolortype(505)>sed<colortype(0)>"u8 +
                "<edgecolortype(0)> nisl <colortype(500)><edgecolortype(501)>digni<-><colortype(504)>"u8 +
                "<edgecolortype(505)>ssim<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)>"u8 +
                " est <colortype(500)><edgecolortype(501)>tinci<->dunt<colortype(0)><edgecolortype(0)> iacul<->"u8 +
                "is. Praes<->ent <colortype(500)><edgecolortype(501)>hendr<->erit<colortype(0)><edgecolortype(0)>"u8 +
                " <colortype(500)><edgecolortype(501)>pelle<->ntesque<colortype(0)><edgecolortype(0)>"u8 +
                " nisl, quis lacin<->ia <italic(1)>arcu<italic(0)> dictu<->m <colortype(504)><edgecolortype(505)>"u8 +
                "sit<colortype(0)><edgecolortype(0)> <italic(1)>amet.<italic(0)> <italic(1)>Aliqu<->am<italic(0)>"u8 +
                " variu<->s lectu<->s vel <colortype(502)><edgecolortype(503)>mauri<->s<colortype(0)>"u8 +
                "<edgecolortype(0)> <colortype(500)><edgecolortype(501)>imper<->diet<colortype(0)>"u8 +
                "<edgecolortype(0)> posue<->re. Ut gravi<->da non <colortype(504)><edgecolortype(505)>sapie<->"u8 +
                "n<colortype(0)><edgecolortype(0)> <colortype(504)><edgecolortype(505)>sed<colortype(0)>"u8 +
                "<edgecolortype(0)> <colortype(500)><edgecolortype(501)>hendr<->erit.<colortype(0)>"u8 +
                "<edgecolortype(0)>\n\n<icon(59)>Proin quis dapib<->us odio. Cras <colortype(504)>"u8 +
                "<edgecolortype(505)>sagit<->tis<colortype(0)><edgecolortype(0)> non <colortype(504)>"u8 +
                "<edgecolortype(505)>sem<colortype(0)><edgecolortype(0)> <colortype(504)><edgecolortype(505)>"u8 +
                "sed<colortype(0)><edgecolortype(0)> porta. Donec iacul<->is est ligul<-><italic(1)>a,<italic(0)>"u8 +
                " <colortype(500)><edgecolortype(501)>digni<-><colortype(504)><edgecolortype(505)>"u8 +
                "ssim<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)> <italic(1)>aliqu<->"u8 +
                "et<italic(0)> <italic(1)>augue<italic(0)> <colortype(502)><edgecolortype(503)>matti<->"u8 +
                "s<colortype(0)><edgecolortype(0)> vitae. Duis <colortype(500)><edgecolortype(501)>ullam<->"u8 +
                "corper<colortype(0)><edgecolortype(0)> tempu<->s odio, non <colortype(500)><edgecolortype(501)>"u8 +
                "vesti<-><bold(1)>bulum<colortype(0)><edgecolortype(0)><bold(0)> est <bold(1)>biben<->dum<bold(0)>"u8 +
                " quis. In purus elit, vehic<->ula <colortype(500)><edgecolortype(501)>tinci<->dunt<colortype(0)>"u8 +
                "<edgecolortype(0)> dictu<->m in, <italic(1)>aucto<->r<italic(0)> nec enim. <colortype(500)>"u8 +
                "<edgecolortype(501)>Curab<->itur<colortype(0)><edgecolortype(0)> a nisi in leo <colortype(502)>"u8 +
                "<edgecolortype(503)>matti<->s<colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)>"u8 +
                "pelle<->ntesque<colortype(0)><edgecolortype(0)> id nec <colortype(504)><edgecolortype(505)>"u8 +
                "sem.<colortype(0)><edgecolortype(0)> Nunc vel ultri<->ces nisl. Nam congu<->e <colortype(500)>"u8 +
                "<edgecolortype(501)>vulpu<->tate<colortype(0)><edgecolortype(0)> <colortype(500)>"u8 +
                "<edgecolortype(501)><colortype(502)><edgecolortype(503)>males<->uada.<colortype(0)>"u8 +
                "<edgecolortype(0)><colortype(0)><edgecolortype(0)> <italic(1)>Aenea<->n<italic(0)>"u8 +
                " <colortype(500)><edgecolortype(501)>vesti<-><bold(1)>bulum<colortype(0)><edgecolortype(0)>"u8 +
                "<bold(0)> <colortype(502)><edgecolortype(503)>mauri<->s<colortype(0)><edgecolortype(0)>"u8 +
                " leo, <colortype(504)><edgecolortype(505)>sit<colortype(0)><edgecolortype(0)> <italic(1)>"u8 +
                "amet<italic(0)> iacul<->is est <colortype(500)><edgecolortype(501)>imper<->diet<colortype(0)>"u8 +
                "<edgecolortype(0)> ut. <colortype(500)><edgecolortype(501)>Phase<->llus<colortype(0)>"u8 +
                "<edgecolortype(0)> nec lobor<->tis lacus, <colortype(504)><edgecolortype(505)>sit<colortype(0)>"u8 +
                "<edgecolortype(0)> <italic(1)>amet<italic(0)> <colortype(500)><edgecolortype(501)><colortype(504)>"u8 +
                "<edgecolortype(505)>scele<->risque<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)>"u8 +
                " purus. Nam id lacin<->ia velit, euism<->od feugi<-><italic(1)>at<italic(0)>"u8 +
                " dui. Nulla <colortype(504)><edgecolortype(505)>sodal<->es<colortype(0)><edgecolortype(0)>"u8 +
                " odio ligul<-><italic(1)>a,<italic(0)> et <colortype(500)><edgecolortype(501)>hendr<->"u8 +
                "erit<colortype(0)><edgecolortype(0)> torto<->r <colortype(502)><edgecolortype(503)>maxim<->"u8 +
                "us<colortype(0)><edgecolortype(0)> eu. Donec et <colortype(504)><edgecolortype(505)>"u8 +
                "sem<colortype(0)><edgecolortype(0)> eu <colortype(502)><edgecolortype(503)>magna<colortype(0)>"u8 +
                "<edgecolortype(0)> volut<->pat <italic(1)>accum<-><colortype(504)><edgecolortype(505)>"u8 +
                "san<italic(0)><colortype(0)><edgecolortype(0)> non ut lectu<-><colortype(504)><edgecolortype(505)>"u8 +
                "s.<colortype(0)><edgecolortype(0)>\n\n<icon(60)>Vivam<->us <colortype(504)><edgecolortype(505)>"u8 +
                "<colortype(516)><edgecolortype(517)>sus<colortype(0)><edgecolortype(0)>ci<->pit<colortype(0)>"u8 +
                "<edgecolortype(0)> <colortype(500)><edgecolortype(501)>ferme<->ntum<colortype(0)>"u8 +
                "<edgecolortype(0)> gravi<->da. Cras nec <colortype(500)><edgecolortype(501)>conse<->"u8 +
                "ctetur<colortype(0)><edgecolortype(0)> <colortype(502)><edgecolortype(503)>magna.<colortype(0)>"u8 +
                "<edgecolortype(0)> Vivam<->us <italic(1)>ante<italic(0)> <colortype(502)><edgecolortype(503)>"u8 +
                "massa,<colortype(0)><edgecolortype(0)> <italic(1)>accum<-><colortype(504)><edgecolortype(505)>"u8 +
                "san<italic(0)><colortype(0)><edgecolortype(0)> <colortype(504)><edgecolortype(505)>"u8 +
                "sit<colortype(0)><edgecolortype(0)> <italic(1)>amet<italic(0)> felis et, tempu<->s iacul<->"u8 +
                "is ipsum. <colortype(500)><edgecolortype(501)>Pelle<->ntesque<colortype(0)><edgecolortype(0)>"u8 +
                " vitae nisi <colortype(500)><edgecolortype(501)><italic(1)>accum<-><colortype(504)>"u8 +
                "<edgecolortype(505)>san,<colortype(0)><edgecolortype(0)><italic(0)><colortype(0)>"u8 +
                "<edgecolortype(0)> <colortype(500)><edgecolortype(501)>venen<-><italic(1)>atis<colortype(0)>"u8 +
                "<edgecolortype(0)><italic(0)> lectu<->s <italic(1)>aucto<->r,<italic(0)> <italic(1)>aliqu<->"u8 +
                "et<italic(0)> liber<->o. Nam nec <colortype(500)><edgecolortype(501)>imper<->diet<colortype(0)>"u8 +
                "<edgecolortype(0)> justo. Vivam<->us ut vehic<->ula turpi<-><colortype(504)><edgecolortype(505)>"u8 +
                "s.<colortype(0)><edgecolortype(0)> Nunc lobor<->tis <colortype(500)><edgecolortype(501)>pelle<->"u8 +
                "ntesque<colortype(0)><edgecolortype(0)> urna, <colortype(504)><edgecolortype(505)>"u8 +
                "sit<colortype(0)><edgecolortype(0)> <italic(1)>amet<italic(0)> <colortype(500)>"u8 +
                "<edgecolortype(501)><colortype(504)><edgecolortype(505)>solli<->citudin<colortype(0)>"u8 +
                "<edgecolortype(0)><colortype(0)><edgecolortype(0)> nibh fauci<-><bold(1)>bus<bold(0)>"u8 +
                " in. <colortype(500)><edgecolortype(501)>Curab<->itur<colortype(0)><edgecolortype(0)> eu lobor<->"u8 +
                "tis lacus. Donec eu <colortype(500)><edgecolortype(501)>hendr<->erit<colortype(0)>"u8 +
                "<edgecolortype(0)> diam, vitae cursu<->s odio. Cras eget <colortype(500)><edgecolortype(501)>"u8 +
                "<colortype(504)><edgecolortype(505)>scele<->risque<colortype(0)><edgecolortype(0)><colortype(0)>"u8 +
                "<edgecolortype(0)> <colortype(502)><edgecolortype(503)>mi.<colortype(0)><edgecolortype(0)>"u8 +
                "\n\n· Testing aaaaa<link(0x0E,1,2,3,testlink)>link <icon(61)> aaaaa<link(0xCE)>"u8 +
                "bbbb.\n· Open <link(0x0E,0,0,0,\\[\"test\"\\, \"https://example.com/\"\\])><colortype(502)>"u8 +
                "<edgecolortype(503)>example.com<colortype(0)><edgecolortype(0)><link(0xCE)>"u8 +
                "\n· Open <link(0x0E,2,2,2,\\[\"test\"\\, \"https://example.org/\"\\])><colortype(502)>"u8 +
                "<edgecolortype(503)>example.org<colortype(0)><edgecolortype(0)><link(0xCE)>\n\n<icon2(1)>"u8 +
                "<icon2(2)><icon2(3)><icon2(4)><icon2(5)><icon2(6)><icon2(7)><icon2(8)><icon2(9)><icon2(10)>"u8 +
                "<icon2(11)><icon2(12)><icon2(13)><icon2(14)><icon2(15)><icon2(16)><icon2(17)><icon2(18)>"u8 +
                "<icon2(19)><icon2(20)><icon2(21)><icon2(22)><icon2(23)><icon2(24)><icon2(25)>\n\n<edge(1)>"u8 +
                "<colortype(502)><edgecolortype(503)><icon(1)>colortype502,edgecolortype503<edgecolortype(0)>"u8 +
                "<colortype(0)>\n\nOpacity values are ignored:\n<color(0xFFFF0000)><edgecolor(0xFF0000FF)><icon(2)>"u8 +
                "opacity FF<edgecolor(stackcolor)><color(stackcolor)>\n<color(0x80FF0000)><edgecolor(0x800000FF)>"u8 +
                "<icon(3)>opacity 80<edgecolor(stackcolor)><color(stackcolor)>\n<color(0xFF0000)><edgecolor(0xFF)>"u8 +
                "<icon(4)>opacity 00<edgecolor(stackcolor)><color(stackcolor)>\n<color(0xFF0000)><edgecolor(0xFF)>"u8 +
                "<colortype(502)><edgecolortype(503)><icon(6)>Test 1<edgecolortype(0)><colortype(0)>"u8 +
                "<edgecolor(stackcolor)><color(stackcolor)>\n<colortype(502)><edgecolortype(503)><color(0xFF0000)>"u8 +
                "<edgecolor(0xFF)><icon(6)>Test 2<edgecolortype(0)><colortype(0)><edgecolor(stackcolor)>"u8 +
                "<color(stackcolor)>\n<edge(0)>Without edge<shadow(1)>Shadow<shadow(0)><edge(1)>With edge"u8,
                minCapacity: 65536);
        }

        ImGui.SameLine();

        if (ImGui.Button("Print to Chat Log"u8))
        {
            Service<ChatGui>.Get().Print(
                ReadOnlySeString.FromMacroString(
                    Encoding.UTF8.GetString(this.testStringBuffer.DataSpan).ReplaceLineEndings("<br>"),
                    new(ExceptionMode: MacroStringParseExceptionMode.EmbedError)));
        }

        ImGui.SameLine();

        if (ImGui.Button("Copy as Image"u8))
        {
            // The texture is made now, so the callback only needs to be pushed around the call.
            IDalamudTextureWrap texture;
            using (ImGui.PushTextCallback(this))
            {
                texture = Service<TextureManager>.Get().CreateTextureFromSeString(
                    ReadOnlySeString.FromMacroString(
                        Encoding.UTF8.GetString(this.testStringBuffer.DataSpan),
                        new(ExceptionMode: MacroStringParseExceptionMode.EmbedError)),
                    this.FontFor(TextMarkup.SeString),
                    wrapWidth: ImGui.GetContentRegionAvail().X);
            }

            _ = Service<DevTextureSaveMenu>.Get().ShowTextureSaveMenuAsync(
                this.DisplayName,
                $"From {nameof(FontSamplesSeStringWidget)}",
                Task.FromResult(texture));
        }

        ImGuiHelpers.ScaledDummy(3);
        this.DrawSeString(
            "Optional features implemented for the following test input:<br>"u8 +
            "· <colortype(710)>line breaks<colortype(0)> are automatically replaced to "u8 +
            "<colortype(710)>\\<br><colortype(0)>.<br>"u8 +
            "· <colortype(710)>D<link(0xCE)>alamud<colortype(0)> will display Dalamud.<br>"u8 +
            "· <colortype(710)>W<link(0xCE)>hite<colortype(0)> will display White.<br>"u8 +
            "· <colortype(710)>D<link(0xCE)>efaultIcon<colortype(0)> will display DefaultIcon.<br>"u8 +
            "· <colortype(710)>D<link(0xCE)>isabledIcon<colortype(0)> will display DisabledIcon.<br>"u8 +
            "· <colortype(710)>O<link(0xCE)>utdatedInstallableIcon<colortype(0)> "u8 +
            "will display OutdatedInstallableIcon.<br>"u8 +
            "· <colortype(710)>T<link(0xCE)>roubleIcon<colortype(0)> will display TroubleIcon.<br>"u8 +
            "· <colortype(710)>D<link(0xCE)>evPluginIcon<colortype(0)> will display DevPluginIcon.<br>"u8 +
            "· <colortype(710)>U<link(0xCE)>pdateIcon<colortype(0)> will display UpdateIcon.<br>"u8 +
            "· <colortype(710)>I<link(0xCE)>nstalledIcon<colortype(0)> will display InstalledIcon.<br>"u8 +
            "· <colortype(710)>T<link(0xCE)>hirdIcon<colortype(0)> will display ThirdIcon.<br>"u8 +
            "· <colortype(710)>T<link(0xCE)>hirdInst<link(0xCE)>alledIcon<colortype(0)> "u8 +
            "will display ThirdInstalledIcon.<br>"u8 +
            "· <colortype(710)>C<link(0xCE)>hangelogApiBumpIcon<colortype(0)> "u8 +
            "will display ChangelogApiBumpIcon.<br>"u8 +
            "· <colortype(710)>icon<link(0xCE)>(5)<colortype(0)> will display icon(5). "u8 +
            "This is different from \\<icon<link(0xCE)>(5)>.<br>"u8 +
            "· <colortype(710)>tex<link(0xCE)>(ui/loadingimage/-nowloading_base25_hr1.tex)<colortype(0)> "u8 +
            "will display tex(ui/loadingimage/-nowloading_base25_hr1.tex)."u8,
            TextMarkup.MacroString);
        ImGuiHelpers.ScaledDummy(3);

        fixed (byte* labelPtr = "Test Input"u8)
        {
            if (ImGui.InputTextMultiline(
                    labelPtr,
                    this.testStringBuffer.StorageSpan,
                    new(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight() * 3)))
            {
                var len = this.testStringBuffer.StorageSpan.IndexOf((byte)0);
                if (len + 4 >= this.testStringBuffer.Capacity)
                    this.testStringBuffer.EnsureCapacityExponential(len + 4);

                if (len < this.testStringBuffer.Capacity)
                {
                    this.testStringBuffer.LengthUnsafe = len;
                    this.testStringBuffer.StorageSpan[len] = 0;
                }
            }
        }

        if (this.alignToFramePadding)
            ImGui.AlignTextToFramePadding();

        this.DrawSeString(this.testStringBuffer.DataSpan, TextMarkup.MacroString);
        if (this.showInteraction)
        {
            ImGui.Separator();
            if (ImGui.TryGetTextInteraction(out var interaction, TextInteractionFlags.All))
            {
                ReadOnlySpan<byte> macro =
                    this.testStringBuffer.DataSpan.Slice(interaction.DrawnOffset, interaction.DrawnLength);
                var target = new ReadOnlySeStringSpan(interaction.Link.Target);
                if (this.alignToFramePadding)
                    ImGui.AlignTextToFramePadding();
                ImGui.Text($"{interaction.Flags}[{interaction.DrawnOffset}]: {macro}; {target.ToString()}");
                if ((interaction.Flags & TextInteractionFlags.LeftClick) != 0 &&
                    interaction.Link.TryGetSePayload(out var payload) &&
                    payload.TryReadDalamudLink(out var link) &&
                    link.PluginName == "test")
                {
                    Util.OpenLink(link.ExtraString);
                }
            }
            else
            {
                if (this.alignToFramePadding)
                    ImGui.AlignTextToFramePadding();
                this.DrawSeString("If a link is hovered, it will be displayed here."u8, TextMarkup.MacroString);
            }
        }

        ImGui.Separator();
        if (this.alignToFramePadding)
            ImGui.AlignTextToFramePadding();
        this.DrawSeString("Extra line for alignment testing."u8, TextMarkup.MacroString);
    }

    // The white of an entity, with the alpha of the text, so that it fades with the text.
    private static uint TintOf(in TextEntityContext context) => 0x00FFFFFFu | (context.Color & 0xFF000000u);

    // The offset of the ')' that ends the argument at the start of a text, or -1. It stays within the text payload.
    private static int ArgumentEnd(ReadOnlySpan<byte> span)
    {
        for (var i = 0; i < span.Length; i++)
        {
            if (span[i] == ')')
                return i;
            if (span[i] < 0x20)
                return -1;
        }

        return -1;
    }

    private static ISharedImmediateTexture? IconOf(ReadOnlySpan<byte> argument) =>
        uint.TryParse(argument, out var iconId) &&
        Service<TextureManager>.Get().Shared.TryGetFromGameIcon(iconId, out var texture)
            ? texture
            : null;

    private ImFontPtr FontFor(TextMarkup markup) =>
        (this.textStyle with { FamilyId = ImGui.TextStyle.FamilyId, Markup = markup }).GetFont();

    private void DrawSeString(ImU8String text, TextMarkup markup)
    {
        using var pushCallback = ImGui.PushTextCallback(this);
        using var pushWrap = this.wrap ? ImRaii.TextWrapPos(0) : null;
        using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * this.opacity);
        using var font = ImRaii.PushFont(this.FontFor(markup));
        ImGui.Text(text);
    }

    // The answers are part of the layout. A texture that is still loading has no size yet, so the entity is a square
    // until it loads. Then the layout is made again if the texture is not square.
    private Vector2 SizeOf(ISharedImmediateTexture texture, float fontSize)
    {
        if (texture.TryGetWrap(out var wrap, out var exception))
            return wrap.Size * (fontSize / wrap.Size.Y);
        if (exception is null && this.loadingTextures.TryAdd(texture, 0))
            _ = this.InvalidateWhenLoadedAsync(texture);
        return new(fontSize);
    }

    private async Task InvalidateWhenLoadedAsync(ISharedImmediateTexture texture)
    {
        try
        {
            using var wrap = await texture.RentAsync();
            if (wrap.Width != wrap.Height)
                ImGui.InvalidateTextCallback(this);
        }
        catch
        {
            // A texture that fails to load keeps the square, which the layout already has.
        }
        finally
        {
            this.loadingTextures.TryRemove(texture, out _);
        }
    }

    private TextEntity GetEntity(scoped in TextEntityContext context)
    {
        var span = context.Source[context.Offset..];
        var fontSize = context.FontSize;
        if (!this.useEntity)
            return default;

        // One method draws all assets, told apart by the data of the entity.
        foreach (var (name, asset) in AssetEntities)
        {
            if (span.StartsWith(name))
                return new(name.Length, new(fontSize, fontSize), DrawAsset) { Data = (ulong)asset };
        }

        if (span.StartsWith("tex("u8) && ArgumentEnd(span) is > 4 and var texEnd)
        {
            var tex = Service<TextureManager>
                      .Get()
                      .Shared
                      .GetFromGame(Encoding.UTF8.GetString(span[4..texEnd]));
            return new(texEnd + 1, this.SizeOf(tex, fontSize), DrawTexture);
        }

        if (span.StartsWith("icon("u8) &&
            ArgumentEnd(span) is > 5 and var iconEnd &&
            IconOf(span[5..iconEnd]) is { } icon)
        {
            return new(iconEnd + 1, this.SizeOf(icon, fontSize), DrawIcon);
        }

        return default;

        static void DrawTexture(ImDrawListPtr drawList, Vector2 topLeft, in TextEntityContext context)
        {
            var span = context.Source[context.Offset..];
            if (ArgumentEnd(span) is not (> 4 and var end))
                return;
            var tex = Service<TextureManager>
                      .Get()
                      .Shared
                      .GetFromGame(Encoding.UTF8.GetString(span[4..end]))
                      .GetWrapOrEmpty();
            drawList.AddImage(tex.Handle, topLeft, topLeft + context.Size, Vector2.Zero, Vector2.One, TintOf(context));
        }

        static void DrawIcon(ImDrawListPtr drawList, Vector2 topLeft, in TextEntityContext context)
        {
            var span = context.Source[context.Offset..];
            if (ArgumentEnd(span) is not (> 5 and var end) || IconOf(span[5..end]) is not { } icon)
                return;
            var tex = icon.GetWrapOrEmpty();
            drawList.AddImage(tex.Handle, topLeft, topLeft + context.Size, Vector2.Zero, Vector2.One, TintOf(context));
        }

        static void DrawAsset(ImDrawListPtr drawList, Vector2 topLeft, in TextEntityContext context) =>
            drawList.AddImage(
                Service<DalamudAssetManager>.Get().GetDalamudTextureWrap((DalamudAsset)context.Data).Handle,
                topLeft,
                topLeft + context.Size,
                Vector2.Zero,
                Vector2.One,
                TintOf(context));
    }
}
