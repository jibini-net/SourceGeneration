using Generated;
using Microsoft.AspNetCore.Mvc;

namespace TestApp.Controllers;

[Controller]
public class HomeController : Controller
{
    private readonly DashboardPageBase.IView dashboard;
    public HomeController(DashboardPageBase.IView dashboard)
    {
        this.dashboard = dashboard;
    }

    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        var html = await dashboard.RenderPageAsync();
        return Content(html, "text/html");
    }
}
