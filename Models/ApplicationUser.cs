using Microsoft.AspNetCore.Identity;

namespace ReservationApi.Models;

public class ApplicationUser : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}