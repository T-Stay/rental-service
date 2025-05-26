using System;

namespace RentalService.Models
{
    public class RoomImage
    {
        public Guid Id { get; set; }
        public Guid RoomId { get; set; }
        public required string ImageUrl { get; set; }
    }
}
