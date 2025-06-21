using System;

namespace RentalService.Models
{
    public class Order
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
        public OrderStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum OrderStatus
    {
        Pending = 0,
        Paid = 1,
        Failed = 2,
        Cancelled = 3
    }
}
