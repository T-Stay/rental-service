using System;
using System.Collections.Generic;

namespace RentalService.Models
{
    public class User
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Email { get; set; }
        public required string PasswordHash { get; set; }
        public required string Phone { get; set; }
        public required string AvatarUrl { get; set; }
        public UserRole Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public enum UserRole
    {
        Guest,
        RegisteredCustomer,
        Host,
        Admin,
        Consultant
    }
}
