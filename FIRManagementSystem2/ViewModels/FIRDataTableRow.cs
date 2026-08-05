using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FIRManagementSystem.ViewModels
{
    public class FIRDataTableRow
    {
        public int firId { get; set; }
        public string firNo { get; set; }
        public string date { get; set; }
        public string desc { get; set; }
        public string complainant { get; set; }
        public string accused { get; set; }
        public string io { get; set; }
        public string status { get; set; }
        public string location { get; set; }
        public string policeStation { get; set; }
        public string sections { get; set; }
    }
}