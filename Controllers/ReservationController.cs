using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservationApi.Data;
using ReservationApi.DTOs;
using ReservationApi.Models;

namespace ReservationApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReservationController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReservationController> _logger;

    public ReservationController(ApplicationDbContext context, ILogger<ReservationController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Reservation
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReservationDto>>> GetReservations()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var query = _context.Reservations
            .Include(r => r.Resource)
            .Include(r => r.User)
            .Include(r => r.PaymentDetails)
            .Include(r => r.Reviews)
            .AsQueryable();

        // Eğer admin değilse, sadece kendi rezervasyonlarını görebilir
        if (!User.IsInRole("Admin"))
        {
            query = query.Where(r => r.UserId == userId);
        }

        var reservations = await query
            .Select(r => new ReservationDto
            {
                Id = r.Id,
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                Description = r.Description,
                ResourceId = r.ResourceId,
                ResourceName = r.Resource != null ? r.Resource.Name : string.Empty,
                UserId = r.UserId,
                UserName = r.User != null ? $"{r.User.FirstName} {r.User.LastName}" : string.Empty,
                CreatedAt = r.CreatedAt,
                Attendees = r.Attendees,
                IsRecurring = r.IsRecurring,
                RecurrencePattern = r.RecurrencePattern,
                RecurrenceInterval = r.RecurrenceInterval,
                RecurrenceEndDate = r.RecurrenceEndDate,
                Status = r.Status,
                IsPaid = r.IsPaid,
                Price = r.Price,
                PaymentDetails = r.PaymentDetails != null ? new PaymentDetailsDto
                {
                    Id = r.PaymentDetails.Id,
                    ReservationId = r.PaymentDetails.ReservationId,
                    Amount = r.PaymentDetails.Amount,
                    Currency = r.PaymentDetails.Currency,
                    Status = r.PaymentDetails.Status,
                    TransactionId = r.PaymentDetails.TransactionId,
                    PaymentMethod = r.PaymentDetails.PaymentMethod,
                    CreatedAt = r.PaymentDetails.CreatedAt,
                    UpdatedAt = r.PaymentDetails.UpdatedAt
                } : null,
                Reviews = r.Reviews.Select(review => new ReviewDto
                {
                    Id = review.Id,
                    ReservationId = review.ReservationId,
                    UserId = review.UserId,
                    UserName = review.User != null ? $"{review.User.FirstName} {review.User.LastName}" : string.Empty,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt,
                    UpdatedAt = review.UpdatedAt,
                    ResourceId = r.ResourceId,
                    ResourceName = r.Resource != null ? r.Resource.Name : string.Empty
                }).ToList(),
                AverageRating = r.Reviews.Any() ? r.Reviews.Average(rv => rv.Rating) : null,
                ResourceCapacity = r.Resource != null ? r.Resource.Capacity : 0,
                ResourceImageUrl = r.Resource != null ? r.Resource.ImageUrl : null,
                ResourceLocation = r.Resource != null ? r.Resource.Location : null
            })
            .OrderByDescending(r => r.StartTime)
            .ToListAsync();

        return reservations;
    }

    // GET: api/Reservation/upcoming
    [Authorize]
    [HttpGet("upcoming")]
    public async Task<ActionResult<IEnumerable<ReservationDto>>> GetUpcomingReservations()
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var query = _context.Reservations
            .Include(r => r.Resource)
            .Include(r => r.User)
            .Include(r => r.PaymentDetails)
            .Include(r => r.Reviews)
            .Where(r => r.StartTime > DateTime.UtcNow);

        // Eğer admin değilse, sadece kendi rezervasyonlarını görebilir
        if (!User.IsInRole("Admin"))
        {
            query = query.Where(r => r.UserId == userId);
        }

        var reservations = await query
            .Select(r => new ReservationDto
            {
                Id = r.Id,
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                Description = r.Description,
                ResourceId = r.ResourceId,
                ResourceName = r.Resource != null ? r.Resource.Name : string.Empty,
                UserId = r.UserId,
                UserName = r.User != null ? $"{r.User.FirstName} {r.User.LastName}" : string.Empty,
                CreatedAt = r.CreatedAt,
                Attendees = r.Attendees,
                IsRecurring = r.IsRecurring,
                RecurrencePattern = r.RecurrencePattern,
                RecurrenceInterval = r.RecurrenceInterval,
                RecurrenceEndDate = r.RecurrenceEndDate,
                Status = r.Status,
                IsPaid = r.IsPaid,
                Price = r.Price,
                PaymentDetails = r.PaymentDetails != null ? new PaymentDetailsDto
                {
                    Id = r.PaymentDetails.Id,
                    ReservationId = r.PaymentDetails.ReservationId,
                    Amount = r.PaymentDetails.Amount,
                    Currency = r.PaymentDetails.Currency,
                    Status = r.PaymentDetails.Status,
                    TransactionId = r.PaymentDetails.TransactionId,
                    PaymentMethod = r.PaymentDetails.PaymentMethod,
                    CreatedAt = r.PaymentDetails.CreatedAt,
                    UpdatedAt = r.PaymentDetails.UpdatedAt
                } : null,
                Reviews = r.Reviews.Select(review => new ReviewDto
                {
                    Id = review.Id,
                    ReservationId = review.ReservationId,
                    UserId = review.UserId,
                    UserName = review.User != null ? $"{review.User.FirstName} {review.User.LastName}" : string.Empty,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt,
                    UpdatedAt = review.UpdatedAt,
                    ResourceId = r.ResourceId,
                    ResourceName = r.Resource != null ? r.Resource.Name : string.Empty
                }).ToList(),
                AverageRating = r.Reviews.Any() ? r.Reviews.Average(rv => rv.Rating) : null,
                ResourceCapacity = r.Resource != null ? r.Resource.Capacity : 0,
                ResourceImageUrl = r.Resource != null ? r.Resource.ImageUrl : null,
                ResourceLocation = r.Resource != null ? r.Resource.Location : null
            })
            .OrderBy(r => r.StartTime)
            .ToListAsync();

        return reservations;
    }

    // GET: api/Reservation/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ReservationDto>> GetReservation(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var reservation = await _context.Reservations
            .Include(r => r.Resource)
            .Include(r => r.User)
            .Include(r => r.PaymentDetails)
            .Include(r => r.Reviews)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
        {
            return NotFound();
        }

        // Eğer admin değilse ve kendi rezervasyonu değilse, erişim engellenir
        if (!User.IsInRole("Admin") && reservation.UserId != userId)
        {
            return Forbid();
        }

        return new ReservationDto
        {
            Id = reservation.Id,
            StartTime = reservation.StartTime,
            EndTime = reservation.EndTime,
            Description = reservation.Description,
            ResourceId = reservation.ResourceId,
            ResourceName = reservation.Resource != null ? reservation.Resource.Name : string.Empty,
            UserId = reservation.UserId,
            UserName = reservation.User != null ? $"{reservation.User.FirstName} {reservation.User.LastName}" : string.Empty,
            CreatedAt = reservation.CreatedAt,
            Attendees = reservation.Attendees,
            IsRecurring = reservation.IsRecurring,
            RecurrencePattern = reservation.RecurrencePattern,
            RecurrenceInterval = reservation.RecurrenceInterval,
            RecurrenceEndDate = reservation.RecurrenceEndDate,
            Status = reservation.Status,
            IsPaid = reservation.IsPaid,
            Price = reservation.Price,
            PaymentDetails = reservation.PaymentDetails != null ? new PaymentDetailsDto
            {
                Id = reservation.PaymentDetails.Id,
                ReservationId = reservation.PaymentDetails.ReservationId,
                Amount = reservation.PaymentDetails.Amount,
                Currency = reservation.PaymentDetails.Currency,
                Status = reservation.PaymentDetails.Status,
                TransactionId = reservation.PaymentDetails.TransactionId,
                PaymentMethod = reservation.PaymentDetails.PaymentMethod,
                CreatedAt = reservation.PaymentDetails.CreatedAt,
                UpdatedAt = reservation.PaymentDetails.UpdatedAt
            } : null,
            Reviews = reservation.Reviews.Select(review => new ReviewDto
            {
                Id = review.Id,
                ReservationId = review.ReservationId,
                UserId = review.UserId,
                UserName = review.User != null ? $"{review.User.FirstName} {review.User.LastName}" : string.Empty,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
                UpdatedAt = review.UpdatedAt,
                ResourceId = reservation.ResourceId,
                ResourceName = reservation.Resource != null ? reservation.Resource.Name : string.Empty
            }).ToList(),
            AverageRating = reservation.Reviews.Any() ? reservation.Reviews.Average(rv => rv.Rating) : null,
            ResourceCapacity = reservation.Resource != null ? reservation.Resource.Capacity : 0,
            ResourceImageUrl = reservation.Resource != null ? reservation.Resource.ImageUrl : null,
            ResourceLocation = reservation.Resource != null ? reservation.Resource.Location : null
        };
    }

    // POST: api/Reservation
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ReservationDto>> CreateReservation(CreateReservationDto createReservationDto)
    {
        // Kullanıcı kimliğini al
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        // Kaynak kontrolü
        var resource = await _context.Resources.FindAsync(createReservationDto.ResourceId);
        if (resource == null)
        {
            return BadRequest("The specified resource does not exist.");
        }

        // Kapasite kontrolü
        if (createReservationDto.Attendees > resource.Capacity)
        {
            return BadRequest($"The resource capacity is {resource.Capacity}, but you requested for {createReservationDto.Attendees} attendees.");
        }

        // Çakışma kontrolü
        var conflict = await CheckReservationConflict(createReservationDto.ResourceId,
            createReservationDto.StartTime, createReservationDto.EndTime, null);

        if (conflict)
        {
            return BadRequest("There is already a reservation for this resource during the requested time period.");
        }

        // Fiyat hesaplama
        decimal? price = CalculatePrice(resource, createReservationDto.StartTime, createReservationDto.EndTime);

        // Rezervasyon oluşturma
        var reservation = new Reservation
        {
            StartTime = createReservationDto.StartTime,
            EndTime = createReservationDto.EndTime,
            Description = createReservationDto.Description,
            ResourceId = createReservationDto.ResourceId,
            UserId = userId,
            Attendees = createReservationDto.Attendees,
            IsRecurring = createReservationDto.IsRecurring,
            RecurrencePattern = createReservationDto.RecurrencePattern,
            RecurrenceInterval = createReservationDto.RecurrenceInterval,
            RecurrenceEndDate = createReservationDto.RecurrenceEndDate,
            Status = "Pending",
            Price = price,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        // Tekrarlayan rezervasyonlar oluşturma
        if (createReservationDto.IsRecurring &&
            !string.IsNullOrEmpty(createReservationDto.RecurrencePattern) &&
            createReservationDto.RecurrenceInterval.HasValue &&
            createReservationDto.RecurrenceEndDate.HasValue)
        {
            await CreateRecurringReservations(reservation, createReservationDto.RecurrencePattern,
                createReservationDto.RecurrenceInterval.Value, createReservationDto.RecurrenceEndDate.Value);
        }

        // Yeni rezervasyon için bildirim oluşturma
        var notification = new Notification
        {
            UserId = userId,
            Title = "New Reservation",
            Message = $"Your reservation for {resource.Name} has been created and is pending confirmation.",
            Type = "Reservation",
            ReservationId = reservation.Id,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        // Yeni oluşturulan rezervasyon detaylarını al
        var createdReservation = await _context.Reservations
            .Include(r => r.Resource)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == reservation.Id);

        return CreatedAtAction(
            nameof(GetReservation),
            new { id = reservation.Id },
            new ReservationDto
            {
                Id = reservation.Id,
                StartTime = reservation.StartTime,
                EndTime = reservation.EndTime,
                Description = reservation.Description,
                ResourceId = reservation.ResourceId,
                ResourceName = createdReservation?.Resource?.Name ?? string.Empty,
                UserId = reservation.UserId,
                UserName = createdReservation?.User != null ? $"{createdReservation.User.FirstName} {createdReservation.User.LastName}" : string.Empty,
                CreatedAt = reservation.CreatedAt,
                Attendees = reservation.Attendees,
                IsRecurring = reservation.IsRecurring,
                RecurrencePattern = reservation.RecurrencePattern,
                RecurrenceInterval = reservation.RecurrenceInterval,
                RecurrenceEndDate = reservation.RecurrenceEndDate,
                Status = reservation.Status,
                IsPaid = reservation.IsPaid,
                Price = reservation.Price,
                ResourceCapacity = createdReservation?.Resource?.Capacity ?? 0,
                ResourceImageUrl = createdReservation?.Resource?.ImageUrl,
                ResourceLocation = createdReservation?.Resource?.Location
            });
    }

    // PUT: api/Reservation/5
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReservation(int id, UpdateReservationDto updateReservationDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var reservation = await _context.Reservations.FindAsync(id);
        if (reservation == null)
        {
            return NotFound();
        }

        // Eğer admin değilse ve kendi rezervasyonu değilse, erişim engellenir
        if (!User.IsInRole("Admin") && reservation.UserId != userId)
        {
            return Forbid();
        }

        // Kaynak kontrolü
        var resource = await _context.Resources.FindAsync(updateReservationDto.ResourceId);
        if (resource == null)
        {
            return BadRequest("The specified resource does not exist.");
        }

        // Kapasite kontrolü
        if (updateReservationDto.Attendees > resource.Capacity)
        {
            return BadRequest($"The resource capacity is {resource.Capacity}, but you requested for {updateReservationDto.Attendees} attendees.");
        }

        // Çakışma kontrolü
        var conflict = await CheckReservationConflict(updateReservationDto.ResourceId,
            updateReservationDto.StartTime, updateReservationDto.EndTime, id);

        if (conflict)
        {
            return BadRequest("There is already a reservation for this resource during the requested time period.");
        }

        // Fiyat hesaplama (kaynak değişirse)
        if (reservation.ResourceId != updateReservationDto.ResourceId ||
            reservation.StartTime != updateReservationDto.StartTime ||
            reservation.EndTime != updateReservationDto.EndTime)
        {
            reservation.Price = CalculatePrice(resource, updateReservationDto.StartTime, updateReservationDto.EndTime);
        }

        // Rezervasyonu güncelleme
        reservation.StartTime = updateReservationDto.StartTime;
        reservation.EndTime = updateReservationDto.EndTime;
        reservation.Description = updateReservationDto.Description;
        reservation.ResourceId = updateReservationDto.ResourceId;
        reservation.Attendees = updateReservationDto.Attendees;
        reservation.Status = updateReservationDto.Status;
        reservation.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();

            // Bildirim oluşturma
            var notification = new Notification
            {
                UserId = reservation.UserId ?? string.Empty,
                Title = "Reservation Updated",
                Message = $"Your reservation for {resource.Name} has been updated.",
                Type = "Reservation",
                ReservationId = reservation.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ReservationExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // PATCH: api/Reservation/5/status
    [Authorize]
    [HttpPatch("{id}/status")]
    public async Task<IActionResult> UpdateReservationStatus(int id, UpdateReservationStatusDto statusDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var reservation = await _context.Reservations
            .Include(r => r.Resource)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
        {
            return NotFound();
        }

        // Eğer admin değilse ve kendi rezervasyonu değilse, erişim engellenir
        if (!User.IsInRole("Admin") && reservation.UserId != userId)
        {
            return Forbid();
        }

        // Status kontrolü
        if (statusDto.Status != "Pending" && statusDto.Status != "Confirmed" &&
            statusDto.Status != "Cancelled" && statusDto.Status != "Completed")
        {
            return BadRequest("Invalid status value. Allowed values: Pending, Confirmed, Cancelled, Completed");
        }

        reservation.Status = statusDto.Status;
        reservation.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();

            // Bildirim oluşturma
            var notification = new Notification
            {
                UserId = reservation.UserId ?? string.Empty,
                Title = "Reservation Status Changed",
                Message = $"Your reservation for {reservation.Resource?.Name} is now {statusDto.Status}.",
                Type = "Reservation",
                ReservationId = reservation.Id,
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ReservationExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // DELETE: api/Reservation/5
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReservation(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

        var reservation = await _context.Reservations
            .Include(r => r.Resource)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
        {
            return NotFound();
        }

        // Eğer admin değilse ve kendi rezervasyonu değilse, erişim engellenir
        if (!User.IsInRole("Admin") && reservation.UserId != userId)
        {
            return Forbid();
        }

        // İlişkili ödemeleri silme
        var payments = await _context.PaymentDetails.Where(p => p.ReservationId == id).ToListAsync();
        _context.PaymentDetails.RemoveRange(payments);

        // İlişkili bildirimleri silme
        var notifications = await _context.Notifications.Where(n => n.ReservationId == id).ToListAsync();
        _context.Notifications.RemoveRange(notifications);

        // İlişkili yorumları silme
        var reviews = await _context.Reviews.Where(r => r.ReservationId == id).ToListAsync();
        _context.Reviews.RemoveRange(reviews);

        // Rezervasyonu silme
        _context.Reservations.Remove(reservation);

        await _context.SaveChangesAsync();

        // Bildirim oluşturma (kullanıcı için)
        if (userId != null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = "Reservation Deleted",
                Message = $"Your reservation for {reservation.Resource?.Name} has been deleted.",
                Type = "Reservation",
                CreatedAt = DateTime.UtcNow
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();
        }

        return NoContent();
    }

    // GET: api/Reservation/availability
    [HttpGet("availability")]
    public async Task<ActionResult<IEnumerable<ResourceDto>>> CheckAvailability([FromQuery] DateRangeDto dateRange)
    {
        if (dateRange.StartDate >= dateRange.EndDate)
        {
            return BadRequest("End date must be after start date.");
        }

        var resources = await _context.Resources
            .Where(r => r.IsActive)
            .Select(r => new ResourceDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                HourlyRate = r.HourlyRate,
                DailyRate = r.DailyRate,
                Capacity = r.Capacity,
                Category = r.Category,
                Location = r.Location,
                ImageUrl = r.ImageUrl,
                IsActive = r.IsActive,
                AverageRating = r.Reservations
                    .SelectMany(res => res.Reviews)
                    .Where(rev => rev.Rating > 0)
                    .Select(rev => rev.Rating)
                    .DefaultIfEmpty()
                    .Average(),
                IsAvailableNow = !r.Reservations.Any(res =>
                    (dateRange.StartDate < res.EndTime && dateRange.EndDate > res.StartTime) &&
                    res.Status != "Cancelled"),
                NextAvailableTime = r.Reservations
                    .Where(res => res.StartTime > dateRange.StartDate && res.Status != "Cancelled")
                    .OrderBy(res => res.StartTime)
                    .Select(res => res.StartTime)
                    .FirstOrDefault()
            })
            .ToListAsync();

        // Filtrele: Belirli bir kaynak ID'si verilmişse
        if (dateRange.ResourceId.HasValue)
        {
            resources = resources.Where(r => r.Id == dateRange.ResourceId).ToList();
        }

        return resources;
    }

    // GET: api/Reservation/user/{userId}
    [Authorize(Roles = "Admin")]
    [HttpGet("user/{userId}")]
    public async Task<ActionResult<IEnumerable<ReservationDto>>> GetUserReservations(string userId)
    {
        var reservations = await _context.Reservations
            .Where(r => r.UserId == userId)
            .Include(r => r.Resource)
            .Include(r => r.User)
            .Include(r => r.PaymentDetails)
            .Include(r => r.Reviews)
            .Select(r => new ReservationDto
            {
                Id = r.Id,
                StartTime = r.StartTime,
                EndTime = r.EndTime,
                Description = r.Description,
                ResourceId = r.ResourceId,
                ResourceName = r.Resource != null ? r.Resource.Name : string.Empty,
                UserId = r.UserId,
                UserName = r.User != null ? $"{r.User.FirstName} {r.User.LastName}" : string.Empty,
                CreatedAt = r.CreatedAt,
                Attendees = r.Attendees,
                IsRecurring = r.IsRecurring,
                RecurrencePattern = r.RecurrencePattern,
                RecurrenceInterval = r.RecurrenceInterval,
                RecurrenceEndDate = r.RecurrenceEndDate,
                Status = r.Status,
                IsPaid = r.IsPaid,
                Price = r.Price,
                PaymentDetails = r.PaymentDetails != null ? new PaymentDetailsDto
                {
                    Id = r.PaymentDetails.Id,
                    ReservationId = r.PaymentDetails.ReservationId,
                    Amount = r.PaymentDetails.Amount,
                    Currency = r.PaymentDetails.Currency,
                    Status = r.PaymentDetails.Status,
                    TransactionId = r.PaymentDetails.TransactionId,
                    PaymentMethod = r.PaymentDetails.PaymentMethod,
                    CreatedAt = r.PaymentDetails.CreatedAt,
                    UpdatedAt = r.PaymentDetails.UpdatedAt
                } : null,
                Reviews = r.Reviews.Select(review => new ReviewDto
                {
                    Id = review.Id,
                    ReservationId = review.ReservationId,
                    UserId = review.UserId,
                    UserName = review.User != null ? $"{review.User.FirstName} {review.User.LastName}" : string.Empty,
                    Rating = review.Rating,
                    Comment = review.Comment,
                    CreatedAt = review.CreatedAt,
                    UpdatedAt = review.UpdatedAt,
                    ResourceId = r.ResourceId,
                    ResourceName = r.Resource != null ? r.Resource.Name : string.Empty
                }).ToList(),
                AverageRating = r.Reviews.Any() ? r.Reviews.Average(rv => rv.Rating) : null,
                ResourceCapacity = r.Resource != null ? r.Resource.Capacity : 0,
                ResourceImageUrl = r.Resource != null ? r.Resource.ImageUrl : null,
                ResourceLocation = r.Resource != null ? r.Resource.Location : null
            })
            .OrderByDescending(r => r.StartTime)
            .ToListAsync();

        return reservations;
    }

    private bool ReservationExists(int id)
    {
        return _context.Reservations.Any(e => e.Id == id);
    }

    private async Task<bool> CheckReservationConflict(int resourceId, DateTime startTime, DateTime endTime, int? excludeReservationId = null)
    {
        var query = _context.Reservations
            .Where(r => r.ResourceId == resourceId)
            .Where(r => (startTime < r.EndTime && endTime > r.StartTime))
            .Where(r => r.Status != "Cancelled");

        if (excludeReservationId.HasValue)
        {
            query = query.Where(r => r.Id != excludeReservationId);
        }

        return await query.AnyAsync();
    }

    private decimal? CalculatePrice(Resource resource, DateTime startTime, DateTime endTime)
    {
        if (resource == null)
            return null;

        var duration = endTime - startTime;

        // Saat bazında ücretlendirme
        if (resource.HourlyRate.HasValue && duration.TotalHours <= 24)
        {
            var hours = Math.Ceiling(duration.TotalHours);
            return resource.HourlyRate.Value * (decimal)hours;
        }
        // Gün bazında ücretlendirme
        else if (resource.DailyRate.HasValue)
        {
            var days = Math.Ceiling(duration.TotalDays);
            return resource.DailyRate.Value * (decimal)days;
        }

        return null;
    }

    private async Task CreateRecurringReservations(Reservation originalReservation, string pattern, int interval, DateTime endDate)
    {
        var resource = await _context.Resources.FindAsync(originalReservation.ResourceId);
        if (resource == null)
            return;

        var reservations = new List<Reservation>();
        var duration = originalReservation.EndTime - originalReservation.StartTime;
        var startTime = originalReservation.StartTime;
        var endTime = originalReservation.EndTime;

        // Tekrarlama desenine göre yeni rezervasyonlar oluştur
        while (true)
        {
            // Bir sonraki rezervasyon zamanını hesapla
            switch (pattern.ToLower())
            {
                case "daily":
                    startTime = startTime.AddDays(interval);
                    endTime = endTime.AddDays(interval);
                    break;
                case "weekly":
                    startTime = startTime.AddDays(7 * interval);
                    endTime = endTime.AddDays(7 * interval);
                    break;
                case "monthly":
                    startTime = startTime.AddMonths(interval);
                    endTime = endTime.AddMonths(interval);
                    break;
                default:
                    return; // Geçersiz desen
            }

            // Bitiş tarihini kontrol et
            if (startTime > endDate)
                break;

            // Çakışma kontrolü
            var conflict = await CheckReservationConflict(originalReservation.ResourceId, startTime, endTime);
            if (conflict)
                continue; // Çakışma varsa bu zaman dilimini atla

            // Yeni rezervasyon oluştur
            var reservation = new Reservation
            {
                StartTime = startTime,
                EndTime = endTime,
                Description = originalReservation.Description,
                ResourceId = originalReservation.ResourceId,
                UserId = originalReservation.UserId,
                Attendees = originalReservation.Attendees,
                IsRecurring = false, // Alt rezervasyonlar tekrarlanmaz
                Status = "Pending",
                Price = CalculatePrice(resource, startTime, endTime),
                CreatedAt = DateTime.UtcNow
            };

            reservations.Add(reservation);
        }

        // Tüm rezervasyonları toplu ekle
        if (reservations.Any())
        {
            await _context.Reservations.AddRangeAsync(reservations);
            await _context.SaveChangesAsync();

            // Bildirim oluşturma
            var notifications = reservations.Select(r => new Notification
            {
                UserId = r.UserId ?? string.Empty,
                Title = "Recurring Reservation",
                Message = $"A recurring reservation for {resource.Name} has been created.",
                Type = "Reservation",
                ReservationId = r.Id,
                CreatedAt = DateTime.UtcNow
            }).ToList();

            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();
        }
    }
}