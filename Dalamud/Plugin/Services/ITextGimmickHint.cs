using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dalamud.Plugin.Services
{

    /// <summary>
    /// This class handles interacting with the native Raid TextGimmickHint UI.
    /// </summary>
    internal interface ITextGimmickHint
    {
        /// <summary>
        /// Show a text gimmick hint.
        /// </summary>
        /// <param name="text">text on hint</param>
        /// <param name="style">is red or blue</param>
        /// <param name="hundredMS">time of hint.</param>
        public void ShowTextGimmickHint(string text, TextHintStyle style, int hundredMS);

        /// <summary>
        /// Hint style.
        /// </summary>
        public enum TextHintStyle : byte
        {
            Red = 0,
            Blue = 1,
        }
    }
}
