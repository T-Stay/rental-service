using System;
using System.Collections.Generic;

namespace RentalService.Models
{
    public class UserAdPackage
    {
        public Guid Id { get; set; }
        public string UserId { get; set; } // Chủ trọ
        public AdPackageType PackageType { get; set; }
        public DateTime PurchaseDate { get; set; }
        public DateTime ExpiryDate { get; set; }
        public int RemainingPosts { get; set; }
        public bool IsActive { get; set; }
        public virtual List<AdPost> AdPosts { get; set; }
    }
}
