using System;

namespace RentalService.Models
{
    public class Contract
    {
        public Guid Id { get; set; }
        public Guid BookingRequestId { get; set; }
        public BookingRequest? BookingRequest { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public string? ContractFileUrl { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
