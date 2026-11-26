using Microsoft.AspNetCore.Diagnostics;
using EDA_Website.Localization;

namespace EDA_Website.Services;

/// <summary>
/// Absolute URLs for canonical links, Open Graph tags, structured data and the sitemap.
/// Set "Seo:SiteUrl" (for example https://www.example.com) once the live domain is known: every page then
/// points search engines to that one address, whatever host name or scheme a request arrived on.
/// Without it the current request's address is used, which is fine for local runs.
/// </summary>
public static class SiteUrls
{
    /// <summary>
    /// The current page in another language, for the language switcher: /en/projects?category=x ↔ /projects?category=x.
    /// The query string is kept, so a selected category or service stays selected.
    /// </summary>
    public static string InLanguage(HttpContext context, SiteLanguage language)
    {
        var request = context.Request;
        string pathBase = request.PathBase, path = request.Path, query = request.QueryString.Value ?? "";

        // Error and 404 pages are rendered under /status/… or /Home/Error: switch the address the visitor asked for
        if (context.Features.Get<IStatusCodeReExecuteFeature>() is { } statusPage)
            (pathBase, path, query) = (statusPage.OriginalPathBase, statusPage.OriginalPath, statusPage.OriginalQueryString ?? "");
        else if (context.Features.Get<IExceptionHandlerPathFeature>() is { } errorPage)
            (path, query) = (errorPage.Path, "");

        return pathBase + SiteLanguages.Localize(SiteLanguages.Split(path + query).Path, language);
    }

    /// <summary>The site's root address without a trailing slash.</summary>
    public static string Base(HttpRequest request, IConfiguration config)
    {
        var configured = config["Seo:SiteUrl"]?.Trim();
        return string.IsNullOrEmpty(configured)
            ? $"{request.Scheme}://{request.Host}{request.PathBase}".TrimEnd('/')
            : configured.TrimEnd('/');
    }

    /// <summary>Absolute URL for a site path such as "/about" or "/images/logo.png" (absolute URLs pass through).</summary>
    public static string Absolute(string baseUrl, string path) =>
        path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) || path.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            ? path
            : baseUrl + "/" + path.TrimStart('~').TrimStart('/');

    public static string Absolute(HttpRequest request, IConfiguration config, string path) =>
        Absolute(Base(request, config), path);

    /// <summary>
    /// The current page's path in its one indexable form: lower case, no trailing slash, no query string.
    /// So /About, /about/ and /contact?service=… all name the same page as /about and /contact.
    /// </summary>
    public static string CurrentPath(HttpRequest request)
    {
        var path = (request.Path.Value ?? "/").TrimEnd('/').ToLowerInvariant();
        return path.Length == 0 ? "/" : path;
    }
}
