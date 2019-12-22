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
                // this basically pauses background rendering to reduce cpu load by the scene when it isn't actively in focus
                // the impact is generally pretty minor, but it's probably best to enable when we can
                // If we have any windows that we want to update dynamically even when the game is the focus
                // and not the overlay, this should be disabled.
                // It is dynamic, so we could disable it only when dynamic windows are open etc
                scene.PauseWhenUnfocused = true;

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
