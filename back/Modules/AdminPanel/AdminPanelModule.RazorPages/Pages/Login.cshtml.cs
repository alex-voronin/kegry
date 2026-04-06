using System.Security.Claims;
using AdminPanelModule.RazorPages.Config;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;

namespace AdminPanelModule.RazorPages.Pages;

public class LoginModel : PageModel
{
    private readonly AdminCredentials _credentials;

    public LoginModel(IOptions<AdminCredentials> credentials)
    {
        _credentials = credentials.Value;
    }

    [BindProperty]
    public string Username { get; set; } = string.Empty;

    public string ErrorMessage { get; set; } = string.Empty;

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true)
            return RedirectToPage("/Index");

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string password)
    {
        var hasher = new PasswordHasher<string>();

        var verificationResult = hasher.VerifyHashedPassword(
            _credentials.Username, _credentials.PasswordHash, password);
        if (Username != _credentials.Username || verificationResult == PasswordVerificationResult.Failed)
        {
            ErrorMessage = "Invalid username or password.";
            return Page();
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, Username),
            new Claim(ClaimTypes.Role, "SuperAdmin")
        };

        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync(
            AdminPanelConstants.AuthScheme,
            principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(AdminPanelConstants.AuthExpirationDays)
            });

        return RedirectToPage("/Index");
    }
}
