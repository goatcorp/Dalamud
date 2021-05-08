using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading.Tasks;

using Newtonsoft.Json.Linq;
using Serilog;

namespace Dalamud
{
    internal class XivApi
    {
        private const string URL = "https://xivapi.com/";

        private static readonly ConcurrentDictionary<string, JObject> cachedResponses = new();

        [Obsolete("This class will not be supported anymore in the future. Please migrate to your own version.", true)]
        public static async Task<JObject> GetWorld(int world)
        {
            var res = await Get("World/" + world);

            return res;
        }

        [Obsolete("This class will not be supported anymore in the future. Please migrate to your own version.", true)]
        public static async Task<JObject> GetClassJob(int id)
        {
            var res = await Get("ClassJob/" + id);

            return res;
        }

        [Obsolete("This class will not be supported anymore in the future. Please migrate to your own version.", true)]
        public static async Task<JObject> GetFate(int id)
        {
            var res = await Get("Fate/" + id);

            return res;
        }

        [Obsolete("This class will not be supported anymore in the future. Please migrate to your own version.", true)]
        public static async Task<JObject> GetCharacterSearch(string name, string world)
        {
            var res = await Get("character/search" + $"?name={name}&server={world}");

            return res;
        }

        [Obsolete("This class will not be supported anymore in the future. Please migrate to your own version.", true)]
        public static async Task<JObject> GetContentFinderCondition(int contentFinderCondition)
        {
            return await Get("ContentFinderCondition/" + contentFinderCondition);
        }

        [Obsolete("This class will not be supported anymore in the future. Please migrate to your own version.", true)]
        public static async Task<JObject> Search(string query, string indexes, int limit = 100, bool exact = false)
        {
            query = System.Net.WebUtility.UrlEncode(query);

            var queryString = $"?string={query}&indexes={indexes}&limit={limit}";
            if (exact)
            {
                queryString += "&string_algo=match";
            }

            return await Get("search" + queryString);
        }

        [Obsolete("This class will not be supported anymore in the future. Please migrate to your own version.", true)]
        public static async Task<JObject> GetMarketInfoWorld(int itemId, string worldName)
        {
            return await Get($"market/{worldName}/item/{itemId}", true);
        }

        [Obsolete("This class will not be supported anymore in the future. Please migrate to your own version.", true)]
        public static async Task<JObject> GetMarketInfoDc(int itemId, string dcName)
        {
            return await Get($"market/item/{itemId}?dc={dcName}", true);
        }

        [Obsolete("This class will not be supported anymore in the future. Please migrate to your own version.", true)]
        public static async Task<JObject> GetItem(uint itemId)
        {
            return await Get($"Item/{itemId}", true);
        }

        [Obsolete("This class will not be supported anymore in the future. Please migrate to your own version.", true)]
        public static async Task<dynamic> Get(string endpoint, bool noCache = false)
        {
            Log.Verbose("XIVAPI FETCH: {0}", endpoint);

            if (cachedResponses.TryGetValue(endpoint, out var val) && !noCache)
                return val;

            var client = new HttpClient();
            var response = await client.GetAsync(URL + endpoint);
            var result = await response.Content.ReadAsStringAsync();

            var obj = JObject.Parse(result);

            if (!noCache)
                cachedResponses.TryAdd(endpoint, obj);

            return obj;
        }
    }
}
