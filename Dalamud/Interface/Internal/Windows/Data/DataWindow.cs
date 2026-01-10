using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;
using Dalamud.Game.Gui;
using Dalamud.Interface.Components;
using Dalamud.Interface.Internal.Windows.Data.Widgets;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;

using Serilog;

namespace Dalamud.Interface.Internal.Windows.Data;

/// <summary>
/// Class responsible for drawing the data/debug window.
/// </summary>
internal class DataWindow : Window, IDisposable
{
    private readonly IDataWindowWidget[] modules =
    [
        new AddonInspectorWidget(),
        new AddonInspectorWidget2(),
        new AddonLifecycleWidget(),
        new AddonWidget(),
        new AddressesWidget(),
        new AetherytesWidget(),
        new AtkArrayDataBrowserWidget(),
        new BuddyListWidget(),
        new CommandWidget(),
        new ConditionWidget(),
        new ConfigurationWidget(),
        new DataShareWidget(),
        new DtrBarWidget(),
        new FateTableWidget(),
        new FlyTextWidget(),
        new FontAwesomeTestWidget(),
        new GameInventoryTestWidget(),
        new GamePrebakedFontsTestWidget(),
        new GamepadWidget(),
        new GaugeWidget(),
        new HookWidget(),
        new IconBrowserWidget(),
        new ImGuiWidget(),
        new InventoryWidget(),
        new KeyStateWidget(),
        new LogMessageMonitorWidget(),
        new MarketBoardWidget(),
        new NetworkMonitorWidget(),
        new NounProcessorWidget(),
        new ObjectTableWidget(),
        new PartyListWidget(),
        new PluginIpcWidget(),
        new SeFontTestWidget(),
        new ServicesWidget(),
        new SeStringCreatorWidget(),
        new SeStringRendererTestWidget(),
        new StartInfoWidget(),
        new TargetWidget(),
        new TaskSchedulerWidget(),
        new TexWidget(),
        new ToastWidget(),
        new UiColorWidget(),
        new UldWidget(),
        new VfsWidget(),
    ];

    private readonly IOrderedEnumerable<IDataWindowWidget> orderedModules;

    private bool isExcept;
    private bool selectionCollapsed;

    private bool isLoaded;

    /// <summary>
    /// Initializes a new instance of the <see cref="DataWindow"/> class.
    /// </summary>
    public DataWindow()
        : base("Dalamud Data", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.Size = new Vector2(400, 300);
        this.SizeCondition = ImGuiCond.FirstUseEver;

        this.RespectCloseHotkey = false;
        this.orderedModules = this.modules.OrderBy(module => module.DisplayName);
        this.CurrentWidget = this.orderedModules.First();
    }

    /// <summary>Gets or sets the current widget.</summary>
    public IDataWindowWidget CurrentWidget { get; set; }

    /// <inheritdoc/>
    public void Dispose() => this.modules.OfType<IDisposable>().AggregateToDisposable().Dispose();

    /// <inheritdoc/>
    public override void OnOpen()
    {
        this.Load();
    }

    /// <inheritdoc/>
    public override void OnClose()
    {
    }

    /// <summary>Gets the data window widget of the specified type.</summary>
    /// <typeparam name="T">Type of the data window widget to find.</typeparam>
    /// <returns>Found widget.</returns>
    public T GetWidget<T>() where T : IDataWindowWidget
    {
        foreach (var m in this.modules)
        {
            if (m is T w)
                return w;
        }

        throw new ArgumentException($"No widget of type {typeof(T).FullName} found.");
    }

    /// <summary>
    /// Set the DataKind dropdown menu.
    /// </summary>
    /// <param name="dataKind">Data kind name, can be lower and/or without spaces.</param>
    public void SetDataKind(string dataKind)
    {
        if (string.IsNullOrEmpty(dataKind))
            return;

        if (this.modules.FirstOrDefault(module => module.IsWidgetCommand(dataKind)) is { } targetModule)
        {
            this.CurrentWidget = targetModule;
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
        // Only draw the widget contents if the selection pane is collapsed.
        if (this.selectionCollapsed)
        {
            this.DrawContents();
            return;
        }

        if (ImGui.BeginTable("XlData_Table"u8, 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.Resizable))
        {
            ImGui.TableSetupColumn("##SelectionColumn"u8, ImGuiTableColumnFlags.WidthFixed, 200.0f * ImGuiHelpers.GlobalScale);
            ImGui.TableSetupColumn("##ContentsColumn"u8, ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextColumn();
            this.DrawSelection();

            ImGui.TableNextColumn();
            this.DrawContents();

            ImGui.EndTable();
        }
    }

    private void DrawSelection()
    {
        if (ImGui.BeginChild("XlData_SelectionPane"u8, ImGui.GetContentRegionAvail()))
        {
            if (ImGui.BeginListBox("WidgetSelectionListbox"u8, ImGui.GetContentRegionAvail()))
            {
                foreach (var widget in this.orderedModules)
                {
                    if (ImGui.Selectable(widget.DisplayName, this.CurrentWidget == widget))
                    {
                        this.CurrentWidget = widget;
                    }
                }

                ImGui.EndListBox();
            }
        }

        ImGui.EndChild();
    }

    private void DrawContents()
    {
        if (ImGui.BeginChild("XlData_ContentsPane"u8, ImGui.GetContentRegionAvail()))
        {
            if (ImGuiComponents.IconButton("collapse-expand", this.selectionCollapsed ? FontAwesomeIcon.ArrowRight : FontAwesomeIcon.ArrowLeft))
            {
                this.selectionCollapsed = !this.selectionCollapsed;
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip($"{(this.selectionCollapsed ? "Expand" : "Collapse")} selection pane");
            }

            ImGui.SameLine();

            if (ImGuiComponents.IconButton("forceReload", FontAwesomeIcon.Sync))
            {
                this.isLoaded = false;
                this.Load();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Force Reload"u8);
            }

            ImGui.SameLine();

            var copy = ImGuiComponents.IconButton("copyAll", FontAwesomeIcon.ClipboardList);

            ImGuiHelpers.ScaledDummy(10.0f);

            if (ImGui.BeginChild("XlData_WidgetContents"u8, ImGui.GetContentRegionAvail()))
            {
                if (copy)
                    ImGui.LogToClipboard();

                try
                {
                    if (this.CurrentWidget is { Ready: true })
                    {
                        this.CurrentWidget.Draw();
                    }
                    else
                    {
                        ImGui.Text("Data not ready."u8);
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

                    ImGui.Text(ex.ToString());
                }
            }

            ImGui.EndChild();
        }

        ImGui.EndChild();
    }

    private void Load()
    {
        if (this.isLoaded)
            return;

        this.isLoaded = true;

        foreach (var widget in this.modules)
        {
            widget.Load();
        }
    }
}
