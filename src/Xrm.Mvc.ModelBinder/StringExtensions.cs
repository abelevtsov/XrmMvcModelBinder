namespace Xrm.Mvc.ModelBinder
{
    public static class StringExtensions
    {
        public static string ReplaceSpecial(this string value) => value.Replace(" ", string.Empty).Replace("\xA0", string.Empty).Replace(".", ",");

        public static bool IsNull(this string value) => string.IsNullOrEmpty(value) || value == "null";
    }
}
