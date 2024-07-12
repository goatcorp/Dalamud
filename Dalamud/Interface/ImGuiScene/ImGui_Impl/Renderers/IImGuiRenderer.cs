namespace ImGuiScene
{
    /// <summary>
    /// A simple shared public interface that all ImGui render implementations follow.
    /// </summary>
    public interface IImGuiRenderer
    {
        // FIXME - probably a better way to do this than params object[] !
        void Init(params object[] initParams);
        void Shutdown();
        void NewFrame();
        void RenderDrawData(ImGuiNET.ImDrawDataPtr drawData);
    }
}
