using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using ImGuiScene;

namespace Dalamud.Interface
{
    public class InterfaceManager : IDisposable
    {
        private Task _task;

        public void Dispose()
        {
            _task?.Wait();
            _task = null;
        }

        public void Start()
        {
            if (_task == null || _task.IsCompleted || _task.IsFaulted || _task.IsCanceled)
            {
                _task = new Task(Display);
                _task.Start();
            }
        }

        private void Display()
        {
            using (var scene = SimpleImGuiScene.CreateOverlay(RendererFactory.RendererBackend.DirectX11))
            {
                scene.OnBuildUI += DrawUI;
                scene.Run();
            }
        }

        private void DrawUI()
        {
            ImGui.ShowDemoWindow();
        }
    }
}
