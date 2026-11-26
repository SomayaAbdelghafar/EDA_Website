using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Html;
using EDA_Website.Data;
using EDA_Website.Localization;
using EDA_Website.Models;

namespace EDA_Website.Services;

/// <summary>
/// schema.org JSON-LD for search engines: who the business is (name, address, phones, map pin, registration
/// numbers), where each page sits in the site (breadcrumbs) and what each service page offers.
/// Only facts from <see cref="CompanyData"/> go in here: no ratings, prices or opening hours the company hasn't given.
/// </summary>
public static class StructuredData
{
    // Arabic and "+" (phone numbers) stay readable; every "<" is escaped below, so no text can close the <script> tag.
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string Serialize(object data) => JsonSerializer.Serialize(data, JsonOptions).Replace("<", "\\u003c");

    /// <summary>
    /// The whole &lt;script type="application/ld+json"&gt; element. It is written as raw HTML because Razor's
    /// script tag helper would otherwise encode the "+" in the type attribute as &amp;#x2B;.
    /// </summary>
    public static IHtmlContent ScriptTag(object data) =>
        new HtmlString($"<script type=\"application/ld+json\">{Serialize(data)}</script>");

    // Every text below is in the page's language (the same business, described in Arabic on Arabic pages and in
    // English on English pages); the @id stays the same, so search engines see one business.
    private static string BusinessId(string baseUrl) => baseUrl + "/#business";

    /// <summary>The home page of the current language, e.g. https://example.com/ or https://example.com/en.</summary>
    private static string HomeUrl(string baseUrl) => SiteUrls.Absolute(baseUrl, SiteLanguages.Localize("/", SiteLanguages.Current));

    private static string[] OtherNames() =>
        SiteLanguages.All.Where(l => l != SiteLanguages.Current).Select(CompanyData.NameIn).ToArray();

    /// <summary>The business and the website, rendered on every page.</summary>
    public static object SiteGraph(string baseUrl) => new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@graph"] = new object[] { Business(baseUrl), WebSite(baseUrl) }
    };

    private static Dictionary<string, object?> Business(string baseUrl) => new()
    {
        ["@type"] = "HVACBusiness",
        ["@id"] = BusinessId(baseUrl),
        ["name"] = CompanyData.Name,
        ["alternateName"] = OtherNames(),
        ["description"] = CompanyData.DefaultDescription,
        ["url"] = HomeUrl(baseUrl),
        ["logo"] = baseUrl + "/images/logo.png",
        ["image"] = baseUrl + "/images/site/hero.jpg",
        ["telephone"] = CompanyData.Phone1,
        ["email"] = CompanyData.Email,
        ["address"] = new Dictionary<string, object?>
        {
            ["@type"] = "PostalAddress",
            ["streetAddress"] = CompanyData.StreetAddress,
            ["addressLocality"] = CompanyData.City,
            ["addressRegion"] = CompanyData.Region,
            ["addressCountry"] = "SA"
        },
        ["geo"] = new Dictionary<string, object?>
        {
            ["@type"] = "GeoCoordinates",
            ["latitude"] = double.Parse(CompanyData.MapLatitude, CultureInfo.InvariantCulture),
            ["longitude"] = double.Parse(CompanyData.MapLongitude, CultureInfo.InvariantCulture)
        },
        ["hasMap"] = CompanyData.MapShareUrl,
        ["areaServed"] = AreaServed(),
        ["vatID"] = CompanyData.VatNumber,
        ["identifier"] = new Dictionary<string, object?>
        {
            ["@type"] = "PropertyValue",
            ["propertyID"] = Translations.Get("About.Licenses.CrNumber"),
            ["value"] = CompanyData.CrNumber
        },
        ["contactPoint"] = new[] { CompanyData.Phone1, CompanyData.Phone2 }.Select(phone => new Dictionary<string, object?>
        {
            ["@type"] = "ContactPoint",
            ["telephone"] = phone,
            ["contactType"] = "customer service",
            ["areaServed"] = "SA",
            ["availableLanguage"] = "ar"
        }).ToArray(),
        ["hasOfferCatalog"] = new Dictionary<string, object?>
        {
            ["@type"] = "OfferCatalog",
            ["name"] = Translations.Get("Shared.Seo.OfferCatalog"),
            ["itemListElement"] = CompanyData.Services.Select(s => new Dictionary<string, object?>
            {
                ["@type"] = "Offer",
                ["itemOffered"] = new Dictionary<string, object?>
                {
                    ["@type"] = "Service",
                    ["name"] = s.Title,
                    ["url"] = ServiceUrl(s, baseUrl)
                }
            }).ToArray()
        }
    };

    private static Dictionary<string, object?> WebSite(string baseUrl) => new()
    {
        ["@type"] = "WebSite",
        ["@id"] = baseUrl + "/#website",
        ["url"] = HomeUrl(baseUrl),
        ["name"] = CompanyData.Name,
        ["alternateName"] = new[] { CompanyData.ShortName }.Concat(OtherNames()).ToArray(),
        ["inLanguage"] = SiteLanguages.All.Select(l => l.Code).ToArray(),
        ["publisher"] = new Dictionary<string, object?> { ["@id"] = BusinessId(baseUrl) }
    };

    private static string ServiceUrl(Service service, string baseUrl) =>
        SiteUrls.Absolute(baseUrl, SiteLanguages.Localize($"/services/{service.Slug}", SiteLanguages.Current));

    /// <summary>Cities with delivered projects, plus the country the company says it serves.</summary>
    private static object[] AreaServed() =>
        CompanyData.CitiesServed
            .Select(city => (object)new Dictionary<string, object?> { ["@type"] = "City", ["name"] = city })
            .Append(new Dictionary<string, object?> { ["@type"] = "Country", ["name"] = CompanyData.Country })
            .ToArray();

    /// <summary>Breadcrumb trail matching the one shown in the page banner. The current page has no URL.</summary>
    public static object Breadcrumbs(IReadOnlyList<(string Name, string? Url)> items) => new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "BreadcrumbList",
        ["itemListElement"] = items.Select((item, i) =>
        {
            var element = new Dictionary<string, object?> { ["@type"] = "ListItem", ["position"] = i + 1, ["name"] = item.Name };
            if (item.Url is not null)
                element["item"] = item.Url; // the last crumb (the current page) has no link
            return element;
        }).ToArray()
    };

    /// <summary>A service page: what is offered, by whom, and where.</summary>
    public static object Service(Service service, string baseUrl) => new Dictionary<string, object?>
    {
        ["@context"] = "https://schema.org",
        ["@type"] = "Service",
        ["name"] = service.Heading,
        ["serviceType"] = service.Title,
        ["description"] = service.MetaDescription,
        ["url"] = ServiceUrl(service, baseUrl),
        ["inLanguage"] = SiteLanguages.Current.Code,
        ["image"] = SiteUrls.Absolute(baseUrl, service.Image),
        ["provider"] = new Dictionary<string, object?>
        {
            ["@type"] = "HVACBusiness",
            ["@id"] = BusinessId(baseUrl),
            ["name"] = CompanyData.Name,
            ["url"] = HomeUrl(baseUrl)
        },
        ["areaServed"] = AreaServed()
    };
}
