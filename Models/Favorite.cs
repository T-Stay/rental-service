using System;

namespace RentalService.Models
{
    public class Favorite
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public User? User { get; set; }
        public Guid RoomId { get; set; }
        public Room? Room { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
