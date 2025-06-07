using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RentalService.Models
{
    public enum ContactType
    {
        Email,
        PhoneNumber,
        FacebookLink
    }

    public class ContactInformation
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public User? User { get; set; }
        [Required]
        public ContactType Type { get; set; }
        [Required]
        [MaxLength(256)]
        public string Data { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsVerified { get; set; } = false;
    }
}
