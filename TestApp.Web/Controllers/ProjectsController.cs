using Generated;
using Microsoft.AspNetCore.Mvc;

namespace TestApp.Controllers;

[Controller]
public class ProjectsController : Controller
{
    private readonly ProjectsPageBase.IView Projects;
    public ProjectsController(ProjectsPageBase.IView Projects)
    {
        this.Projects = Projects;
    }

    [HttpGet("/projects")]
    public async Task<IActionResult> Index()
    {
        var html = await Projects.RenderPageAsync();
        return Content(html, "text/html");
    }
}
