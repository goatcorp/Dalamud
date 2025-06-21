using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

using CheapLoc;

using Serilog;

namespace Dalamud;

/// <summary>
/// Class handling localization.
/// </summary>
[ServiceManager.ProvidedService]
public class Localization : IServiceType
{
    /// <summary>
    /// Array of language codes which have a valid translation in Dalamud.
    /// </summary>
    public static readonly string[] ApplicableLangCodes = { "de", "ja", "fr", "it", "es", "ko", "no", "ru", "zh", "tw" };

    private const string FallbackLangCode = "en";

    private readonly string locResourceDirectory;
    private readonly string locResourcePrefix;
    private readonly bool useEmbedded;
    private readonly Assembly assembly;

    /// <summary>
    /// Initializes a new instance of the <see cref="Localization"/> class.
    /// </summary>
    /// <param name="locResourceDirectory">The working directory to load language files from.</param>
    /// <param name="locResourcePrefix">The prefix on the loc resource file name (e.g. dalamud_).</param>
    /// <param name="useEmbedded">Use embedded loc resource files.</param>
    public Localization(string locResourceDirectory, string locResourcePrefix = "", bool useEmbedded = false)
    {
        this.DalamudLanguageCultureInfo = CultureInfo.InvariantCulture;
        this.locResourceDirectory = locResourceDirectory;
        this.locResourcePrefix = locResourcePrefix;
        this.useEmbedded = useEmbedded;
        this.assembly = Assembly.GetCallingAssembly();
    }

    /// <summary>
    /// Delegate for the <see cref="Localization.LocalizationChanged"/> event that occurs when the language is changed.
    /// </summary>
    /// <param name="langCode">The language code of the new language.</param>
    public delegate void LocalizationChangedDelegate(string langCode);

    /// <summary>
    /// Event that occurs when the language is changed.
    /// </summary>
    public event LocalizationChangedDelegate? LocalizationChanged;

    /// <summary>
    /// Gets an instance of <see cref="CultureInfo"/> that corresponds to the language configured from Dalamud Settings.
    /// </summary>
    public CultureInfo DalamudLanguageCultureInfo { get; private set; }

    /// <summary>
    /// Gets an instance of <see cref="CultureInfo"/> that corresponds to a Dalamud <paramref name="langCode"/>.
    /// </summary>
    /// <param name="langCode">The language code which should be in <see cref="ApplicableLangCodes"/>.</param>
    /// <returns>The corresponding instance of <see cref="CultureInfo"/>.</returns>
    public static CultureInfo GetCultureInfoFromLangCode(string langCode) =>
        CultureInfo.GetCultureInfo(langCode switch
        {
            "tw" => "zh-hant",
            "zh" => "zh-hans",
            _ => langCode,
        });

    /// <summary>
    /// Search the set-up localization data for the provided assembly for the given string key and return it.
    /// If the key is not present, the fallback is shown.
    /// The fallback is also required to create the string files to be localized.
    /// </summary>
    /// <param name="key">The string key to be returned.</param>
    /// <param name="fallBack">The fallback string, usually your source language.</param>
    /// <returns>The localized string, fallback or string key if not found.</returns>
    // TODO: This breaks loc export, since it's being called without string args. Fix in CheapLoc.
    public static string Localize(string key, string fallBack)
    {
        return Loc.Localize(key, fallBack, Assembly.GetCallingAssembly());
    }

    /// <summary>
    /// Set up the UI language with the users' local UI culture.
    /// </summary>
    public void SetupWithUiCulture()
    {
        try
        {
            var currentUiLang = CultureInfo.CurrentUICulture;
            Log.Information("Trying to set up Loc for culture {0}", currentUiLang.TwoLetterISOLanguageName);

            if (ApplicableLangCodes.Any(langCode => currentUiLang.TwoLetterISOLanguageName == langCode))
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
        this.DalamudLanguageCultureInfo = CultureInfo.InvariantCulture;
        foreach (var d in Delegate.EnumerateInvocationList(this.LocalizationChanged))
        {
            try
            {
                d(FallbackLangCode);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception during raise of {handler}", d.Method);
            }
        }

        Loc.SetupWithFallbacks(this.assembly);
    }

    /// <summary>
    /// Set up the UI language with the provided language code.
    /// </summary>
    /// <param name="langCode">The language code to set up the UI language with.</param>
    public void SetupWithLangCode(string langCode)
    {
        if (langCode.Equals(FallbackLangCode, StringComparison.InvariantCultureIgnoreCase))
        {
            this.SetupWithFallbacks();
            return;
        }

        this.DalamudLanguageCultureInfo = GetCultureInfoFromLangCode(langCode);
        foreach (var d in Delegate.EnumerateInvocationList(this.LocalizationChanged))
        {
            try
            {
                d(langCode);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Exception during raise of {handler}", d.Method);
            }
        }

        try
        {
            Loc.Setup(this.ReadLocData(langCode), this.assembly);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Could not load loc {0}. Setting up fallbacks.", langCode);
            this.SetupWithFallbacks();
        }
    }

    /// <summary>
    /// Saves localizable JSON data in the current working directory for the provided assembly.
    /// </summary>
    /// <param name="ignoreInvalidFunctions">If set to true, this ignores malformed Localize functions instead of failing.</param>
    public void ExportLocalizable(bool ignoreInvalidFunctions = false)
    {
        Loc.ExportLocalizableForAssembly(this.assembly, ignoreInvalidFunctions);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="Localization"/> class.
    /// </summary>
    /// <param name="assetDirectory">Path to Dalamud assets.</param>
    /// <param name="languageOverride">Optional language override.</param>
    /// <returns>A new instance.</returns>
    internal static Localization FromAssets(string assetDirectory, string? languageOverride)
    {
        var t = new Localization(Path.Combine(assetDirectory, "UIRes", "loc", "dalamud"), "dalamud_");
        if (!string.IsNullOrEmpty(languageOverride))
            t.SetupWithLangCode(languageOverride);
        else
            t.SetupWithUiCulture();
        return t;
    }

    private string ReadLocData(string langCode)
    {
        if (this.useEmbedded)
        {
            var resourceStream = this.assembly.GetManifestResourceStream($"{this.locResourceDirectory}{this.locResourcePrefix}{langCode}.json");
            if (resourceStream == null)
                return null;

            using var reader = new StreamReader(resourceStream);
            return reader.ReadToEnd();
        }

        return File.ReadAllText(Path.Combine(this.locResourceDirectory, $"{this.locResourcePrefix}{langCode}.json"));
    }
}
