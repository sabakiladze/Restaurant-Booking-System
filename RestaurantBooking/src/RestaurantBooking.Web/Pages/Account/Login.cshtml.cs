using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using RestaurantBooking.Application.Services;
using RestaurantBooking.Infrastructure.Auth;
using RestaurantBooking.Web.Auth;

namespace RestaurantBooking.Web.Pages.Account;

public class LoginModel(AuthService auth, IOptions<JwtSettings> jwt) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty(SupportsGet = true)] public string? ReturnUrl { get; set; }
    [BindProperty(SupportsGet = true)] public bool Registered { get; set; }

    public class InputModel
    {
        [Required(ErrorMessage = "Required"), EmailAddress(ErrorMessage = "Invalid email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Required"), DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) return Page();

        var result = await auth.LoginAsync(Input.Email, Input.Password, ct);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return Page();
        }

        AuthCookie.Set(Response, result.Value!, jwt.Value.ExpiryMinutes);

        if (!string.IsNullOrEmpty(ReturnUrl) && Url.IsLocalUrl(ReturnUrl))
            return LocalRedirect(ReturnUrl);

        return RedirectToPage("/Reservations/Index");
    }
}
