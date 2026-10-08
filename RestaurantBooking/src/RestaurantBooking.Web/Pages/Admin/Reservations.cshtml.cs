using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RestaurantBooking.Application.Services;
using RestaurantBooking.Domain.Entities;
using RestaurantBooking.Web.Auth;

namespace RestaurantBooking.Web.Pages.Admin;

public class ReservationsModel(ReservationService reservations) : PageModel
{
    public List<Reservation> Items { get; private set; } = [];

    [TempData] public string? Message { get; set; }
    [TempData] public string? Error { get; set; }

    public async Task OnGetAsync(CancellationToken ct) =>
        Items = await reservations.GetUpcomingAsync(ct);

    public async Task<IActionResult> OnPostCancelAsync(Guid id, CancellationToken ct)
    {
        var result = await reservations.CancelAsync(id, User.GetUserId(), isAdmin: true, ct);
        if (result.Success) Message = "Reservation canceled ."; else Error = result.Error;
        return RedirectToPage();
    }
}
