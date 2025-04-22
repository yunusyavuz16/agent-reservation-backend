using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservationApi.Data;
using ReservationApi.DTOs;
using ReservationApi.Models;

namespace ReservationApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ReservationController : ControllerBase
{
    private readonly ApplicationDbContext _context;

    public ReservationController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: api/Reservation
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ReservationDto>>> GetReservations()
    {
        return await _context.Reservations
            .Include(r => r.Resource)
            .Include(r => r.User)
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
                CreatedAt = r.CreatedAt
            })
            .ToListAsync();
    }

    // GET: api/Reservation/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ReservationDto>> GetReservation(int id)
    {
        var reservation = await _context.Reservations
            .Include(r => r.Resource)
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (reservation == null)
        {
            return NotFound();
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
            CreatedAt = reservation.CreatedAt
        };
    }

    // POST: api/Reservation
    [HttpPost]
    public async Task<ActionResult<ReservationDto>> CreateReservation(CreateReservationDto createReservationDto)
    {
        // Check if the resource exists
        var resource = await _context.Resources.FindAsync(createReservationDto.ResourceId);
        if (resource == null)
        {
            return BadRequest("The specified resource does not exist.");
        }

        // Check for conflicting reservations
        var conflictingReservation = await _context.Reservations
            .Where(r => r.ResourceId == createReservationDto.ResourceId)
            .Where(r => (createReservationDto.StartTime < r.EndTime &&
                        createReservationDto.EndTime > r.StartTime))
            .FirstOrDefaultAsync();

        if (conflictingReservation != null)
        {
            return BadRequest("There is already a reservation for this resource during the requested time period.");
        }

        var reservation = new Reservation
        {
            StartTime = createReservationDto.StartTime,
            EndTime = createReservationDto.EndTime,
            Description = createReservationDto.Description,
            ResourceId = createReservationDto.ResourceId,
            UserId = User.Identity?.IsAuthenticated == true ? User.Identity.Name : "anonymous", // In a real app, use the authenticated user's ID
            CreatedAt = DateTime.UtcNow
        };

        _context.Reservations.Add(reservation);
        await _context.SaveChangesAsync();

        // Reload the reservation with its related entities
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
                CreatedAt = reservation.CreatedAt
            });
    }

    // PUT: api/Reservation/5
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateReservation(int id, UpdateReservationDto updateReservationDto)
    {
        var reservation = await _context.Reservations.FindAsync(id);
        if (reservation == null)
        {
            return NotFound();
        }

        // Check if the resource exists
        var resource = await _context.Resources.FindAsync(updateReservationDto.ResourceId);
        if (resource == null)
        {
            return BadRequest("The specified resource does not exist.");
        }

        // Check for conflicting reservations, excluding the current one
        var conflictingReservation = await _context.Reservations
            .Where(r => r.Id != id)
            .Where(r => r.ResourceId == updateReservationDto.ResourceId)
            .Where(r => (updateReservationDto.StartTime < r.EndTime &&
                        updateReservationDto.EndTime > r.StartTime))
            .FirstOrDefaultAsync();

        if (conflictingReservation != null)
        {
            return BadRequest("There is already a reservation for this resource during the requested time period.");
        }

        reservation.StartTime = updateReservationDto.StartTime;
        reservation.EndTime = updateReservationDto.EndTime;
        reservation.Description = updateReservationDto.Description;
        reservation.ResourceId = updateReservationDto.ResourceId;

        try
        {
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
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteReservation(int id)
    {
        var reservation = await _context.Reservations.FindAsync(id);
        if (reservation == null)
        {
            return NotFound();
        }

        _context.Reservations.Remove(reservation);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ReservationExists(int id)
    {
        return _context.Reservations.Any(e => e.Id == id);
    }
}