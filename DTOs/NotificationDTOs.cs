using System.ComponentModel.DataAnnotations;

namespace ReservationApi.DTOs;

public class NotificationDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? ReservationId { get; set; }
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateNotificationDto
{
    [Required]
    public string UserId { get; set; } = string.Empty;

    [Required]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Message { get; set; } = string.Empty;

    [Required]
    public string Type { get; set; } = string.Empty;

    public int? ReservationId { get; set; }
}

public class UpdateNotificationDto
{
    public bool IsRead { get; set; } = true;
}