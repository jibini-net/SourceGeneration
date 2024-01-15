using Generated;

namespace TestApp.Views;

public class CascadingState : CascadingStateBase
{
    public CascadingState(IServiceProvider sp) : base(sp)
    {
        loggedIn = new()
        {
            suID = 1,
            suFirstName = "John"
        };
    }

    override public void SetLoggedIn(SiteUser loggedIn)
    {
        this.loggedIn = loggedIn;
    }
}
