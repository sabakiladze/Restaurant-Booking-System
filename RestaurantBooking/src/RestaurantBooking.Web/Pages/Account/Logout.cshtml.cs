using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RestaurantBooking.Web.Auth;

namespace RestaurantBooking.Web.Pages.Account;

public class LogoutModel : PageModel
{
    public IActionResult OnGet() => RedirectToPage("/Index");

    public IActionResult OnPost()
    {
        AuthCookie.Clear(Response);
        return RedirectToPage("/Index");
    }
}
