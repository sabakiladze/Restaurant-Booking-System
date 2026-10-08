using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RestaurantBooking.Application.Services;

namespace RestaurantBooking.Web.Pages.Account;

public class RegisterModel(AuthService auth) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();

    public class InputModel
    {
        [Required(ErrorMessage = "Required"), StringLength(100)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "Required"), EmailAddress(ErrorMessage = "Invalid email")]
        [Display(Name = "Email")]
        public string Email { get; set; } = "";

        [Required(ErrorMessage = "Required"), DataType(DataType.Password)]
        [MinLength(AuthService.MinPasswordLength, ErrorMessage = "Minimum 8 characters")]
        [Display(Name = "Password")]
        public string Password { get; set; } = "";

        [DataType(DataType.Password), Compare(nameof(Password), ErrorMessage = "Passwords do not match")]
        [Display(Name = "Confirm Password")]
        public string ConfirmPassword { get; set; } = "";
    }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) return Page();

        var result = await auth.RegisterAsync(Input.FullName, Input.Email, Input.Password, ct);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return Page();
        }
        return RedirectToPage("Login", new { registered = true });
    }
}
