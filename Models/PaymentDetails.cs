using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ReservationApi.Models;

public class PaymentDetails
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int ReservationId { get; set; }

    [ForeignKey("ReservationId")]
    public virtual Reservation? Reservation { get; set; }

    [Required]
    public decimal Amount { get; set; }

    [Required]
    public string Currency { get; set; } = "USD";

    [Required]
    public string Status { get; set; } = "Pending"; // Pending, Completed, Failed, Refunded

    public string? TransactionId { get; set; }

    public string? PaymentMethod { get; set; } // Credit Card, PayPal, etc.

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }
}