using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using MyCal.Infrastructure.Identity;

namespace MyCal.Web.Pages.Account;

[AllowAnonymous]
public sealed class AcceptModel(BetaAccounts accounts) : PageModel
{
    [BindProperty(SupportsGet = true), Required] public string Token { get; set; } = "";
    [BindProperty, Required, StringLength(128, MinimumLength = 12)] public string Password { get; set; } = "";
    [BindProperty, Required, Compare(nameof(Password))] public string ConfirmPassword { get; set; } = "";
    public bool Success { get; set; }
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();
        try
        {
            await accounts.RedeemAsync(Token, Password, HttpContext.RequestAborted);
            Success = true;
        }
        catch (ValidationException e) { ModelState.AddModelError("", e.Message); }
        return Page();
    }
}
