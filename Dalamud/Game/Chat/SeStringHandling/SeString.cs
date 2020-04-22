using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Dalamud.Game.Chat.SeStringHandling
{
    /// <summary>
    /// This class represents a parsed SeString.
    /// </summary>
    public class SeString
    {
        // TODO: probably change how this is done/where it comes from
        public static Dalamud Dalamud { get; internal set; }

        public List<Payload> Payloads { get; }

        public SeString(List<Payload> payloads)
        {
            Payloads = payloads;
        }

        /// <summary>
        /// Helper function to get all raw text from a message as a single joined string
        /// </summary>
        /// <returns>
        /// All the raw text from the contained payloads, joined into a single string
        /// </returns>
        public string TextValue
        {
            get
            {
                return Payloads
                    .Where(p => p is ITextProvider)
                    .Cast<ITextProvider>()
                    .Aggregate(new StringBuilder(), (sb, tp) => sb.Append(tp.Text), sb => sb.ToString());
            }
        }

        /// <summary>
        /// Parse an array of bytes to a SeString.
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static SeString Parse(byte[] bytes)
        {
            var payloads = new List<Payload>();

            using (var stream = new MemoryStream(bytes))
            using (var reader = new BinaryReader(stream))
            {
                while (stream.Position < bytes.Length)
                {
                    var payload = Payload.Decode(reader);
                    if (payload != null)
                        payloads.Add(payload);
                }
            }

            return new SeString(payloads);
        }

        /// <summary>
        /// Encode a parsed/created SeString to an array of bytes, to be used for injection.
        /// </summary>
        /// <param name="payloads"></param>
        /// <returns>The bytes of the message.</returns>
        public byte[] Encode()
        {
            var messageBytes = new List<byte>();
            foreach (var p in Payloads)
            {
                messageBytes.AddRange(p.Encode());
            }

            return messageBytes.ToArray();
        }
    }
}
