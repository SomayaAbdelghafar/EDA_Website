using System.ComponentModel.DataAnnotations;

namespace EDA_Website.Models;

/// <summary>
/// The contact form. ErrorMessage and Display(Name) values are translation keys (Contact.json → Form / Validation):
/// ASP.NET Core's data-annotations localization looks them up in the visitor's language, for the labels,
/// the server-side checks and the in-browser validation messages alike.
/// </summary>
public class ContactViewModel
{
    /// <summary>Value of the "other service" choice in the service list.</summary>
    public const string OtherService = "other";

    [Required(ErrorMessage = "Contact.Validation.NameRequired")]
    [StringLength(100, ErrorMessage = "Contact.Validation.NameTooLong")]
    [Display(Name = "Contact.Form.Name")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Contact.Validation.PhoneRequired")]
    [RegularExpression(@"^[0-9+\s\-]{9,16}$", ErrorMessage = "Contact.Validation.PhoneInvalid")]
    [Display(Name = "Contact.Form.Phone")]
    public string Phone { get; set; } = "";

    [EmailAddress(ErrorMessage = "Contact.Validation.EmailInvalid")]
    [StringLength(150, ErrorMessage = "Contact.Validation.EmailTooLong")]
    [Display(Name = "Contact.Form.Email")]
    public string? Email { get; set; }

    /// <summary>Slug of the chosen service (see CompanyData.Services), or <see cref="OtherService"/>.</summary>
    [StringLength(100, ErrorMessage = "Contact.Validation.ServiceInvalid")]
    [Display(Name = "Contact.Form.Service")]
    public string? Service { get; set; }

    [StringLength(100, ErrorMessage = "Contact.Validation.CityTooLong")]
    [Display(Name = "Contact.Form.City")]
    public string? City { get; set; }

    [Required(ErrorMessage = "Contact.Validation.MessageRequired")]
    [StringLength(2000, MinimumLength = 10, ErrorMessage = "Contact.Validation.MessageLength")]
    [Display(Name = "Contact.Form.Message")]
    public string Message { get; set; } = "";

    /// <summary>Honeypot: hidden from visitors, so only spam bots fill it in.</summary>
    public string? Website { get; set; }
}
