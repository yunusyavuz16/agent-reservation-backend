using System.ComponentModel.DataAnnotations;

namespace ReservationApi.Models;

public class Resource
{
    [Key]
    public int Id { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();
}