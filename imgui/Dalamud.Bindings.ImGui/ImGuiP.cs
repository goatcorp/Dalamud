#nullable disable

namespace Dalamud.Bindings.ImGui
{
    using HexaGen.Runtime;

    public static unsafe partial class ImGuiP
    {
        internal static FunctionTable funcTable;

        static ImGuiP()
        {
            funcTable = ImGui.funcTable;
        }
    }
}
