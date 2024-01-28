using Generated;
using Microsoft.AspNetCore.Mvc;

namespace TestApp.Controllers;

[Controller]
public class HomeController : Controller
{
    private readonly HomePageBase.IView Home;
    public HomeController(HomePageBase.IView Home)
    {
        this.Home = Home;
    }

    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        var html = await Home.RenderPageAsync();
        return Content(html, "text/html");
    }
}
