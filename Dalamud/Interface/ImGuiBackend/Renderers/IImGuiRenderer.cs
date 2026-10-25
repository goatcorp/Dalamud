using Dalamud.Bindings.ImGui;

namespace Dalamud.Interface.ImGuiBackend.Renderers;

/// <summary>A simple shared public interface that all ImGui render implementations follow.</summary>
internal interface IImGuiRenderer : IDisposable
{
    /// <summary>Notifies that the window is about to be resized.</summary>
    void OnPreResize();

    /// <summary>Notifies that the window has been resized.</summary>
    /// <param name="width">The new window width.</param>
    /// <param name="height">The new window height.</param>
    void OnPostResize(int width, int height);

    /// <summary>Marks the beginning of a new frame.</summary>
    void OnNewFrame();

    /// <summary>Renders the draw data.</summary>
    /// <param name="drawData">The draw data.</param>
    void RenderDrawData(ImDrawDataPtr drawData);
}
