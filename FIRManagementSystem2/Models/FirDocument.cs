using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FIRManagementSystem.Models
{
    public class FirDocument
    {
        public int DocId { get; set; }
        public int FirId { get; set; }
        public string DocType { get; set; }
        public string FileName { get; set; }
        public string FileExt { get; set; }
        public string DocPath { get; set; }
        public string MimeType { get; set; }
        public int? FileSizeKb { get; set; }
        public string Description { get; set; }
        public DateTime? UploadedAt { get; set; }
        public int UploadedBy { get; set; }
    }
}