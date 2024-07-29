using System.Linq;
using System.Text;

using Dalamud.Data;
using Dalamud.Game.Gui;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Interface.ImGuiSeStringRenderer;
using Dalamud.Interface.ImGuiSeStringRenderer.Internal;
using Dalamud.Interface.Utility;
using Dalamud.Utility;

using ImGuiNET;

using Lumina.Excel.GeneratedSheets2;
using Lumina.Text;
using Lumina.Text.ReadOnly;

using Serilog;

namespace Dalamud.Interface.Internal.Windows.Data.Widgets;

/// <summary>
/// Widget for displaying Addon Data.
/// </summary>
internal unsafe class SeStringRendererTestWidget : IDataWindowWidget
{
    private ImVectorWrapper<byte> testStringBuffer;
    private string testString = string.Empty;
    private ReadOnlySeString ross;
    private Addon[]? addons;
    private SeStringRenderStyle style;
    private bool interactable;

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "SeStringRenderer Test";

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; }

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load()
    {
        this.style = default;
        this.addons = null;
        this.ross = default;
        this.testString = string.Empty;
        this.interactable = false;
        this.Ready = true;
    }

    /// <inheritdoc/>
    public void Draw()
    {
        var t2 = ImGui.ColorConvertU32ToFloat4(this.style.Color);
        if (ImGui.ColorEdit4("Color", ref t2))
            this.style.Color = ImGui.ColorConvertFloat4ToU32(t2);

        t2 = ImGui.ColorConvertU32ToFloat4(this.style.EdgeColor);
        if (ImGui.ColorEdit4("Edge Color", ref t2))
            this.style.EdgeColor = ImGui.ColorConvertFloat4ToU32(t2);

        ImGui.SameLine();
        var t = this.style.ForceEdgeColor;
        if (ImGui.Checkbox("Forced", ref t))
            this.style.ForceEdgeColor = t;

        t2 = ImGui.ColorConvertU32ToFloat4(this.style.ShadowColor);
        if (ImGui.ColorEdit4("Shadow Color", ref t2))
            this.style.ShadowColor = ImGui.ColorConvertFloat4ToU32(t2);

        t2 = ImGui.ColorConvertU32ToFloat4(this.style.LinkHoverColor);
        if (ImGui.ColorEdit4("Link Hover Color", ref t2))
            this.style.LinkHoverColor = ImGui.ColorConvertFloat4ToU32(t2);

        t2 = ImGui.ColorConvertU32ToFloat4(this.style.LinkActiveColor);
        if (ImGui.ColorEdit4("Link Active Color", ref t2))
            this.style.LinkActiveColor = ImGui.ColorConvertFloat4ToU32(t2);

        t = this.style.Edge;
        if (ImGui.Checkbox("Edge", ref t))
            this.style.Edge = t;

        ImGui.SameLine();
        t = this.style.Bold;
        if (ImGui.Checkbox("Bold", ref t))
            this.style.Bold = t;

        ImGui.SameLine();
        t = this.style.Italic;
        if (ImGui.Checkbox("Italic", ref t))
            this.style.Italic = t;

        ImGui.SameLine();
        t = this.style.Shadow;
        if (ImGui.Checkbox("Shadow", ref t))
            this.style.Shadow = t;

        ImGui.SameLine();
        t = this.style.LinkUnderline;
        if (ImGui.Checkbox("Link Underline", ref t))
            this.style.LinkUnderline = t;

        ImGui.SameLine();
        t = this.interactable;
        if (ImGui.Checkbox("Interactable", ref t))
            this.interactable = t;

        if (ImGui.CollapsingHeader("Addon Table"))
        {
            this.addons ??= Service<DataManager>.Get().GetExcelSheet<Addon>()!.ToArray();
            if (ImGui.BeginTable("Addon Sheet", 3))
            {
                ImGui.TableSetupScrollFreeze(0, 1);
                ImGui.TableSetupColumn("Row ID", ImGuiTableColumnFlags.WidthFixed, ImGui.CalcTextSize("0000000").X);
                ImGui.TableSetupColumn("Text", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn(
                    "Misc",
                    ImGuiTableColumnFlags.WidthFixed,
                    ImGui.CalcTextSize("AAAAAAAAAAAAAAAAA").X);
                ImGui.TableHeadersRow();

                var clipper = new ImGuiListClipperPtr(ImGuiNative.ImGuiListClipper_ImGuiListClipper());
                clipper.Begin(this.addons.Length);
                while (clipper.Step())
                {
                    for (var i = clipper.DisplayStart; i < clipper.DisplayEnd; i++)
                    {
                        ImGui.TableNextRow();
                        ImGui.PushID(i);

                        ImGui.TableNextColumn();
                        ImGui.AlignTextToFramePadding();
                        ImGui.TextUnformatted($"{this.addons[i].RowId}");

                        ImGui.TableNextColumn();
                        ImGui.AlignTextToFramePadding();
                        ImGuiHelpers.SeStringWrapped(this.addons[i].Text.AsReadOnly(), this.style);

                        ImGui.TableNextColumn();
                        if (ImGui.Button("Print to Chat"))
                            Service<ChatGui>.Get().Print(this.addons[i].Text.ToDalamudString());

                        ImGui.PopID();
                    }
                }

                clipper.Destroy();
                ImGui.EndTable();
            }
        }

        if (ImGui.Button("Reset Text") || this.testStringBuffer.IsDisposed)
        {
            this.testStringBuffer.Dispose();
            this.testStringBuffer = ImVectorWrapper.CreateFromSpan(
                "<icon(1)><icon(2)><icon(3)><icon(4)><icon(5)><icon(6)><icon(7)><icon(8)><icon(9)><icon(10)><icon(11)><icon(12)><icon(13)><icon(14)><icon(15)><icon(16)><icon(17)><icon(18)><icon(19)><icon(20)><icon(21)><icon(22)><icon(23)><icon(24)><icon(25)>\n\n<icon(56)>Lorem ipsum dolor <colortype(504)><edgecolortype(505)>sit<colortype(0)><edgecolortype(0)> <italic(1)>amet,<italic(0)> <colortype(500)><edgecolortype(501)>conse<->ctetur<colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)><italic(1)>adipi<-><colortype(504)><edgecolortype(505)>scing<colortype(0)><edgecolortype(0)><italic(0)><colortype(0)><edgecolortype(0)> elit. <colortype(502)><edgecolortype(503)>Maece<->nas<colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)>digni<-><colortype(504)><edgecolortype(505)>ssim<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)> <colortype(504)><edgecolortype(505)>sem<colortype(0)><edgecolortype(0)> <italic(1)>at<italic(0)> inter<->dum <colortype(500)><edgecolortype(501)>ferme<->ntum.<colortype(0)><edgecolortype(0)> Praes<->ent <colortype(500)><edgecolortype(501)>ferme<->ntum<colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)>conva<->llis<colortype(0)><edgecolortype(0)> velit <colortype(504)><edgecolortype(505)>sit<colortype(0)><edgecolortype(0)> <italic(1)>amet<italic(0)> <colortype(500)><edgecolortype(501)>hendr<->erit.<colortype(0)><edgecolortype(0)> <colortype(504)><edgecolortype(505)>Sed<colortype(0)><edgecolortype(0)> eu nibh <colortype(502)><edgecolortype(503)>magna.<colortype(0)><edgecolortype(0)> Integ<->er nec lacus in velit porta euism<->od <colortype(504)><edgecolortype(505)>sed<colortype(0)><edgecolortype(0)> et lacus. <colortype(504)><edgecolortype(505)>Sed<colortype(0)><edgecolortype(0)> non <colortype(502)><edgecolortype(503)>mauri<->s<colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)>venen<-><italic(1)>atis,<colortype(0)><edgecolortype(0)><italic(0)> <colortype(502)><edgecolortype(503)>matti<->s<colortype(0)><edgecolortype(0)> <colortype(502)><edgecolortype(503)>metus<colortype(0)><edgecolortype(0)> in, <italic(1)>aliqu<->et<italic(0)> dolor. <italic(1)>Aliqu<->am<italic(0)> erat <colortype(500)><edgecolortype(501)>volut<->pat.<colortype(0)><edgecolortype(0)> Nulla <colortype(500)><edgecolortype(501)>venen<-><italic(1)>atis<colortype(0)><edgecolortype(0)><italic(0)> velit <italic(1)>ac<italic(0)> <colortype(504)><edgecolortype(505)><colortype(516)><edgecolortype(517)>sus<colortype(0)><edgecolortype(0)>ci<->pit<colortype(0)><edgecolortype(0)> euism<->od. <colortype(500)><edgecolortype(501)><colortype(504)><edgecolortype(505)><colortype(516)><edgecolortype(517)>sus<colortype(0)><edgecolortype(0)>pe<->ndisse<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)> <colortype(502)><edgecolortype(503)>maxim<->us<colortype(0)><edgecolortype(0)> viver<->ra dui id dapib<->us. Nam torto<->r dolor, <colortype(500)><edgecolortype(501)>eleme<->ntum<colortype(0)><edgecolortype(0)> quis orci id, pulvi<->nar <colortype(500)><edgecolortype(501)>fring<->illa<colortype(0)><edgecolortype(0)> quam. <colortype(500)><edgecolortype(501)>Pelle<->ntesque<colortype(0)><edgecolortype(0)> laore<->et viver<->ra torto<->r eget <colortype(502)><edgecolortype(503)>matti<-><colortype(504)><edgecolortype(505)>s.<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)>Vesti<-><bold(1)>bulum<colortype(0)><edgecolortype(0)><bold(0)> eget porta <italic(1)>ante,<italic(0)> a <colortype(502)><edgecolortype(503)>molli<->s<colortype(0)><edgecolortype(0)> nulla. <colortype(500)><edgecolortype(501)>Curab<->itur<colortype(0)><edgecolortype(0)> a ligul<->a leo. <italic(1)>Aliqu<->am<italic(0)> volut<->pat <colortype(504)><edgecolortype(505)>sagit<->tis<colortype(0)><edgecolortype(0)> dapib<->us.\n\n<icon(57)>Fusce iacul<->is <italic(1)>aliqu<->am<italic(0)> <colortype(502)><edgecolortype(503)>mi,<colortype(0)><edgecolortype(0)> eget <colortype(500)><edgecolortype(501)>portt<->itor<colortype(0)><edgecolortype(0)> <italic(1)>arcu<italic(0)> <colortype(500)><edgecolortype(501)><colortype(504)><edgecolortype(505)>solli<->citudin<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)>conse<->ctetur.<colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)><colortype(504)><edgecolortype(505)><colortype(516)><edgecolortype(517)>sus<colortype(0)><edgecolortype(0)>pe<->ndisse<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)> <italic(1)>aliqu<->am<italic(0)> commo<->do <colortype(500)><edgecolortype(501)>tinci<->dunt.<colortype(0)><edgecolortype(0)> Duis <colortype(504)><edgecolortype(505)>sed<colortype(0)><edgecolortype(0)> posue<->re tellu<-><colortype(504)><edgecolortype(505)>s.<colortype(0)><edgecolortype(0)> <colortype(504)><edgecolortype(505)>Sed<colortype(0)><edgecolortype(0)> phare<->tra ex vel torto<->r <colortype(500)><edgecolortype(501)>pelle<->ntesque,<colortype(0)><edgecolortype(0)> inter<->dum porta <colortype(504)><edgecolortype(505)>sapie<->n<colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)>digni<-><colortype(504)><edgecolortype(505)>ssim.<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)> Queue Dun <colortype(504)><edgecolortype(505)>Scait<->h.<colortype(0)><edgecolortype(0)> Cras <italic(1)>aliqu<->et<italic(0)> <italic(1)>at<italic(0)> nulla quis <colortype(500)><edgecolortype(501)><colortype(502)><edgecolortype(503)>moles<->tie.<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)>Vesti<-><bold(1)>bulum<colortype(0)><edgecolortype(0)><bold(0)> eu ligul<->a <colortype(504)><edgecolortype(505)>sapie<->n.<colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)>Curab<->itur<colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)>digni<-><colortype(504)><edgecolortype(505)>ssim<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)> feugi<-><italic(1)>at<italic(0)> <colortype(500)><edgecolortype(501)>volut<->pat.<colortype(0)><edgecolortype(0)>\n\n<icon(58)><colortype(500)><edgecolortype(501)>Vesti<-><bold(1)>bulum<colortype(0)><edgecolortype(0)><bold(0)> <colortype(500)><edgecolortype(501)>condi<-><colortype(502)><edgecolortype(503)>mentum<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)> laore<->et rhonc<->us. Vivam<->us et <italic(1)>accum<-><colortype(504)><edgecolortype(505)>san<italic(0)><colortype(0)><edgecolortype(0)> purus. <colortype(500)><edgecolortype(501)>Curab<->itur<colortype(0)><edgecolortype(0)> inter<->dum vel ligul<->a <italic(1)>ac<italic(0)> euism<->od. Donec <colortype(504)><edgecolortype(505)>sed<colortype(0)><edgecolortype(0)> nisl <colortype(500)><edgecolortype(501)>digni<-><colortype(504)><edgecolortype(505)>ssim<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)> est <colortype(500)><edgecolortype(501)>tinci<->dunt<colortype(0)><edgecolortype(0)> iacul<->is. Praes<->ent <colortype(500)><edgecolortype(501)>hendr<->erit<colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)>pelle<->ntesque<colortype(0)><edgecolortype(0)> nisl, quis lacin<->ia <italic(1)>arcu<italic(0)> dictu<->m <colortype(504)><edgecolortype(505)>sit<colortype(0)><edgecolortype(0)> <italic(1)>amet.<italic(0)> <italic(1)>Aliqu<->am<italic(0)> variu<->s lectu<->s vel <colortype(502)><edgecolortype(503)>mauri<->s<colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)>imper<->diet<colortype(0)><edgecolortype(0)> posue<->re. Ut gravi<->da non <colortype(504)><edgecolortype(505)>sapie<->n<colortype(0)><edgecolortype(0)> <colortype(504)><edgecolortype(505)>sed<colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)>hendr<->erit.<colortype(0)><edgecolortype(0)>\n\n<icon(59)>Proin quis dapib<->us odio. Cras <colortype(504)><edgecolortype(505)>sagit<->tis<colortype(0)><edgecolortype(0)> non <colortype(504)><edgecolortype(505)>sem<colortype(0)><edgecolortype(0)> <colortype(504)><edgecolortype(505)>sed<colortype(0)><edgecolortype(0)> porta. Donec iacul<->is est ligul<-><italic(1)>a,<italic(0)> <colortype(500)><edgecolortype(501)>digni<-><colortype(504)><edgecolortype(505)>ssim<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)> <italic(1)>aliqu<->et<italic(0)> <italic(1)>augue<italic(0)> <colortype(502)><edgecolortype(503)>matti<->s<colortype(0)><edgecolortype(0)> vitae. Duis <colortype(500)><edgecolortype(501)>ullam<->corper<colortype(0)><edgecolortype(0)> tempu<->s odio, non <colortype(500)><edgecolortype(501)>vesti<-><bold(1)>bulum<colortype(0)><edgecolortype(0)><bold(0)> est <bold(1)>biben<->dum<bold(0)> quis. In purus elit, vehic<->ula <colortype(500)><edgecolortype(501)>tinci<->dunt<colortype(0)><edgecolortype(0)> dictu<->m in, <italic(1)>aucto<->r<italic(0)> nec enim. <colortype(500)><edgecolortype(501)>Curab<->itur<colortype(0)><edgecolortype(0)> a nisi in leo <colortype(502)><edgecolortype(503)>matti<->s<colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)>pelle<->ntesque<colortype(0)><edgecolortype(0)> id nec <colortype(504)><edgecolortype(505)>sem.<colortype(0)><edgecolortype(0)> Nunc vel ultri<->ces nisl. Nam congu<->e <colortype(500)><edgecolortype(501)>vulpu<->tate<colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)><colortype(502)><edgecolortype(503)>males<->uada.<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)> <italic(1)>Aenea<->n<italic(0)> <colortype(500)><edgecolortype(501)>vesti<-><bold(1)>bulum<colortype(0)><edgecolortype(0)><bold(0)> <colortype(502)><edgecolortype(503)>mauri<->s<colortype(0)><edgecolortype(0)> leo, <colortype(504)><edgecolortype(505)>sit<colortype(0)><edgecolortype(0)> <italic(1)>amet<italic(0)> iacul<->is est <colortype(500)><edgecolortype(501)>imper<->diet<colortype(0)><edgecolortype(0)> ut. <colortype(500)><edgecolortype(501)>Phase<->llus<colortype(0)><edgecolortype(0)> nec lobor<->tis lacus, <colortype(504)><edgecolortype(505)>sit<colortype(0)><edgecolortype(0)> <italic(1)>amet<italic(0)> <colortype(500)><edgecolortype(501)><colortype(504)><edgecolortype(505)>scele<->risque<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)> purus. Nam id lacin<->ia velit, euism<->od feugi<-><italic(1)>at<italic(0)> dui. Nulla <colortype(504)><edgecolortype(505)>sodal<->es<colortype(0)><edgecolortype(0)> odio ligul<-><italic(1)>a,<italic(0)> et <colortype(500)><edgecolortype(501)>hendr<->erit<colortype(0)><edgecolortype(0)> torto<->r <colortype(502)><edgecolortype(503)>maxim<->us<colortype(0)><edgecolortype(0)> eu. Donec et <colortype(504)><edgecolortype(505)>sem<colortype(0)><edgecolortype(0)> eu <colortype(502)><edgecolortype(503)>magna<colortype(0)><edgecolortype(0)> volut<->pat <italic(1)>accum<-><colortype(504)><edgecolortype(505)>san<italic(0)><colortype(0)><edgecolortype(0)> non ut lectu<-><colortype(504)><edgecolortype(505)>s.<colortype(0)><edgecolortype(0)>\n\n<icon(60)>Vivam<->us <colortype(504)><edgecolortype(505)><colortype(516)><edgecolortype(517)>sus<colortype(0)><edgecolortype(0)>ci<->pit<colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)>ferme<->ntum<colortype(0)><edgecolortype(0)> gravi<->da. Cras nec <colortype(500)><edgecolortype(501)>conse<->ctetur<colortype(0)><edgecolortype(0)> <colortype(502)><edgecolortype(503)>magna.<colortype(0)><edgecolortype(0)> Vivam<->us <italic(1)>ante<italic(0)> <colortype(502)><edgecolortype(503)>massa,<colortype(0)><edgecolortype(0)> <italic(1)>accum<-><colortype(504)><edgecolortype(505)>san<italic(0)><colortype(0)><edgecolortype(0)> <colortype(504)><edgecolortype(505)>sit<colortype(0)><edgecolortype(0)> <italic(1)>amet<italic(0)> felis et, tempu<->s iacul<->is ipsum. <colortype(500)><edgecolortype(501)>Pelle<->ntesque<colortype(0)><edgecolortype(0)> vitae nisi <colortype(500)><edgecolortype(501)><italic(1)>accum<-><colortype(504)><edgecolortype(505)>san,<colortype(0)><edgecolortype(0)><italic(0)><colortype(0)><edgecolortype(0)> <colortype(500)><edgecolortype(501)>venen<-><italic(1)>atis<colortype(0)><edgecolortype(0)><italic(0)> lectu<->s <italic(1)>aucto<->r,<italic(0)> <italic(1)>aliqu<->et<italic(0)> liber<->o. Nam nec <colortype(500)><edgecolortype(501)>imper<->diet<colortype(0)><edgecolortype(0)> justo. Vivam<->us ut vehic<->ula turpi<-><colortype(504)><edgecolortype(505)>s.<colortype(0)><edgecolortype(0)> Nunc lobor<->tis <colortype(500)><edgecolortype(501)>pelle<->ntesque<colortype(0)><edgecolortype(0)> urna, <colortype(504)><edgecolortype(505)>sit<colortype(0)><edgecolortype(0)> <italic(1)>amet<italic(0)> <colortype(500)><edgecolortype(501)><colortype(504)><edgecolortype(505)>solli<->citudin<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)> nibh fauci<-><bold(1)>bus<bold(0)> in. <colortype(500)><edgecolortype(501)>Curab<->itur<colortype(0)><edgecolortype(0)> eu lobor<->tis lacus. Donec eu <colortype(500)><edgecolortype(501)>hendr<->erit<colortype(0)><edgecolortype(0)> diam, vitae cursu<->s odio. Cras eget <colortype(500)><edgecolortype(501)><colortype(504)><edgecolortype(505)>scele<->risque<colortype(0)><edgecolortype(0)><colortype(0)><edgecolortype(0)> <colortype(502)><edgecolortype(503)>mi.<colortype(0)><edgecolortype(0)>\n\n· Testing aaaaa<link(0x0E,1,2,3,testlink)>link <icon(61)> aaaaa<link(0xCE)>bbbb.\n· Open <link(0x0E,0,0,0,\\[\"shell\"\\, \"https://example.com/\"\\])><colortype(502)><edgecolortype(503)>example.com<colortype(0)><edgecolortype(0)><link(0xCE)>\n· Open <link(0x0E,2,2,2,\\[\"shell\"\\, \"https://example.org/\"\\])><colortype(502)><edgecolortype(503)>example.org<colortype(0)><edgecolortype(0)><link(0xCE)>\n\n<icon2(1)><icon2(2)><icon2(3)><icon2(4)><icon2(5)><icon2(6)><icon2(7)><icon2(8)><icon2(9)><icon2(10)><icon2(11)><icon2(12)><icon2(13)><icon2(14)><icon2(15)><icon2(16)><icon2(17)><icon2(18)><icon2(19)><icon2(20)><icon2(21)><icon2(22)><icon2(23)><icon2(24)><icon2(25)>\n\n<edge(1)><colortype(502)><edgecolortype(503)><icon(1)>colortype502,edgecolortype503<edgecolortype(0)><colortype(0)>\n\nOpacity values are ignored:\n<color(0xFFFF0000)><edgecolor(0xFF0000FF)><icon(2)>opacity FF<edgecolor(stackcolor)><color(stackcolor)>\n<color(0x80FF0000)><edgecolor(0x800000FF)><icon(3)>opacity 80<edgecolor(stackcolor)><color(stackcolor)>\n<color(0xFF0000)><edgecolor(0xFF)><icon(4)>opacity 00<edgecolor(stackcolor)><color(stackcolor)>\n<color(0xFF0000)><edgecolor(0xFF)><colortype(502)><edgecolortype(503)><icon(6)>Test 1<edgecolortype(0)><colortype(0)><edgecolor(stackcolor)><color(stackcolor)>\n<colortype(502)><edgecolortype(503)><color(0xFF0000)><edgecolor(0xFF)><icon(6)>Test 2<edgecolortype(0)><colortype(0)><edgecolor(stackcolor)><color(stackcolor)>\n<edge(0)>Without edge<shadow(1)>Shadow<shadow(0)><edge(1)>With edge"u8,
                minCapacity: 65536);
            this.testString = string.Empty;
            this.ross = default;
        }

        ImGui.SameLine();

        if (ImGui.Button("Print to Chat Log"))
        {
            fixed (byte* p = Service<SeStringRenderer>.Get().CompileAndCache(this.testString).Data.Span)
                Service<ChatGui>.Get().Print(Game.Text.SeStringHandling.SeString.Parse(p));
        }

        ImGuiHelpers.ScaledDummy(3);
        ImGuiHelpers.CompileSeStringWrapped(
            "· For ease of testing, <colortype(506)><edgecolortype(507)>line breaks<colortype(0)><edgecolortype(0)> are automatically replaced to <colortype(502)><edgecolortype(503)>\\<br><colortype(0)><edgecolortype(0)>.",
            this.style);
        ImGuiHelpers.ScaledDummy(3);

        fixed (byte* labelPtr = "Test Input"u8)
        {
            if (ImGuiNative.igInputTextMultiline(
                    labelPtr,
                    this.testStringBuffer.Data,
                    (uint)this.testStringBuffer.Capacity,
                    new(ImGui.GetContentRegionAvail().X, ImGui.GetTextLineHeight() * 3),
                    0,
                    null,
                    null) != 0)
            {
                var len = this.testStringBuffer.StorageSpan.IndexOf((byte)0);
                if (len + 4 >= this.testStringBuffer.Capacity)
                    this.testStringBuffer.EnsureCapacityExponential(len + 4);
                if (len < this.testStringBuffer.Capacity)
                {
                    this.testStringBuffer.LengthUnsafe = len;
                    this.testStringBuffer.StorageSpan[len] = default;
                }

                this.testString = string.Empty;
                this.ross = default;
            }
        }

        if (this.testString == string.Empty && this.testStringBuffer.Length != 0)
            this.testString = Encoding.UTF8.GetString(this.testStringBuffer.DataSpan);

        ImGui.Separator();
        if (this.interactable)
        {
            if (this.ross.IsEmpty && this.testStringBuffer.Length != 0)
            {
                this.ross = new SeStringBuilder()
                            .AppendMacroString(this.testString.ReplaceLineEndings("<br>"))
                            .ToReadOnlySeString();
            }

            var test = ImGuiHelpers.SeStringWrapped(this.ross, this.style, new("this is an ImGui id"));

            ImGui.TextUnformatted($"Hovered: {test.ByteOffset}\nClicked: {test.Clicked}");

            if (test.ByteOffset != -1)
            {
                var enu = new ReadOnlySeStringSpan(this.ross.AsSpan().Data[test.ByteOffset..]).GetOffsetEnumerator();
                enu.MoveNext();
                ImGui.TextUnformatted($"Hovered: {enu.Current.Payload.ToString()} at {test.ByteOffset}");
                if (test.Clicked)
                {
                    var dss = new SeString(
                        this.ross.AsSpan().Data.Slice(
                            test.ByteOffset,
                            enu.Current.Payload.EnvelopeByteLength).ToArray()).ToDalamudString();
                    Log.Information("{who}: clicked payload: {payload}", this.DisplayName, dss.Payloads[0]);
                    switch (dss.Payloads[0])
                    {
                        // "http" is for the sake of this example; it won't be handled by Dalamud automatically.
                        case DalamudLinkPayload { Plugin: "shell" } dlp:
                            Util.OpenLink(dlp.ExtraString);
                            break;
                    }
                }
            }
        }
        else
        {
            ImGuiHelpers.CompileSeStringWrapped(this.testString, this.style);
        }
    }
}
