using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using ImGuiScene;
using SDL2;

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
            using (var scene = new SimpleImGuiScene("Debug", fullscreen: true))
            {
                scene.Window.MakeTransparent(SimpleSDLWindow.CreateColorKey(0, 0, 0));

                scene.Window.OnSDLEvent += (ref SDL.SDL_Event sdlEvent) =>
                {
                    if (sdlEvent.type == SDL.SDL_EventType.SDL_KEYDOWN && sdlEvent.key.keysym.scancode == SDL.SDL_Scancode.SDL_SCANCODE_ESCAPE)
                    {
                        scene.ShouldQuit = true;
                    }
                    return true;
                };

                scene.OnBuildUI += () =>
                {
                    ImGui.ShowDemoWindow();
                };

                scene.Run();
            }
        }

    }
}
