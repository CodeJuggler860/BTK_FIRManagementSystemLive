using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FIRManagementSystem.Models
{
    public class FirUpdate
    {
        public int UpdateId { get; set; }
        public int FirId { get; set; }
        public string UpdateType { get; set; }
        public string UpdateText { get; set; }
        public string IsLatest { get; set; }
        public DateTime? UpdateDate { get; set; }
        public int UpdatedBy { get; set; }
    }
}