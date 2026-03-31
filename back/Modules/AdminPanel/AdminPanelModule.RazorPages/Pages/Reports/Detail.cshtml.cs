using FeedModule.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AdminPanelModule.RazorPages.Pages.Reports;

public class DetailModel : PageModel
{
    private readonly FeedContext _feed;

    public DetailModel(FeedContext feed) => _feed = feed;

    public string TargetUser { get; set; } = "Unknown User";
    public List<ReportDto> Reports { get; set; } = [];

    public async Task OnGetAsync(Guid id)
    {
        var targetUserObj = await _feed.Users.FirstOrDefaultAsync(u => u.Id == id);
        if (targetUserObj != null)
        {
            TargetUser = targetUserObj.FullName;
        }

        var relations = await _feed.FeedUserRelations
            .Where(r => r.OtherUserId == id && r.ReportMessage != null && r.ReportMessage != "")
            .OrderByDescending(r => r.InvitedAt)
            .ToListAsync();

        var reporterIds = relations.Select(r => r.MyUserId).ToList();
        var reporters = await _feed.Users
            .Where(u => reporterIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id);

        Reports = relations.Select(r => new ReportDto
        {
            Date = r.InvitedAt, // using InvitedAt as proxy for report date
            ReporterId = r.MyUserId,
            ReporterName = reporters.TryGetValue(r.MyUserId, out var reporter) ? reporter.FullName : "Unknown User",
            Message = r.ReportMessage ?? ""
        }).ToList();
    }

    public class ReportDto
    {
        public DateTimeOffset Date { get; set; }
        public Guid ReporterId { get; set; }
        public string ReporterName { get; set; } = "";
        public string Message { get; set; } = "";
    }
}
