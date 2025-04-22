using System.ComponentModel.DataAnnotations;

namespace ReservationApi.DTOs;

public class CreatePaymentDto
{
    [Required]
    public int ReservationId { get; set; }

    [Required]
    public decimal Amount { get; set; }

    public string Currency { get; set; } = "USD";

    [Required]
    public string PaymentMethod { get; set; } = string.Empty; // Credit Card, PayPal, etc.

    // Kredi kartı bilgileri (gerçekte bu bilgiler doğrudan saklanmamalı)
    public string? CardNumber { get; set; }
    public string? CardHolderName { get; set; }
    public string? ExpiryMonth { get; set; }
    public string? ExpiryYear { get; set; }
    public string? CVV { get; set; }
}

public class PaymentDetailsDto
{
    public int Id { get; set; }
    public int ReservationId { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "USD";
    public string Status { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public string? PaymentMethod { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class UpdatePaymentStatusDto
{
    [Required]
    public string Status { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
}