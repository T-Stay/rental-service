using System;
using System.Collections.Generic;

namespace RentalService.Models
{
    public class Customer : User
    {
        public  List<Favorite>? Favorites { get; set; }
        public  List<BookingRequest>? Bookings { get; set; }
        public  List<ViewAppointment>? ViewAppointments { get; set; }
        public  List<Review>? Reviews { get; set; }
        public  List<Notification>? Notifications { get; set; }
    }
}
