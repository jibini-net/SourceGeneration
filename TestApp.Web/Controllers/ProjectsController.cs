namespace TestApp.Controllers;

using Generated;
using Microsoft.AspNetCore.Mvc;

[Controller]
public class ProjectsController(
    ProjectsPageBase.IView projects
    ) : Controller
{
    [HttpGet("/projects")]
    public async Task<IActionResult> Index()
    {
        var html = await projects.RenderPageAsync();
        return Content(html, "text/html");
    }

    [HttpGet("/projects/check-engine")]
    public async Task<IActionResult> CheckEngine()
    {
        projects.LoadState(new()
        {
            ["openDrawer"] = "Check Engine"
        });
        var html = await projects.RenderPageAsync();
        return Content(html, "text/html");
    }

    [HttpGet("/projects/fsas")]
    public async Task<IActionResult> Fsas()
    {
        projects.LoadState(new()
        {
            ["openDrawer"] = "Finite State Automata"
        });
        var html = await projects.RenderPageAsync();
        return Content(html, "text/html");
    }

    [HttpGet("/projects/source-gen")]
    public async Task<IActionResult> SourceGen()
    {
        projects.LoadState(new()
        {
            ["openDrawer"] = "Source Generation"
        });
        var html = await projects.RenderPageAsync();
        return Content(html, "text/html");
    }
}
