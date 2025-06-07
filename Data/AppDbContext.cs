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
        public DbSet<ContactInformation> ContactInformations { get; set; }
        public DbSet<PhoneOtp> PhoneOtps { get; set; }

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
                .HasMany(c => c.BookingRequests)
                .WithOne()
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Building - Host (one-to-many)
            modelBuilder.Entity<Building>()
                .HasOne(b => b.Host)
                .WithMany(h => h.Buildings)
                .HasForeignKey(b => b.HostId)
                .OnDelete(DeleteBehavior.Cascade);

            // Room - Building (one-to-many)
            modelBuilder.Entity<Room>()
                .HasOne(r => r.Building)
                .WithMany(b => b.Rooms)
                .HasForeignKey(r => r.BuildingId)
                .OnDelete(DeleteBehavior.Cascade);

            // RoomImage - Room (one-to-many)
            modelBuilder.Entity<RoomImage>()
                .HasOne(ri => ri.Room)
                .WithMany(r => r.RoomImages)
                .HasForeignKey(ri => ri.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // Room - Amenity (many-to-many)
            modelBuilder.Entity<Room>()
                .HasMany(r => r.Amenities)
                .WithMany();

            // Favorite (User-Room, many-to-one)
            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.User)
                .WithMany(u => u.Favorites)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Favorite>()
                .HasOne(f => f.Room)
                .WithMany(r => r.Favorites)
                .HasForeignKey(f => f.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // ViewAppointment (User-Room)
            modelBuilder.Entity<ViewAppointment>()
                .HasOne(v => v.User)
                .WithMany(u => u.ViewAppointments)
                .HasForeignKey(v => v.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ViewAppointment>()
                .HasOne(v => v.Room)
                .WithMany(r => r.ViewAppointments)
                .HasForeignKey(v => v.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // BookingRequest (User-Room)
            modelBuilder.Entity<BookingRequest>()
                .HasOne(b => b.User)
                .WithMany(u => u.BookingRequests)
                .HasForeignKey(b => b.UserId)
                .OnDelete(DeleteBehavior.Restrict); // Ensure restrict to avoid cascade path
            modelBuilder.Entity<BookingRequest>()
                .HasOne(b => b.Room)
                .WithMany(r => r.BookingRequests)
                .HasForeignKey(b => b.RoomId)
                .OnDelete(DeleteBehavior.Restrict); // Ensure restrict to avoid cascade path

            // Contract - BookingRequest (one-to-one)
            modelBuilder.Entity<Contract>()
                .HasOne(c => c.BookingRequest)
                .WithOne()
                .HasForeignKey<Contract>(c => c.BookingRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            // ChatRoom - Messages (one-to-many)
            modelBuilder.Entity<Message>()
                .HasOne(m => m.ChatRoom)
                .WithMany(c => c.Messages)
                .HasForeignKey(m => m.ChatRoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // Message - Sender (User)
            modelBuilder.Entity<Message>()
                .HasOne(m => m.Sender)
                .WithMany()
                .HasForeignKey(m => m.SenderId)
                .OnDelete(DeleteBehavior.Restrict);

            // ChatRoom - User1/User2
            modelBuilder.Entity<ChatRoom>()
                .HasOne(c => c.User1)
                .WithMany()
                .HasForeignKey(c => c.User1Id)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<ChatRoom>()
                .HasOne(c => c.User2)
                .WithMany()
                .HasForeignKey(c => c.User2Id)
                .OnDelete(DeleteBehavior.Restrict);

            // Review (User-Room)
            modelBuilder.Entity<Review>()
                .HasOne(r => r.User)
                .WithMany(u => u.Reviews)
                .HasForeignKey(r => r.UserId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Review>()
                .HasOne(r => r.Room)
                .WithMany(rm => rm.Reviews)
                .HasForeignKey(r => r.RoomId)
                .OnDelete(DeleteBehavior.Cascade);

            // Notification - User (many-to-one)
            modelBuilder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany(u => u.Notifications)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Report (User-User)
            modelBuilder.Entity<Report>()
                .HasOne(r => r.Reporter)
                .WithMany(u => u.ReportsFiled)
                .HasForeignKey(r => r.ReportedBy)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Report>()
                .HasOne(r => r.Reported)
                .WithMany(u => u.ReportsAgainst)
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

            // ContactInformation - User (many-to-one)
            modelBuilder.Entity<ContactInformation>()
                .HasOne(ci => ci.User)
                .WithMany(u => u.ContactInformations)
                .HasForeignKey(ci => ci.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: PhoneNumber must be unique
            modelBuilder.Entity<ContactInformation>()
                .HasIndex(ci => ci.Data)
                .IsUnique()
                .HasFilter("[Type] = 1"); // 1 = PhoneNumber enum

            // PhoneOtp: unique per phone, not used for FK
            modelBuilder.Entity<PhoneOtp>()
                .HasIndex(p => p.PhoneNumber);
        }
    }
}
