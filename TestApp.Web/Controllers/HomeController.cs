using Generated;
using Microsoft.AspNetCore.Mvc;

namespace TestApp.Controllers;

[Controller]
public class HomeController : Controller
{
    private readonly HostBase.IView host;
    public HomeController(HostBase.IView host)
    {
        this.host = host;
    }

    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        var html = await host.RenderPageAsync();
        return Content(html, "text/html");
    }
}
