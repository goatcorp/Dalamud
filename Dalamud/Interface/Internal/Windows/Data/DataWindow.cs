using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Game.Gui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Serilog;

namespace Dalamud.Interface.Internal.Windows.Data;

/// <summary>
/// Class responsible for drawing the data/debug window.
/// </summary>
internal class DataWindow : Window
{
    private readonly IDataWindowWidget[] modules =
    {
        new ServerOpcodeWidget(),
        new AddressesWidget(),
        new ObjectTableWidget(),
        new FateTableWidget(),
        new SeFontTestWidget(),
        new FontAwesomeTestWidget(),
        new PartyListWidget(),
        new BuddyListWidget(),
        new PluginIpcWidget(),
        new ConditionWidget(),
        new GaugeWidget(),
        new CommandWidget(),
        new AddonWidget(),
        new AddonInspectorWidget(),
        new AtkArrayDataBrowserWidget(),
        new StartInfoWidget(),
        new TargetWidget(),
        new ToastWidget(),
        new FlyTextWidget(),
        new ImGuiWidget(),
        new TexWidget(),
        new KeyStateWidget(),
        new GamepadWidget(),
        new ConfigurationWidget(),
        new TaskSchedulerWidget(),
        new HookWidget(),
        new AetherytesWidget(),
        new DtrBarWidget(),
        new UIColorWidget(),
        new DataShareWidget(),
        new NetworkMonitorWidget(),
    };

    private readonly Dictionary<DataKind, string> dataKindNames = new();

    private bool isExcept;
    private DataKind currentKind;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="DataWindow"/> class.
    /// </summary>
    public DataWindow()
        : base("Dalamud Data")
    {
        this.Size = new Vector2(500, 500);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.RespectCloseHotkey = false;

        foreach (var dataKind in Enum.GetValues<DataKind>())
        {
            this.dataKindNames[dataKind] = dataKind.ToString().Replace("_", " ");
        }

        this.Load();
    }

    /// <inheritdoc/>
    public override void OnOpen()
    {
    }

    /// <inheritdoc/>
    public override void OnClose()
    {
    }

    /// <summary>
    /// Set the DataKind dropdown menu.
    /// </summary>
    /// <param name="dataKind">Data kind name, can be lower and/or without spaces.</param>
    public void SetDataKind(string dataKind)
    {
        if (string.IsNullOrEmpty(dataKind))
            return;

        dataKind = dataKind switch
        {
            "ai" => "Addon Inspector",
            "at" => "Object Table", // Actor Table
            "ot" => "Object Table",
            "uic" => "UIColor",
            _ => dataKind,
        };

        dataKind = dataKind.Replace(" ", string.Empty).ToLower();

        var matched = Enum
                      .GetValues<DataKind>()
                      .FirstOrDefault(kind => Enum.GetName(kind)?.Replace("_", string.Empty).ToLower() == dataKind);

        if (matched != default)
        {
            this.currentKind = matched;
        }
        else
        {
            Service<ChatGui>.Get().PrintError($"/xldata: Invalid data type {dataKind}");
        }
    }

    /// <summary>
    /// Draw the window via ImGui.
    /// </summary>
    public override void Draw()
    {
        if (ImGuiComponents.IconButton("forceReload", FontAwesomeIcon.Sync)) this.Load();
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Force Reload");
        ImGui.SameLine();
        var copy = ImGuiComponents.IconButton("copyAll", FontAwesomeIcon.ClipboardList);
        if (ImGui.IsItemHovered()) ImGui.SetTooltip("Copy All");
        ImGui.SameLine();

        ImGui.SetNextItemWidth(275.0f * ImGuiHelpers.GlobalScale);
        if (ImGui.BeginCombo("Data Kind", this.dataKindNames[this.currentKind]))
        {
            foreach (var module in this.modules.OrderBy(module => this.dataKindNames[module.DataKind]))
            {
                if (ImGui.Selectable(this.dataKindNames[module.DataKind], this.currentKind == module.DataKind))
                {
                    this.currentKind = module.DataKind;
                }
            }
            
            ImGui.EndCombo();
        }
        
        ImGuiHelpers.ScaledDummy(10.0f);

        ImGui.BeginChild("scrolling", Vector2.Zero, false, ImGuiWindowFlags.HorizontalScrollbar);

        if (copy)
            ImGui.LogToClipboard();

        try
        {
            var selectedWidget = this.modules.FirstOrDefault(dataWindowWidget => dataWindowWidget.DataKind == this.currentKind);

            if (selectedWidget is { Ready: true })
            {
                selectedWidget.Draw();
            }
            else
            {
                ImGui.TextUnformatted("Data not ready.");
            }

            this.isExcept = false;
        }
        catch (Exception ex)
        {
            if (!this.isExcept)
            {
                Log.Error(ex, "Could not draw data");
            }

            this.isExcept = true;

            ImGui.TextUnformatted(ex.ToString());
        }

        ImGui.EndChild();
    }

    private void Load()
    {
        foreach (var widget in this.modules)
        {
            widget.Load();
        }
    }
}
