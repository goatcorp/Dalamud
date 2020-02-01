using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Dalamud.Data
{
    /// <summary>
    /// This class provides data for Dalamud-internal features, but can also be used by plugins if needed.
    /// </summary>
    public class DataManager {
        private const string DataBaseUrl = "https://goaaats.github.io/ffxiv/tools/launcher/addons/Hooks/Data/";

        public ReadOnlyDictionary<string, ushort> ServerOpCodes;
        public ReadOnlyDictionary<uint, JObject> ContentFinderCondition;

        public bool IsDataReady { get; private set; }

        public DataManager() {
            // Set up default values so plugins do not null-reference when data is being loaded.
            this.ServerOpCodes = new ReadOnlyDictionary<string, ushort>(new Dictionary<string, ushort>());
            this.ContentFinderCondition = new ReadOnlyDictionary<uint, JObject>(new Dictionary<uint, JObject>());
        }

        public async Task Initialize() {
            try {
                Log.Verbose("Starting data download...");

                // This is due to GitHub not supporting TLS 1.0
                System.Net.ServicePointManager.SecurityProtocol =
                    SecurityProtocolType.Tls11 | SecurityProtocolType.Tls12;

                using var client = new HttpClient() {
                    BaseAddress = new Uri(DataBaseUrl)
                };

                var opCodeDict =
                    JsonConvert.DeserializeObject<Dictionary<string, ushort>>(
                        await client.GetStringAsync(DataBaseUrl + "serveropcode.json"));
                this.ServerOpCodes = new ReadOnlyDictionary<string, ushort>(opCodeDict);

                Log.Verbose("Loaded {0} ServerOpCodes.", opCodeDict.Count);

                var cfcs = JsonConvert.DeserializeObject<Dictionary<uint, JObject>>(
                    await client.GetStringAsync(DataBaseUrl + "contentfindercondition.json"));
                this.ContentFinderCondition = new ReadOnlyDictionary<uint, JObject>(cfcs);

                Log.Verbose("Loaded {0} ContentFinderCondition.", cfcs.Count);

                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;

                IsDataReady = true;
            } catch (Exception ex) {
                Log.Error(ex, "Could not download data.");
            }
        }
    }
}
