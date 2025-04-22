using System.ComponentModel.DataAnnotations;

namespace ReservationApi.DTOs;

public class CreateResourceDto
{
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    // Yeni eklenen alanlar
    public decimal? HourlyRate { get; set; }
    public decimal? DailyRate { get; set; }
    public int Capacity { get; set; } = 1;
    public string? Category { get; set; }
    public string? Location { get; set; }
    public string? ImageUrl { get; set; }
}

public class ResourceDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    // Yeni eklenen alanlar
    public decimal? HourlyRate { get; set; }
    public decimal? DailyRate { get; set; }
    public int Capacity { get; set; } = 1;
    public string? Category { get; set; }
    public string? Location { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
    public double? AverageRating { get; set; }

    // Duruma göre kullanılabilirlik bilgisi
    public bool IsAvailableNow { get; set; }
    public DateTime? NextAvailableTime { get; set; }
}

public class UpdateResourceDto
{
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Description { get; set; }

    // Yeni eklenen alanlar
    public decimal? HourlyRate { get; set; }
    public decimal? DailyRate { get; set; }
    public int Capacity { get; set; } = 1;
    public string? Category { get; set; }
    public string? Location { get; set; }
    public string? ImageUrl { get; set; }
    public bool IsActive { get; set; } = true;
}

// Kaynak filtreleme DTO'su
public class ResourceFilterDto
{
    public string? Category { get; set; }
    public string? SearchTerm { get; set; }
    public int? MinCapacity { get; set; }
    public int? MaxCapacity { get; set; }
    public decimal? MaxHourlyRate { get; set; }
    public decimal? MaxDailyRate { get; set; }
    public string? Location { get; set; }
    public bool? IsAvailableNow { get; set; }
    public DateTime? AvailableFrom { get; set; }
    public DateTime? AvailableTo { get; set; }
}