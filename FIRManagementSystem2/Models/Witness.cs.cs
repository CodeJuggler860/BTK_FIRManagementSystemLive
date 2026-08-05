using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FIRManagementSystem.Models
{
    public class Witness
    {
        public int WitnessId { get; set; }
        public int FirId { get; set; }
        public string FullName { get; set; }
        public string FatherName { get; set; }
        public string CnicNo { get; set; }
        public string Address { get; set; }
        public string ContactNo { get; set; }
        public string Statement { get; set; }             // CLOB -> string
        public string WitnessType { get; set; }
        public string Remarks { get; set; }
    }
}