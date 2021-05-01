using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ImGuiNET;
using Serilog;

namespace Dalamud.Interface.Scratchpad
{
    class ScratchpadWindow : Window
    {
        private readonly Dalamud dalamud;

        public ScratchExecutionManager Execution { get; private set; }

        private List<ScratchpadDocument> documents = new List<ScratchpadDocument>();

        public ScratchpadWindow(Dalamud dalamud) :
            base("Plugin Scratchpad")
        {
            this.dalamud = dalamud;
            this.documents.Add(new ScratchpadDocument());

            this.SizeConstraintsMin = new Vector2(400, 400);

            this.Execution = new ScratchExecutionManager(dalamud);
        }

        public override void Draw()
        {
            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Load & Watch"))
                    {

                    }
                }
            }

            var flags = ImGuiTabBarFlags.Reorderable | ImGuiTabBarFlags.TabListPopupButton |
                        ImGuiTabBarFlags.FittingPolicyScroll;

            if (ImGui.BeginTabBar("ScratchDocTabBar", flags))
            {
                if (ImGui.TabItemButton("+", ImGuiTabItemFlags.Trailing | ImGuiTabItemFlags.NoTooltip))
                    this.documents.Add(new ScratchpadDocument());

                for (var i = 0; i < this.documents.Count; i++)
                {
                    var isOpen = true;

                    if (ImGui.BeginTabItem(this.documents[i].Title + (this.documents[i].HasUnsaved ? "*" : string.Empty) + "###ScratchItem" + i, ref isOpen))
                    {
                        if (ImGui.InputTextMultiline("###ScratchInput" + i, ref this.documents[i].Content, 20000,
                                                     new Vector2(-1, -34), ImGuiInputTextFlags.AllowTabInput))
                        {
                            this.documents[i].HasUnsaved = true;
                        }

                        ImGuiHelpers.ScaledDummy(3);

                        if (ImGui.Button("Compile & Reload"))
                        {
                            this.documents[i].Status = this.Execution.RenewScratch(this.documents[i]);
                        }

                        ImGui.SameLine();

                        if (ImGui.Button("Dispose all"))
                        {
                            this.Execution.DisposeAllScratches();
                        }

                        ImGui.SameLine();

                        if (ImGui.Button("Dump processed code"))
                        {
                            try
                            {
                                var code = this.Execution.MacroProcessor.Process(this.documents[i].Content);
                                Log.Information(code);
                                ImGui.SetClipboardText(code);
                            }
                            catch (Exception ex)
                            {
                                Log.Error(ex, "Could not process macros");
                            }
                        }

                        ImGui.SameLine();

                        if (ImGui.Button("Toggle Log"))
                        {
                            this.dalamud.DalamudUi.ToggleLog();
                        }

                        ImGui.SameLine();

                        ImGui.Checkbox("Use Macros", ref this.documents[i].IsMacro);

                        ImGui.SameLine();

                        switch (this.documents[i].Status)
                        {
                            case ScratchLoadStatus.Unknown:
                                ImGui.TextColored(ImGuiColors.DalamudGrey, "Compile scratch to see status");
                                break;
                            case ScratchLoadStatus.FailureCompile:
                                ImGui.TextColored(ImGuiColors.DalamudRed, "Error during compilation");
                                break;
                            case ScratchLoadStatus.FailureInit:
                                ImGui.TextColored(ImGuiColors.DalamudRed, "Error during init");
                                break;
                            case ScratchLoadStatus.Success:
                                ImGui.TextColored(ImGuiColors.HealerGreen, "OK!");
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        ImGui.SameLine();

                        ImGui.TextColored(ImGuiColors.DalamudGrey, this.documents[i].Id.ToString());

                        ImGui.EndTabItem();
                    }
                }

                ImGui.EndTabBar();
            }
        }
    }
}
