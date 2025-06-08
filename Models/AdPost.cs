using System;
using System.Collections.Generic;

namespace RentalService.Models
{
    public class AdPost
    {
        public Guid Id { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public string ImageUrls { get; set; } // Có thể lưu dạng chuỗi JSON hoặc phân tách bằng dấu phẩy
        public string HostId { get; set; }
        public Guid UserAdPackageId { get; set; }
        public AdPackageType PackageType { get; set; }
        public int PriorityOrder { get; set; }
        public string Badge { get; set; }
        public int ViewCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public virtual UserAdPackage UserAdPackage { get; set; }
        public virtual List<Room> Rooms { get; set; }
    }
}
