using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FIRManagementSystem.Models
{
    public class LookupCode
    {
        public int CodeId { get; set; }
        public string CodeType { get; set; }
        public string CodeValue { get; set; }
        public string CodeLabel { get; set; }
        public int SortOrder { get; set; }
        public string IsActive { get; set; }
    }
}