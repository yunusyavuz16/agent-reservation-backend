using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservationApi.Data;
using ReservationApi.DTOs;
using ReservationApi.Models;

namespace ReservationApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ReviewController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ReviewController> _logger;

    public ReviewController(ApplicationDbContext context, ILogger<ReviewController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Review
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetReviews()
    {
        var reviews = await _context.Reviews
            .Include(r => r.User)
            .Include(r => r.Reservation)
            .ThenInclude(res => res.Resource)
            .Select(r => new ReviewDto
            {
                Id = r.Id,
                ReservationId = r.ReservationId,
                UserId = r.UserId,
                UserName = r.User != null ? $"{r.User.FirstName} {r.User.LastName}" : string.Empty,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                ResourceId = r.Reservation.ResourceId,
                ResourceName = r.Reservation.Resource != null ? r.Reservation.Resource.Name : string.Empty
            })
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return reviews;
    }

    // GET: api/Review/Resource/5
    [HttpGet("Resource/{resourceId}")]
    public async Task<ActionResult<IEnumerable<ReviewDto>>> GetResourceReviews(int resourceId)
    {
        var reviews = await _context.Reviews
            .Include(r => r.User)
            .Include(r => r.Reservation)
            .ThenInclude(res => res.Resource)
            .Where(r => r.Reservation.ResourceId == resourceId)
            .Select(r => new ReviewDto
            {
                Id = r.Id,
                ReservationId = r.ReservationId,
                UserId = r.UserId,
                UserName = r.User != null ? $"{r.User.FirstName} {r.User.LastName}" : string.Empty,
                Rating = r.Rating,
                Comment = r.Comment,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt,
                ResourceId = r.Reservation.ResourceId,
                ResourceName = r.Reservation.Resource != null ? r.Reservation.Resource.Name : string.Empty
            })
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

        return reviews;
    }

    // GET: api/Review/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ReviewDto>> GetReview(int id)
    {
        var review = await _context.Reviews
            .Include(r => r.User)
            .Include(r => r.Reservation)
            .ThenInclude(res => res.Resource)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (review == null)
        {
            return NotFound();
        }

        return new ReviewDto
        {
            Id = review.Id,
            ReservationId = review.ReservationId,
            UserId = review.UserId,
            UserName = review.User != null ? $"{review.User.FirstName} {review.User.LastName}" : string.Empty,
            Rating = review.Rating,
            Comment = review.Comment,
            CreatedAt = review.CreatedAt,
            UpdatedAt = review.UpdatedAt,
            ResourceId = review.Reservation.ResourceId,
            ResourceName = review.Reservation.Resource != null ? review.Reservation.Resource.Name : string.Empty
        };
    }

    // POST: api/Review
    [Authorize]
    [HttpPost]
    public async Task<ActionResult<ReviewDto>> CreateReview(CreateReviewDto createReviewDto)
    {
        // Get user ID from claims
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized(new { message = "User not authenticated" });
        }

        // Check if reservation exists
        var reservation = await _context.Reservations
            .Include(r => r.Resource)
            .FirstOrDefaultAsync(r => r.Id == createReviewDto.ReservationId);

        if (reservation == null)
        {
            return BadRequest(new { message = "Reservation not found" });
        }

        // Check if the reservation belongs to the user
        if (reservation.UserId != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        // Check if a review already exists for this reservation
        var existingReview = await _context.Reviews
            .FirstOrDefaultAsync(r => r.ReservationId == createReviewDto.ReservationId && r.UserId == userId);

        if (existingReview != null)
        {
            return Conflict(new { message = "You have already reviewed this reservation" });
        }

        // Create new review
        var review = new Review
        {
            ReservationId = createReviewDto.ReservationId,
            UserId = userId,
            Rating = createReviewDto.Rating,
            Comment = createReviewDto.Comment,
            CreatedAt = DateTime.UtcNow
        };

        _context.Reviews.Add(review);
        await _context.SaveChangesAsync();

        // Return created review
        return CreatedAtAction(
            nameof(GetReview),
            new { id = review.Id },
            new ReviewDto
            {
                Id = review.Id,
                ReservationId = review.ReservationId,
                UserId = review.UserId,
                UserName = User.Identity?.Name ?? string.Empty,
                Rating = review.Rating,
                Comment = review.Comment,
                CreatedAt = review.CreatedAt,
                ResourceId = reservation.ResourceId,
                ResourceName = reservation.Resource?.Name ?? string.Empty
            });
    }

    // PUT: api/Review/5
    [Authorize]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReview(int id, UpdateReviewDto updateReviewDto)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
        {
            return NotFound();
        }

        // Check if the review belongs to the user
        if (review.UserId != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        // Update the review
        review.Rating = updateReviewDto.Rating;
        review.Comment = updateReviewDto.Comment;
        review.UpdatedAt = DateTime.UtcNow;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ReviewExists(id))
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

    // DELETE: api/Review/5
    [Authorize]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReview(int id)
    {
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (userId == null)
        {
            return Unauthorized();
        }

        var review = await _context.Reviews.FindAsync(id);
        if (review == null)
        {
            return NotFound();
        }

        // Check if the review belongs to the user or the user is an admin
        if (review.UserId != userId && !User.IsInRole("Admin"))
        {
            return Forbid();
        }

        _context.Reviews.Remove(review);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ReviewExists(int id)
    {
        return _context.Reviews.Any(e => e.Id == id);
    }
}