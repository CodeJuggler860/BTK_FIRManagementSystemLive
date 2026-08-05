using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FIRManagementSystem2.Models
{
    public class FIR_DTL
    {
        public int SRNO { get; set; }
        public int FIRMSTSRNO { get; set; }
        public string INVESTIGATIONOFFICER { get; set; }
        public DateTime HEARINGDATE { get; set; }
        public string HEARINGJUDGE { get; set; }
        public string HEARINGNOTES { get; set; }
        public string HEARINGDOCLOCATION { get; set; }
        public string WITNESS1 { get; set; }
        public string WITNESS2 { get; set; }
    }
}
