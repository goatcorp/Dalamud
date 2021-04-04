// ReSharper disable StringLiteralTypo

using System.Linq;
using Xunit;

namespace Dalamud.Test.Plugin.Sanitizer {

    public class SanitizerTests {
        global::Dalamud.Plugin.Sanitizer.Sanitizer sanitizer;

        [Fact]
        public void Sanitize_NormalString_NoChange() {
            sanitizer = new global::Dalamud.Plugin.Sanitizer.Sanitizer(ClientLanguage.English);
            const string str = "Pixie Cotton Hood of Healing";
            var sanitizedString = sanitizer.Sanitize(str);
            Assert.Equal(str, sanitizedString);
        }

        [Fact]
        public void Sanitize_DESpecialCharacters_Sanitized() {
            sanitizer = new global::Dalamud.Plugin.Sanitizer.Sanitizer(ClientLanguage.German);
            const string str = @"Anemos-Panzerhandschuhe des Drachenbluts";
            var sanitizedString = sanitizer.Sanitize(str);
            Assert.Equal(@"Anemos-Panzerhandschuhe des Drachenbluts", sanitizedString);
        }

        [Fact]
        public void Sanitize_FRSpecialCharacters_Sanitized() {
            sanitizer = new global::Dalamud.Plugin.Sanitizer.Sanitizer(ClientLanguage.French);
            const string str = @"Le Diademe: terrains de chasse|Le Diademe: terrains de chasse";
            var sanitizedString = sanitizer.Sanitize(str);
            Assert.Equal(@"Le Diademe: terrains de chasse|Le Diademe: terrains de chasse", sanitizedString);
        }

        [Fact]
        public void Sanitize_SpecifyLanguage_Sanitized() {
            sanitizer = new global::Dalamud.Plugin.Sanitizer.Sanitizer(ClientLanguage.French);
            const string str = @"Le Diademe: terrains de chasse|Le Diademe: terrains de chasse";
            var sanitizedString = sanitizer.Sanitize(str, ClientLanguage.French);
            Assert.Equal(@"Le Diademe: terrains de chasse|Le Diademe: terrains de chasse", sanitizedString);
        }

        [Fact]
        public void Sanitize_List_Sanitized() {
            sanitizer = new global::Dalamud.Plugin.Sanitizer.Sanitizer(ClientLanguage.German);
            const string str = @"Anemos-Panzerhandschuhe des Drachenbluts";
            var sanitizedStrings = sanitizer.Sanitize(new[] {str});
            Assert.Equal(@"Anemos-Panzerhandschuhe des Drachenbluts", sanitizedStrings.First());
        }

        [Fact]
        public void Sanitize_SpecifyLanguageList_Sanitized() {
            sanitizer = new global::Dalamud.Plugin.Sanitizer.Sanitizer(ClientLanguage.German);
            const string str = @"Anemos-Panzerhandschuhe des Drachenbluts";
            var sanitizedStrings = sanitizer.Sanitize(new[] {str}, ClientLanguage.German);
            Assert.Equal(@"Anemos-Panzerhandschuhe des Drachenbluts", sanitizedStrings.First());
        }
        
        [Fact]
        public void Sanitize_UseAlternateLanguage_Sanitized() {
            sanitizer = new global::Dalamud.Plugin.Sanitizer.Sanitizer(ClientLanguage.English);
            const string str = @"Anemos-Panzerhandschuhe des Drachenbluts";
            var sanitizedStrings = sanitizer.Sanitize(new[] {str}, ClientLanguage.German);
            Assert.Equal(@"Anemos-Panzerhandschuhe des Drachenbluts", sanitizedStrings.First());
        }
    }
}
