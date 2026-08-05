using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FIRManagementSystem.Models
{
    public class Accused
    {
        public int AccusedId { get; set; }
        public int FirId { get; set; }
        public string FullName { get; set; }
        public string FatherName { get; set; }
        public string CnicNo { get; set; }
        public int? Age { get; set; }
        public string Gender { get; set; }
        public string Address { get; set; }
        public string ContactNo { get; set; }
        public string PhysicalDesc { get; set; }
        public string ArrestStatus { get; set; }
        public DateTime? ArrestDate { get; set; }
        public string Remarks { get; set; }
    }
}