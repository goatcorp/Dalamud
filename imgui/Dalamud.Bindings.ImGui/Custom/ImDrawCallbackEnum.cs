namespace Dalamud.Bindings.ImGui;

public enum ImDrawCallbackEnum : long
{
    Empty,

    /// <summary>
    /// Special Draw callback value to request renderer backend to reset the graphics/render state.
    /// The renderer backend needs to handle this special value, otherwise it will crash trying to call a function at
    /// this address. This is useful for example if you submitted callbacks which you know have altered the render
    /// state, and you want it to be restored. It is not done by default because they are many perfectly useful way of
    /// altering render state for imgui contents (e.g. changing shader/blending settings before an Image call).
    /// </summary>
    ResetRenderState = ImGui.ImDrawCallbackResetRenderState,
}
