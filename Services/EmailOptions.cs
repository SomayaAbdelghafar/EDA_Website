using MailKit.Security;

namespace EDA_Website.Services;

/// <summary>SMTP settings, bound from the "Email" section of configuration (appsettings.json).</summary>
public class EmailOptions
{
    public const string SectionName = "Email";

    public string Host { get; set; } = "";

    public int Port { get; set; } = 587;

    /// <summary>StartTls for port 587, SslOnConnect for port 465. None is only for a local test server.</summary>
    public SecureSocketOptions Security { get; set; } = SecureSocketOptions.StartTls;

    /// <summary>The mail account the site signs in with.</summary>
    public string? Username { get; set; }

    /// <summary>
    /// Never put this in appsettings.json. Development: <c>dotnet user-secrets set "Email:Password" "..."</c>.
    /// Server: the <c>Email__Password</c> environment variable. For Gmail this is a 16-letter app password.
    /// </summary>
    public string? Password { get; set; }

    /// <summary>Sender address. Defaults to <see cref="Username"/>; Gmail requires it to be the signed-in account.</summary>
    public string? FromAddress { get; set; }

    public string FromName { get; set; } = "موقع شركة إيدا";

    /// <summary>Who receives the contact-form requests.</summary>
    public string[] To { get; set; } = [];
}
