using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Game.Gui
{
    internal sealed class TextGimmickHintAddressResolver : BaseAddressResolver
    {
        public IntPtr ShowTextGimmickHint { get; private set; }

        protected override void Setup64Bit(ISigScanner scanner)
        {
            this.ShowTextGimmickHint = scanner.ScanText("48 ?? ?? 0F 84 ?? ?? ?? ?? 4C ?? ?? 49 89 5B ?? 49 89 73");
        }
    }
}
