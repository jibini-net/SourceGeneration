using Generated;

namespace TestApp.Views;

public class UpDownDemo : UpDownDemoBase
{
    public UpDownDemo(IServiceProvider sp) : base(sp)
    {
    }

    public override void Decrement()
    {
        count--;
    }

    public override void Increment()
    {
        count++;
    }
}
