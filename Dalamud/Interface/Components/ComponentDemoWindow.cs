using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Interface.Windowing;
using Dalamud.Plugin;
using ImGuiNET;

namespace Dalamud.Interface.Components
{
    /// <summary>
    /// Component Demo Window to view custom components.
    /// </summary>
    internal class ComponentDemoWindow : Window
    {
        private List<IComponent> components = new List<IComponent>();

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentDemoWindow"/> class.
        /// </summary>
        public ComponentDemoWindow()
            : base("Dalamud Components Demo")
        {
            this.Size = new Vector2(600, 500);
            this.SizeCondition = ImGuiCond.FirstUseEver;
            this.AddComponents();
            this.SortComponents();
        }

        /// <inheritdoc/>
        public override void Draw()
        {
            ImGui.BeginChild("comp_scrolling", new Vector2(0, 0), false, ImGuiWindowFlags.AlwaysVerticalScrollbar | ImGuiWindowFlags.HorizontalScrollbar);

            ImGui.Text("This is a collection of UI components you can use in your plugin.");

            for (var i = 0; i < this.components.Count; i++)
            {
                var thisComp = this.components[i];

                if (ImGui.CollapsingHeader($"{thisComp.Name} ({thisComp.GetType().FullName})###comp{i}"))
                {
                    thisComp.Draw();
                }
            }

            ImGui.EndChild();
        }

        private void AddComponents()
        {
            this.components.Add(new TestComponent());
            this.components.Add(new HelpMarkerComponent("help me!")
            {
                SameLine = false,
            });
            var iconButtonComponent = new IconButtonComponent(1, FontAwesomeIcon.Carrot);
            iconButtonComponent.OnButtonClicked += id => PluginLog.Log("Button#{0} clicked!", id);
            this.components.Add(iconButtonComponent);
        }

        private void SortComponents()
        {
            this.components = this.components.OrderBy(component => component.Name).ToList();
        }
    }
}
