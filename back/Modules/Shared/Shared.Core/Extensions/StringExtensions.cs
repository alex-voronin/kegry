namespace SharedCore.Extensions;

public static class StringExtensions
{
    public static Guid ToDeterministicGuid(this string str)
    {
        var bytes = System.Text.Encoding.UTF8.GetBytes(str);
        var hash = System.Security.Cryptography.MD5.HashData(bytes);
        return new Guid(hash);
    }
}