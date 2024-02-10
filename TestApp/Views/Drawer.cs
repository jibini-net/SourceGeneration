using Generated;

namespace TestApp.Views;

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
