using Generated;

namespace TestApp;

internal class Program
{
    public static void Main(string[] _)
    {
        {
            var test = new SiteUser.WithPermissions()
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
            test.user_perms.Add(test2);
        }

        {
            var test = new SiteUser.Repository();
            var test2 = test.dbo__SiteUser_GetByID(1);
            var test3 = test.dbo__SiteUser_GetWithPassword(test2.suEmail);
            Console.WriteLine(test3.suPasswordHash);
        }

        {
            SiteUser.IService test = null;
            test?.ValidatePassword("zgoethel12@gmail.com", "S3cur3P4ssw0rd");
        }
    }
}