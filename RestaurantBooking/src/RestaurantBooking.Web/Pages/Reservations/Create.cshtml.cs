using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RestaurantBooking.Application.Abstractions;
using RestaurantBooking.Application.Services;
using RestaurantBooking.Web.Auth;

namespace RestaurantBooking.Web.Pages.Reservations;

public class CreateModel(ReservationService reservations, IRestaurantClock clock) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public List<SelectListItem> Tables { get; private set; } = [];

    public class InputModel
    {
        [Required(ErrorMessage = "Required")]
        [Display(Name = "Table")]
        public int TableId { get; set; }

        [Required(ErrorMessage = "Required"), DataType(DataType.DateTime)]
        [Display(Name = "Time (Restaurant Time)")]
        public DateTime StartLocal { get; set; }

        [Range(1, 50, ErrorMessage = "1-50")]
        [Display(Name = "Guest Count")]
        public int GuestCount { get; set; } = 2;
    }

    public async Task OnGetAsync(CancellationToken ct)
    {
        await LoadTablesAsync(ct);
        var now = clock.ToLocal(clock.UtcNow);
        Input.StartLocal = new DateTime(now.Year, now.Month, now.Day, now.Hour, 0, 0).AddHours(2);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        await LoadTablesAsync(ct);
        if (!ModelState.IsValid) return Page();

        if (!clock.TryToUtc(Input.StartLocal, out var startUtc))
        {
            ModelState.AddModelError("Input.StartLocal", "This time does not exist.");
            return Page();
        }

        var result = await reservations.CreateAsync(User.GetUserId(), Input.TableId, startUtc, Input.GuestCount, ct);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return Page();
        }

        TempData["Message"] = "Reservation confirmed.";
        return RedirectToPage("Index");
    }

    private async Task LoadTablesAsync(CancellationToken ct) =>
        Tables = (await reservations.GetTablesAsync(ct))
            .Select(t => new SelectListItem($"Table №{t.Number} ({t.Capacity} seats)", t.Id.ToString()))
            .ToList();
}
