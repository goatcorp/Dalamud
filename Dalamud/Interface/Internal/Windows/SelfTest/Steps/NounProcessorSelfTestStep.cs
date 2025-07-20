using Dalamud.Bindings.ImGui;
using Dalamud.Game;
using Dalamud.Game.Text.Noun;
using Dalamud.Game.Text.Noun.Enums;

using LSheets = Lumina.Excel.Sheets;

namespace Dalamud.Interface.Internal.Windows.SelfTest.Steps;

/// <summary>
/// Test setup for NounProcessor.
/// </summary>
internal class NounProcessorSelfTestStep : ISelfTestStep
{
    private NounTestEntry[] tests =
    [
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.Japanese, 1, (int)JapaneseArticleType.NearListener, 1, "その蜂蜜酒の運び人"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.Japanese, 1, (int)JapaneseArticleType.Distant, 1, "蜂蜜酒の運び人"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.Japanese, 2, (int)JapaneseArticleType.NearListener, 1, "それらの蜂蜜酒の運び人"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.Japanese, 2, (int)JapaneseArticleType.Distant, 1, "あれらの蜂蜜酒の運び人"),

        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.English, 1, (int)EnglishArticleType.Indefinite, 1, "a mead-porting Midlander"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.English, 1, (int)EnglishArticleType.Definite, 1, "the mead-porting Midlander"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.English, 2, (int)EnglishArticleType.Indefinite, 1, "mead-porting Midlanders"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.English, 2, (int)EnglishArticleType.Definite, 1, "mead-porting Midlanders"),

        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.Indefinite, (int)GermanCases.Nominative, "ein Met schleppender Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.Indefinite, (int)GermanCases.Genitive, "eines Met schleppenden Wiesländers"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.Indefinite, (int)GermanCases.Dative, "einem Met schleppenden Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.Indefinite, (int)GermanCases.Accusative, "einen Met schleppenden Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.Definite, (int)GermanCases.Nominative, "der Met schleppender Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.Definite, (int)GermanCases.Genitive, "des Met schleppenden Wiesländers"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.Definite, (int)GermanCases.Dative, "dem Met schleppenden Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.Definite, (int)GermanCases.Accusative, "den Met schleppenden Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.Possessive, (int)GermanCases.Nominative, "dein Met schleppende Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.Possessive, (int)GermanCases.Genitive, "deines Met schleppenden Wiesländers"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.Possessive, (int)GermanCases.Dative, "deinem Met schleppenden Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.Possessive, (int)GermanCases.Accusative, "deinen Met schleppenden Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.Negative, (int)GermanCases.Nominative, "kein Met schleppender Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.Negative, (int)GermanCases.Genitive, "keines Met schleppenden Wiesländers"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.Negative, (int)GermanCases.Dative, "keinem Met schleppenden Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.Negative, (int)GermanCases.Accusative, "keinen Met schleppenden Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.ZeroArticle, (int)GermanCases.Nominative, "Met schleppender Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.ZeroArticle, (int)GermanCases.Genitive, "Met schleppenden Wiesländers"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.ZeroArticle, (int)GermanCases.Dative, "Met schleppendem Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.ZeroArticle, (int)GermanCases.Accusative, "Met schleppenden Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.Demonstrative, (int)GermanCases.Nominative, "dieser Met schleppende Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.Demonstrative, (int)GermanCases.Genitive, "dieses Met schleppenden Wiesländers"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.Demonstrative, (int)GermanCases.Dative, "diesem Met schleppenden Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 1, (int)GermanArticleType.Demonstrative, (int)GermanCases.Accusative, "diesen Met schleppenden Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.Indefinite, (int)GermanCases.Nominative, "2 Met schleppenden Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.Indefinite, (int)GermanCases.Genitive, "2 Met schleppenden Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.Indefinite, (int)GermanCases.Dative, "2 Met schleppenden Wiesländern"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.Indefinite, (int)GermanCases.Accusative, "2 Met schleppenden Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.Definite, (int)GermanCases.Nominative, "die Met schleppende Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.Definite, (int)GermanCases.Genitive, "der Met schleppender Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.Definite, (int)GermanCases.Dative, "den Met schleppenden Wiesländern"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.Definite, (int)GermanCases.Accusative, "die Met schleppende Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.Possessive, (int)GermanCases.Nominative, "deine Met schleppenden Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.Possessive, (int)GermanCases.Genitive, "deiner Met schleppenden Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.Possessive, (int)GermanCases.Dative, "deinen Met schleppenden Wiesländern"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.Possessive, (int)GermanCases.Accusative, "deine Met schleppenden Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.Negative, (int)GermanCases.Nominative, "keine Met schleppenden Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.Negative, (int)GermanCases.Genitive, "keiner Met schleppenden Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.Negative, (int)GermanCases.Dative, "keinen Met schleppenden Wiesländern"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.Negative, (int)GermanCases.Accusative, "keine Met schleppenden Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.ZeroArticle, (int)GermanCases.Nominative, "Met schleppende Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.ZeroArticle, (int)GermanCases.Genitive, "Met schleppender Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.ZeroArticle, (int)GermanCases.Dative, "Met schleppenden Wiesländern"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.ZeroArticle, (int)GermanCases.Accusative, "Met schleppende Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.Demonstrative, (int)GermanCases.Nominative, "diese Met schleppenden Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.Demonstrative, (int)GermanCases.Genitive, "dieser Met schleppenden Wiesländer"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.Demonstrative, (int)GermanCases.Dative, "diesen Met schleppenden Wiesländern"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.German, 2, (int)GermanArticleType.Demonstrative, (int)GermanCases.Accusative, "diese Met schleppenden Wiesländer"),

        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.French, 1, (int)FrenchArticleType.Indefinite, 1, "un livreur d'hydromel"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.French, 1, (int)FrenchArticleType.Definite, 1, "le livreur d'hydromel"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.French, 1, (int)FrenchArticleType.PossessiveFirstPerson, 1, "mon livreur d'hydromel"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.French, 1, (int)FrenchArticleType.PossessiveSecondPerson, 1, "ton livreur d'hydromel"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.French, 1, (int)FrenchArticleType.PossessiveThirdPerson, 1, "son livreur d'hydromel"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.French, 2, (int)FrenchArticleType.Indefinite, 1, "des livreurs d'hydromel"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.French, 2, (int)FrenchArticleType.Definite, 1, "les livreurs d'hydromel"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.French, 2, (int)FrenchArticleType.PossessiveFirstPerson, 1, "mes livreurs d'hydromel"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.French, 2, (int)FrenchArticleType.PossessiveSecondPerson, 1, "tes livreurs d'hydromel"),
        new(nameof(LSheets.BNpcName), 1330, ClientLanguage.French, 2, (int)FrenchArticleType.PossessiveThirdPerson, 1, "ses livreurs d'hydromel"),

        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.Japanese, 1, (int)JapaneseArticleType.NearListener, 1, "その酔いどれのネル"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.Japanese, 1, (int)JapaneseArticleType.Distant, 1, "酔いどれのネル"),

        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.English, 1, (int)EnglishArticleType.Indefinite, 1, "Nell Half-full"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.English, 1, (int)EnglishArticleType.Definite, 1, "Nell Half-full"),

        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.Indefinite, (int)GermanCases.Nominative, "Nell die Beschwipste"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.Indefinite, (int)GermanCases.Genitive, "Nell der Beschwipsten"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.Indefinite, (int)GermanCases.Dative, "Nell der Beschwipsten"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.Indefinite, (int)GermanCases.Accusative, "Nell die Beschwipste"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.Definite, (int)GermanCases.Nominative, "Nell die Beschwipste"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.Definite, (int)GermanCases.Genitive, "Nell der Beschwipsten"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.Definite, (int)GermanCases.Dative, "Nell der Beschwipsten"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.Definite, (int)GermanCases.Accusative, "Nell die Beschwipste"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.Possessive, (int)GermanCases.Nominative, "Nell die Beschwipste"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.Possessive, (int)GermanCases.Genitive, "Nell der Beschwipsten"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.Possessive, (int)GermanCases.Dative, "Nell der Beschwipsten"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.Possessive, (int)GermanCases.Accusative, "Nell die Beschwipste"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.Negative, (int)GermanCases.Nominative, "Nell die Beschwipste"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.Negative, (int)GermanCases.Genitive, "Nell der Beschwipsten"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.Negative, (int)GermanCases.Dative, "Nell der Beschwipsten"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.Negative, (int)GermanCases.Accusative, "Nell die Beschwipste"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.ZeroArticle, (int)GermanCases.Nominative, "Nell die Beschwipste"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.ZeroArticle, (int)GermanCases.Genitive, "Nell der Beschwipsten"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.ZeroArticle, (int)GermanCases.Dative, "Nell der Beschwipsten"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.ZeroArticle, (int)GermanCases.Accusative, "Nell die Beschwipste"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.Demonstrative, (int)GermanCases.Nominative, "Nell die Beschwipste"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.Demonstrative, (int)GermanCases.Genitive, "Nell der Beschwipsten"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.Demonstrative, (int)GermanCases.Dative, "Nell der Beschwipsten"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.German, 1, (int)GermanArticleType.Demonstrative, (int)GermanCases.Accusative, "Nell die Beschwipste"),

        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.French, 1, (int)FrenchArticleType.Indefinite, 1, "Nell la Boit-sans-soif"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.French, 1, (int)FrenchArticleType.Definite, 1, "Nell la Boit-sans-soif"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.French, 1, (int)FrenchArticleType.PossessiveFirstPerson, 1, "ma Nell la Boit-sans-soif"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.French, 1, (int)FrenchArticleType.PossessiveSecondPerson, 1, "ta Nell la Boit-sans-soif"),
        new(nameof(LSheets.ENpcResident), 1031947, ClientLanguage.French, 1, (int)FrenchArticleType.PossessiveThirdPerson, 1, "sa Nell la Boit-sans-soif"),

        new(nameof(LSheets.Item), 44348, ClientLanguage.Japanese, 1, (int)JapaneseArticleType.NearListener, 1, "その希少トームストーン:幻想"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.Japanese, 1, (int)JapaneseArticleType.Distant, 1, "希少トームストーン:幻想"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.Japanese, 2, (int)JapaneseArticleType.NearListener, 1, "それらの希少トームストーン:幻想"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.Japanese, 2, (int)JapaneseArticleType.Distant, 1, "あれらの希少トームストーン:幻想"),

        new(nameof(LSheets.Item), 44348, ClientLanguage.English, 1, (int)EnglishArticleType.Indefinite, 1, "an irregular tomestone of phantasmagoria"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.English, 1, (int)EnglishArticleType.Definite, 1, "the irregular tomestone of phantasmagoria"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.English, 2, (int)EnglishArticleType.Indefinite, 1, "irregular tomestones of phantasmagoria"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.English, 2, (int)EnglishArticleType.Definite, 1, "irregular tomestones of phantasmagoria"),

        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.Indefinite, (int)GermanCases.Nominative, "ein ungewöhnlicher Allagischer Stein der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.Indefinite, (int)GermanCases.Genitive, "eines ungewöhnlichen Allagischen Steins der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.Indefinite, (int)GermanCases.Dative, "einem ungewöhnlichen Allagischen Stein der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.Indefinite, (int)GermanCases.Accusative, "einen ungewöhnlichen Allagischen Stein der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.Definite, (int)GermanCases.Nominative, "der ungewöhnlicher Allagischer Stein der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.Definite, (int)GermanCases.Genitive, "des ungewöhnlichen Allagischen Steins der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.Definite, (int)GermanCases.Dative, "dem ungewöhnlichen Allagischen Stein der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.Definite, (int)GermanCases.Accusative, "den ungewöhnlichen Allagischen Stein der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.Possessive, (int)GermanCases.Nominative, "dein ungewöhnliche Allagische Stein der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.Possessive, (int)GermanCases.Genitive, "deines ungewöhnlichen Allagischen Steins der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.Possessive, (int)GermanCases.Dative, "deinem ungewöhnlichen Allagischen Stein der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.Possessive, (int)GermanCases.Accusative, "deinen ungewöhnlichen Allagischen Stein der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.Negative, (int)GermanCases.Nominative, "kein ungewöhnlicher Allagischer Stein der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.Negative, (int)GermanCases.Genitive, "keines ungewöhnlichen Allagischen Steins der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.Negative, (int)GermanCases.Dative, "keinem ungewöhnlichen Allagischen Stein der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.Negative, (int)GermanCases.Accusative, "keinen ungewöhnlichen Allagischen Stein der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.ZeroArticle, (int)GermanCases.Nominative, "ungewöhnlicher Allagischer Stein der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.ZeroArticle, (int)GermanCases.Genitive, "ungewöhnlichen Allagischen Steins der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.ZeroArticle, (int)GermanCases.Dative, "ungewöhnlichem Allagischem Stein der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.ZeroArticle, (int)GermanCases.Accusative, "ungewöhnlichen Allagischen Stein der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.Demonstrative, (int)GermanCases.Nominative, "dieser ungewöhnliche Allagische Stein der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.Demonstrative, (int)GermanCases.Genitive, "dieses ungewöhnlichen Allagischen Steins der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.Demonstrative, (int)GermanCases.Dative, "diesem ungewöhnlichen Allagischen Stein der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 1, (int)GermanArticleType.Demonstrative, (int)GermanCases.Accusative, "diesen ungewöhnlichen Allagischen Stein der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.Indefinite, (int)GermanCases.Nominative, "2 ungewöhnlichen Allagischen Steine der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.Indefinite, (int)GermanCases.Genitive, "2 ungewöhnlichen Allagischen Steine der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.Indefinite, (int)GermanCases.Dative, "2 ungewöhnlichen Allagischen Steinen der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.Indefinite, (int)GermanCases.Accusative, "2 ungewöhnlichen Allagischen Steine der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.Definite, (int)GermanCases.Nominative, "die ungewöhnliche Allagische Steine der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.Definite, (int)GermanCases.Genitive, "der ungewöhnlicher Allagischer Steine der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.Definite, (int)GermanCases.Dative, "den ungewöhnlichen Allagischen Steinen der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.Definite, (int)GermanCases.Accusative, "die ungewöhnliche Allagische Steine der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.Possessive, (int)GermanCases.Nominative, "deine ungewöhnlichen Allagischen Steine der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.Possessive, (int)GermanCases.Genitive, "deiner ungewöhnlichen Allagischen Steine der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.Possessive, (int)GermanCases.Dative, "deinen ungewöhnlichen Allagischen Steinen der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.Possessive, (int)GermanCases.Accusative, "deine ungewöhnlichen Allagischen Steine der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.Negative, (int)GermanCases.Nominative, "keine ungewöhnlichen Allagischen Steine der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.Negative, (int)GermanCases.Genitive, "keiner ungewöhnlichen Allagischen Steine der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.Negative, (int)GermanCases.Dative, "keinen ungewöhnlichen Allagischen Steinen der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.Negative, (int)GermanCases.Accusative, "keine ungewöhnlichen Allagischen Steine der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.ZeroArticle, (int)GermanCases.Nominative, "ungewöhnliche Allagische Steine der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.ZeroArticle, (int)GermanCases.Genitive, "ungewöhnlicher Allagischer Steine der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.ZeroArticle, (int)GermanCases.Dative, "ungewöhnlichen Allagischen Steinen der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.ZeroArticle, (int)GermanCases.Accusative, "ungewöhnliche Allagische Steine der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.Demonstrative, (int)GermanCases.Nominative, "diese ungewöhnlichen Allagischen Steine der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.Demonstrative, (int)GermanCases.Genitive, "dieser ungewöhnlichen Allagischen Steine der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.Demonstrative, (int)GermanCases.Dative, "diesen ungewöhnlichen Allagischen Steinen der Phantasmagorie"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.German, 2, (int)GermanArticleType.Demonstrative, (int)GermanCases.Accusative, "diese ungewöhnlichen Allagischen Steine der Phantasmagorie"),

        new(nameof(LSheets.Item), 44348, ClientLanguage.French, 1, (int)FrenchArticleType.Indefinite, 1, "un mémoquartz inhabituel fantasmagorique"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.French, 1, (int)FrenchArticleType.Definite, 1, "le mémoquartz inhabituel fantasmagorique"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.French, 1, (int)FrenchArticleType.PossessiveFirstPerson, 1, "mon mémoquartz inhabituel fantasmagorique"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.French, 1, (int)FrenchArticleType.PossessiveSecondPerson, 1, "ton mémoquartz inhabituel fantasmagorique"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.French, 1, (int)FrenchArticleType.PossessiveThirdPerson, 1, "son mémoquartz inhabituel fantasmagorique"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.French, 2, (int)FrenchArticleType.Indefinite, 1, "des mémoquartz inhabituels fantasmagoriques"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.French, 2, (int)FrenchArticleType.Definite, 1, "les mémoquartz inhabituels fantasmagoriques"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.French, 2, (int)FrenchArticleType.PossessiveFirstPerson, 1, "mes mémoquartz inhabituels fantasmagoriques"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.French, 2, (int)FrenchArticleType.PossessiveSecondPerson, 1, "tes mémoquartz inhabituels fantasmagoriques"),
        new(nameof(LSheets.Item), 44348, ClientLanguage.French, 2, (int)FrenchArticleType.PossessiveThirdPerson, 1, "ses mémoquartz inhabituels fantasmagoriques"),
    ];

    private enum GermanCases
    {
        Nominative,
        Genitive,
        Dative,
        Accusative,
    }

    /// <inheritdoc/>
    public string Name => "Test NounProcessor";

    /// <inheritdoc/>
    public unsafe SelfTestStepResult RunStep()
    {
        var nounProcessor = Service<NounProcessor>.Get();

        for (var i = 0; i < this.tests.Length; i++)
        {
            var e = this.tests[i];

            var nounParams = new NounParams()
            {
                SheetName = e.SheetName,
                RowId = e.RowId,
                Language = e.Language,
                Quantity = e.Quantity,
                ArticleType = e.ArticleType,
                GrammaticalCase = e.GrammaticalCase,
            };
            var output = nounProcessor.ProcessNoun(nounParams);

            if (e.ExpectedResult != output)
            {
                ImGui.TextUnformatted($"Mismatch detected (Test #{i}):");
                ImGui.TextUnformatted($"Got: {output}");
                ImGui.TextUnformatted($"Expected: {e.ExpectedResult}");

                if (ImGui.Button("Continue"u8))
                    return SelfTestStepResult.Fail;

                return SelfTestStepResult.Waiting;
            }
        }

        return SelfTestStepResult.Pass;
    }

    /// <inheritdoc/>
    public void CleanUp()
    {
        // ignored
    }

    private record struct NounTestEntry(
        string SheetName,
        uint RowId,
        ClientLanguage Language,
        int Quantity,
        int ArticleType,
        int GrammaticalCase,
        string ExpectedResult);
}
