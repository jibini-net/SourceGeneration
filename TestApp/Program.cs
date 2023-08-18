using Generated;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TestApp.Services;

namespace TestApp;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        //TODO builder.AddBackendServices();
        //TODO builder.AddBackendServices();
        //TODO builder.AddSiteUserBackend();
        //TODO builder.AddPermissionFrontend();

        // Services for backend/generated API
        builder.Services.AddScoped<BlogPost.Repository>();
        builder.Services.AddScoped<BlogPost.IBackendService, BlogPostService>();
        builder.Services.AddScoped<BlogPost.DbService>();
        // Service for connecting to generated API
        builder.Services.AddScoped<BlogPost.ApiService>();
        // Default service used by business logic
        builder.Services.AddScoped<BlogPost.IService>((sp) => sp.GetRequiredService<BlogPost.DbService>());

        var app = builder.Build();
        app.Start();

        {
            var db = app.Services.GetService<BlogPost.IService>();

        }

        {
            var api = app.Services.GetService<BlogPost.IService>();

        }

        app.WaitForShutdown();
    }
}