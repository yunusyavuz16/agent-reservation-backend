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

    // Yeni eklenen alanlar
    public decimal? HourlyRate { get; set; } // Saatlik ücret

    public decimal? DailyRate { get; set; } // Günlük ücret

    public int Capacity { get; set; } = 1; // Maksimum kapasite

    public string? Category { get; set; } // Toplantı Odası, Araç, Ekipman gibi

    public string? Location { get; set; } // Konum bilgisi

    public string? ImageUrl { get; set; } // Görsel URL'i

    public bool IsActive { get; set; } = true; // Aktif/Pasif durumu

    public virtual ICollection<Reservation> Reservations { get; set; } = new List<Reservation>();

    // Ortalama değerlendirme puanını hesaplayan özellik (computed property)
    [System.Text.Json.Serialization.JsonIgnore]
    public double? AverageRating => Reservations
        .SelectMany(r => r.Reviews)
        .Where(r => r.Rating > 0)
        .DefaultIfEmpty()
        .Average(r => r?.Rating ?? 0);
}