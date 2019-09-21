using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Dalamud.Settings
{
    public class PersistentSettings {
        private static PersistentSettings _instance = null;
        
        private static readonly string ConfigPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "settings.json");

        public static PersistentSettings Instance {
            get {
                if (_instance == null) {
                    if (!File.Exists(ConfigPath)) {
                        _instance = new PersistentSettings();
                        return _instance;
                    }

                    _instance = JsonConvert.DeserializeObject<PersistentSettings>(File.ReadAllText(ConfigPath));
                }

                return _instance;
            }
        }

        public class FateInfo {
            public string Name { get; set; }
            public int Id { get; set; }
        }

        public List<FateInfo> Fates;

        public List<string> BadWords;

        public void Save() {
            File.WriteAllText(ConfigPath, JsonConvert.SerializeObject(this));
        }

        public static void Reset() {
            _instance = new PersistentSettings();
            Instance.Save();
        }
    }
}
