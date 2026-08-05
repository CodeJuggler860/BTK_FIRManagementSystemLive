using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FIRManagementSystem2.Models
{
    public class FIR_MST
    {
        public int SRNO    { get; set; }
        public string FIR_NO { get; set; }
        public DateTime FIR_Date { get; set; }

        public string FIR_COMPLAINT { get; set; }
        public string FIR_ACCUSED { get; set; }
        public string FIR_DESCRP { get; set; }

        public string FIR_STATUS { get; set; }
        public string FIR_LOCATION { get; set; }
        public string POLICESTATION { get; set; }

        public string COMPLAINT { get; set; }
        public string SECTIONS { get; set; }
        public int IS_DELETED { get; set; }

        public string CreatedBy { get; set; }
        public DateTime? CreatedAt { get; set; }
        public string CreatedIp { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedAt { get; set; }
        public string ModifiedIp { get; set; }
    }
}
