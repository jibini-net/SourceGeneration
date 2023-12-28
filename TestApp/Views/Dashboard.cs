using Generated;
using System.Web;

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
            : $@"
                <div class=""d-flex flex-row"">
                    <strong>{HttpUtility.HtmlEncode(loggedIn.suFirstName)}</strong>
                    <a href=""logout"" class=""ms-2"">Log out</a>
                </div>
                ".Trim();
}
