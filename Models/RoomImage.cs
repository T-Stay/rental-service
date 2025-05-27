using System;

namespace RentalService.Models
{
    public class RoomImage
    {
        public Guid Id { get; set; }
        public Guid RoomId { get; set; }
        public string? ImageUrl { get; set; }
        public Room? Room { get; set; }
    }
}
