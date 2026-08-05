using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FIRManagementSystem2.ViewModels
{
    public class FIRFormViewModel
    {
        public int? FirId { get; set; }          
        public string FirNo { get; set; }        
        public DateTime DateReported { get; set; }
        public string BriefDescription { get; set; }
        public string ComplainantName { get; set; }
        public string AccusedName { get; set; }
        public int IoId { get; set; }            
        public string Status { get; set; }
    }
}