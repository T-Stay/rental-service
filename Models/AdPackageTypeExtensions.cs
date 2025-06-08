using System;

namespace RentalService.Models
{
    public static class AdPackageTypeExtensions
    {
        public static string ToVietnameseLabel(this AdPackageType type)
        {
            return type switch
            {
                AdPackageType.Free => "Miễn phí",
                AdPackageType.Dong => "Gói Đồng",
                AdPackageType.Bac => "Gói Bạc",
                AdPackageType.Vang => "Gói Vàng",
                AdPackageType.KimCuong => "Gói Kim Cương",
                _ => type.ToString()
            };
        }
    }
}
