using Generated;
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
        services.AddUserCardView<UserCardBase.Default>();
        services.AddRecursiveViewView<RecursiveView>();
        services.AddLayoutView<LayoutBase.Default>();
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.Services.AddControllers();
        
        //builder.Services.AddBackendServices();
        // or
        //builder.Services.AddFrontendServices();

        // +
        builder.Services.AddViewServices();

        var app = builder.Build();
        app.UseRouting();
        app.MapControllers();
        app.UseStaticFiles();
        app.Start();

        app.WaitForShutdown();
    }
}
