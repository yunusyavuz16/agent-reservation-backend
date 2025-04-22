using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReservationApi.Models;

public class Reservation
{
    [Key]
    public int Id { get; set; }

    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    public string? Description { get; set; }

    [Required]
    public int ResourceId { get; set; }

    [ForeignKey("ResourceId")]
    public virtual Resource? Resource { get; set; }

    public string? UserId { get; set; }

    [ForeignKey("UserId")]
    public virtual ApplicationUser? User { get; set; }

    // Yeni eklenen alanlar
    public int Attendees { get; set; } = 1; // Katılımcı sayısı

    public bool IsRecurring { get; set; } = false; // Tekrarlayan rezervasyon mu?

    public string? RecurrencePattern { get; set; } // Daily, Weekly, Monthly, Custom

    public int? RecurrenceInterval { get; set; } // 1 = her hafta, 2 = iki haftada bir gibi

    public DateTime? RecurrenceEndDate { get; set; } // Tekrarın biteceği tarih

    public string Status { get; set; } = "Pending"; // Pending, Confirmed, Cancelled, Completed

    // Ödeme durumu
    public bool IsPaid { get; set; } = false;

    public decimal? Price { get; set; }

    [InverseProperty("Reservation")]
    public virtual PaymentDetails? PaymentDetails { get; set; }

    [InverseProperty("Reservation")]
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    [InverseProperty("Reservation")]
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}