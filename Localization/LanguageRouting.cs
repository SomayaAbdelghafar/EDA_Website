using System.Globalization;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;

namespace EDA_Website.Localization;

/// <summary>Keeps a controller at one address for all languages (robots.txt, sitemap.xml).</summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class LanguageNeutralAttribute : Attribute;

/// <summary>
/// Gives every attribute route a language-prefixed twin: [Route("about")] answers at /about (default language)
/// and at /en/about. New pages get their English address automatically.
/// </summary>
public sealed class LanguageRouteConvention : IApplicationModelConvention
{
    private const string Prefix = "{" + SiteLanguages.RouteKey + ":lang}";

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            if (controller.Attributes.OfType<LanguageNeutralAttribute>().Any())
                continue;

            var controllerRoutes = controller.Selectors.Where(s => s.AttributeRouteModel is not null).ToList();
            foreach (var selector in controllerRoutes)
                controller.Selectors.Add(Prefixed(selector));

            foreach (var action in controller.Actions)
            {
                // With a controller route, only action templates starting with "/" or "~/" skip its prefix
                var actionRoutes = action.Selectors
                    .Where(s => s.AttributeRouteModel?.Template is { } t && (controllerRoutes.Count == 0 || AttributeRouteModel.IsOverridePattern(t)))
                    .ToList();
                foreach (var selector in actionRoutes)
                    action.Selectors.Add(Prefixed(selector));
            }
        }
    }

    private static SelectorModel Prefixed(SelectorModel selector)
    {
        var copy = new SelectorModel(selector);
        var route = copy.AttributeRouteModel!;
        var template = route.Template ?? "";
        var isOverride = AttributeRouteModel.IsOverridePattern(template);
        var body = template.TrimStart('~').TrimStart('/');
        route.Template = (isOverride ? "/" : "") + (body.Length == 0 ? Prefix : Prefix + "/" + body);
        // Tried first when links are generated: it only applies when a language is given, else the plain route is used
        route.Order = (route.Order ?? 0) - 1;
        route.Name = null; // route names must stay unique
        return copy;
    }
}

/// <summary>{culture:lang} matches the code of every published language except the default one.</summary>
public sealed class LanguageRouteConstraint : IRouteConstraint
{
    public bool Match(HttpContext? httpContext, IRouter? route, string routeKey, RouteValueDictionary values, RouteDirection routeDirection) =>
        values.TryGetValue(routeKey, out var value)
        && SiteLanguages.Find(Convert.ToString(value, CultureInfo.InvariantCulture)) is { IsDefault: false };
}

/// <summary>The language comes from the URL alone (/en/… is English, everything else the default language), never from cookies or browser settings, so every address always shows the same language to visitors and search engines.</summary>
public sealed class UrlPrefixRequestCultureProvider : RequestCultureProvider
{
    public override Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        var language = SiteLanguages.Split(httpContext.Request.Path.Value).Language;
        return Task.FromResult<ProviderCultureResult?>(new ProviderCultureResult(language.Code));
    }
}

/// <summary>Hands out URL helpers that keep the current language in generated links (see <see cref="LanguageUrlHelper"/>).</summary>
public sealed class LanguageUrlHelperFactory(UrlHelperFactory inner) : IUrlHelperFactory
{
    public IUrlHelper GetUrlHelper(ActionContext context) => new LanguageUrlHelper(inner.GetUrlHelper(context));
}

/// <summary>
/// Adds the current language to every generated page link (asp-controller/asp-action, Url.Action,
/// RedirectToAction…), so a link on an English page leads to the English page. Views need nothing extra.
/// asp-route-culture="ar" (or any explicit language) overrides it.
/// </summary>
public sealed class LanguageUrlHelper(IUrlHelper inner) : IUrlHelper
{
    public ActionContext ActionContext => inner.ActionContext;

    public string? Action(UrlActionContext actionContext) => inner.Action(new UrlActionContext
    {
        Action = actionContext.Action,
        Controller = actionContext.Controller,
        Values = WithLanguage(actionContext.Values),
        Protocol = actionContext.Protocol,
        Host = actionContext.Host,
        Fragment = actionContext.Fragment
    });

    public string? RouteUrl(UrlRouteContext routeContext) => inner.RouteUrl(new UrlRouteContext
    {
        RouteName = routeContext.RouteName,
        Values = WithLanguage(routeContext.Values),
        Protocol = routeContext.Protocol,
        Host = routeContext.Host,
        Fragment = routeContext.Fragment
    });

    public string? Link(string? routeName, object? values) => inner.Link(routeName, WithLanguage(values));

    public string? Content(string? contentPath) => inner.Content(contentPath); // files (CSS, images) have one address

    public bool IsLocalUrl(string? url) => inner.IsLocalUrl(url);

    private RouteValueDictionary WithLanguage(object? values)
    {
        var result = new RouteValueDictionary(values);
        if (result.TryGetValue(SiteLanguages.RouteKey, out var requested))
        {
            // The default language has no prefix: drop it rather than ending up with "?culture=ar"
            if (SiteLanguages.Find(Convert.ToString(requested, CultureInfo.InvariantCulture)) is not { IsDefault: false })
                result.Remove(SiteLanguages.RouteKey);
            return result;
        }

        var current = SiteLanguages.FromRequest(ActionContext.HttpContext);
        if (!current.IsDefault)
            result[SiteLanguages.RouteKey] = current.Code;
        return result;
    }
}

/// <summary>
/// Two redirects that keep one address per page and language:
/// <list type="bullet">
/// <item>"/ar/…" (the default language's code typed as a prefix) → "/…" (permanent).</item>
/// <item>A visitor who opens the home page directly (typed address, bookmark, another site) and earlier chose
/// another language in the switcher goes to that language's home page. Search engines send no cookie, so they
/// always see the page itself.</item>
/// </list>
/// </summary>
public sealed class LanguageRedirectMiddleware(RequestDelegate next)
{
    public Task InvokeAsync(HttpContext context)
    {
        var request = context.Request;
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsHead(request.Method))
            return next(context);

        var path = request.Path.Value ?? "/";
        var defaultPrefix = "/" + SiteLanguages.Default.Code;
        if (path.Equals(defaultPrefix, StringComparison.OrdinalIgnoreCase) || path.StartsWith(defaultPrefix + "/", StringComparison.OrdinalIgnoreCase))
        {
            var target = path[defaultPrefix.Length..];
            context.Response.Redirect(request.PathBase + (target.Length == 0 ? "/" : target) + request.QueryString, permanent: true);
            return Task.CompletedTask;
        }

        if (path == "/"
            && request.Cookies.TryGetValue(SiteLanguages.PreferenceCookie, out var preferred)
            && SiteLanguages.Find(preferred) is { IsDefault: false } language
            && !CameFromThisSite(request))
        {
            context.Response.Headers.CacheControl = "no-store";
            context.Response.Redirect(request.PathBase + language.UrlPrefix + request.QueryString);
            return Task.CompletedTask;
        }

        return next(context);
    }

    /// <summary>A click inside the site (e.g. "Home" on an Arabic page) is a deliberate choice: no redirect.</summary>
    private static bool CameFromThisSite(HttpRequest request) =>
        Uri.TryCreate(request.Headers.Referer.ToString(), UriKind.Absolute, out var referrer)
        && string.Equals(referrer.Authority, request.Host.Value, StringComparison.OrdinalIgnoreCase);
}
