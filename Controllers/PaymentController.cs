using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservationApi.Data;
using ReservationApi.DTOs;
using ReservationApi.Models;

namespace ReservationApi.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PaymentController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(ApplicationDbContext context, ILogger<PaymentController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Payment
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PaymentDetailsDto>>> GetPayments()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
        {
            return Unauthorized();
        }

        var query = _context.PaymentDetails
            .Include(p => p.Reservation)
            .AsQueryable();

        // If not admin, only show user's payments
        if (!User.IsInRole("Admin"))
        {
            query = query.Where(p => p.Reservation!.UserId == userId);
        }

        var payments = await query
            .Select(p => new PaymentDetailsDto
            {
                Id = p.Id,
                ReservationId = p.ReservationId,
                Amount = p.Amount,
                Currency = p.Currency,
                Status = p.Status,
                TransactionId = p.TransactionId,
                PaymentMethod = p.PaymentMethod,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt
            })
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        return payments;
    }

    // GET: api/Payment/5
    [HttpGet("{id}")]
    public async Task<ActionResult<PaymentDetailsDto>> GetPayment(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
        {
            return Unauthorized();
        }

        var payment = await _context.PaymentDetails
            .Include(p => p.Reservation)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
        {
            return NotFound();
        }

        // Check if payment belongs to user or user is admin
        if (payment.Reservation?.UserId != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        return new PaymentDetailsDto
        {
            Id = payment.Id,
            ReservationId = payment.ReservationId,
            Amount = payment.Amount,
            Currency = payment.Currency,
            Status = payment.Status,
            TransactionId = payment.TransactionId,
            PaymentMethod = payment.PaymentMethod,
            CreatedAt = payment.CreatedAt,
            UpdatedAt = payment.UpdatedAt
        };
    }

    // POST: api/Payment
    [HttpPost]
    public async Task<ActionResult<PaymentDetailsDto>> CreatePayment(CreatePaymentDto paymentDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        if (userId == null)
        {
            return Unauthorized();
        }

        // Check if reservation exists
        var reservation = await _context.Reservations
            .Include(r => r.Resource)
            .FirstOrDefaultAsync(r => r.Id == paymentDto.ReservationId);

        if (reservation == null)
        {
            return BadRequest(new { message = "Reservation not found" });
        }

        // Check if reservation belongs to user or user is admin
        if (reservation.UserId != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        // Check if payment already exists for this reservation
        var existingPayment = await _context.PaymentDetails
            .AnyAsync(p => p.ReservationId == paymentDto.ReservationId);

        if (existingPayment)
        {
            return Conflict(new { message = "Payment already exists for this reservation" });
        }

        // Here we would normally process the payment with a real payment gateway
        // For demo purposes, we'll create a mock successful payment

        // Generate a fake transaction ID
        string transactionId = $"TRANS-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";

        // Create payment record
        var payment = new PaymentDetails
        {
            ReservationId = paymentDto.ReservationId,
            Amount = paymentDto.Amount,
            Currency = paymentDto.Currency,
            PaymentMethod = paymentDto.PaymentMethod,
            Status = "Completed", // In a real system, this might start as "Pending"
            TransactionId = transactionId,
            CreatedAt = DateTime.UtcNow
        };

        _context.PaymentDetails.Add(payment);

        // Update reservation to mark as paid
        reservation.IsPaid = true;
        reservation.Status = "Confirmed";
        reservation.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Create notification for payment
        var notification = new Notification
        {
            UserId = userId,
            Title = "Payment Successful",
            Message = $"Your payment of {paymentDto.Amount} {paymentDto.Currency} for {reservation.Resource?.Name} has been processed successfully.",
            Type = "Payment",
            ReservationId = reservation.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetPayment),
            new { id = payment.Id },
            new PaymentDetailsDto
            {
                Id = payment.Id,
                ReservationId = payment.ReservationId,
                Amount = payment.Amount,
                Currency = payment.Currency,
                Status = payment.Status,
                TransactionId = payment.TransactionId,
                PaymentMethod = payment.PaymentMethod,
                CreatedAt = payment.CreatedAt
            });
    }

    // PATCH: api/Payment/5/status
    [HttpPatch("{id}/status")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdatePaymentStatus(int id, UpdatePaymentStatusDto statusDto)
    {
        var payment = await _context.PaymentDetails
            .Include(p => p.Reservation)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (payment == null)
        {
            return NotFound();
        }

        // Update payment status
        payment.Status = statusDto.Status;
        payment.UpdatedAt = DateTime.UtcNow;

        if (statusDto.TransactionId != null)
        {
            payment.TransactionId = statusDto.TransactionId;
        }

        // If payment is completed, mark reservation as paid
        if (statusDto.Status == "Completed" && payment.Reservation != null)
        {
            payment.Reservation.IsPaid = true;
            payment.Reservation.Status = "Confirmed";
            payment.Reservation.UpdatedAt = DateTime.UtcNow;

            // Create notification
            var notification = new Notification
            {
                UserId = payment.Reservation.UserId ?? string.Empty,
                Title = "Payment Completed",
                Message = $"Your payment of {payment.Amount} {payment.Currency} has been processed successfully.",
                Type = "Payment",
                ReservationId = payment.ReservationId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
        }
        // If payment is failed or refunded
        else if ((statusDto.Status == "Failed" || statusDto.Status == "Refunded") && payment.Reservation != null)
        {
            payment.Reservation.IsPaid = false;
            payment.Reservation.UpdatedAt = DateTime.UtcNow;

            // Create notification
            var notification = new Notification
            {
                UserId = payment.Reservation.UserId ?? string.Empty,
                Title = $"Payment {statusDto.Status}",
                Message = $"Your payment of {payment.Amount} {payment.Currency} has been {statusDto.Status.ToLower()}.",
                Type = "Payment",
                ReservationId = payment.ReservationId,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
        }

        await _context.SaveChangesAsync();

        return NoContent();
    }
}