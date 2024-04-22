using System.Collections.Generic;

using Dalamud.Configuration.Internal;
using Dalamud.Game.Inventory;
using Dalamud.Game.Inventory.InventoryEventArgTypes;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Logging.Internal;

using ImGuiNET;

using Serilog.Events;

namespace Dalamud.Interface.Internal.Windows.Data;

/// <summary>
/// Tester for <see cref="GameInventory"/>.
/// </summary>
internal class GameInventoryTestWidget : IDataWindowWidget
{
    private static readonly ModuleLog Log = new(nameof(GameInventoryTestWidget));

    private GameInventoryPluginScoped? scoped;
    private bool standardEnabled;
    private bool rawEnabled;

    /// <inheritdoc/>
    public string[]? CommandShortcuts { get; init; } = { "gameinventorytest" };

    /// <inheritdoc/>
    public string DisplayName { get; init; } = "GameInventory Test";

    /// <inheritdoc/>
    public bool Ready { get; set; }

    /// <inheritdoc/>
    public void Load() => this.Ready = true;

    /// <inheritdoc/>
    public void Draw()
    {
        if (Service<DalamudConfiguration>.Get().LogLevel > LogEventLevel.Information)
        {
            ImGuiHelpers.SafeTextColoredWrapped(
                ImGuiColors.DalamudRed,
                "Enable LogLevel=Information display to see the logs.");
        }
        
        using var table = ImRaii.Table(this.DisplayName, 3, ImGuiTableFlags.SizingFixedFit);
        if (!table.Success)
            return;

        ImGui.TableNextColumn();
        ImGui.TextUnformatted("Standard Logging");

        ImGui.TableNextColumn();
        using (ImRaii.Disabled(this.standardEnabled))
        {
            if (ImGui.Button("Enable##standard-enable") && !this.standardEnabled)
            {
                this.scoped ??= new();
                this.scoped.InventoryChanged += ScopedOnInventoryChanged;
                this.standardEnabled = true;
            }
        }

        ImGui.TableNextColumn();
        using (ImRaii.Disabled(!this.standardEnabled))
        {
            if (ImGui.Button("Disable##standard-disable") && this.scoped is not null && this.standardEnabled)
            {
                this.scoped.InventoryChanged -= ScopedOnInventoryChanged;
                this.standardEnabled = false;
                if (!this.rawEnabled)
                {
                    ((IInternalDisposableService)this.scoped).DisposeService();
                    this.scoped = null;
                }
            }
        }

        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        ImGui.TextUnformatted("Raw Logging");

        ImGui.TableNextColumn();
        using (ImRaii.Disabled(this.rawEnabled))
        {
            if (ImGui.Button("Enable##raw-enable") && !this.rawEnabled)
            {
                this.scoped ??= new();
                this.scoped.InventoryChangedRaw += ScopedOnInventoryChangedRaw;
                this.rawEnabled = true;
            }
        }

        ImGui.TableNextColumn();
        using (ImRaii.Disabled(!this.rawEnabled))
        {
            if (ImGui.Button("Disable##raw-disable") && this.scoped is not null && this.rawEnabled)
            {
                this.scoped.InventoryChangedRaw -= ScopedOnInventoryChangedRaw;
                this.rawEnabled = false;
                if (!this.standardEnabled)
                {
                    ((IInternalDisposableService)this.scoped).DisposeService();
                    this.scoped = null;
                }
            }
        }

        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        ImGui.TextUnformatted("All");

        ImGui.TableNextColumn();
        using (ImRaii.Disabled(this.standardEnabled && this.rawEnabled))
        {
            if (ImGui.Button("Enable##all-enable"))
            {
                this.scoped ??= new();
                if (!this.standardEnabled)
                    this.scoped.InventoryChanged += ScopedOnInventoryChanged;
                if (!this.rawEnabled)
                    this.scoped.InventoryChangedRaw += ScopedOnInventoryChangedRaw;
                this.standardEnabled = this.rawEnabled = true;
            }
        }

        ImGui.TableNextColumn();
        using (ImRaii.Disabled(this.scoped is null))
        {
            if (ImGui.Button("Disable##all-disable"))
            {
                ((IInternalDisposableService)this.scoped)?.DisposeService();
                this.scoped = null;
                this.standardEnabled = this.rawEnabled = false;
            }
        }
    }

    private static void ScopedOnInventoryChangedRaw(IReadOnlyCollection<InventoryEventArgs> events)
    {
        var i = 0;
        foreach (var e in events)
            Log.Information($"[{++i}/{events.Count}] Raw: {e}");
    }

    private static void ScopedOnInventoryChanged(IReadOnlyCollection<InventoryEventArgs> events)
    {
        var i = 0;
        foreach (var e in events)
        {
            if (e is InventoryComplexEventArgs icea)
                Log.Information($"[{++i}/{events.Count}] {icea}\n\t├ {icea.SourceEvent}\n\t└ {icea.TargetEvent}");
            else
                Log.Information($"[{++i}/{events.Count}] {e}");
        }
    }
}
