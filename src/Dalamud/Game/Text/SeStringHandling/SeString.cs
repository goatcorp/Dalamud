using System.Collections.Generic;
using System.Linq;
using System.Text;

using Dalamud.Data;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Newtonsoft.Json;

namespace Dalamud.Game.Text.SeStringHandling
{
    /// <summary>
    /// This class represents a parsed SeString.
    /// </summary>
    public class SeString
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SeString"/> class.
        /// Creates a new SeString from an ordered list of payloads.
        /// </summary>
        /// <param name="payloads">The Payload objects to make up this string.</param>
        [JsonConstructor]
        public SeString(List<Payload> payloads)
        {
            this.Payloads = payloads;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SeString"/> class.
        /// Creates a new SeString from an ordered list of payloads.
        /// </summary>
        /// <param name="payloads">The Payload objects to make up this string.</param>
        public SeString(Payload[] payloads)
        {
            this.Payloads = new List<Payload>(payloads);
        }

        /// <summary>
        /// Gets the ordered list of payloads included in this SeString.
        /// </summary>
        public List<Payload> Payloads { get; }

        /// <summary>
        /// Gets all of the raw text from a message as a single joined string.
        /// </summary>
        /// <returns>
        /// All the raw text from the contained payloads, joined into a single string.
        /// </returns>
        public string TextValue
        {
            get
            {
                return this.Payloads
                    .Where(p => p is ITextProvider)
                    .Cast<ITextProvider>()
                    .Aggregate(new StringBuilder(), (sb, tp) => sb.Append(tp.Text), sb => sb.ToString());
            }
        }

        /// <summary>
        /// Implicitly convert a string into a SeString containing a <see cref="TextPayload"/>.
        /// </summary>
        /// <param name="str">string to convert.</param>
        /// <returns>Equivalent SeString.</returns>
        public static implicit operator SeString(string str) => new(new Payload[] { new TextPayload(str) });

        /// <summary>
        /// Creates a SeString from a json. (For testing - not recommended for production use.)
        /// </summary>
        /// <param name="json">A serialized SeString produced by ToJson() <see cref="ToJson"/>.</param>
        /// <param name="dataManager">An initialized instance of DataManager for Lumina queries.</param>
        /// <returns>A SeString initialized with values from the json.</returns>
        public static SeString FromJson(string json, DataManager dataManager)
        {
            var s = JsonConvert.DeserializeObject<SeString>(json, new JsonSerializerSettings
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                TypeNameHandling = TypeNameHandling.Auto,
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
            });

            foreach (var payload in s.Payloads)
            {
                payload.DataResolver = dataManager;
            }

            return s;
        }

        /// <summary>
        /// Serializes the SeString to json.
        /// </summary>
        /// <returns>An json representation of this object.</returns>
        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented, new JsonSerializerSettings()
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects,
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto,
            });
        }

        /// <summary>
        /// Appends the contents of one SeString to this one.
        /// </summary>
        /// <param name="other">The SeString to append to this one.</param>
        /// <returns>This object.</returns>
        public SeString Append(SeString other)
        {
            this.Payloads.AddRange(other.Payloads);
            return this;
        }

        /// <summary>
        /// Appends a list of payloads to this SeString.
        /// </summary>
        /// <param name="payloads">The Payloads to append.</param>
        /// <returns>This object.</returns>
        public SeString Append(List<Payload> payloads)
        {
            this.Payloads.AddRange(payloads);
            return this;
        }

        /// <summary>
        /// Appends a single payload to this SeString.
        /// </summary>
        /// <param name="payload">The payload to append.</param>
        /// <returns>This object.</returns>
        public SeString Append(Payload payload)
        {
            this.Payloads.Add(payload);
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
            foreach (var p in this.Payloads)
            {
                messageBytes.AddRange(p.Encode());
            }

            return messageBytes.ToArray();
        }

        /// <summary>
        /// Get the text value of this SeString.
        /// </summary>
        /// <returns>The TextValue property.</returns>
        public override string ToString()
        {
            return this.TextValue;
        }
    }
}
