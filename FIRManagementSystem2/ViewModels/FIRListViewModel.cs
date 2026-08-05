using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FIRManagementSystem.ViewModels
{
    public class FIRListViewModel
    {
        public int Srno { get; set; }
        public string FirNo { get; set; }
        public DateTime? FirDate { get; set; }
        public string Description { get; set; }
        public string Complainant { get; set; }
        public string Accused { get; set; }
        public string IoName { get; set; }
        public string Status { get; set; }
        public string Location { get; set; }
        public string PoliceStation { get; set; }
        public string Sections { get; set; }

    }
}