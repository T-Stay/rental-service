using System;

namespace RentalService.Models
{
    public class Review
    {
        public Guid Id { get; set; }
        public Guid RoomId { get; set; }
        public Room? Room { get; set; }
        public Guid UserId { get; set; }
        public User? User { get; set; }
        public int Rating { get; set; }
        public required string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
