using System.ComponentModel.DataAnnotations;

namespace ReservationApi.DTOs;

public class CreateReservationDto
{
    [Required]
    public DateTime StartTime { get; set; }

    [Required]
    public DateTime EndTime { get; set; }

    public string? Description { get; set; }

    [Required]
    public int ResourceId { get; set; }

    // Yeni eklenen alanlar
    public int Attendees { get; set; } = 1;

    public bool IsRecurring { get; set; } = false;

    public string? RecurrencePattern { get; set; }

    public int? RecurrenceInterval { get; set; }

    public DateTime? RecurrenceEndDate { get; set; }
}

public class ReservationDto
{
    public int Id { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Description { get; set; }
    public int ResourceId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
    public string? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    // Yeni eklenen alanlar
    public int Attendees { get; set; } = 1;
    public bool IsRecurring { get; set; } = false;
    public string? RecurrencePattern { get; set; }
    public int? RecurrenceInterval { get; set; }
    public DateTime? RecurrenceEndDate { get; set; }
    public string Status { get; set; } = "Pending";
    public bool IsPaid { get; set; } = false;
    public decimal? Price { get; set; }

    // İlişkili veriler
    public PaymentDetailsDto? PaymentDetails { get; set; }
    public List<ReviewDto>? Reviews { get; set; }
    public double? AverageRating { get; set; }

    // Kaynak detayları
    public int ResourceCapacity { get; set; }
    public string? ResourceImageUrl { get; set; }
    public string? ResourceLocation { get; set; }
}

public class UpdateReservationDto
{
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
    public string? Description { get; set; }
    public int ResourceId { get; set; }

    // Yeni eklenen alanlar
    public int Attendees { get; set; } = 1;
    public string Status { get; set; } = "Pending";
}

// Rezervasyon durumu güncelleme DTO'su
public class UpdateReservationStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty;
}

// Tarih aralığı sorgulama DTO'su
public class DateRangeDto
{
    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }

    public int? ResourceId { get; set; }
}