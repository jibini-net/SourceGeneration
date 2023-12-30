using Generated;

namespace TestApp.Views;

public class UserCard : UserCardBase
{
    public UserCard(IServiceProvider sp) : base(sp)
    {
    }

    override public Task<bool> IsGuest()
        => Task.FromResult((loggedIn?.suID ?? 0) == 0);
}
