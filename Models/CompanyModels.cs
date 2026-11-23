namespace EDA_Website.Models;

public enum ProjectCategory
{
    /// <summary>مشاريع التكييف والتبريد — design, supply and installation.</summary>
    AirConditioning,

    /// <summary>أعمال الصيانة بضمان شركة TRANE.</summary>
    TraneMaintenance,

    /// <summary>أعمال صيانة الشركة — maintenance under EDA's own management.</summary>
    CompanyMaintenance
}

public record Project(
    string Slug,
    string Title,
    string City,
    ProjectCategory Category,
    string Summary,
    int ImageCount,
    bool Featured = false)
{
    public string ThumbUrl => $"/images/projects/{Slug}/thumb.jpg";

    public IEnumerable<string> ImageUrls =>
        Enumerable.Range(1, ImageCount).Select(i => $"/images/projects/{Slug}/{i}.jpg");
}

public record Service(string Slug, string Title, string ShortDescription, string Description, string Icon, IReadOnlyList<string> Points);

public record CompanyValue(string Title, string Description, string Icon);

public record Client(string Name, string Logo);

public record ProcessStep(string Title, string Description, string Icon);
