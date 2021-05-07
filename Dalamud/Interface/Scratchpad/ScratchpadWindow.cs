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
    class ScratchpadWindow : Window, IDisposable
    {
        private readonly Dalamud dalamud;

        public ScratchExecutionManager Execution { get; private set; }

        private List<ScratchpadDocument> documents = new List<ScratchpadDocument>();

        private ScratchFileWatcher watcher = new ScratchFileWatcher();

        private string pathInput = string.Empty;

        public ScratchpadWindow(Dalamud dalamud)
            : base("Plugin Scratchpad", ImGuiWindowFlags.MenuBar)
        {
            this.dalamud = dalamud;
            this.documents.Add(new ScratchpadDocument());

            this.SizeConstraintsMin = new Vector2(400, 400);

            this.Execution = new ScratchExecutionManager(dalamud);
        }

        public override void Draw()
        {
            if (ImGui.BeginPopupModal("Choose Path"))
            {
                ImGui.Text("Enter path:\n\n");

                ImGui.InputText("###ScratchPathInput", ref this.pathInput, 1000);

                if (ImGui.Button("OK", new Vector2(120, 0)))
                {
                    ImGui.CloseCurrentPopup();
                    this.watcher.Load(this.pathInput);
                    this.pathInput = string.Empty;
                }

                ImGui.SetItemDefaultFocus();
                ImGui.SameLine();
                if (ImGui.Button("Cancel", new Vector2(120, 0)))
                {
                    ImGui.CloseCurrentPopup();
                }

                ImGui.EndPopup();
            }

            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("Load & Watch"))
                    {
                        ImGui.OpenPopup("Choose Path");
                    }

                    ImGui.EndMenu();
                }

                ImGui.EndMenuBar();
            }

            var flags = ImGuiTabBarFlags.Reorderable | ImGuiTabBarFlags.TabListPopupButton |
                        ImGuiTabBarFlags.FittingPolicyScroll;

            if (ImGui.BeginTabBar("ScratchDocTabBar", flags))
            {
                if (ImGui.TabItemButton("+", ImGuiTabItemFlags.Trailing | ImGuiTabItemFlags.NoTooltip))
                    this.documents.Add(new ScratchpadDocument());

                var docs = this.documents.Concat(this.watcher.TrackedScratches).ToArray();

                for (var i = 0; i < docs.Length; i++)
                {
                    var isOpen = true;

                    if (ImGui.BeginTabItem(docs[i].Title + (docs[i].HasUnsaved ? "*" : string.Empty) + "###ScratchItem" + i, ref isOpen))
                    {
                        if (ImGui.InputTextMultiline("###ScratchInput" + i, ref docs[i].Content, 20000, new Vector2(-1, -34), ImGuiInputTextFlags.AllowTabInput))
                        {
                            docs[i].HasUnsaved = true;
                        }

                        ImGuiHelpers.ScaledDummy(3);

                        if (ImGui.Button("Compile & Reload"))
                        {
                            docs[i].Status = this.Execution.RenewScratch(docs[i]);
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
                                var code = this.Execution.MacroProcessor.Process(docs[i].Content);
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

                        ImGui.Checkbox("Use Macros", ref docs[i].IsMacro);

                        ImGui.SameLine();

                        switch (docs[i].Status)
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

                        ImGui.TextColored(ImGuiColors.DalamudGrey, docs[i].Id.ToString());

                        ImGui.EndTabItem();
                    }
                }

                ImGui.EndTabBar();
            }
        }

        public void Dispose()
        {
            this.Execution.DisposeAllScratches();
        }
    }
}
