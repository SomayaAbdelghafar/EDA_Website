using System.Text;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using EDA_Website.Data;
using EDA_Website.Localization;
using EDA_Website.Models;
using EDA_Website.Services;

namespace EDA_Website.Controllers;

/// <summary>
/// robots.txt and sitemap.xml for search engines. The sitemap is built from <see cref="CompanyData"/>,
/// so a new service or project is listed automatically, in every language. Submit /sitemap.xml in Google Search Console.
/// One address for all languages: no /en/sitemap.xml.
/// </summary>
[LanguageNeutral]
public class SeoController(IConfiguration config) : Controller
{
    private static readonly XNamespace SitemapNs = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private static readonly XNamespace ImageNs = "http://www.google.com/schemas/sitemap-image/1.1";
    private static readonly XNamespace XhtmlNs = "http://www.w3.org/1999/xhtml";

    [HttpGet("robots.txt")]
    public IActionResult Robots()
    {
        var text = new StringBuilder();
        text.AppendLine("User-agent: *");
        // A preview copy must stay out of search results; the live site is open to all crawlers
        text.AppendLine(config.GetValue<bool>("StaticPreview") ? "Disallow: /" : "Allow: /");
        text.AppendLine();
        text.Append("Sitemap: ").AppendLine(SiteUrls.Absolute(Request, config, "/sitemap.xml"));
        return Content(text.ToString(), "text/plain", Encoding.UTF8);
    }

    [HttpGet("sitemap.xml")]
    public IActionResult Sitemap()
    {
        var baseUrl = SiteUrls.Base(Request, config);

        string Address(string path, SiteLanguage language) => SiteUrls.Absolute(baseUrl, SiteLanguages.Localize(path, language));

        // One <url> per indexable page and language. Each lists all its language versions (hreflang), so search
        // engines pair /about with /en/about; photos are listed too so they can appear in Google Images.
        IEnumerable<XElement> Page(string path, IEnumerable<string>? images = null) =>
            SiteLanguages.All.Select(language => new XElement(SitemapNs + "url",
                new XElement(SitemapNs + "loc", Address(path, language)),
                SiteLanguages.All.Select(alternate => Alternate(alternate.Code, Address(path, alternate))),
                Alternate("x-default", Address(path, SiteLanguages.Default)),
                images?.Select(src => new XElement(ImageNs + "image",
                    new XElement(ImageNs + "loc", SiteUrls.Absolute(baseUrl, src))))));

        var pages = new List<XElement>();
        pages.AddRange(Page("/", ["/images/site/hero.jpg"]));
        pages.AddRange(Page("/services"));
        pages.AddRange(CompanyData.Services.SelectMany(s => Page($"/services/{s.Slug}", [s.Image])));
        pages.AddRange(Page("/projects"));
        pages.AddRange(Enum.GetValues<ProjectCategory>().SelectMany(c => Page($"/projects?category={CompanyData.CategorySlug(c)}")));
        pages.AddRange(CompanyData.Projects.SelectMany(p => Page($"/projects/{p.Slug}", p.ImageUrls)));
        pages.AddRange(Page("/about"));
        pages.AddRange(Page("/contact"));
        pages.AddRange(Page("/privacy"));

        var sitemap = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(SitemapNs + "urlset",
                new XAttribute(XNamespace.Xmlns + "image", ImageNs),
                new XAttribute(XNamespace.Xmlns + "xhtml", XhtmlNs),
                pages));
        return Content(sitemap.Declaration + Environment.NewLine + sitemap, "application/xml", Encoding.UTF8);
    }

    private static XElement Alternate(string hreflang, string href) =>
        new(XhtmlNs + "link", new XAttribute("rel", "alternate"), new XAttribute("hreflang", hreflang), new XAttribute("href", href));
}
