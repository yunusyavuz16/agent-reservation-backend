using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReservationApi.Models;

public class Review
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ReservationId { get; set; }

    [ForeignKey("ReservationId")]
    public virtual Reservation? Reservation { get; set; }

    [Required]
    public string UserId { get; set; } = string.Empty;

    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }

    [Required]
    public int Rating { get; set; } // 1-5 stars

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}