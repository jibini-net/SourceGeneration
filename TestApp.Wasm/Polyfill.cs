namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
#pragma warning disable CS9113 // Parameter is unread.
    internal class ApplicationPartAttribute(string _) : Attribute
#pragma warning restore CS9113 // Parameter is unread.
    {
    }
}
