namespace TestApp.Controllers;

using Generated;
using Microsoft.AspNetCore.Mvc;

[Controller]
public class HomeController(
    HomePageBase.IView home
    ) : Controller
{
    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        var html = await home.RenderPageAsync();
        return Content(html, "text/html");
    }
}
