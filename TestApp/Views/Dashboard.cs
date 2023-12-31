using Generated;

namespace TestApp.Views;

public class Dashboard : DashboardBase
{
    public Dashboard(IServiceProvider sp) : base(sp)
    {
        /*
        loggedIn = new()
        {
            suID = 1,
            suFirstName = "John",
            suLastName = "Smith"
        };
        */
    }

    override public void SetTitle(string title)
    {
        this.title = title;
    }

    override public void SetDescription(string description)
    {
        this.description = description;
    }
}
