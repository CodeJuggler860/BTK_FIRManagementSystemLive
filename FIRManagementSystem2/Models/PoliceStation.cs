using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FIRManagementSystem.Models
{
    public class PoliceStation
    {
        public int StationId { get; set; }
        public string StationName { get; set; }
        public string District { get; set; }
        public string City { get; set; }
        public string Address { get; set; }
        public string ContactNo { get; set; }
        public string IsActive { get; set; }   // 'Y' or 'N'
    }
}