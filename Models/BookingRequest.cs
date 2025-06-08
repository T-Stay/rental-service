using System;

namespace RentalService.Models
{
    public class BookingRequest
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid RoomId { get; set; }
        public Room? Room { get; set; }
        public User? User { get; set; }
        public required string Message { get; set; }
        public BookingRequestStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        // Bổ sung các trường cho đặt cọc giữ phòng
        public decimal DepositAmount { get; set; } // Số tiền cọc
        public DateTime? HoldUntil { get; set; } // Thời gian giữ phòng
        public DepositStatus DepositStatus { get; set; } // Trạng thái cọc
    }

    public enum BookingRequestStatus
    {
        Pending,
        Approved,
        Rejected,
        Cancelled
    }

    public enum DepositStatus
    {
        None, // Chưa đặt cọc
        Pending, // Đang chờ xác nhận
        Confirmed, // Đã xác nhận cọc
        Refunded, // Đã hoàn tiền
        Forfeited // Bị giữ cọc
    }
}
