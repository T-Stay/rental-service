using System;

namespace RentalService.Models
{
    public class Report
    {
        public Guid Id { get; set; }
        public Guid ReportedBy { get; set; }
        public User? Reporter { get; set; }
        public Guid ReportedUser { get; set; }
        public User? Reported { get; set; }
        public required string Reason { get; set; }
        public ReportStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public enum ReportStatus
    {
        Open,
        Resolved
    }
}
