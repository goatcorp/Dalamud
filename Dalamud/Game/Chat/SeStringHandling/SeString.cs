using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dalamud.Game.Chat.SeStringHandling.Payloads;

namespace Dalamud.Game.Chat.SeStringHandling
{
    /// <summary>
    /// This class represents a parsed SeString.
    /// </summary>
    public class SeString
    {
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
            get {
                var sb = new StringBuilder();
                foreach (var p in Payloads)
                {
                    if (p.Type == PayloadType.RawText)
                    {
                        sb.Append(((TextPayload)p).Text);
                    }
                }

                return sb.ToString();
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

            using (var stream = new MemoryStream(bytes)) {
                using var reader = new BinaryReader(stream);

                while (stream.Position < bytes.Length)
                {
                    var payload = Payload.Process(reader);
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
