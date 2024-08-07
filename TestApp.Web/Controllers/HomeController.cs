﻿namespace TestApp.Controllers;

using Generated;
using Microsoft.AspNetCore.Mvc;

[Controller]
public class HomeController : Controller
{
    private readonly HomePageBase.IView home;
    public HomeController(HomePageBase.IView home)
    {
        this.home = home;
    }

    [HttpGet("/")]
    public async Task<IActionResult> Index()
    {
        var html = await home.RenderPageAsync();
        return Content(html, "text/html");
    }
}
