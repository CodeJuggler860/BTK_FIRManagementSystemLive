using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FIRManagementSystem2.Models
{
    public class CASE_MST
    {
        public int CASE_ID { get; set; }
        public string CASE_DESCRIPTION { get; set; }
        public string COMPLAINANT_NAME { get; set; }
        public string ACCUSED_NAME { get; set; }
        public string CASE_STATUS { get; set; }
        public string FIR_REGISTERED { get; set; }
        public string CREATED_BY { get; set; }
        public DateTime? CREATED_AT { get; set; }
        public string CREATED_IP { get; set; }
        public string MODIFIED_BY { get; set; }
        public DateTime? MODIFIED_AT { get; set; }
        public string MODIFIED_IP { get; set; }
        public string LOCATION { get; set; }

        public int? FIR_MST_SRNO { get; set; }
    }
}