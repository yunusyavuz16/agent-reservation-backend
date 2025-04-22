using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ReservationApi.Data;
using ReservationApi.DTOs;
using ReservationApi.Models;

namespace ReservationApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ResourceController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<ResourceController> _logger;

    public ResourceController(ApplicationDbContext context, ILogger<ResourceController> logger)
    {
        _context = context;
        _logger = logger;
    }

    // GET: api/Resource
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ResourceDto>>> GetResources([FromQuery] ResourceFilterDto? filter = null)
    {
        var query = _context.Resources.AsQueryable();

        // Filtreleme işlemleri
        if (filter != null)
        {
            if (!string.IsNullOrEmpty(filter.Category))
            {
                query = query.Where(r => r.Category == filter.Category);
            }

            if (!string.IsNullOrEmpty(filter.SearchTerm))
            {
                query = query.Where(r =>
                    r.Name.Contains(filter.SearchTerm) ||
                    (r.Description != null && r.Description.Contains(filter.SearchTerm)) ||
                    (r.Location != null && r.Location.Contains(filter.SearchTerm)));
            }

            if (filter.MinCapacity.HasValue)
            {
                query = query.Where(r => r.Capacity >= filter.MinCapacity);
            }

            if (filter.MaxCapacity.HasValue)
            {
                query = query.Where(r => r.Capacity <= filter.MaxCapacity);
            }

            if (filter.MaxHourlyRate.HasValue)
            {
                query = query.Where(r => r.HourlyRate == null || r.HourlyRate <= filter.MaxHourlyRate);
            }

            if (filter.MaxDailyRate.HasValue)
            {
                query = query.Where(r => r.DailyRate == null || r.DailyRate <= filter.MaxDailyRate);
            }

            if (!string.IsNullOrEmpty(filter.Location))
            {
                query = query.Where(r => r.Location == filter.Location);
            }

            // Kullanılabilirlik kontrolü
            if (filter.IsAvailableNow.HasValue && filter.IsAvailableNow.Value)
            {
                var now = DateTime.UtcNow;
                query = query.Where(r => !r.Reservations.Any(res =>
                    now >= res.StartTime && now <= res.EndTime &&
                    res.Status != "Cancelled"));
            }

            if (filter.AvailableFrom.HasValue && filter.AvailableTo.HasValue)
            {
                var from = filter.AvailableFrom.Value;
                var to = filter.AvailableTo.Value;

                query = query.Where(r => !r.Reservations.Any(res =>
                    (from < res.EndTime && to > res.StartTime) &&
                    res.Status != "Cancelled"));
            }
        }

        // Sadece aktif kaynakları göster
        query = query.Where(r => r.IsActive);

        var resources = await query
            .Include(r => r.Reservations)
                .ThenInclude(res => res.Reviews)
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
                    .Where(rv => rv.Rating > 0)
                    .Select(rv => (double)rv.Rating)
                    .DefaultIfEmpty()
                    .Average(),
                IsAvailableNow = !r.Reservations.Any(res =>
                    DateTime.UtcNow >= res.StartTime &&
                    DateTime.UtcNow <= res.EndTime &&
                    res.Status != "Cancelled"),
                NextAvailableTime = r.Reservations
                    .Where(res => res.StartTime > DateTime.UtcNow && res.Status != "Cancelled")
                    .OrderBy(res => res.StartTime)
                    .Select(res => res.StartTime)
                    .FirstOrDefault()
            })
            .ToListAsync();

        return resources;
    }

    // GET: api/Resource/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ResourceDto>> GetResource(int id)
    {
        var resource = await _context.Resources
            .Include(r => r.Reservations)
                .ThenInclude(res => res.Reviews)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (resource == null)
        {
            return NotFound();
        }

        // Ortalama değerlendirme puanını hesaplama
        var averageRating = resource.Reservations
            .SelectMany(res => res.Reviews)
            .Where(rv => rv.Rating > 0)
            .Select(rv => (double)rv.Rating)
            .DefaultIfEmpty()
            .Average();

        // Şu anda kullanılabilir mi kontrol et
        var isAvailableNow = !resource.Reservations.Any(res =>
            DateTime.UtcNow >= res.StartTime &&
            DateTime.UtcNow <= res.EndTime &&
            res.Status != "Cancelled");

        // Bir sonraki müsait zaman
        var nextAvailableTime = resource.Reservations
            .Where(res => res.StartTime > DateTime.UtcNow && res.Status != "Cancelled")
            .OrderBy(res => res.StartTime)
            .Select(res => res.StartTime)
            .FirstOrDefault();

        return new ResourceDto
        {
            Id = resource.Id,
            Name = resource.Name,
            Description = resource.Description,
            HourlyRate = resource.HourlyRate,
            DailyRate = resource.DailyRate,
            Capacity = resource.Capacity,
            Category = resource.Category,
            Location = resource.Location,
            ImageUrl = resource.ImageUrl,
            IsActive = resource.IsActive,
            AverageRating = averageRating,
            IsAvailableNow = isAvailableNow,
            NextAvailableTime = nextAvailableTime
        };
    }

    // POST: api/Resource
    [Authorize(Roles = "Admin")]
    [HttpPost]
    public async Task<ActionResult<ResourceDto>> CreateResource(CreateResourceDto createResourceDto)
    {
        var resource = new Resource
        {
            Name = createResourceDto.Name,
            Description = createResourceDto.Description,
            HourlyRate = createResourceDto.HourlyRate,
            DailyRate = createResourceDto.DailyRate,
            Capacity = createResourceDto.Capacity,
            Category = createResourceDto.Category,
            Location = createResourceDto.Location,
            ImageUrl = createResourceDto.ImageUrl,
            IsActive = true
        };

        _context.Resources.Add(resource);
        await _context.SaveChangesAsync();

        return CreatedAtAction(
            nameof(GetResource),
            new { id = resource.Id },
            new ResourceDto
            {
                Id = resource.Id,
                Name = resource.Name,
                Description = resource.Description,
                HourlyRate = resource.HourlyRate,
                DailyRate = resource.DailyRate,
                Capacity = resource.Capacity,
                Category = resource.Category,
                Location = resource.Location,
                ImageUrl = resource.ImageUrl,
                IsActive = resource.IsActive
            });
    }

    // PUT: api/Resource/5
    [Authorize(Roles = "Admin")]
    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateResource(int id, UpdateResourceDto updateResourceDto)
    {
        var resource = await _context.Resources.FindAsync(id);
        if (resource == null)
        {
            return NotFound();
        }

        resource.Name = updateResourceDto.Name;
        resource.Description = updateResourceDto.Description;
        resource.HourlyRate = updateResourceDto.HourlyRate;
        resource.DailyRate = updateResourceDto.DailyRate;
        resource.Capacity = updateResourceDto.Capacity;
        resource.Category = updateResourceDto.Category;
        resource.Location = updateResourceDto.Location;
        resource.ImageUrl = updateResourceDto.ImageUrl;
        resource.IsActive = updateResourceDto.IsActive;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ResourceExists(id))
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

    // DELETE: api/Resource/5
    [Authorize(Roles = "Admin")]
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteResource(int id)
    {
        var resource = await _context.Resources.FindAsync(id);
        if (resource == null)
        {
            return NotFound();
        }

        // İlişkili rezervasyonları kontrol et
        var hasReservations = await _context.Reservations
            .AnyAsync(r => r.ResourceId == id && r.StartTime > DateTime.UtcNow);

        if (hasReservations)
        {
            // Aktif rezervasyonlar varsa, kaynağı silmek yerine pasif yapalım
            resource.IsActive = false;
            await _context.SaveChangesAsync();
            return NoContent();
        }

        _context.Resources.Remove(resource);
        await _context.SaveChangesAsync();

        return NoContent();
    }

    // GET: api/Resource/categories
    [HttpGet("categories")]
    public async Task<ActionResult<IEnumerable<string>>> GetCategories()
    {
        var categories = await _context.Resources
            .Where(r => r.IsActive && r.Category != null)
            .Select(r => r.Category)
            .Distinct()
            .ToListAsync();

        return categories.Where(c => c != null).Cast<string>().ToList();
    }

    // GET: api/Resource/locations
    [HttpGet("locations")]
    public async Task<ActionResult<IEnumerable<string>>> GetLocations()
    {
        var locations = await _context.Resources
            .Where(r => r.IsActive && r.Location != null)
            .Select(r => r.Location)
            .Distinct()
            .ToListAsync();

        return locations.Where(l => l != null).Cast<string>().ToList();
    }

    private bool ResourceExists(int id)
    {
        return _context.Resources.Any(e => e.Id == id);
    }
}