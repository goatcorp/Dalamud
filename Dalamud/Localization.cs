using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CheapLoc;
using Serilog;

namespace Dalamud
{ 
    class Localization {
        private readonly string workingDirectory;

        public static readonly string[] ApplicableLangCodes = { "de", "ja", "fr", "it", "es", "ko", "no", "ru" };

        public Localization(string workingDirectory) {
            this.workingDirectory = workingDirectory;
        }

        public void SetupWithUiCulture() {
            try
            {
                var currentUiLang = CultureInfo.CurrentUICulture;
                Log.Information("Trying to set up Loc for culture {0}", currentUiLang.TwoLetterISOLanguageName);

                if (ApplicableLangCodes.Any(x => currentUiLang.TwoLetterISOLanguageName == x)) {
                    SetupWithLangCode(currentUiLang.TwoLetterISOLanguageName);
                } else {
                    Loc.SetupWithFallbacks();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not get language information. Setting up fallbacks.");
                Loc.SetupWithFallbacks();
            }
        }

        public void SetupWithLangCode(string langCode) {
            if (langCode.ToLower() == "en") {
                Loc.SetupWithFallbacks();
                return;
            }

            Loc.Setup(File.ReadAllText(Path.Combine(this.workingDirectory, "UIRes", "loc", "dalamud", $"dalamud_{langCode}.json")));
        }
    }
}
