using System;
using System.Collections.Generic;

namespace RentalService.Models
{
    public class Building
    {
        public Guid Id { get; set; }
        public Guid HostId { get; set; }
        public Host? Host { get; set; }
        public required string Name { get; set; }
        public required string Address { get; set; }
        public required string Description { get; set; }
        public required string Location { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }

        public required List<Room> Rooms { get; set; }
    }
}
