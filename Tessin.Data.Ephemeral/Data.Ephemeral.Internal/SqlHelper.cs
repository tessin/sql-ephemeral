namespace Tessin.Data.Ephemeral.Internal
{
    public static class SqlHelper
    {
        public static string Escape(string identifier, char delimiter)
        {
            return identifier.Replace(delimiter.ToString(), new string(delimiter, 2));
        }
    }
}
