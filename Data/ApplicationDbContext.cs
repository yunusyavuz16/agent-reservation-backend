using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ReservationApi.Models;

namespace ReservationApi.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Resource> Resources { get; set; }
    public DbSet<Reservation> Reservations { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Seed some initial resources
        builder.Entity<Resource>().HasData(
            new Resource { Id = 1, Name = "Meeting Room A", Description = "Large conference room with projector" },
            new Resource { Id = 2, Name = "Meeting Room B", Description = "Small meeting room for up to 4 people" },
            new Resource { Id = 3, Name = "Office Car", Description = "Company vehicle for business trips" }
        );
    }
}