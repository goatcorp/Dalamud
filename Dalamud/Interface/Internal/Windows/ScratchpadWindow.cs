using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Colors;
using Dalamud.Interface.Internal.Scratchpad;
using Dalamud.Interface.Windowing;
using ImGuiNET;
using Serilog;

namespace Dalamud.Interface.Internal.Windows
{
    /// <summary>
    /// This class facilitates interacting with the ScratchPad window.
    /// </summary>
    internal class ScratchpadWindow : Window, IDisposable
    {
        private readonly Dalamud dalamud;
        private readonly List<ScratchpadDocument> documents = new();
        private readonly ScratchFileWatcher watcher = new();
        private string pathInput = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScratchpadWindow"/> class.
        /// </summary>
        /// <param name="dalamud">The Dalamud instance.</param>
        public ScratchpadWindow(Dalamud dalamud)
            : base("Plugin Scratchpad", ImGuiWindowFlags.MenuBar)
        {
            this.dalamud = dalamud;
            this.documents.Add(new ScratchpadDocument());

            this.SizeConstraintsMin = new Vector2(400, 400);

            this.Execution = new ScratchExecutionManager(dalamud);
        }

        /// <summary>
        /// Gets the ScratchPad execution manager.
        /// </summary>
        public ScratchExecutionManager Execution { get; private set; }

        /// <inheritdoc/>
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
                        var content = docs[i].Content;
                        if (ImGui.InputTextMultiline("###ScratchInput" + i, ref content, 20000, new Vector2(-1, -34), ImGuiInputTextFlags.AllowTabInput))
                        {
                            docs[i].Content = content;
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
                            this.dalamud.DalamudUi.ToggleLogWindow();
                        }

                        ImGui.SameLine();

                        var isMacro = docs[i].IsMacro;
                        if (ImGui.Checkbox("Use Macros", ref isMacro))
                        {
                            docs[i].IsMacro = isMacro;
                        }

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

        /// <summary>
        /// Dispose of managed and unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Execution.DisposeAllScratches();
        }
    }
}
