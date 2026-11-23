using Microsoft.AspNetCore.Mvc;
using EDA_Website.Data;

namespace EDA_Website.Controllers;

[Route("projects")]
public class ProjectsController : Controller
{
    [HttpGet("")]
    public IActionResult Index(string? category)
    {
        var selected = CompanyData.CategoryFromSlug(category);
        ViewData["Category"] = selected;
        var projects = selected is null
            ? CompanyData.Projects
            : CompanyData.Projects.Where(p => p.Category == selected).ToList();
        return View(projects);
    }

    [HttpGet("{slug}")]
    public IActionResult Details(string slug)
    {
        var project = CompanyData.FindProject(slug);
        if (project is null)
            return NotFound();
        return View(project);
    }
}
