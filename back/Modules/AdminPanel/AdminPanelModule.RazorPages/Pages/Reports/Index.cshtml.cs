using FeedModule.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AdminPanelModule.RazorPages.Pages.Reports;

public class IndexModel : PageModel
{
    private readonly FeedContext _feed;
    private const int PageSize = 20;

    public IndexModel(FeedContext feed) => _feed = feed;

    public List<ReportedUserDto> ReportedUsers { get; set; } = [];
    public int PageNumber { get; set; } = 1;
    public bool HasNextPage { get; set; }

    public async Task OnGetAsync(int page = 1)
    {
        PageNumber = page;

        var reportedGroups = await _feed.FeedUserRelations
            .Where(r => r.ReportMessage != null && r.ReportMessage != "")
            .GroupBy(r => r.OtherUserId)
            .Select(g => new
            {
                UserId = g.Key,
                ReportCount = g.Count(),
                LatestReportDate = g.Max(x => x.InvitedAt) // Or maybe just LastSeenAt/InviteExpiresAt? 
                // We'll just take Max of InvitedAt as a proxy for the timeline since there is no ReportCreatedAt field
            })
            .OrderByDescending(g => g.ReportCount)
            .Skip((page - 1) * PageSize)
            .Take(PageSize + 1)
            .ToListAsync();

        HasNextPage = reportedGroups.Count > PageSize;
        if (HasNextPage) reportedGroups = reportedGroups.Take(PageSize).ToList();

        var userIds = reportedGroups.Select(g => g.UserId).ToList();

        // Fetch users info
        var users = await _feed.Users
            .Where(u => userIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        ReportedUsers = reportedGroups.Select(g => new ReportedUserDto
        {
            UserId = g.UserId,
            ReportCount = g.ReportCount,
            LatestReportDate = g.LatestReportDate,
            FullName = users.TryGetValue(g.UserId, out var u) ? u.FullName : "Unknown User",
            IsPublic = users.TryGetValue(g.UserId, out var u2) && u2.IsPublic
        }).ToList();
    }

    public class ReportedUserDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = "";
        public bool IsPublic { get; set; }
        public int ReportCount { get; set; }
        public DateTimeOffset LatestReportDate { get; set; }
    }
}
