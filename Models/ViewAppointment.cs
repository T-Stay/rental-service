using System;

namespace RentalService.Models
{
    public class ViewAppointment
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid RoomId { get; set; }
        public DateTime AppointmentTime { get; set; }
        public ViewAppointmentStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum ViewAppointmentStatus
    {
        Pending,
        Confirmed,
        Cancelled
    }
}
