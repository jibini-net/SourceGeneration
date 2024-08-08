namespace TestApp.Views;

using Generated;

public class UpDownDemo(
    IServiceProvider sp
    ) : UpDownDemoBase(sp)
{
    public override void Decrement()
    {
        count--;
    }

    public override void Increment()
    {
        count++;
    }
}
