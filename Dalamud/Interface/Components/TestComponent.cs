using ImGuiNET;

namespace Dalamud.Interface.Components
{
    /// <summary>
    /// Test component to demonstrate how ImGui components work.
    /// </summary>
    public class TestComponent : IComponent
    {
        /// <summary>
        /// Gets component name.
        /// </summary>
        public string Name { get; } = "Test Component";

        /// <summary>
        /// Draw test component.
        /// </summary>
        public void Draw()
        {
            ImGui.Text("You are viewing the test component. The test was a success.");
        }
    }
}
