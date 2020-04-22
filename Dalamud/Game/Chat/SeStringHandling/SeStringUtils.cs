using Dalamud.Data.TransientSheet;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dalamud.Game.Chat.SeStringHandling
{
    public class SeStringUtils
    {
        public static SeString CreateItemLink(uint itemId, bool isHQ, string displayNameOverride = null)
        {
            string displayName = displayNameOverride ?? SeString.Dalamud.Data.GetExcelSheet<Item>().GetRow((int)itemId).Name;
            if (isHQ)
            {
                displayName += " \uE03C";
            }

            var payloads = new List<Payload>(new Payload[]
            {
                new UIForegroundPayload(0x0225),
                new UIGlowPayload(0x0226),
                new ItemPayload(itemId, isHQ),
                new UIForegroundPayload(0x01F4),
                new UIGlowPayload(0x01F5),
                new TextPayload("\uE0BB"),
                new UIGlowPayload(0),
                new UIForegroundPayload(0),
                new TextPayload(displayName),
                new RawPayload(new byte[] { 0x02, 0x27, 0x07, 0xCF, 0x01, 0x01, 0x01, 0xFF, 0x01, 0x03 })
            });

            return new SeString(payloads);
        }
    }
}
