using System;
using System.Collections.Generic;

namespace RentalService.Models
{
    public class Host : User
    {
        public required List<Building> Buildings { get; set; }
    }
}
