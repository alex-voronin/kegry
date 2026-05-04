using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecurityModule.Domain.Aggregates;
using SecurityModule.Infrastructure.Persistence;
using SecurityModule.Domain.ValueObjects;
using UserProfileModule.Infrastructure.Persistence;

namespace AdminPanelModule.RazorPages.Pages.B2CUsers;

public class IndexModel : PageModel
{
    private readonly SecurityContext _securityContext;
    private readonly UserProfileContext _userProfileContext;

    public IndexModel(SecurityContext securityContext, UserProfileContext userProfileContext)
    {
        _securityContext = securityContext;
        _userProfileContext = userProfileContext;
    }

    [BindProperty(SupportsGet = true)]
    public string? SearchQuery { get; set; }

    public User? FoundUser { get; set; }
    public UserProfileModule.Domain.Aggregates.User? UserProfile { get; set; }
    public string? Message { get; set; }
    public bool IsError { get; set; }

    public async Task OnGetAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery))
        {
            return;
        }

        var query = SearchQuery.Trim();
        if (Guid.TryParse(query, out var userId))
        {
            FoundUser = await _securityContext.Users.FindAsync(userId);
        }
        else
        {
            var normalizedEmail = query.ToUpperInvariant();
            FoundUser = await _securityContext.Users
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail);
        }

        if (FoundUser == null)
        {
            Message = "User not found.";
            IsError = true;
        }
        else
        {
            UserProfile = await _userProfileContext.Users.FindAsync(FoundUser.Id);
        }
    }

    public async Task<IActionResult> OnPostBanAsync(Guid userId)
    {
        var user = await _securityContext.Users.FindAsync(userId);
        if (user == null)
        {
            Message = "User not found.";
            IsError = true;
            return Page();
        }

        user.IsBanned = true;
        await _securityContext.SaveChangesAsync();

        Message = $"User {user.Email} has been banned.";
        IsError = false;
        FoundUser = user;
        SearchQuery = user.Email;

        return Page();
    }

    public async Task<IActionResult> OnPostUnbanAsync(Guid userId)
    {
        var user = await _securityContext.Users.FindAsync(userId);
        if (user == null)
        {
            Message = "User not found.";
            IsError = true;
            return Page();
        }

        user.IsBanned = false;
        await _securityContext.SaveChangesAsync();

        Message = $"User {user.Email} has been un-banned.";
        IsError = false;
        FoundUser = user;
        SearchQuery = user.Email;

        return Page();
    }

    public async Task<IActionResult> OnPostConvertAsync(Guid userId)
    {
        var user = await _securityContext.Users.FindAsync(userId);
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
        await _securityContext.SaveChangesAsync();

        Message = $"User {user.Email} successfully converted to B2B!";
        IsError = false;
        FoundUser = user;
        SearchQuery = user.Email;

        return Page();
    }

    public async Task<IActionResult> OnPostMakePublicAsync(Guid userId)
    {
        var profile = await _userProfileContext.Users.FindAsync(userId);
        if (profile == null)
        {
            Message = "User profile not found.";
            IsError = true;
            return Page();
        }

        profile.IsPublic = true;
        await _userProfileContext.SaveChangesAsync();

        Message = "User profile is now public.";
        IsError = false;

        // Refresh data
        FoundUser = await _securityContext.Users.FindAsync(userId);
        UserProfile = profile;
        SearchQuery = FoundUser?.Email;

        return Page();
    }

    public async Task<IActionResult> OnPostMakePrivateAsync(Guid userId)
    {
        var profile = await _userProfileContext.Users.FindAsync(userId);
        if (profile == null)
        {
            Message = "User profile not found.";
            IsError = true;
            return Page();
        }

        profile.IsPublic = false;
        await _userProfileContext.SaveChangesAsync();

        Message = "User profile is now private.";
        IsError = false;

        // Refresh data
        FoundUser = await _securityContext.Users.FindAsync(userId);
        UserProfile = profile;
        SearchQuery = FoundUser?.Email;

        return Page();
    }
}
