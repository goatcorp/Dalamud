using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Newtonsoft.Json;

namespace Dalamud.Game.Chat.SeStringHandling
{
    /// <summary>
    /// This class represents a parsed SeString.
    /// </summary>
    public class SeString
    {
        /// <summary>
        /// The ordered list of payloads included in this SeString.
        /// </summary>
        public List<Payload> Payloads { get; }

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
        /// Creates a new SeString from an ordered list of payloads.
        /// </summary>
        /// <param name="payloads">The Payload objects to make up this string.</param>
        [JsonConstructor]
        public SeString(List<Payload> payloads)
        {
            Payloads = payloads;
        }

        /// <summary>
        /// Creates a new SeString from an ordered list of payloads.
        /// </summary>
        /// <param name="payloads">The Payload objects to make up this string.</param>
        public SeString(Payload[] payloads)
        {
            Payloads = new List<Payload>(payloads);
        }

        /// <summary>
        /// Appends the contents of one SeString to this one.
        /// </summary>
        /// <param name="other">The SeString to append to this one.</param>
        /// <returns>This object.</returns>
        public SeString Append(SeString other)
        {
            Payloads.AddRange(other.Payloads);
            return this;
        }

        /// <summary>
        /// Appends a list of payloads to this SeString.
        /// </summary>
        /// <param name="payloads">The Payloads to append.</param>
        /// <returns>This object.</returns>
        public SeString Append(List<Payload> payloads)
        {
            Payloads.AddRange(payloads);
            return this;
        }

        /// <summary>
        /// Appends a single payload to this SeString.
        /// </summary>
        /// <param name="payload">The payload to append.</param>
        /// <returns>This object.</returns>
        public SeString Append(Payload payload)
        {
            Payloads.Add(payload);
            return this;
        }

        /// <summary>
        /// Encodes the Payloads in this SeString into a binary representation
        /// suitable for use by in-game handlers, such as the chat log.
        /// </summary>
        /// <returns>The binary encoded payload data.</returns>
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
