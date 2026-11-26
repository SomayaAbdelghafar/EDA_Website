using EDA_Website.Localization;
using EDA_Website.Models;

namespace EDA_Website.Data;

/// <summary>
/// The company's content, taken from the EDA company profile (PDF).
/// <para>
/// This file holds what is the same in every language: contact details, registration numbers, and the list,
/// order, icons, photos and links of services, projects, values and clients.
/// All wording (Arabic and English) lives in Resources/Translations/{language}/Data.*.json, keyed by the
/// slugs and ids used here. To add a project: add it below, then add its text to Data.Projects.json in every language.
/// </para>
/// </summary>
public static class CompanyData
{
    // ---------- Identity (text: Data.Company.json) ----------

    /// <summary>Official company name in the current language.</summary>
    public static string Name => Translations.Get("Data.Company.Name");

    public static string NameIn(SiteLanguage language) => Translations.Get("Data.Company.Name", language.Code);

    /// <summary>Short brand name used in page titles ("إيدا للتكييف", "EDA Air Conditioning").</summary>
    public static string ShortName => Translations.Get("Data.Company.ShortName");

    public static string Tagline => Translations.Get("Data.Company.Tagline");

    /// <summary>Meta description for pages that don't set their own (Google shows about 150 characters).</summary>
    public static string DefaultDescription => Translations.Get("Data.Company.Description");

    // ---------- Contact (the same in every language) ----------
    public const string Phone1 = "+966547777603";
    public const string Phone1Display = "966 547777603";
    public const string Phone2 = "+966540010402";
    public const string Phone2Display = "966 540010402";
    public const string WhatsApp = "966540010402";
    public const string Email = "edaac.co@gmail.com";

    public static string StreetAddress => Translations.Get("Data.Company.StreetAddress");
    public static string City => Translations.Get("Data.Company.City");
    public static string Region => Translations.Get("Data.Company.Region");
    public static string Country => Translations.Get("Data.Company.Country");
    public static string Address => Translations.Get("Data.Company.Address");

    // Exact company pin, from the Google Maps link the owner shared (https://maps.app.goo.gl/tpCCQJcjegsVFDKv9)
    public const string MapLatitude = "21.5815468";
    public const string MapLongitude = "39.1662788";
    public const string MapShareUrl = "https://maps.app.goo.gl/tpCCQJcjegsVFDKv9";

    // From the Commercial Registration & VAT certificates (profile slide 34)
    public const string UnifiedNumber = "7032447745";
    public const string CrNumber = "4030496076";
    public const string VatNumber = "311507541300003";
    public const string CrIssueDate = "25/12/2022";

    public static string WhatsAppUrl(string? text = null) =>
        $"https://wa.me/{WhatsApp}" + (string.IsNullOrEmpty(text) ? "" : "?text=" + Uri.EscapeDataString(text));

    /// <summary>Embedded Google map, with its labels in the page's language.</summary>
    public static string MapEmbedUrl => $"https://maps.google.com/maps?q={MapLatitude},{MapLongitude}&z=17&hl={SiteLanguages.Current.Code}&output=embed";
    public static string MapLinkUrl => MapShareUrl;

    // ---------- About (text: Data.Company.json → About) ----------
    public static string AboutIntro => Translations.Get("Data.Company.About.Intro");
    public static string AboutDesign => Translations.Get("Data.Company.About.Design");
    public static string AboutSupply => Translations.Get("Data.Company.About.Supply");
    public static string AboutPosition => Translations.Get("Data.Company.About.Position");

    // Text: Data.Company.json → Values.{id}
    public static readonly IReadOnlyList<CompanyValue> Values =
    [
        new("quality", "bi-award"),
        new("team", "bi-people"),
        new("safety", "bi-shield-check"),
        new("partner", "bi-hand-thumbs-up"),
        new("punctuality", "bi-clock-history"),
        new("innovation", "bi-lightbulb")
    ];

    // ---------- Org chart (profile slide 05; text: Data.Company.json → Org.{id}) ----------
    public static readonly OrgRole OrgTop = new("general-manager");
    public static readonly IReadOnlyList<OrgRole> OrgManagers = [new("admin-manager"), new("project-manager", LeadsProjectTeam: true), new("finance")];
    public static readonly IReadOnlyList<OrgRole> OrgProjectTeam = [new("supervisor"), new("project-engineer"), new("technicians")];

    // ---------- Services (profile slide 04; text: Data.Services.json → {slug}) ----------
    // Order here = order of the service cards, the footer list and the contact form choices.
    // Each service has its own page, /services/{slug}, whose wording is in the translation files.
    public static readonly IReadOnlyList<Service> Services =
    [
        new("central-ventilation", "bi-wind", "/images/site/rooftop.jpg")
        {
            RelatedProjects = ["in2-fitness-gym", "al-barza-mosque", "king-fahd-mosque-madinah"]
        },
        new("split-window", "bi-snow", "/images/site/about-unit.jpg")
        {
            RelatedProjects = ["bin-mahfouz-villa", "mohammed-abdu-palaces", "hada-al-sham-rest-house"]
        },
        new("central-package", "bi-building", "/images/site/hero.jpg")
        {
            RelatedProjects = ["al-hijrain-village", "seychelles-2-resort", "snb-red-sea-mall"]
        },
        // the duct photo from the profile (its old project path now holds a different photo)
        new("ducts-grilles", "bi-bounding-box", "/images/site/ducts.jpg")
        {
            RelatedProjects = ["al-hijrain-village", "in2-fitness-gym", "snb-red-sea-mall"]
        },
        new("operation-monitoring", "bi-speedometer2", "/images/site/chillers.jpg")
        {
            RelatedProjects = ["al-jamoum-municipality", "air-defense-jeddah", "adeeks-complex"]
        },
        new("maintenance-contracts", "bi-tools", "/images/site/technician.jpg")
        {
            RelatedProjects = ["ikea-madinah", "muvi-cinema-jeddah", "jazan-economy-hall"]
        }
    ];

    public static Service? FindService(string? slug) =>
        Services.FirstOrDefault(s => string.Equals(s.Slug, slug, StringComparison.OrdinalIgnoreCase));

    // Text: Data.Company.json → Process.{id}
    public static readonly IReadOnlyList<ProcessStep> Process =
    [
        new("survey", "bi-clipboard-data"),
        new("design", "bi-rulers"),
        new("installation", "bi-gear-wide-connected"),
        new("handover", "bi-check2-circle"),
        new("maintenance", "bi-arrow-repeat")
    ];

    // ---------- Clients (profile slide "عملاء نفتخر بثقتهم بنا"; names: Data.Company.json → Clients.{id}) ----------
    // Square WebP logos (~3–12 KB each); the original PNG/JPG/WebP sources stay next to them.
    // Order matters: the first cards are the ones seen first (IPC and Al-Rayan are featured).
    public static readonly IReadOnlyList<Client> Clients =
    [
        new("ipc", "/images/clients/ipc.webp"),
        new("al-rayan", "/images/clients/al-rayan.webp"),
        new("ikea", "/images/clients/ikea.webp"),
        new("mcdonalds", "/images/clients/mcdonalds.webp"),
        new("muvi", "/images/clients/muvi.webp"),
        new("haram-affairs", "/images/clients/haram-affairs.webp"),
        new("king-abdulaziz-hospital-jeddah", "/images/clients/king-abdulaziz-hospital-jeddah.webp"),
        new("cenomi", "/images/clients/cenomi.webp"),
        new("haifaa-mall", "/images/clients/haifaa-mall.webp"),
        new("red-sea-mall", "/images/clients/red-sea-mall.webp"),
        new("al-fahhad", "/images/clients/al-fahhad.webp")
    ];

    // ---------- Project categories (text: Data.ProjectCategories.json → {slug}) ----------
    public static string CategorySlug(ProjectCategory c) => c switch
    {
        ProjectCategory.AirConditioning => "ac-projects",
        ProjectCategory.TraneMaintenance => "trane-maintenance",
        ProjectCategory.CompanyMaintenance => "maintenance",
        _ => ""
    };

    public static ProjectCategory? CategoryFromSlug(string? slug) =>
        Enum.GetValues<ProjectCategory>().Cast<ProjectCategory?>().FirstOrDefault(c => CategorySlug(c!.Value) == slug);

    private static string CategoryText(ProjectCategory c, string field) =>
        Translations.Get($"Data.ProjectCategories.{CategorySlug(c)}.{field}");

    public static string CategoryTitle(ProjectCategory c) => CategoryText(c, "Title");

    public static string CategoryDescription(ProjectCategory c) => CategoryText(c, "Description");

    /// <summary>Title of /projects?category=… in Google results (each category page gets its own).</summary>
    public static string CategorySeoTitle(ProjectCategory c) => CategoryText(c, "SeoTitle");

    public static string CategoryMetaDescription(ProjectCategory c) => CategoryText(c, "MetaDescription");

    // ---------- Projects (profile slides 06–33; text: Data.Projects.json → {slug}) ----------
    // Order here = order on the projects page; Featured ones also appear on the home page.
    public static readonly IReadOnlyList<Project> Projects =
    [
        // 1) Air-conditioning projects
        new("mohammed-abdu-palaces", ProjectCategory.AirConditioning, 13, Featured: true),
        new("sudais-hall-mina", ProjectCategory.AirConditioning, 27, Featured: true),
        new("bin-mahfouz-villa", ProjectCategory.AirConditioning, 3),
        new("al-hijrain-village", ProjectCategory.AirConditioning, 49, Featured: true),
        new("al-jamoum-municipality", ProjectCategory.AirConditioning, 3),
        new("hada-al-sham-rest-house", ProjectCategory.AirConditioning, 4),
        new("seychelles-2-resort", ProjectCategory.AirConditioning, 52, Featured: true),
        new("al-barza-mosque", ProjectCategory.AirConditioning, 11),
        new("king-fahd-mosque-madinah", ProjectCategory.AirConditioning, 7, Featured: true),
        new("in2-fitness-gym", ProjectCategory.AirConditioning, 35),
        new("snb-red-sea-mall", ProjectCategory.AirConditioning, 29, Featured: true),

        // 2) Maintenance under TRANE warranty
        new("ikea-madinah", ProjectCategory.TraneMaintenance, 5, Featured: true),
        new("jazan-economy-hall", ProjectCategory.TraneMaintenance, 5),
        new("air-defense-jeddah", ProjectCategory.TraneMaintenance, 3),
        new("adeeks-complex", ProjectCategory.TraneMaintenance, 4),
        new("jarir-jazan", ProjectCategory.TraneMaintenance, 4),
        new("benchmark-events-hall", ProjectCategory.TraneMaintenance, 4),
        new("madinah-museum", ProjectCategory.TraneMaintenance, 3),
        new("al-lith-desalination", ProjectCategory.TraneMaintenance, 3),
        new("muvi-cinema-jeddah", ProjectCategory.TraneMaintenance, 4, Featured: true),
        new("homemax-taif", ProjectCategory.TraneMaintenance, 5),
        new("u-walk-jeddah", ProjectCategory.TraneMaintenance, 4),

        // 3) Company maintenance
        new("aldo-al-andalus-mall", ProjectCategory.CompanyMaintenance, 5),
        new("oxxo-al-yasmin-mall", ProjectCategory.CompanyMaintenance, 4),
        new("spring-al-yasmin-mall", ProjectCategory.CompanyMaintenance, 5),
        new("aldo-haifaa-mall", ProjectCategory.CompanyMaintenance, 5)
    ];

    public static Project? FindProject(string slug) =>
        Projects.FirstOrDefault(p => string.Equals(p.Slug, slug, StringComparison.OrdinalIgnoreCase));

    /// <summary>Cities where the projects above were delivered (text: Data.Company.json → Cities).</summary>
    public static IReadOnlyList<string> CitiesServed => Translations.List("Data.Company.Cities");
}
