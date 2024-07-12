namespace ImGuiScene
{
    public interface IImGuiInputHandler : IDisposable
    {
        void NewFrame(int width, int height);
        void SetIniPath(string path);
    }
}
