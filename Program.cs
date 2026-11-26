using System.Text.Encodings.Web;
using System.Text.Unicode;
using Microsoft.Extensions.WebEncoders;
using EDA_Website.Localization;
using EDA_Website.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews()
    // Form field names and validation messages are translation keys (Models/ContactViewModel.cs)
    .AddDataAnnotationsLocalization();

// Arabic and English: texts in Resources/Translations/{language}/, English pages under /en/ (see Localization/LocalizationSetup.cs)
builder.Services.AddSiteLocalization(builder.Configuration, builder.Environment);

// Contact form emails (SMTP settings in the "Email" section; the password comes from user-secrets / environment)
builder.Services.Configure<EmailOptions>(builder.Configuration.GetSection(EmailOptions.SectionName));
builder.Services.AddTransient<ContactEmailSender>();

// Render Arabic text as-is instead of HTML entities (&#x...;), which keeps pages smaller and readable.
builder.Services.Configure<WebEncoderOptions>(o => o.TextEncoderSettings = new TextEncoderSettings(UnicodeRanges.All));

var app = builder.Build();

// First: the page language comes from the URL, so the error and 404 pages below are in the visitor's language too
app.UseSiteLocalization();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

// Friendly pages (Arabic or English) for 404 and other error status codes
app.UseStatusCodePagesWithReExecute("/status/{0}");
app.UseHttpsRedirection();
app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
