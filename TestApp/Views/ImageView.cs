namespace TestApp.Views;

using Generated;

public class ImageView : ImageViewBase
{
    public ImageView(IServiceProvider sp) : base(sp)
    {
    }

    public override void Toggle()
    {
        open = !open;
    }
}
