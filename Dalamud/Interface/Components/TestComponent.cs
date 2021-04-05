using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;

namespace Dalamud.Interface.Components
{
    class TestComponent : IComponent
    {
        public string Name { get; } = "Test Component";

        public void Draw()
        {
            ImGui.Text("You are viewing the test component. The test was a success.");
        }
    }
}
