using Generated;

namespace TestApp.Views;

public class Dashboard : DashboardBase
{
    public Dashboard(IServiceProvider sp) : base(sp)
    {
    }

    override public void SetLoggedIn(SiteUser loggedIn)
    {
        this.loggedIn = loggedIn;
    }
}
