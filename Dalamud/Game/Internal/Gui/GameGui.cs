using System;
using Serilog;

namespace Dalamud.Game.Internal.Gui {
    public sealed class GameGui : IDisposable {
        private GameGuiAddressResolver Address { get; }
        
        public ChatGui Chat { get; private set; } 
        
        public GameGui(IntPtr baseAddress, SigScanner scanner, Dalamud dalamud) {
            Address = new GameGuiAddressResolver(baseAddress);
            Address.Setup(scanner);
            
            Log.Verbose("GameGuiManager address {Address}", Address.BaseAddress);
            
            Chat = new ChatGui(Address.ChatManager, scanner, dalamud);
        }

        public void Enable() {
            Chat.Enable();
        }

        public void Dispose() {
            Chat.Dispose();
        }
    }
}
