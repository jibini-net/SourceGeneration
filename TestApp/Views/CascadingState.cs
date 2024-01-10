using Generated;

namespace TestApp.Views;

public class CascadingState : CascadingStateBase
{
    public CascadingState(IServiceProvider sp) : base(sp)
    {
    }

    override public void SetLoggedIn(SiteUser loggedIn)
    {
        this.loggedIn = loggedIn;
    }
}
