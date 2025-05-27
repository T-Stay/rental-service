using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Identity;

namespace RentalService.Models
{
    public class User : IdentityUser<Guid>
    {
        public required string Name { get; set; }
        public required string AvatarUrl { get; set; }
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public List<BookingRequest>? BookingRequests { get; set; }
        public List<ViewAppointment>? ViewAppointments { get; set; }
        public List<Review>? Reviews { get; set; }
        public List<Favorite>? Favorites { get; set; }
        public List<Notification>? Notifications { get; set; }
        public List<Report>? ReportsFiled { get; set; }
        public List<Report>? ReportsAgainst { get; set; }
    }

    public enum UserRole
    {
        Guest,
        RegisteredCustomer,
        Host,
        Admin,
        Consultant
    }

    public static class UserRoleHelper
    {
        public static string ToIdentityRoleString(UserRole role)
        {
            return role switch
            {
                UserRole.Guest => "guest",
                UserRole.RegisteredCustomer => "customer",
                UserRole.Host => "host",
                UserRole.Admin => "admin",
                UserRole.Consultant => "consultant",
                _ => "customer"
            };
        }

        public static UserRole FromIdentityRoleString(string role)
        {
            return role.ToLower() switch
            {
                "guest" => UserRole.Guest,
                "customer" => UserRole.RegisteredCustomer,
                "host" => UserRole.Host,
                "admin" => UserRole.Admin,
                "consultant" => UserRole.Consultant,
                _ => UserRole.RegisteredCustomer
            };
        }
    }
}
