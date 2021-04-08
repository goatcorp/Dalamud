using System.Numerics;

using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace Dalamud.Interface.Components
{
    /// <summary>
    /// Component Demo Window to view custom components.
    /// </summary>
    internal class ComponentDemoWindow : Window
    {
        private readonly IComponent[] components =
        {
            new TestComponent(),
        };

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentDemoWindow"/> class.
        /// </summary>
        public ComponentDemoWindow()
            : base("Dalamud Components Demo")
        {
            this.Size = new Vector2(600, 500);
            this.SizeCondition = ImGuiCond.FirstUseEver;
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            ImGui.BeginChild("comp_scrolling", new Vector2(0, 0), false, ImGuiWindowFlags.AlwaysVerticalScrollbar | ImGuiWindowFlags.HorizontalScrollbar);

            ImGui.Text("This is a collection of UI components you can use in your plugin.");

            for (var i = 0; i < this.components.Length; i++)
            {
                var thisComp = this.components[i];

                if (ImGui.CollapsingHeader($"{thisComp.Name} ({thisComp.GetType().FullName})###comp{i}"))
                {
                    thisComp.Draw();
                }
            }

            ImGui.EndChild();
        }
    }
}
