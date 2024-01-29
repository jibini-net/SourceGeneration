using Generated;

namespace TestApp.Views;

public class TableRowDemo : TableRowDemoBase
{
    public TableRowDemo(IServiceProvider sp) : base(sp)
    {
    }

    public override void Add(string row)
    {
        rows.Add(row);
    }

    public override void MoveDown(int index)
    {
        if (index >= rows.Count - 1)
        {
            return;
        }
        var temp = rows[index];
        rows[index] = rows[index + 1];
        rows[index + 1] = temp;
    }

    public override void MoveUp(int index)
    {
        if (index <= 0)
        {
            return;
        }
        var temp = rows[index];
        rows[index] = rows[index - 1];
        rows[index - 1] = temp;
    }

    public override void Remove(int index)
    {
        rows.RemoveAt(index);
    }
}
