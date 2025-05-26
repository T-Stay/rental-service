using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using RentalService.Models;

namespace RentalService.Data
{
    public class AppDbContext : IdentityDbContext<User, IdentityRole<Guid>, Guid>
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // public DbSet<Customer> Customers { get; set; }
        // public DbSet<Models.Host> Hosts { get; set; }
        // public DbSet<Admin> Admins { get; set; }
        // public DbSet<Consultant> Consultants { get; set; }
        public DbSet<Building> Buildings { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<Amenity> Amenities { get; set; }
        public DbSet<RoomImage> RoomImages { get; set; }
        public DbSet<Favorite> Favorites { get; set; }
        public DbSet<ViewAppointment> ViewAppointments { get; set; }
        public DbSet<BookingRequest> BookingRequests { get; set; }
        public DbSet<Contract> Contracts { get; set; }
        public DbSet<ChatRoom> ChatRooms { get; set; }
        public DbSet<Message> Messages { get; set; }
        public DbSet<Review> Reviews { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<Category> Categories { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User inheritance (TPH)
            modelBuilder.Entity<User>()
                .HasDiscriminator<UserRole>(u => u.Role)
                .HasValue<User>(UserRole.Guest)
                .HasValue<Customer>(UserRole.RegisteredCustomer)
                .HasValue<RentalService.Models.Host>(UserRole.Host)
                .HasValue<Admin>(UserRole.Admin)
                .HasValue<Consultant>(UserRole.Consultant);

            // Customer
            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Favorites)
                .WithOne()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Customer>()
                .HasMany(c => c.ViewAppointments)
                .WithOne()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Reviews)
                .WithOne()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Notifications)
                .WithOne()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<Customer>()
                .HasMany(c => c.Bookings)
                .WithOne()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Building - Host (one-to-many)
            modelBuilder.Entity<Building>()
                .HasOne<RentalService.Models.Host>()
                .WithMany(h => h.Buildings)
                .HasForeignKey(b => b.HostId)
                .OnDelete(DeleteBehavior.Cascade);

            // Room - Building (one-to-many)
            modelBuilder.Entity<Room>()
                .HasOne<Building>()
                .WithMany(b => b.Rooms)
                .HasForeignKey(r => r.BuildingId)
                .OnDelete(DeleteBehavior.Cascade);

            // RoomImage - Room (one-to-many)
            modelBuilder.Entity<RoomImage>()
                .HasOne<Room>()
                .WithMany(r => r.Images)
                .HasForeignKey(ri => ri.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // Room - Amenity (many-to-many)
            modelBuilder.Entity<Room>()
                .HasMany(r => r.Amenities)
                .WithMany(); // If you want a join entity, define it explicitly

            // Favorite (User-Room, many-to-one)
            modelBuilder.Entity<Favorite>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Favorite>()
                .HasOne<Room>()
                .WithMany()
                .HasForeignKey(f => f.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // ViewAppointment (User-Room)
            modelBuilder.Entity<ViewAppointment>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ViewAppointment>()
                .HasOne<Room>()
                .WithMany()
                .HasForeignKey(v => v.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // BookingRequest (User-Room)
            modelBuilder.Entity<BookingRequest>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Cascade);
            modelBuilder.Entity<BookingRequest>()
                .HasOne<Room>()
                .WithMany()
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict); // Prevent deletion of Room if there are BookingRequests

            // Contract - BookingRequest (one-to-one)
            modelBuilder.Entity<Contract>()
                .HasOne<BookingRequest>()
                .WithOne()
                .HasForeignKey<Contract>(c => c.BookingRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // ChatRoom - Messages (one-to-many)
            modelBuilder.Entity<Message>()
                .HasOne<ChatRoom>()
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatRoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // Message - Sender (User)
            modelBuilder.Entity<Message>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // Review (User-Room)
            modelBuilder.Entity<Review>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Review>()
                .HasOne<Room>()
                .WithMany()
                .HasForeignKey(r => r.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // Notification - User (many-to-one)
            modelBuilder.Entity<Notification>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Report (User-User)
            modelBuilder.Entity<Report>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(r => r.ReportedBy)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Report>()
                .HasOne<User>()
                .WithMany()
                .HasForeignKey(r => r.ReportedUser)
                .OnDelete(DeleteBehavior.Restrict);

            // Category: nothing special needed unless you want constraints

            // Enum conversions (if needed)
            modelBuilder.Entity<User>()
                .Property(u => u.Role)
                .HasConversion<string>();
            modelBuilder.Entity<Room>()
                .Property(r => r.Status)
                .HasConversion<string>();
            modelBuilder.Entity<ViewAppointment>()
                .Property(v => v.Status)
                .HasConversion<string>();
            modelBuilder.Entity<BookingRequest>()
                .Property(b => b.Status)
                .HasConversion<string>();
            modelBuilder.Entity<Report>()
                .Property(r => r.Status)
                .HasConversion<string>();
            modelBuilder.Entity<Category>()
                .Property(c => c.Type)
                .HasConversion<string>();
        }
    }
}
