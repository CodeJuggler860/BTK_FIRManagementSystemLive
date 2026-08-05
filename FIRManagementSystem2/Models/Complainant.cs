using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FIRManagementSystem.Models
{
    public class Complainant
    {
        public int ComplainantId { get; set; }
        public string FullName { get; set; }
        public string FatherName { get; set; }
        public string CnicNo { get; set; }
        public string Gender { get; set; }               
        public DateTime? DateOfBirth { get; set; }
        public string Nationality { get; set; }
        public string Religion { get; set; }
        public string Occupation { get; set; }
        public string Address { get; set; }
        public string ContactNo { get; set; }
        public DateTime? CreatedAt { get; set; }

    }
}