using System;

namespace RentalService.Models
{
    public class Message
    {
        public Guid Id { get; set; }
        public Guid ChatRoomId { get; set; }
        public Guid SenderId { get; set; }
        public required string Content { get; set; }
        public DateTime SentAt { get; set; }
    }
}
