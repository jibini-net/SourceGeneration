namespace TestApp.Views;

using Generated;

public class TableRowDemo(
    IServiceProvider sp
    ) : TableRowDemoBase(sp)
{
    public override void Add(string row)
    {
        if (rows.Count < 5)
        {
            rows.Add(row);
        }
    }

    public override void MoveDown(int index)
    {
        if (index >= rows.Count - 1)
        {
            return;
        }
        (rows[index + 1], rows[index]) = (rows[index], rows[index + 1]);
    }

    public override void MoveUp(int index)
    {
        if (index <= 0)
        {
            return;
        }
        (rows[index - 1], rows[index]) = (rows[index], rows[index - 1]);
    }

    public override void Remove(int index)
    {
        rows.RemoveAt(index);
    }
}
