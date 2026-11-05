using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;

using CheapLoc;

using Dalamud.Bindings.ImGui;
using Dalamud.Configuration.Internal;
using Dalamud.Game.Tooltip;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;

namespace Dalamud.Interface.Internal.Windows.Settings.Tabs;

[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1600:Elements should be documented", Justification = "Internals")]
internal class SettingsTabTooltips : SettingsTab
{
    public override SettingsEntry[] Entries { get; } = [];

    public override string Title => Loc.Localize("DalamudSettingsGameTooltips", "Game Tooltips");

    public override SettingsOpenKind Kind => SettingsOpenKind.Tooltips;

    public override void Draw()
    {
        ImGui.TextColoredWrapped(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingGameTooltipsHint", "Plugins can put additional information into your in-game tooltips.\nYou can reorder and disable these here."));

        ImGuiHelpers.ScaledDummy(10);

        var configuration = Service<DalamudConfiguration>.Get();
        var tooltipService = Service<Tooltip>.Get();

        var activeListeners = tooltipService
                              .EventListeners
                              .SelectMany(listenerPair => listenerPair.Value)
                              .Select(listener => listener.SourcePluginName.ToString())
                              .Distinct()
                              .ToList();

        var order = configuration.TooltipOrder.Where(activeListeners.Contains).ToList();
        var ignore = configuration.TooltipIgnore.Where(activeListeners.Contains).ToList();
        var orderLeft = configuration.TooltipOrder.Where(entry => !activeListeners.Contains(entry)).ToList();
        var ignoreLeft = configuration.TooltipIgnore.Where(entry => !activeListeners.Contains(entry)).ToList();

        if (order.Count is 0)
        {
            ImGui.TextColoredWrapped(ImGuiColors.DalamudGrey, Loc.Localize("DalamudSettingGameTooltipsNone", "You have no plugins that use this feature."));
        }

        var isOrderChange = false;
        Span<Vector2> upButtonCenters = stackalloc Vector2[order.Count];
        Span<Vector2> downButtonCenters = stackalloc Vector2[order.Count];
        scoped Span<Vector2> moveMouseTo = default;
        var moveMouseToIndex = -1;

        for (var i = 0; i < order.Count; i++)
        {
            var currentEntryName = order[i];

            using var id = ImRaii.PushId(currentEntryName);

            using (ImRaii.PushFont(UiBuilder.IconFontFixedWidth))
            using (ImRaii.Disabled(i is 0))
            {
                if (ImGui.Button(FontAwesomeIcon.ArrowUp.ToIconString()))
                {
                    (order[i], order[i - 1]) = (order[i - 1], order[i]);
                    isOrderChange = true;
                    moveMouseToIndex = i - 1;
                    moveMouseTo = upButtonCenters;
                }
            }

            upButtonCenters[i] = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) / 2;

            ImGui.SameLine();

            using (ImRaii.PushFont(UiBuilder.IconFontFixedWidth))
            using (ImRaii.Disabled(i == order.Count - 1))
            {
                if (ImGui.Button(FontAwesomeIcon.ArrowDown.ToIconString()))
                {
                    (order[i], order[i + 1]) = (order[i + 1], order[i]);
                    isOrderChange = true;
                    moveMouseToIndex = i + 1;
                    moveMouseTo = downButtonCenters;
                }
            }

            downButtonCenters[i] = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) / 2;

            ImGui.SameLine();

            var isShown = ignore.All(disabled => disabled != currentEntryName);
            var nextIsShow = isShown;
            if (ImGui.Checkbox(currentEntryName, ref nextIsShow) && nextIsShow != isShown)
            {
                if (nextIsShow)
                {
                    ignore.Remove(currentEntryName);
                }
                else
                {
                    ignore.Add(currentEntryName);
                }
            }
        }

        if (moveMouseToIndex >= 0 && moveMouseToIndex < moveMouseTo.Length)
        {
            ImGui.GetIO().WantSetMousePos = true;
            ImGui.GetIO().MousePos = moveMouseTo[moveMouseToIndex];
        }

        configuration.TooltipOrder = [.. order, .. orderLeft];
        configuration.TooltipIgnore = [.. ignore, .. ignoreLeft];

        if (isOrderChange)
        {
            configuration.QueueSave();
        }

        base.Draw();
    }
}
