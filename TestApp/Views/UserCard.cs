using Generated;

namespace TestApp.Views;

public class UserCard : UserCardBase
{
    public UserCard(IServiceProvider sp) : base(sp)
    {
    }

    public override void LogIn()
    {
        loggedIn = new()
        {
            suID = 1,
            suFirstName = "John"
        };
    }

    public override void LogOut()
    {
        loggedIn = new();
    }
}
