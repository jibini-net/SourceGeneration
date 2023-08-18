using Generated;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TestApp.Services;

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
}

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddBackendServices();
        // or
        //builder.Services.AddFrontendServices();

        var app = builder.Build();
        app.Start();

        {
            var blogPosts = app.Services.GetRequiredService<BlogPost.IService>();
            blogPosts.Get(1);
            blogPosts.MakePost("Hello, world!");
        }

        app.WaitForShutdown();
    }
}