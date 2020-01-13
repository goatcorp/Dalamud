using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ImGuiNET;
using ImGuiScene;

namespace Dalamud.Interface
{
    public class UiBuilder : IDisposable {
        private readonly string namespaceName;

        public event RawDX11Scene.BuildUIDelegate OnBuildUi;

        private InterfaceManager interfaceManager;

        public UiBuilder(InterfaceManager interfaceManager, string namespaceName) {
            this.namespaceName = namespaceName;

            this.interfaceManager = interfaceManager;
            this.interfaceManager.OnDraw += OnDraw;
        }

        public void Dispose() {
            this.interfaceManager.OnDraw -= OnDraw;
        }

        private void OnDraw() {
            ImGui.PushID(this.namespaceName);
            OnBuildUi?.Invoke();
            ImGui.PopID();
        }
    }
}
