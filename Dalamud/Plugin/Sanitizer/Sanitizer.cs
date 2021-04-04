using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Dalamud.Plugin.Sanitizer
{
    /// <summary>
    /// Sanitize strings to remove soft hyphens and other special characters.
    /// </summary>
    public class Sanitizer : ISanitizer
    {
        private readonly KeyValuePair<string, string> softHyphen = new KeyValuePair<string, string>("\u0002\u0016\u0001\u0003", string.Empty);
        private readonly KeyValuePair<string, string> emphasisOpen = new KeyValuePair<string, string>("\u0002\u001A\u0003", string.Empty);
        private readonly KeyValuePair<string, string> emphasisClose = new KeyValuePair<string, string>("\u0002\u001A\u0001\u0003", string.Empty);
        private readonly KeyValuePair<string, string> indent = new KeyValuePair<string, string>("\u0002\u001D\u0001\u0003", string.Empty);
        private readonly KeyValuePair<string, string> dagger = new KeyValuePair<string, string>("\u0020\u2020", string.Empty);
        private readonly KeyValuePair<string, string> ligatureOE = new KeyValuePair<string, string>("\u0153", "\u006F\u0065");
        private readonly List<KeyValuePair<string, string>> defaultSanitizationList;

        /// <summary>
        /// Initializes a new instance of the <see cref="Sanitizer"/> class.
        /// </summary>
        /// <param name="clientLanguage">Default clientLanguage for sanitizing strings.</param>
        public Sanitizer(ClientLanguage clientLanguage)
        {
            this.defaultSanitizationList = this.BuildSanitizationList(clientLanguage);
        }

        /// <summary>
        /// Creates a sanitized string using current clientLanguage.
        /// </summary>
        /// <param name="unsanitizedString">An unsanitized string to sanitize.</param>
        /// <returns>A sanitized string.</returns>
        public string Sanitize(string unsanitizedString)
        {
            return this.defaultSanitizationList == null ? unsanitizedString : ApplySanitizationList(unsanitizedString, this.defaultSanitizationList);
        }

        /// <summary>
        /// Creates a sanitized string using request clientLanguage.
        /// </summary>
        /// <param name="unsanitizedString">An unsanitized string to sanitize.</param>
        /// <param name="clientLanguage">Target language for sanitized strings.</param>
        /// <returns>A sanitized string.</returns>
        public string Sanitize(string unsanitizedString, ClientLanguage clientLanguage)
        {
            var newSanitizationList = this.BuildSanitizationList(clientLanguage);
            return newSanitizationList == null ? unsanitizedString : ApplySanitizationList(unsanitizedString, newSanitizationList);
        }

        /// <summary>
        /// Creates a list of sanitized strings using current clientLanguage.
        /// </summary>
        /// <param name="unsanitizedStrings">List of unsanitized string to sanitize.</param>
        /// <returns>A list of sanitized strings.</returns>
        public IEnumerable<string> Sanitize(IEnumerable<string> unsanitizedStrings)
        {
            return this.defaultSanitizationList == null ? unsanitizedStrings.Select(unsanitizedString => unsanitizedString) :
                       unsanitizedStrings.Select(unsanitizedString => ApplySanitizationList(unsanitizedString, this.defaultSanitizationList));
        }

        /// <summary>
        /// Creates a list of sanitized strings using requested clientLanguage.
        /// </summary>
        /// <param name="unsanitizedStrings">List of unsanitized string to sanitize.</param>
        /// <param name="clientLanguage">Target language for sanitized strings.</param>
        /// <returns>A list of sanitized strings.</returns>
        public IEnumerable<string> Sanitize(IEnumerable<string> unsanitizedStrings, ClientLanguage clientLanguage)
        {
            var newSanitizationList = this.BuildSanitizationList(clientLanguage);
            return newSanitizationList == null ? unsanitizedStrings.Select(unsanitizedString => unsanitizedString) :
                       unsanitizedStrings.Select(unsanitizedString => ApplySanitizationList(unsanitizedString, newSanitizationList));
        }

        private static string ApplySanitizationList(string unsanitizedString, IEnumerable<KeyValuePair<string, string>> sanitizationList)
        {
            var sanitizedValue = new StringBuilder(unsanitizedString);
            foreach (var item in sanitizationList) sanitizedValue.Replace(item.Key, item.Value);
            return sanitizedValue.ToString();
        }

        private List<KeyValuePair<string, string>> BuildSanitizationList(ClientLanguage clientLanguage)
        {
            switch (clientLanguage)
            {
                case ClientLanguage.Japanese:
                    break;
                case ClientLanguage.English:
                    break;
                case ClientLanguage.German:
                    return new List<KeyValuePair<string, string>>
                    {
                        this.softHyphen,
                        this.emphasisOpen,
                        this.emphasisClose,
                        this.dagger,
                    };
                case ClientLanguage.French:
                    return new List<KeyValuePair<string, string>>
                    {
                        this.softHyphen,
                        this.indent,
                        this.ligatureOE,
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return null;
        }
    }
}
