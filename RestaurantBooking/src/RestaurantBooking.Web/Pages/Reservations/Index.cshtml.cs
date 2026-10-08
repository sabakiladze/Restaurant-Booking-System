using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using RestaurantBooking.Application.Services;
using RestaurantBooking.Domain.Entities;
using RestaurantBooking.Web.Auth;

namespace RestaurantBooking.Web.Pages.Reservations;

public class IndexModel(ReservationService reservations) : PageModel
{
    public List<Reservation> Items { get; private set; } = [];

    [TempData] public string? Message { get; set; }
    [TempData] public string? Error { get; set; }

    public async Task OnGetAsync(CancellationToken ct) =>
        Items = await reservations.GetMineAsync(User.GetUserId(), ct);

    public async Task<IActionResult> OnPostCancelAsync(Guid id, CancellationToken ct)
    {
        var result = await reservations.CancelAsync(id, User.GetUserId(), isAdmin: false, ct);
        if (result.Success) Message = "Reservation canceled."; else Error = result.Error;
        return RedirectToPage();
    }
}
