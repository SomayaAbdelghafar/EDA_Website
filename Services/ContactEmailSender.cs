using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using MailKit;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using EDA_Website.Data;
using EDA_Website.Localization;
using EDA_Website.Models;

namespace EDA_Website.Services;

/// <summary>Emails contact-form requests to the addresses in Email:To.</summary>
public partial class ContactEmailSender(IOptions<EmailOptions> options, ILogger<ContactEmailSender> logger)
{
    // Saudi Arabia is UTC+3 all year (no daylight saving)
    private static readonly TimeSpan SaudiOffset = TimeSpan.FromHours(3);

    /// <summary>Sends the request by email. Throws if email is not configured or the mail server rejects it.</summary>
    public async Task SendAsync(ContactViewModel model, DateTimeOffset receivedAt, CancellationToken cancellationToken = default)
    {
        var o = options.Value;
        var from = string.IsNullOrWhiteSpace(o.FromAddress) ? o.Username : o.FromAddress;
        var recipients = o.To.Where(a => !string.IsNullOrWhiteSpace(a)).ToList();
        if (string.IsNullOrWhiteSpace(o.Host) || string.IsNullOrWhiteSpace(from) || recipients.Count == 0)
            throw new InvalidOperationException("Email is not configured: set Email:Host, Email:Username and Email:To in appsettings.json.");
        if (!string.IsNullOrEmpty(o.Username) && string.IsNullOrEmpty(o.Password))
            throw new InvalidOperationException(
                "Email:Password is not set. In development run: dotnet user-secrets set \"Email:Password\" \"<app password>\"; on the server set the Email__Password environment variable.");

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(o.FromName, from));
        foreach (var address in recipients)
            message.To.Add(MailboxAddress.Parse(address));

        // So that "Reply" in the mail app goes straight to the customer
        if (!string.IsNullOrWhiteSpace(model.Email) && MailboxAddress.TryParse(model.Email, out var customer))
        {
            customer.Name = OneLine(model.Name);
            message.ReplyTo.Add(customer);
        }

        message.Subject = $"طلب جديد من الموقع: {OneLine(string.IsNullOrWhiteSpace(model.Service) ? "استفسار" : ServiceName(model.Service))} – {OneLine(model.Name)}";
        message.Body = new BodyBuilder
        {
            TextBody = BuildText(model, receivedAt),
            HtmlBody = BuildHtml(model, receivedAt),
        }.ToMessageBody();

        using var client = new SmtpClient { Timeout = 20_000 };
        await client.ConnectAsync(o.Host, o.Port, o.Security, cancellationToken);
        if (!string.IsNullOrEmpty(o.Username))
            await client.AuthenticateAsync(o.Username, o.Password ?? "", cancellationToken);
        await client.SendAsync(message, cancellationToken);
        await client.DisconnectAsync(true, cancellationToken);

        logger.LogInformation("Contact request from {Name} emailed to {Recipients}", model.Name, string.Join(", ", recipients));
    }

    private static string BuildText(ContactViewModel m, DateTimeOffset receivedAt)
    {
        var sb = new StringBuilder();
        sb.AppendLine("طلب جديد من نموذج التواصل في موقع شركة إيدا للتكييف والتبريد");
        sb.AppendLine();
        sb.AppendLine($"الاسم: {m.Name}");
        sb.AppendLine($"رقم الجوال: {m.Phone}");
        if (!string.IsNullOrWhiteSpace(m.Email)) sb.AppendLine($"البريد الإلكتروني: {m.Email}");
        if (!string.IsNullOrWhiteSpace(m.City)) sb.AppendLine($"المدينة: {m.City}");
        sb.AppendLine($"الخدمة المطلوبة: {ServiceName(m.Service)}");
        sb.AppendLine($"لغة الموقع: {VisitorLanguage()}");
        sb.AppendLine($"وقت الاستلام: {FormatTime(receivedAt)}");
        sb.AppendLine();
        sb.AppendLine("تفاصيل الطلب:");
        sb.AppendLine(m.Message);
        return sb.ToString();
    }

    private static string BuildHtml(ContactViewModel m, DateTimeOffset receivedAt)
    {
        static string E(string? s) => WebUtility.HtmlEncode(s ?? "");

        var rows = new StringBuilder();
        void Row(string label, string valueHtml) => rows.Append(
            $"""<tr><th style="text-align:right;vertical-align:top;padding:10px 12px;background:#f3f7f5;color:#1f3d33;white-space:nowrap;border-bottom:1px solid #e3ebe7;">{label}</th><td style="padding:10px 12px;border-bottom:1px solid #e3ebe7;">{valueHtml}</td></tr>""");

        var phoneLink = $"""<a href="tel:{E(PhoneSpaces().Replace(m.Phone, ""))}" dir="ltr" style="color:#157347;">{E(m.Phone)}</a>""";
        var whatsApp = SaudiWhatsAppNumber(m.Phone);
        if (whatsApp is not null)
            phoneLink += $"""&nbsp;·&nbsp;<a href="https://wa.me/{whatsApp}" style="color:#157347;">مراسلة عبر واتساب</a>""";

        Row("الاسم", E(m.Name));
        Row("رقم الجوال", phoneLink);
        if (!string.IsNullOrWhiteSpace(m.Email))
            Row("البريد الإلكتروني", $"""<a href="mailto:{E(m.Email)}" dir="ltr" style="color:#157347;">{E(m.Email)}</a>""");
        if (!string.IsNullOrWhiteSpace(m.City))
            Row("المدينة", E(m.City));
        Row("الخدمة المطلوبة", E(ServiceName(m.Service)));
        Row("لغة الموقع", E(VisitorLanguage()));
        Row("تفاصيل الطلب", E(m.Message).ReplaceLineEndings("<br>"));
        Row("وقت الاستلام", E(FormatTime(receivedAt)));

        return $"""
            <!doctype html>
            <html lang="ar" dir="rtl">
            <body style="margin:0;padding:24px;background:#f5f5f5;font-family:Tahoma,Arial,sans-serif;font-size:15px;line-height:1.7;color:#222;">
              <div dir="rtl" style="max-width:640px;margin:0 auto;background:#fff;border-radius:8px;overflow:hidden;border:1px solid #e3ebe7;">
                <div style="background:#157347;color:#fff;padding:16px 20px;font-size:18px;font-weight:bold;">طلب جديد من موقع شركة إيدا</div>
                <table role="presentation" style="width:100%;border-collapse:collapse;">{rows}</table>
                <p style="margin:0;padding:14px 20px;color:#666;font-size:13px;">أُرسلت هذه الرسالة تلقائيًا من نموذج التواصل في الموقع. للرد على العميل استخدم زر الرد أو تواصل معه على رقم الجوال أعلاه.</p>
              </div>
            </body>
            </html>
            """;
    }

    // This e-mail goes to the company's staff, so it is always in the site's default language (Arabic),
    // whatever language the visitor used; the visitor's language is listed so the reply can match it.

    /// <summary>The chosen service's name in the default language (the form sends the service's slug).</summary>
    private static string ServiceName(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return "—";
        var staffLanguage = SiteLanguages.Default.Code;
        if (slug == ContactViewModel.OtherService)
            return Translations.Get("Contact.Form.OtherService", staffLanguage);
        return CompanyData.FindService(slug) is { } service
            ? Translations.Get($"Data.Services.{service.Slug}.Title", staffLanguage)
            : slug;
    }

    /// <summary>Language of the page the form was sent from ("العربية", "English").</summary>
    private static string VisitorLanguage() => SiteLanguages.Current.NativeName;

    private static string FormatTime(DateTimeOffset time) =>
        time.ToOffset(SaudiOffset).ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture) + " (بتوقيت السعودية)";

    /// <summary>Keeps header values on one line.</summary>
    private static string OneLine(string value) => ControlChars().Replace(value, " ").Trim();

    /// <summary>Turns a Saudi mobile number (05xxxxxxxx, +9665xxxxxxxx, 009665xxxxxxxx) into wa.me format, or null.</summary>
    private static string? SaudiWhatsAppNumber(string phone)
    {
        var digits = new string(phone.Where(char.IsAsciiDigit).ToArray());
        if (digits.StartsWith("00")) digits = digits[2..];
        if (digits.Length == 10 && digits.StartsWith("05")) digits = "966" + digits[1..];
        else if (digits.Length == 9 && digits.StartsWith('5')) digits = "966" + digits;
        return digits.Length == 12 && digits.StartsWith("9665") ? digits : null;
    }

    [GeneratedRegex(@"[\p{Cc}\p{Zl}\p{Zp}]+")]
    private static partial Regex ControlChars();

    [GeneratedRegex(@"[\s\-]")]
    private static partial Regex PhoneSpaces();
}
