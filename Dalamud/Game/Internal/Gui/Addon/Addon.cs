using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.Internal.Gui.Addon {
    public class Addon {
        public IntPtr Address;
        protected Structs.Addon addonStruct;
        
        public Addon(IntPtr address, Structs.Addon addonStruct) {
            this.Address = address;
            this.addonStruct = addonStruct;
        }

        public string Name => this.addonStruct.Name;
        public short X => this.addonStruct.X;
        public short Y => this.addonStruct.Y;
        public float Scale => this.addonStruct.Scale;
        public unsafe float Width => this.addonStruct.RootNode->Width * Scale;
        public unsafe float Height => this.addonStruct.RootNode->Height * Scale;

        public bool Visible => (this.addonStruct.Flags & 0x20) == 0x20;
    }
}
