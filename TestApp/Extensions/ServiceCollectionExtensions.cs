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
        services.AddLayoutView<LayoutBase.Default>();
        services.AddCascadingStateView<CascadingState>();
        services.AddHomePageView<HomePageBase.Default>();
        services.AddHomeView<HomeBase.Default>();
        services.AddShoutBannerView<ShoutBannerBase.Default>();
        services.AddEmploymentListView<EmploymentListBase.Default>();
        services.AddTechnologyListView<TechnologyList>();
        services.AddUserCardView<UserCardBase.Default>();
        services.AddProjectsPageView<ProjectsPageBase.Default>();
        services.AddProjectsView<ProjectsBase.Default>();
        services.AddSourceGenShowcaseView<SourceGenShowcaseBase.Default>();
        services.AddDemoTabsView<DemoTabsBase.Default>();
        services.AddUpDownDemoView<UpDownDemo>();
    }
}
