using System.Globalization;
using Microsoft.AspNetCore.Localization;

namespace EDA_Website.Localization;

/// <summary>Settings from the "Localization" section of appsettings.json.</summary>
public sealed class SiteLanguageOptions
{
    public const string SectionName = "Localization";

    /// <summary>Language of the pages at the site root ("/about"). Every other language gets a URL prefix ("/en/about").</summary>
    public string DefaultLanguage { get; set; } = "ar";

    /// <summary>Published languages. Each needs a folder of translation files: Resources/Translations/{code}/.</summary>
    public List<string> Languages { get; set; } = [];

    /// <summary>Folder (relative to the content root) that holds one sub-folder of JSON files per language.</summary>
    public string TranslationsPath { get; set; } = "Resources/Translations";
}

/// <summary>One language the site is published in.</summary>
public sealed class SiteLanguage
{
    internal SiteLanguage(string code, bool isDefault)
    {
        Code = code;
        IsDefault = isDefault;
        Culture = CultureInfo.GetCultureInfo(code);
    }

    /// <summary>Language code ("ar", "en"): URL prefix, lang attribute, hreflang and translation folder name.</summary>
    public string Code { get; }

    public CultureInfo Culture { get; }

    /// <summary>The default language has no URL prefix: its pages live at the site root.</summary>
    public bool IsDefault { get; }

    /// <summary>Right-to-left script (Arabic, Hebrew, Persian, Urdu…), taken from the culture itself.</summary>
    public bool IsRtl => Culture.TextInfo.IsRightToLeft;

    /// <summary>Value for the dir attribute.</summary>
    public string Dir => IsRtl ? "rtl" : "ltr";

    /// <summary>"" for the default language, otherwise "/" + code.</summary>
    public string UrlPrefix => IsDefault ? "" : "/" + Code;

    /// <summary>The language's name written in that language ("العربية", "English"), for the language switcher.</summary>
    public string NativeName => Translations.Get("Shared.Language.Name", Code);

    /// <summary>Open Graph locale for link previews, e.g. ar_SA.</summary>
    public string OgLocale => Translations.Get("Shared.Language.OgLocale", Code);

    public override string ToString() => Code;
}

/// <summary>
/// The languages the site is published in and the language-prefixed addresses:
/// the default language lives at the root (/about), the others under their code (/en/about).
/// Configured once at startup from appsettings.json ("Localization").
/// </summary>
public static class SiteLanguages
{
    /// <summary>Route value that carries a non-default language: the "{culture}" in "/{culture}/about".</summary>
    public const string RouteKey = "culture";

    /// <summary>Cookie written by the language switcher (site.js) to remember the visitor's choice for future visits.</summary>
    public const string PreferenceCookie = "eda_lang";

    private static SiteLanguage[] _all = [new("ar", isDefault: true)];

    /// <summary>All published languages, the default one first.</summary>
    public static IReadOnlyList<SiteLanguage> All => _all;

    public static SiteLanguage Default => _all[0];

    internal static void Configure(SiteLanguageOptions options)
    {
        var defaultCode = options.DefaultLanguage.Trim().ToLowerInvariant();
        var others = options.Languages
            .Select(code => code.Trim().ToLowerInvariant())
            .Where(code => code.Length > 0 && code != defaultCode)
            .Distinct();
        _all = [new SiteLanguage(defaultCode, isDefault: true), .. others.Select(code => new SiteLanguage(code, isDefault: false))];
    }

    public static SiteLanguage? Find(string? code) =>
        string.IsNullOrEmpty(code) ? null : _all.FirstOrDefault(l => string.Equals(l.Code, code, StringComparison.OrdinalIgnoreCase));

    /// <summary>Language of the current request (set from the URL by the request localization middleware).</summary>
    public static SiteLanguage Current => FromCulture(CultureInfo.CurrentUICulture);

    public static SiteLanguage FromRequest(HttpContext? context) =>
        context?.Features.Get<IRequestCultureFeature>()?.RequestCulture.UICulture is { } culture ? FromCulture(culture) : Current;

    private static SiteLanguage FromCulture(CultureInfo culture)
    {
        // "en-GB" falls back to "en"; anything unknown is the default language
        for (var c = culture; !Equals(c, CultureInfo.InvariantCulture); c = c.Parent)
        {
            if (Find(c.Name) is { } language)
                return language;
        }
        return Default;
    }

    /// <summary>
    /// Splits an address into its language and the language-neutral path:
    /// "/en/about?x=1" → (English, "/about?x=1"); "/about" → (default language, "/about"); "/en" → (English, "/").
    /// </summary>
    public static (SiteLanguage Language, string Path) Split(string? pathAndQuery)
    {
        var path = string.IsNullOrEmpty(pathAndQuery) ? "/" : pathAndQuery;
        if (path[0] != '/')
            path = "/" + path;

        var end = path.IndexOfAny(['/', '?', '#'], 1);
        var segment = end < 0 ? path[1..] : path[1..end];
        if (Find(segment) is not { IsDefault: false } language)
            return (Default, path);

        var rest = end < 0 ? "" : path[end..];
        return (language, rest.StartsWith('/') ? rest : "/" + rest);
    }

    /// <summary>
    /// The address of a language-neutral path in a language: ("/about", English) → "/en/about",
    /// ("/", English) → "/en", ("/projects?category=x", English) → "/en/projects?category=x".
    /// </summary>
    public static string Localize(string path, SiteLanguage language)
    {
        if (string.IsNullOrEmpty(path))
            path = "/";
        if (language.IsDefault)
            return path;
        return path == "/" || path.StartsWith("/?") || path.StartsWith("/#")
            ? language.UrlPrefix + path[1..]
            : language.UrlPrefix + path;
    }
}
