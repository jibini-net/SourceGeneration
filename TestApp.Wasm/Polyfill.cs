namespace Microsoft.AspNetCore.Mvc.ApplicationParts
{
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    internal class ApplicationPartAttribute : Attribute
    {
        public ApplicationPartAttribute(string _)
        {
        }
    }
}
