using Generated;

namespace TestApp.Views;

public class RecursiveView : RecursiveViewBase
{
    public RecursiveView(IServiceProvider sp) : base(sp)
    {
    }

    public override void GoBack()
    {
        depth--;
    }
}
