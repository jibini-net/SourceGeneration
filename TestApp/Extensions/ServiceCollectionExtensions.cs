using Generated;
using Microsoft.Extensions.DependencyInjection;
using TestApp.Services;
using TestApp.Views;

namespace TestApp.Extensions;

public static class ServiceCollectionExtensions
{
    public static void AddBackendServices(this IServiceCollection services)
    {
        services.AddAccountBackend<AccountService>();
    }

    public static void AddFrontendServices(this IServiceCollection services)
    {
        services.AddAccountFrontend();
    }

    public static void AddViewServices(this IServiceCollection services)
    {
        services.AddHostView<HostBase.Default>();
        services.AddAppView<AppBase.Default>();
        services.AddCascadingStateView<CascadingState>();
        services.AddDashboardView<DashboardBase.Default>();
        services.AddUserCardView<UserCardBase.Default>();
    }
}
