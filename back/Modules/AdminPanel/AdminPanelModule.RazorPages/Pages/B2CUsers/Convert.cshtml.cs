using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecurityModule.Domain.Aggregates;
using SecurityModule.Infrastructure.Persistence;
using SecurityModule.Domain.ValueObjects;

namespace AdminPanelModule.RazorPages.Pages.B2CUsers;

public class ConvertModel : PageModel
{
    private readonly SecurityContext _security;

    public ConvertModel(SecurityContext security)
    {
        _security = security;
    }

    [BindProperty(SupportsGet = true)]
    public string? SearchEmail { get; set; }

    public User? FoundUser { get; set; }
    public string? Message { get; set; }
    public bool IsError { get; set; }

    public async Task OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchEmail))
        {
            return;
        }

        var normalizedEmail = SearchEmail.Trim().ToUpperInvariant();
        FoundUser = await _security.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);

        if (FoundUser == null)
        {
            Message = $"User with email '{SearchEmail}' not found.";
            IsError = true;
        }
    }

    public async Task<IActionResult> OnPostConvertAsync(Guid userId)
    {
        var user = await _security.Users.FindAsync(userId);
        if (user == null)
        {
            Message = "User not found.";
            IsError = true;
            return Page();
        }

        if (user.Type == UserType.B2B)
        {
            Message = "User is already a B2B account.";
            return Page();
        }

        user.Type = UserType.B2B;
        await _security.SaveChangesAsync();

        Message = $"User {user.Email} successfully converted to B2B!";
        IsError = false;
        FoundUser = user;
        SearchEmail = user.Email;

        return Page();
    }
}
