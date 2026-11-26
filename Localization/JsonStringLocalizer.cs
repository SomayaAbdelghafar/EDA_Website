using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Html;
using Microsoft.Extensions.Localization;

namespace EDA_Website.Localization;

/// <summary>
/// Marker for the site's one set of translations. Views get it as <c>T</c> (see _ViewImports.cshtml):
/// <c>@T["Shared.Nav.Home"]</c>, <c>@T["Contact.Form.Success", name]</c>.
/// </summary>
public sealed class SharedResource;

/// <summary>
/// ASP.NET Core's standard localization interface, backed by the JSON translation files instead of .resx files.
/// Because it is the framework's own <see cref="IStringLocalizerFactory"/>, the same texts also localize
/// validation messages and field names ([Required(ErrorMessage = "key")], [Display(Name = "key")]).
/// </summary>
public sealed class JsonStringLocalizerFactory(TranslationStore store) : IStringLocalizerFactory
{
    private readonly JsonStringLocalizer _localizer = new(store);

    // One shared set of keys: the resource type or view path makes no difference
    public IStringLocalizer Create(Type resourceSource) => _localizer;

    public IStringLocalizer Create(string baseName, string location) => _localizer;
}

public sealed class JsonStringLocalizer(TranslationStore store) : IStringLocalizer
{
    public LocalizedString this[string name]
    {
        get
        {
            var value = store.Find(name, SiteLanguages.Current.Code);
            // An unknown key shows the key itself, which makes it easy to spot on the page
            return new LocalizedString(name, value ?? name, resourceNotFound: value is null);
        }
    }

    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            var text = this[name];
            return new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, text.Value, arguments), text.ResourceNotFound);
        }
    }

    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) =>
        store.All(SiteLanguages.Current.Code).Select(pair => new LocalizedString(pair.Key, pair.Value));
}

public static partial class LocalizerExtensions
{
    /// <summary>
    /// A translation with HTML placed at its {0}, {1}… markers, e.g. a link or a styled word inside a sentence.
    /// The translated text itself is always HTML-encoded; arguments that are already HTML (IHtmlContent, such as
    /// Html.Raw or a Razor template) are inserted as they are, other arguments are encoded.
    /// <code>@T.Html("Privacy.ContactText", Html.Raw($"&lt;a href=\"mailto:{email}\"&gt;{email}&lt;/a&gt;"))</code>
    /// </summary>
    public static IHtmlContent Html(this IStringLocalizer localizer, string key, params object?[] args)
    {
        var format = localizer[key].Value;
        var content = new HtmlContentBuilder();
        var last = 0;
        foreach (Match marker in Placeholder().Matches(format))
        {
            content.Append(format[last..marker.Index]);
            var index = int.Parse(marker.Groups[1].Value, CultureInfo.InvariantCulture);
            var arg = index < args.Length ? args[index] : null;
            if (arg is IHtmlContent html)
                content.AppendHtml(html);
            else
                content.Append(Convert.ToString(arg, CultureInfo.CurrentCulture));
            last = marker.Index + marker.Length;
        }
        content.Append(format[last..]);
        return content;
    }

    [GeneratedRegex(@"\{(\d+)\}")]
    private static partial Regex Placeholder();
}
