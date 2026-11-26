using System.Globalization;

namespace EDA_Website.Localization;

/// <summary>
/// Translated text for C# code outside the views: <see cref="Data.CompanyData"/>, the content models and the
/// structured data. Views use the injected localizer instead: <c>@T["Home.Hero.Title"]</c>.
/// Texts come from Resources/Translations/{language}/*.json in the current request's language.
/// </summary>
public static class Translations
{
    private static TranslationStore? _store;

    internal static void Use(TranslationStore store) => _store = store;

    private static TranslationStore Store =>
        _store ?? throw new InvalidOperationException("Translations are not loaded: call app.UseSiteLocalization() in Program.cs.");

    /// <summary>The text in the current language; falls back to the default language, then to the key itself.</summary>
    public static string Get(string key) => Get(key, SiteLanguages.Current.Code);

    /// <summary>The text in a given language (e.g. the company e-mail, which is always in the default language).</summary>
    public static string Get(string key, string language) => Store.Find(key, language) ?? key;

    /// <summary>The text with {0}, {1}… filled in.</summary>
    public static string Format(string key, params object?[] args) =>
        string.Format(CultureInfo.CurrentCulture, Get(key), args);

    /// <summary>An optional text: null when no language defines it.</summary>
    public static string? Find(string key) => Store.Find(key, SiteLanguages.Current.Code, reportMissing: false);

    /// <summary>Number of items in a list ("Key": [ … ] in the JSON).</summary>
    public static int Count(string key) => Store.Count(key, SiteLanguages.Current.Code);

    /// <summary>A list of texts ("Key": [ "…", "…" ] in the JSON).</summary>
    public static IReadOnlyList<string> List(string key)
    {
        var language = SiteLanguages.Current.Code;
        var count = Store.Count(key, language);
        var items = new string[count];
        for (var i = 0; i < count; i++)
            items[i] = Get($"{key}.{i}", language);
        return items;
    }

    /// <summary>All texts under a prefix, keyed by the rest of their key (used to hand the JavaScript texts to the browser).</summary>
    public static IReadOnlyDictionary<string, string> Section(string prefix) => Store.Section(prefix, SiteLanguages.Current.Code);
}
