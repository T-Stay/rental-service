using System;

namespace RentalService.Models
{
    public class Category
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public CategoryType Type { get; set; }
    }

    public enum CategoryType
    {
        Amenity,
        Location
    }
}
