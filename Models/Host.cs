using System;
using System.Collections.Generic;

namespace RentalService.Models
{
    public class Host : User
    {
        public List<Building>? Buildings { get; set; }
    }
}
