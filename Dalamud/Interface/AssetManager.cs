using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Serilog;

namespace Dalamud.Interface
{
    class AssetManager {
        private const string AssetStoreUrl = "https://goatcorp.github.io/DalamudAssets/";

        private static readonly Dictionary<string, string> AssetDictionary = new Dictionary<string, string> {
            {AssetStoreUrl + "UIRes/serveropcode.json", "UIRes/serveropcode.json" },
            {AssetStoreUrl + "UIRes/clientopcode.json", "UIRes/clientopcode.json" },
            {AssetStoreUrl + "UIRes/NotoSansCJKjp-Medium.otf", "UIRes/NotoSansCJKjp-Medium.otf" },
            {AssetStoreUrl + "UIRes/FontAwesome5FreeSolid.otf", "UIRes/FontAwesome5FreeSolid.otf" },
            {AssetStoreUrl + "UIRes/logo.png", "UIRes/logo.png" },
            {AssetStoreUrl + "UIRes/loc/dalamud/dalamud_de.json", "UIRes/loc/dalamud/dalamud_de.json" },
            {AssetStoreUrl + "UIRes/loc/dalamud/dalamud_es.json", "UIRes/loc/dalamud/dalamud_es.json" },
            {AssetStoreUrl + "UIRes/loc/dalamud/dalamud_fr.json", "UIRes/loc/dalamud/dalamud_fr.json" },
            {AssetStoreUrl + "UIRes/loc/dalamud/dalamud_it.json", "UIRes/loc/dalamud/dalamud_it.json" },
            {AssetStoreUrl + "UIRes/loc/dalamud/dalamud_ja.json", "UIRes/loc/dalamud/dalamud_ja.json" },
            {AssetStoreUrl + "UIRes/loc/dalamud/dalamud_ko.json", "UIRes/loc/dalamud/dalamud_ko.json" },
            {AssetStoreUrl + "UIRes/loc/dalamud/dalamud_no.json", "UIRes/loc/dalamud/dalamud_no.json" },
            {AssetStoreUrl + "UIRes/loc/dalamud/dalamud_ru.json", "UIRes/loc/dalamud/dalamud_ru.json" },
            {"https://img.finalfantasyxiv.com/lds/pc/global/fonts/FFXIV_Lodestone_SSF.ttf", "UIRes/gamesym.ttf" }
        };

        public static async Task<bool> EnsureAssets(string baseDir) {
            using var client = new WebClient();

            Log.Verbose("Starting asset download");

            foreach (var entry in AssetDictionary) {
                var filePath = Path.Combine(baseDir, entry.Value);

                Directory.CreateDirectory(Path.GetDirectoryName(filePath));

                if (!File.Exists(filePath)) {
                    Log.Verbose("Downloading {0} to {1}...", entry.Key, entry.Value);
                    try {
                        File.WriteAllBytes(filePath, client.DownloadData(entry.Key));
                    } catch (Exception ex) {
                        Log.Error(ex, "Could not download asset.");
                        return false;
                    }
                }
            }

            return true;
        }
    }
}
