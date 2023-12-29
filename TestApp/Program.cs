using Generated;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TestApp.Services;
using TestApp.Views;

namespace TestApp;

public static class ServiceCollectionExtensions
{
    public static void AddBackendServices(this IServiceCollection services)
    {
        services.AddBlogPostBackend<BlogPostService>();
        /*
        services.AddLogEntryBackend<LogEntryService>();
        services.AddPermissionBackend<PermissionService>();
        services.AddSiteUserBackend<SiteUserService>();
        */
    }

    public static void AddFrontendServices(this IServiceCollection services)
    {
        services.AddBlogPostFrontend();
        services.AddLogEntryFrontend();
        services.AddPermissionFrontend();
        services.AddSiteUserFrontend();
    }

    public static void AddViewServices(this IServiceCollection services)
    {
        services.AddDashboardView<Dashboard>();
    }
}

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddBackendServices();
        // or
        //builder.Services.AddFrontendServices();

        // +
        builder.Services.AddViewServices();

        var app = builder.Build();
        app.Start();

        /*
        {
            var blogPosts = app.Services.GetRequiredService<BlogPost.IService>();
            await blogPosts.Get(1);
            await blogPosts.MakePost("Hello, world!");
        }
        */

        {
            var dashboard = app.Services.GetRequiredService<DashboardBase.IView>();
            dashboard.SetTitle("Hello, world!");
            dashboard.SetDescription("Foo bar");
            var html = await dashboard.RenderAsync();
            Console.WriteLine(html);
        }

        app.WaitForShutdown();
    }
}