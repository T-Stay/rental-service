using System;
using System.ComponentModel.DataAnnotations;

namespace RentalService.Models
{
    public class PhoneOtp
    {
        public Guid Id { get; set; }
        [Required]
        [MaxLength(20)]
        public string PhoneNumber { get; set; } = string.Empty;
        [Required]
        [MaxLength(8)]
        public string Otp { get; set; } = string.Empty;
        public DateTime ExpiredAt { get; set; }
        public DateTime LastSentAt { get; set; }
        public bool IsUsed { get; set; } = false;
    }
}
