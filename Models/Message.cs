using System;

namespace RentalService.Models
{
    public class Message
    {
        public Guid Id { get; set; }
        public Guid ChatRoomId { get; set; }
        public ChatRoom? ChatRoom { get; set; }
        public Guid SenderId { get; set; }
        public User? Sender { get; set; }
        public required string Content { get; set; }
        public DateTime SentAt { get; set; }
    }
}
