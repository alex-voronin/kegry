using AdminPanelModule.RazorPages.Config;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AdminPanelModule.RazorPages.Pages;

public class LogoutModel : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        await HttpContext.SignOutAsync(AdminPanelConstants.AuthScheme);
        return RedirectToPage("/Login");
    }
}
