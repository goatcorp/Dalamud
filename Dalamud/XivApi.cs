using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CSharp.RuntimeBinder;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Dalamud
{
    class XivApi
    {
        private const string URL = "http://xivapi.com/";

        private static readonly Dictionary<string, JObject> cachedResponses = new Dictionary<string, JObject>();

        public static async Task<JObject> GetWorld(int world)
        {
            var res = await Get("World/" + world);

            return res;
        }

        public static async Task<JObject> GetClassJob(int id)
        {
            var res = await Get("ClassJob/" + id);

            return res;
        }

        public static async Task<JObject> GetFate(int id)
        {
            var res = await Get("Fate/" + id);

            return res;
        }

        public static async Task<JObject> GetCharacterSearch(string name, string world)
        {
            var res = await Get("character/search" + $"?name={name}&server={world}");

            return res;
        }

        public static async Task<JObject> GetContentFinderCondition(int contentFinderCondition) {
            return await Get("ContentFinderCondition/" + contentFinderCondition);
        }

        public static async Task<JObject> Search(string query, string indexes, int limit = 100) {
            query = System.Net.WebUtility.UrlEncode(query);

            return await Get("search" + $"?string={query}&indexes={indexes}&limit={limit}");
        }

        public static async Task<JObject> GetMarketInfoWorld(int itemId, string worldName) {
            return await Get($"market/{worldName}/item/{itemId}", true);
        }

        public static async Task<JObject> GetMarketInfoDc(int itemId, string dcName) {
            return await Get($"market/item/{itemId}?dc={dcName}", true);
        }

        public static async Task<JObject> GetItem(int itemId) {
            return await Get($"Item/{itemId}", true);
        }

        public static async Task<dynamic> Get(string endpoint, bool noCache = false)
        {
            Log.Verbose("XIVAPI FETCH: {0}", endpoint);

            if (cachedResponses.ContainsKey(endpoint) && !noCache)
                return cachedResponses[endpoint];

            var client = new HttpClient();
            var response = await client.GetAsync(URL + endpoint);
            var result = await response.Content.ReadAsStringAsync();

            var obj = JObject.Parse(result);

            if (!noCache)
                cachedResponses.Add(endpoint, obj);

            return obj;
        }
    }
}
