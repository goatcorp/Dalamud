using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

using CheapLoc;
using Dalamud.Configuration.Internal;
using Dalamud.Game.Gui.Dtr;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Components;
using Dalamud.Interface.Utility;

using ImGuiNET;

namespace Dalamud.Interface.Internal.Windows.Settings.Tabs;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
public class SettingsTabDtr : SettingsTab
{
    private List<string>? dtrOrder;
    private List<string>? dtrIgnore;
    private int dtrSpacing;
    private bool dtrSwapDirection;

    public override SettingsEntry[] Entries { get; } = Array.Empty<SettingsEntry>();

    public override string Title => Loc.Localize("DalamudSettingsServerInfoBar", "Server Info Bar");

    public override void Draw()
    {
        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingServerInfoBarHint", "Plugins can put additional information into your server information bar(where world & time can be seen).\nYou can reorder and disable these here."));

        ImGuiHelpers.ScaledDummy(10);

        var configuration = Service<DalamudConfiguration>.Get();
        var dtrBar = Service<DtrBar>.Get();

        var order = configuration.DtrOrder!.Where(x => dtrBar.HasEntry(x)).ToList();
        var ignore = configuration.DtrIgnore!.Where(x => dtrBar.HasEntry(x)).ToList();
        var orderLeft = configuration.DtrOrder!.Where(x => !order.Contains(x)).ToList();
        var ignoreLeft = configuration.DtrIgnore!.Where(x => !ignore.Contains(x)).ToList();

        if (order.Count == 0)
        {
            ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingServerInfoBarDidNone", "You have no plugins that use this feature."));
        }

        var isOrderChange = false;
        for (var i = 0; i < order.Count; i++)
        {
            var title = order[i];

            // TODO: Maybe we can also resort the rest of the bar in the future?
            // var isRequired = search is Configuration.SearchSetting.Internal or Configuration.SearchSetting.MacroLinks;

            ImGui.PushFont(UiBuilder.IconFont);

            var arrowUpText = $"{FontAwesomeIcon.ArrowUp.ToIconString()}##{title}";
            if (i == 0)
            {
                ImGuiComponents.DisabledButton(arrowUpText);
            }
            else
            {
                if (ImGui.Button(arrowUpText))
                {
                    (order[i], order[i - 1]) = (order[i - 1], order[i]);
                    isOrderChange = true;
                }
            }

            ImGui.SameLine();

            var arrowDownText = $"{FontAwesomeIcon.ArrowDown.ToIconString()}##{title}";
            if (i == order.Count - 1)
            {
                ImGuiComponents.DisabledButton(arrowDownText);
            }
            else
            {
                if (ImGui.Button(arrowDownText) && i != order.Count - 1)
                {
                    (order[i], order[i + 1]) = (order[i + 1], order[i]);
                    isOrderChange = true;
                }
            }

            ImGui.PopFont();

            ImGui.SameLine();

            // if (isRequired) {
            //     ImGui.TextUnformatted($"Search in {name}");
            // } else {

            var isShown = ignore.All(x => x != title);
            var nextIsShow = isShown;
            if (ImGui.Checkbox($"{title}###dtrEntry{i}", ref nextIsShow) && nextIsShow != isShown)
            {
                if (nextIsShow)
                    ignore.Remove(title);
                else
                    ignore.Add(title);

                dtrBar.MakeDirty(title);
            }

            // }
        }

        configuration.DtrOrder = order.Concat(orderLeft).ToList();
        configuration.DtrIgnore = ignore.Concat(ignoreLeft).ToList();

        if (isOrderChange)
            dtrBar.ApplySort();

        ImGuiHelpers.ScaledDummy(10);

        ImGui.Text(Loc.Localize("DalamudSettingServerInfoBarSpacing", "Server Info Bar spacing"));
        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingServerInfoBarSpacingHint", "Configure the amount of space between entries in the server info bar here."));
        ImGui.SliderInt("Spacing", ref this.dtrSpacing, 0, 40);

        ImGui.Text(Loc.Localize("DalamudSettingServerInfoBarDirection", "Server Info Bar direction"));
        ImGuiHelpers.SafeTextColoredWrapped(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingServerInfoBarDirectionHint", "If checked, the Server Info Bar elements will expand to the right instead of the left."));
        ImGui.Checkbox("Swap Direction", ref this.dtrSwapDirection);

        base.Draw();
    }

    public override void OnClose()
    {
        var configuration = Service<DalamudConfiguration>.Get();
        configuration.DtrOrder = this.dtrOrder;
        configuration.DtrIgnore = this.dtrIgnore;

        base.OnClose();
    }

    public override void Load()
    {
        var configuration = Service<DalamudConfiguration>.Get();

        this.dtrSpacing = configuration.DtrSpacing;
        this.dtrSwapDirection = configuration.DtrSwapDirection;

        this.dtrOrder = configuration.DtrOrder;
        this.dtrIgnore = configuration.DtrIgnore;

        base.Load();
    }

    public override void Save()
    {
        var configuration = Service<DalamudConfiguration>.Get();

        configuration.DtrSpacing = this.dtrSpacing;
        configuration.DtrSwapDirection = this.dtrSwapDirection;

        this.dtrOrder = configuration.DtrOrder;
        this.dtrIgnore = configuration.DtrIgnore;

        base.Save();
    }
}
