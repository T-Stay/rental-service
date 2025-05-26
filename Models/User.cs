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
