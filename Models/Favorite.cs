using System;

namespace RentalService.Models
{
    public class Favorite
    {
        public Guid Id { get; set; }
        public Guid? UserId { get; set; }
        public Guid RoomId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
