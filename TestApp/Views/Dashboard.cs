using Generated;

namespace TestApp.Views;

public class Dashboard : DashboardBase
{
    public override void SetTitle(string title)
    {
        this.title = title;
    }

    public override void SetDescription(string description)
    {
        this.description = description;
    }

    public override bool IsGuest()
        => (loggedIn?.suID ?? 0) == 0;

    public override string GetProfileHtml()
        => IsGuest()
            ? "<span>Guest</span>"
            : $"<strong>{loggedIn.suFirstName}</strong>";
}
