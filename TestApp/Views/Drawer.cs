using Generated;

namespace TestApp.Views;

public class Drawer(
    IServiceProvider sp
    ) : DrawerBase(sp)
{
    public override void Toggle()
    {
        open = !(open ?? startOpen);
    }
}
