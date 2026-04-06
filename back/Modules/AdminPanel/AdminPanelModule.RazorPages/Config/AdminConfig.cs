namespace AdminPanelModule.RazorPages.Config;

public class AdminCredentials
{
    public string Username { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
}

public static class AdminPanelConstants
{
    public const string AuthScheme = "AdminCookies";
    public const string PathPrefix = "/admin";
    public const int AuthExpirationDays = 7;
}
