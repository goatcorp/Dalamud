using System;
using System.Globalization;
using System.IO;
using System.Linq;

using CheapLoc;
using Serilog;

namespace Dalamud
{
    /// <summary>
    /// Class handling localization.
    /// </summary>
    public class Localization
    {
        /// <summary>
        /// Array of language codes which have a valid translation in Dalamud.
        /// </summary>
        public static readonly string[] ApplicableLangCodes = { "de", "ja", "fr", "it", "es", "ko", "no", "ru" };

        private const string FallbackLangCode = "en";

        private readonly string workingDirectory;

        /// <summary>
        /// Initializes a new instance of the <see cref="Localization"/> class.
        /// </summary>
        /// <param name="workingDirectory">The working directory to load language files from.</param>
        public Localization(string workingDirectory)
        {
            this.workingDirectory = workingDirectory;
        }

        /// <summary>
        /// Delegate for the <see cref="Localization.OnLocalizationChanged"/> event that occurs when the language is changed.
        /// </summary>
        /// <param name="langCode">The language code of the new language.</param>
        public delegate void LocalizationChangedDelegate(string langCode);

        /// <summary>
        /// Event that occurs when the language is changed.
        /// </summary>
        public event LocalizationChangedDelegate OnLocalizationChanged;

        /// <summary>
        /// Set up the UI language with the users' local UI culture.
        /// </summary>
        public void SetupWithUiCulture()
        {
            try
            {
                var currentUiLang = CultureInfo.CurrentUICulture;
                Log.Information("Trying to set up Loc for culture {0}", currentUiLang.TwoLetterISOLanguageName);

                if (ApplicableLangCodes.Any(x => currentUiLang.TwoLetterISOLanguageName == x))
                {
                    this.SetupWithLangCode(currentUiLang.TwoLetterISOLanguageName);
                }
                else
                {
                    this.SetupWithFallbacks();
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Could not get language information. Setting up fallbacks.");
                this.SetupWithFallbacks();
            }
        }

        /// <summary>
        /// Set up the UI language with "fallbacks"(original English text).
        /// </summary>
        public void SetupWithFallbacks()
        {
            this.OnLocalizationChanged?.Invoke(FallbackLangCode);
            Loc.SetupWithFallbacks();
        }

        /// <summary>
        /// Set up the UI language with the provided language code.
        /// </summary>
        /// <param name="langCode">The language code to set up the UI language with.</param>
        public void SetupWithLangCode(string langCode)
        {
            if (langCode.ToLower() == FallbackLangCode)
            {
                this.SetupWithFallbacks();
                return;
            }

            this.OnLocalizationChanged?.Invoke(langCode);
            Loc.Setup(File.ReadAllText(Path.Combine(this.workingDirectory, "UIRes", "loc", "dalamud", $"dalamud_{langCode}.json")));
        }
    }
}
