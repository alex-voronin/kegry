namespace SharedCore.Extensions;

public static class StringExtensions
{
    public static Guid ToDeterministicGuid(this string str)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(str));
        return new Guid(hash);
    }
}