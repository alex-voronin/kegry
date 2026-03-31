using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SecurityModule.Domain.Aggregates;
using SecurityModule.Domain.ValueObjects;
using SecurityModule.Infrastructure.Persistence;

namespace AdminPanelModule.RazorPages.Pages.B2BUsers;

public class IndexModel : PageModel
{
    private readonly SecurityContext _security;
    private readonly UserManager<User> _userManager;
    private const int PageSize = 20;

    public IndexModel(SecurityContext security, UserManager<User> userManager)
    {
        _security = security;
        _userManager = userManager;
    }

    public List<UserRowDto> Users { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public bool HasNextPage { get; set; }
    public int TotalCount { get; set; }
    
    [TempData]
    public string ErrorMessage { get; set; } = "";

    public async Task OnGetAsync(int page = 1)
    {
        PageNumber = page;

        var query = _security.Users.Where(u => u.Type == UserType.B2B);
        TotalCount = await query.CountAsync();

        Users = await query
            .OrderByDescending(u => u.Id)
            .Skip((page - 1) * PageSize)
            .Take(PageSize + 1)
            .Select(u => new UserRowDto
            {
                Email = u.Email ?? "",
                UserName = u.UserName ?? "",
                EmailConfirmed = u.EmailConfirmed,
                IsLocked = u.LockoutEnd.HasValue && u.LockoutEnd > DateTimeOffset.UtcNow,
                CreatedAt = DateTimeOffset.UtcNow //TODO: use real createdAt
            })
            .ToListAsync();

        HasNextPage = Users.Count > PageSize;
        if (HasNextPage) Users = Users.Take(PageSize).ToList();
    }

    public async Task<IActionResult> OnPostCreateAsync(string email, string? userName, string password)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            UserName = string.IsNullOrWhiteSpace(userName) ? email : userName,
            Type = UserType.B2B,
            RequiredAction = RequiredAction.None,
            EmailConfirmed = true 
        };

        var result = await _userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            ErrorMessage = "Failed to create user: " + string.Join(", ", result.Errors.Select(e => e.Description));
            return RedirectToPage();
        }

        TempData["Success"] = $"B2B User {email} created successfully.";
        return RedirectToPage();
    }

    public record UserRowDto
    {
        public string Email { get; init; } = "";
        public string UserName { get; init; } = "";
        public bool EmailConfirmed { get; init; }
        public bool IsLocked { get; init; }
        public DateTimeOffset CreatedAt { get; init; }
    }
}
