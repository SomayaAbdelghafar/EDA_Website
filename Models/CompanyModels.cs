using EDA_Website.Localization;

namespace EDA_Website.Models;

// The records below hold what is the same in every language (slugs, icons, photos, links between items).
// Their text properties read the current language's translation, keyed by slug or id, from
// Resources/Translations/{language}/Data.*.json. Views simply use @project.Title, @service.Faqs, …

public enum ProjectCategory
{
    /// <summary>مشاريع التكييف والتبريد — design, supply and installation.</summary>
    AirConditioning,

    /// <summary>أعمال الصيانة بضمان شركة TRANE.</summary>
    TraneMaintenance,

    /// <summary>أعمال صيانة الشركة — maintenance under EDA's own management.</summary>
    CompanyMaintenance
}

/// <summary>A delivered project. Text: Data.Projects.json → "{Slug}": { Title, ShortTitle?, City, Summary }.</summary>
public record Project(string Slug, ProjectCategory Category, int ImageCount, bool Featured = false)
{
    private string Key => "Data.Projects." + Slug;

    public string Title => Translations.Get(Key + ".Title");

    /// <summary>Optional shorter name for the browser tab and search results when <see cref="Title"/> is very long.</summary>
    public string? ShortTitle => Translations.Find(Key + ".ShortTitle");

    public string City => Translations.Get(Key + ".City");

    public string Summary => Translations.Get(Key + ".Summary");

    public string ThumbUrl => $"/images/projects/{Slug}/thumb.jpg";

    public IEnumerable<string> ImageUrls =>
        Enumerable.Range(1, ImageCount).Select(i => $"/images/projects/{Slug}/{i}.jpg");
}

/// <summary>
/// A service, with its own page at /services/{Slug}. Text: Data.Services.json → "{Slug}": { … } (card text,
/// SEO title and description, H1, details, checklist, FAQs).
/// </summary>
public record Service(string Slug, string Icon, string Image)
{
    private string Key => "Data.Services." + Slug;

    /// <summary>Slugs of projects shown on the page as proof of work.</summary>
    public IReadOnlyList<string> RelatedProjects { get; init; } = [];

    public string Title => Translations.Get(Key + ".Title");

    /// <summary>One line for the service cards.</summary>
    public string ShortDescription => Translations.Get(Key + ".ShortDescription");

    public string Description => Translations.Get(Key + ".Description");

    /// <summary>Three highlights for the services overview.</summary>
    public IReadOnlyList<string> Points => Translations.List(Key + ".Points");

    /// <summary>Title shown in Google results and the browser tab: the service + Jeddah, about 60 characters.</summary>
    public string SeoTitle => Translations.Get(Key + ".SeoTitle");

    /// <summary>Snippet shown under the title in Google results, about 150 characters.</summary>
    public string MetaDescription => Translations.Get(Key + ".MetaDescription");

    /// <summary>Main heading (H1) of the service page.</summary>
    public string Heading => Translations.Get(Key + ".Heading");

    /// <summary>Paragraphs shown after <see cref="Description"/>.</summary>
    public IReadOnlyList<string> Details => Translations.List(Key + ".Details");

    /// <summary>"What the service includes" checklist.</summary>
    public IReadOnlyList<string> Includes => Translations.List(Key + ".Includes");

    /// <summary>Building types the service suits.</summary>
    public IReadOnlyList<string> SuitableFor => Translations.List(Key + ".SuitableFor");

    public IReadOnlyList<Faq> Faqs => Enumerable.Range(0, Translations.Count(Key + ".Faqs"))
        .Select(i => new Faq(Translations.Get($"{Key}.Faqs.{i}.Question"), Translations.Get($"{Key}.Faqs.{i}.Answer")))
        .ToList();
}

public record Faq(string Question, string Answer);

/// <summary>Text: Data.Company.json → Values.{Id}: { Title, Description }.</summary>
public record CompanyValue(string Id, string Icon)
{
    public string Title => Translations.Get($"Data.Company.Values.{Id}.Title");
    public string Description => Translations.Get($"Data.Company.Values.{Id}.Description");
}

/// <summary>Text: Data.Company.json → Clients.{Id}.</summary>
public record Client(string Id, string Logo)
{
    public string Name => Translations.Get($"Data.Company.Clients.{Id}");
}

/// <summary>Text: Data.Company.json → Process.{Id}: { Title, Description }.</summary>
public record ProcessStep(string Id, string Icon)
{
    public string Title => Translations.Get($"Data.Company.Process.{Id}.Title");
    public string Description => Translations.Get($"Data.Company.Process.{Id}.Description");
}

/// <summary>A box of the org chart. Text: Data.Company.json → Org.{Id}.</summary>
/// <param name="LeadsProjectTeam">Draws the connector down to the project team row.</param>
public record OrgRole(string Id, bool LeadsProjectTeam = false)
{
    public string Title => Translations.Get($"Data.Company.Org.{Id}");
}
