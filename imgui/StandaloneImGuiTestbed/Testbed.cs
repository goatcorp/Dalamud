using Dalamud.Bindings.ImGui;

namespace StandaloneImGuiTestbed;

public class Testbed
{
    private bool showDemoWindow = false;

    public unsafe void Draw()
    {
        if (ImGui.Begin("Testbed"))
        {
            ImGui.Text("Hello!");

            if (ImGui.Button("Open demo"))
            {
                this.showDemoWindow = true;
            }

            if (this.showDemoWindow)
            {
                ImGui.ShowDemoWindow(ref this.showDemoWindow);
            }

            if (ImGui.Button("Access context"))
            {
                var context = ImGui.GetCurrentContext();
                var currentWindow = context.CurrentWindow;
                ref var dc = ref currentWindow.DC; // BREAKPOINT HERE, currentWindow will be invalid
                dc.CurrLineTextBaseOffset = 0;
            }

            ImGui.End();
        }
    }
}
