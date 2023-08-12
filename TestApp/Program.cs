using Generated;

namespace TestApp;

internal class Program
{
    public static void Main(string[] _)
    {
        var test = new SiteUser()
        {
            suID = 3,
            suEmail = "zgoethel12@gmail.com"
        };
        var test2 = new Permission()
        {
            perID = 9,
            perName = "Permission",
            perConstant = "PERMISSION",
            perActive = true
        };
        test.granted_permissions.Add(test2);

        foreach (var testInt in test2.perTest)
        {
            Console.WriteLine(testInt);
        }
    }
}