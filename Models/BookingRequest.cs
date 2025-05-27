using System;

namespace RentalService.Models
{
    public class BookingRequest
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid RoomId { get; set; }
        public Room? Room { get; set; }
        public User? User { get; set; }
        public required string Message { get; set; }
        public BookingRequestStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public enum BookingRequestStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled
    }
}
