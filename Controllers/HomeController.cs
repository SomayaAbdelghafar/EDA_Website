using System.Diagnostics;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Microsoft.AspNetCore.Mvc;
using EDA_Website.Data;
using EDA_Website.Models;
using EDA_Website.Services;

namespace EDA_Website.Controllers;

public class HomeController(ILogger<HomeController> logger, IWebHostEnvironment env, ContactEmailSender emailSender) : Controller
{
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    // Keep Arabic readable in the stored file instead of \uXXXX escapes
    private static readonly JsonSerializerOptions JsonOptions = new() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };

    [Route("")]
    public IActionResult Index() => View();

    [Route("about")]
    public IActionResult About() => View();

    [Route("services")]
    public IActionResult Services() => View();

    [HttpGet("contact")]
    public IActionResult Contact(string? service)
    {
        var model = new ContactViewModel();
        if (CompanyData.Services.Any(s => s.Title == service))
            model.Service = service;
        return View(model);
    }

    [HttpPost("contact")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        var receivedAt = DateTimeOffset.UtcNow;
        var isSpam = !string.IsNullOrWhiteSpace(model.Website);
        logger.LogInformation("Contact request from {Name} ({Phone}) about {Service}", model.Name, model.Phone, model.Service);

        // A backup copy goes to App_Data/contact-messages.jsonl so a request is never lost, even if email fails
        await SaveCopyAsync(model, receivedAt, isSpam);

        if (isSpam)
        {
            // The hidden honeypot field was filled in: act as if it worked so the bot moves on, but don't email it
            logger.LogWarning("Contact request from {Name} looks like spam (honeypot filled); stored but not emailed", model.Name);
            TempData["ContactSuccess"] = model.Name;
            return RedirectToAction(nameof(Contact));
        }

        try
        {
            await emailSender.SendAsync(model, receivedAt);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not email contact request from {Name} ({Phone})", model.Name, model.Phone);
            ViewData["ContactError"] = true;
            return View(model);
        }

        TempData["ContactSuccess"] = model.Name;
        return RedirectToAction(nameof(Contact));
    }

    private async Task SaveCopyAsync(ContactViewModel model, DateTimeOffset receivedAt, bool isSpam)
    {
        try
        {
            var dir = Path.Combine(env.ContentRootPath, "App_Data");
            Directory.CreateDirectory(dir);
            var line = JsonSerializer.Serialize(new { ReceivedAt = receivedAt, model.Name, model.Phone, model.Email, model.Service, model.City, model.Message, Spam = isSpam }, JsonOptions);
            await FileLock.WaitAsync();
            try { await System.IO.File.AppendAllTextAsync(Path.Combine(dir, "contact-messages.jsonl"), line + Environment.NewLine); }
            finally { FileLock.Release(); }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not store contact request");
        }
    }

    [Route("privacy")]
    public IActionResult Privacy() => View();

    /// <summary>Target of UseStatusCodePagesWithReExecute: keeps the original status code.</summary>
    [Route("status/{code:int}")]
    public IActionResult StatusPage(int code)
    {
        if (code < 400 || code > 599)
            code = StatusCodes.Status404NotFound;
        Response.StatusCode = code;
        if (code == StatusCodes.Status404NotFound)
            return View("NotFound");
        return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
