using System.Collections.Generic;
using System.Linq;
using Xunit;

// ReSharper disable StringLiteralTypo
namespace Dalamud.Test.Game.Text.Sanitizer
{
    public class SanitizerTests
    {
        private global::Dalamud.Game.Text.Sanitizer.Sanitizer sanitizer;

        [Theory]
        [InlineData(ClientLanguage.English, "Pixie Cotton Hood of Healing", "Pixie Cotton Hood of Healing")]
        [InlineData(ClientLanguage.Japanese, "アラガントームストーン:真理", "アラガントームストーン:真理")]
        [InlineData(ClientLanguage.German, "Anemos-Pan\x02\x16\x01\x03zer\x02\x16\x01\x03hand\x02\x16\x01\x03schu\x02\x16\x01\x03he des Drachenbluts", "Anemos-Panzerhandschuhe des Drachenbluts")]
        [InlineData(ClientLanguage.German, "Bienen-Spatha †", "Bienen-Spatha")]
        [InlineData(ClientLanguage.French, "Le Diademe\x02\x1D\x01\x03: terrains de chasse|Le Diademe\x02\x1D\x01\x03: terrains de chasse", "Le Diademe: terrains de chasse|Le Diademe: terrains de chasse")]
        [InlineData(ClientLanguage.French, "Cuir de bœuf", "Cuir de boeuf")]
        [InlineData(ClientLanguage.Korean, "요정 무명 치유사 두건", "요정 무명 치유사 두건")]
        public void StringsAreSanitizedCorrectly(ClientLanguage clientLanguage, string unsanitizedString, string sanitizedString)
        {
            var sanitizedStrings = new List<string> { unsanitizedString };

            sanitizer = new global::Dalamud.Game.Text.Sanitizer.Sanitizer(clientLanguage);
            Assert.Equal(sanitizedString, sanitizer.Sanitize(unsanitizedString));
            Assert.Equal(sanitizedString, sanitizer.Sanitize(sanitizedStrings).First());

            sanitizer = new global::Dalamud.Game.Text.Sanitizer.Sanitizer(ClientLanguage.English);
            Assert.Equal(sanitizedString, sanitizer.Sanitize(unsanitizedString, clientLanguage));
            Assert.Equal(sanitizedString, sanitizer.Sanitize(sanitizedStrings, clientLanguage).First());
        }
    }
}
