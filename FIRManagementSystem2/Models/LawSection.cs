using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FIRManagementSystem.Models
{
    public class LawSection
    {
        public int SectionId { get; set; }
        public string SectionNo { get; set; }
        public string ActName { get; set; }
        public string Description { get; set; }
        public string IsActive { get; set; }
    }
}