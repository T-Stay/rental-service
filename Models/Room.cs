using System;
using System.Collections.Generic;
using RentalService.Models;

namespace RentalService.Models
{
    public class Room
    {
        public Guid Id { get; set; }
        public Guid BuildingId { get; set; }
        public Building? Building { get; set; }
        public required string Name { get; set; }
        public required string Description { get; set; }
        public decimal Price { get; set; }
        public RoomStatus Status { get; set; }
        public required List<RoomImage> Images { get; set; }
        public required List<Amenity> Amenities { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<BookingRequest>? BookingRequests { get; set; }
        public List<ViewAppointment>? ViewAppointments { get; set; }
        public List<Review>? Reviews { get; set; }
        public List<Favorite>? Favorites { get; set; }
        public List<RoomImage>? RoomImages { get; set; }
    }

    public enum RoomStatus
    {
        Active,
        Inactive,
        Hidden
    }
}
