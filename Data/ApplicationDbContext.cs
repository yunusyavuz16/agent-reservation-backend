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
    public DbSet<Review> Reviews { get; set; }
    public DbSet<Notification> Notifications { get; set; }
    public DbSet<PaymentDetails> PaymentDetails { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        // Seed resources
        builder.Entity<Resource>().HasData(
            new Resource {
                Id = 1,
                Name = "Meeting Room A",
                Description = "Large conference room with projector",
                Capacity = 12,
                HourlyRate = 50.00m,
                Category = "Meeting Room",
                Location = "1st Floor",
                ImageUrl = "https://images.unsplash.com/photo-1431540015161-0bf868a2d407?ixlib=rb-4.0.3&ixid=MnwxMjA3fDB8MHxzZWFyY2h8MjN8fG1lZXRpbmd8ZW58MHx8MHx8&auto=format&fit=crop&w=500&q=60"
            },
            new Resource {
                Id = 2,
                Name = "Meeting Room B",
                Description = "Small meeting room for up to 4 people",
                Capacity = 4,
                HourlyRate = 30.00m,
                Category = "Meeting Room",
                Location = "2nd Floor",
                ImageUrl = "https://images.unsplash.com/photo-1517502884422-41eaead166d4?ixlib=rb-4.0.3&ixid=MnwxMjA3fDB8MHxzZWFyY2h8MTd8fG1lZXRpbmd8ZW58MHx8MHx8&auto=format&fit=crop&w=500&q=60"
            },
            new Resource {
                Id = 3,
                Name = "Office Car",
                Description = "Company vehicle for business trips",
                Capacity = 5,
                DailyRate = 100.00m,
                Category = "Vehicle",
                Location = "Parking Lot",
                ImageUrl = "https://images.unsplash.com/photo-1549317661-bd32c8ce0db2?ixlib=rb-4.0.3&ixid=MnwxMjA3fDB8MHxzZWFyY2h8Mnx8Y2FyfGVufDB8fDB8fA%3D%3D&auto=format&fit=crop&w=500&q=60"
            },
            new Resource {
                Id = 4,
                Name = "Projector",
                Description = "Portable projector with HDMI and VGA connections",
                Capacity = 1,
                HourlyRate = 20.00m,
                Category = "Equipment",
                Location = "Storage Room",
                ImageUrl = "https://images.unsplash.com/photo-1517479149777-5f3b1511d5ad?ixlib=rb-4.0.3&ixid=MnwxMjA3fDB8MHxzZWFyY2h8MTB8fHByb2plY3RvcnxlbnwwfHwwfHw%3D&auto=format&fit=crop&w=500&q=60"
            },
            new Resource {
                Id = 5,
                Name = "Training Room",
                Description = "Large training room with computers for up to 20 people",
                Capacity = 20,
                HourlyRate = 80.00m,
                Category = "Training Room",
                Location = "3rd Floor",
                ImageUrl = "https://images.unsplash.com/photo-1524178232363-1fb2b075b655?ixlib=rb-4.0.3&ixid=MnwxMjA3fDB8MHxzZWFyY2h8NXx8dHJhaW5pbmd8ZW58MHx8MHx8&auto=format&fit=crop&w=500&q=60"
            }
        );
    }
}