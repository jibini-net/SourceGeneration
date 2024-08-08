namespace TestApp.Views;

using Generated;

public class Drawer(
    IServiceProvider sp
    ) : DrawerBase(sp)
{
    public override void Toggle()
    {
        open = !(open ?? startOpen);
    }
}
