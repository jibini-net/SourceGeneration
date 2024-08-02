namespace TestApp.Views;

using Generated;

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
