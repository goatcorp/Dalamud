using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using Dalamud.Plugin.Services;

using FFXIVClientStructs.FFXIV.Client.UI;

using static Dalamud.Plugin.Services.ITextGimmickHint;

namespace Dalamud.Game.Gui
{
    [ServiceManager.EarlyLoadedService]
    internal sealed unsafe class TextGimmickHint : ITextGimmickHint
    {
        private readonly TextGimmickHintAddressResolver address;

        private delegate void ShowTextGimmickHintDelegate(RaptureAtkModule* instance, byte[] str, byte style, int a4);

        private readonly ShowTextGimmickHintDelegate showTextGimmickHint;


        [ServiceManager.ServiceConstructor]
        private TextGimmickHint(TargetSigScanner sigScanner)
        {
            this.address = new TextGimmickHintAddressResolver();
            this.address.Setup(sigScanner);

            this.showTextGimmickHint = Marshal.GetDelegateForFunctionPointer<ShowTextGimmickHintDelegate>(this.address.ShowTextGimmickHint);
        }

        /// <inheritdoc/>
        public void ShowTextGimmickHint(string text, TextHintStyle style, int hundredMS)
        {
            this.showTextGimmickHint?.Invoke(RaptureAtkModule.Instance(), Encoding.UTF8.GetBytes(text + "\0"), (byte)style, hundredMS);
        }

    }
}
