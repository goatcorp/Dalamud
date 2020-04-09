using System.Collections.Generic;
using Lumina.Data;

namespace Dalamud.Data.LuminaExtensions
{
    /// <summary>
    /// Extensions to <see cref="Language"/>.
    /// </summary>
    public static class LanguageExtensions
    {
        private static readonly Dictionary<Language, string> LangToCode = new Dictionary<Language, string>()
        {
            { Language.None, "" },
            { Language.Japanese, "ja" },
            { Language.English, "en" },
            { Language.German, "de" },
            { Language.French, "fr" },
            { Language.ChineseSimplified, "chs" },
            { Language.ChineseTraditional, "cht" },
            { Language.Korean, "ko" },
        };

        /// <summary>
        /// Return the language code for a <see cref="Language"/>.
        /// </summary>
        /// <param name="language">The Language.</param>
        /// <returns>The language code.</returns>
        public static string GetCode(this Language language)
        {
            return LangToCode[language];
        }
    }
}
