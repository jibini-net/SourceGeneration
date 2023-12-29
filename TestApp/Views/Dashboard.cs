using Generated;
using System.Web;

namespace TestApp.Views;

public class Dashboard : DashboardBase
{
    override public void SetTitle(string title)
    {
        this.title = title;
    }

    override public void SetDescription(string description)
    {
        this.description = description;
    }

    override public Task<bool> IsGuest()
        => Task.FromResult((loggedIn?.suID ?? 0) == 0);

    override public async Task<string> GetProfileHtml()
        => (await IsGuest())
            ? "<span>Guest</span>"
            : $@"
                <div class=""d-flex flex-row"">
                    <strong>{HttpUtility.HtmlEncode(loggedIn.suFirstName)}</strong>
                    <a href=""logout"" class=""ms-2"">Log out</a>
                </div>
                ".Trim();
}
