namespace TestApp.Views;

using Generated;

public class Drawer : DrawerBase
{
    public Drawer(IServiceProvider sp) : base(sp)
    {
    }

    public override void Toggle()
    {
        open = !(open ?? startOpen);
    }
}
