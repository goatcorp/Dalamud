using System;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Dalamud.Interface;
using Dalamud.Interface.Colors;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Dalamud.CorePlugin
{
    /// <summary>
    /// Class responsible for drawing the main plugin window.
    /// </summary>
    internal class PluginWindow : Window, IDisposable
    {
        private Vector4 bgCol = ImGuiColors.HealerGreen;
        private Vector4 textCol = ImGuiColors.DalamudWhite;

        /// <summary>
        /// Initializes a new instance of the <see cref="PluginWindow"/> class.
        /// </summary>
        public PluginWindow()
            : base("CorePlugin")
        {
            this.IsOpen = true;

            this.Size = new Vector2(810, 520);
            this.SizeCondition = ImGuiCond.FirstUseEver;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
        }

        /// <inheritdoc/>
        public override void OnOpen()
        {
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            ImGui.ColorPicker4("bg", ref this.bgCol);
            ImGui.ColorPicker4("text", ref this.textCol);

            ImGui.PushStyleColor(ImGuiCol.ChildBg, this.bgCol);
            ImGui.PushStyleColor(ImGuiCol.Text, this.textCol);

            if (ImGui.BeginChild("##changelog", new Vector2(-1, 100), true, ImGuiWindowFlags.NoNavFocus | ImGuiWindowFlags.NoNavInputs | ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("Changelog:");
                ImGuiHelpers.ScaledDummy(2);
                ImGui.TextWrapped("* ASIhif ai fdh adhsfuoadf\n* IUHoiaudsfh adsof hioaudshfuio husiodfh\n* A iiaojfdpasd ijopadfnklafwjenalkfjensgdlkjnasasdfbhnj");
            }

            ImGui.EndChild();
            ImGui.PopStyleColor(2);
        }
    }
}
