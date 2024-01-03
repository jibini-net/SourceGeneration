using Generated;
using Microsoft.AspNetCore.Mvc;

namespace TestApp.Controllers;

[Controller]
public class HomeController : Controller
{
    private readonly LayoutBase.IView layout;
    public HomeController(LayoutBase.IView layout)
    {
        this.layout = layout;
    }

    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        var html = await layout.RenderPageAsync();
        return Content(html, "text/html");
    }
}
