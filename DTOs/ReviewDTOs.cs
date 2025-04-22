using System.ComponentModel.DataAnnotations;

namespace ReservationApi.DTOs;

public class ReviewDto
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Resource bilgileri
    public int ResourceId { get; set; }
    public string ResourceName { get; set; } = string.Empty;
}

public class CreateReviewDto
{
    [Required]
    public int ReservationId { get; set; }

    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Rating { get; set; }

    public string? Comment { get; set; }
}

public class UpdateReviewDto
{
    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5")]
    public int Rating { get; set; }

    public string? Comment { get; set; }
}