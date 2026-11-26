using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;

namespace EDA_Website.Localization;

/// <summary>
/// Wires the multilingual site into ASP.NET Core. Overview:
/// <list type="bullet">
/// <item>Texts: Resources/Translations/{language}/*.json → <see cref="TranslationStore"/>, read through the standard
/// IStringLocalizer (views, validation) and <see cref="Translations"/> (C# code).</item>
/// <item>Addresses: the default language at the root, the others under /{code}/ (<see cref="LanguageRouteConvention"/>);
/// links keep the current language (<see cref="LanguageUrlHelper"/>).</item>
/// <item>Language of a request: taken from its URL (<see cref="UrlPrefixRequestCultureProvider"/>).</item>
/// </list>
/// To add a language: create Resources/Translations/{code}/ with the same files, then add the code to
/// "Localization:Languages" in appsettings.json.
/// </summary>
public static class LocalizationSetup
{
    public static IServiceCollection AddSiteLocalization(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
    {
        var options = configuration.GetSection(SiteLanguageOptions.SectionName).Get<SiteLanguageOptions>() ?? new SiteLanguageOptions();
        SiteLanguages.Configure(options);

        services.AddSingleton(sp => new TranslationStore(
            Path.Combine(environment.ContentRootPath, options.TranslationsPath),
            SiteLanguages.All.Select(l => l.Code).ToList(),
            SiteLanguages.Default.Code,
            reloadOnChange: environment.IsDevelopment(),
            sp.GetRequiredService<ILogger<TranslationStore>>()));

        // The framework's localization interfaces, served from the JSON files
        services.Replace(ServiceDescriptor.Singleton<IStringLocalizerFactory, JsonStringLocalizerFactory>());
        services.TryAddTransient(typeof(IStringLocalizer<>), typeof(StringLocalizer<>));

        // /en/... addresses and links that stay in the current language
        services.Configure<RouteOptions>(o => o.ConstraintMap["lang"] = typeof(LanguageRouteConstraint));
        services.Configure<MvcOptions>(o => o.Conventions.Add(new LanguageRouteConvention()));
        services.TryAddSingleton<UrlHelperFactory>();
        services.Replace(ServiceDescriptor.Singleton<IUrlHelperFactory, LanguageUrlHelperFactory>());

        services.Configure<RequestLocalizationOptions>(o =>
        {
            var cultures = SiteLanguages.All.Select(l => l.Culture).ToList();
            o.DefaultRequestCulture = new RequestCulture(SiteLanguages.Default.Culture);
            o.SupportedCultures = cultures;
            o.SupportedUICultures = cultures;
            o.RequestCultureProviders = [new UrlPrefixRequestCultureProvider()];
            o.ApplyCurrentCultureToResponseHeaders = true; // Content-Language header
        });

        return services;
    }

    /// <summary>
    /// Call first in the pipeline, so error and 404 pages (re-executed further down) are shown in the visitor's language.
    /// </summary>
    public static WebApplication UseSiteLocalization(this WebApplication app)
    {
        var store = app.Services.GetRequiredService<TranslationStore>();
        Translations.Use(store);
        ReportIncompleteTranslations(store, app.Logger);

        app.UseMiddleware<LanguageRedirectMiddleware>();
        app.UseRequestLocalization();
        return app;
    }

    private static void ReportIncompleteTranslations(TranslationStore store, ILogger logger)
    {
        foreach (var language in SiteLanguages.All.Where(l => !l.IsDefault))
        {
            var missing = store.MissingKeys(language.Code);
            if (missing.Count > 0)
                logger.LogWarning("{Count} texts are not translated into {Language} yet (the {Default} text is shown): {Keys}",
                    missing.Count, language.Code, store.DefaultLanguage, string.Join(", ", missing.Take(20)) + (missing.Count > 20 ? ", …" : ""));

            var unknown = store.UnknownKeys(language.Code);
            if (unknown.Count > 0)
                logger.LogWarning("{Count} {Language} texts have no {Default} counterpart (check the key names): {Keys}",
                    unknown.Count, language.Code, store.DefaultLanguage, string.Join(", ", unknown.Take(20)) + (unknown.Count > 20 ? ", …" : ""));
        }
    }
}
