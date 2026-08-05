using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FIRManagementSystem.Models
{
    public class AuditLog
    {
        public int AuditId { get; set; }
        public string TableName { get; set; }
        public int RecordId { get; set; }
        public string Action { get; set; }
        public string FieldName { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public int ChangedBy { get; set; }
        public DateTime? ChangedAt { get; set; }
        public string Remarks { get; set; }
    }
}