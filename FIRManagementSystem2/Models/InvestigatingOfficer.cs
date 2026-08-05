using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FIRManagementSystem.Models
{
    public class InvestigatingOfficer
    {
        public int IoId { get; set; }
        public string FullName { get; set; }
        public string FatherName { get; set; }
        public string RankTitle { get; set; }
        public string BadgeNo { get; set; }
        public string CnicNo { get; set; }
        public int StationId { get; set; }
        public string ContactNo { get; set; }
        public string IsActive { get; set; }
        public DateTime? CreatedAt { get; set; }
    }
}