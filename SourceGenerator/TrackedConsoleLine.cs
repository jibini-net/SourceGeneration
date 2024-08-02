namespace SourceGenerator;

public class TrackedConsoleLine
{
    private static bool consumedFirst;
    private int top = -1, left;
    private static SemaphoreSlim consoleMutex = new(1, 1);

    static TrackedConsoleLine()
    {
        _ = Task.Run(async () =>
        {
            for (int pos = 0; ; pos = (pos + 1) % 3)
            {
                await Task.Delay(800);
                if (!consumedFirst)
                {
                    continue;
                }

                consoleMutex.Wait();
                int returnLeft = Console.CursorLeft;
                int returnTop = Console.CursorTop;
                try
                {
                    int top = Console.CursorTop + 1;
                    if (top > 1)
                    {
                        top = (top - 2) % (Console.BufferHeight - 3);
                        top += 2;
                    }

                    if (top != 0 && returnTop != Console.BufferHeight - 3 - 1)
                    {
                        Console.WriteLine();
                    }
                    Console.SetCursorPosition(0, top);

                    Console.Write("[");
                    for (int i = 0; i < pos; i++)
                    {
                        Console.Write(" ");
                    }
                    Console.Write(".");
                    for (int i = 0; i < 2 - pos; i++)
                    {
                        Console.Write(" ");
                    }
                    Console.Write("]");
                    for (int i = 5; i < Console.BufferWidth; i++)
                    {
                        Console.Write(" ");
                    }
                } finally
                {
                    consoleMutex.Release();
                    Console.SetCursorPosition(returnLeft, returnTop);
                }
            }
        });
    }

    public void Write(string msg, bool resolved = false, ConsoleColor? color = null)
    {
        consoleMutex.Wait();
        var returnTop = Console.CursorTop;
        var returnLeft = Console.CursorLeft;
        try
        {
            if (top == -1)
            {
                returnLeft = msg.Length;
                left = 0;
                top = ++returnTop;

                if (consumedFirst != (consumedFirst = true))
                {
                    top = returnTop = 0;
                }

                if (top > 1)
                {
                    top = (top - 2) % (Console.BufferHeight - 3);
                    top += 2;
                    returnTop = (returnTop - 2) % (Console.BufferHeight - 3);
                    returnTop += 2;
                }

                if (returnTop != Console.BufferHeight - 3 - 1)
                {
                    Console.WriteLine();
                }

                Console.SetCursorPosition(left, top);
                for (int i = 0; i < Console.BufferWidth; i++)
                {
                    Console.Write(" ");
                }
            }
            Console.SetCursorPosition(left, top);

            var returnColor = Console.ForegroundColor;
            if (color is not null)
            {
                Console.ForegroundColor = color.Value;
            }
            Console.Write(msg.Substring(0, Math.Min(msg.Length, Console.WindowWidth - left)));
            Console.ForegroundColor = returnColor;

            left = Console.CursorLeft;
        } catch (Exception)
        {
            // Don't crash fatally due to blinkenlights
        } finally
        {
            consoleMutex.Release();
            Console.SetCursorPosition(returnLeft, returnTop);
        }
    }
}
