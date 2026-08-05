using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FIRManagementSystem.Models
{
    public class FIR
    {
        public int FirId { get; set; }
        public string FirNo { get; set; }
        public int StationId { get; set; }
        public DateTime DateReported { get; set; }
        public DateTime? DatetimeReported { get; set; }
        public DateTime? DateOfOccurrence { get; set; }
        public DateTime? DatetimeOccurrence { get; set; }
        public int ComplainantId { get; set; }
        public int? IoId { get; set; }
        public string NatureOfOffence { get; set; }
        public string BriefDescription { get; set; }   // CLOB => string
        public string MurasilaText { get; set; }
        public string PropertyDetails { get; set; }
        public string PlaceOfOccurrence { get; set; }
        public string Status { get; set; }
        public string Remarks { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public int? UpdatedBy { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}