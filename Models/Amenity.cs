using System;

namespace RentalService.Models
{
    public class Amenity
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
    }
}
