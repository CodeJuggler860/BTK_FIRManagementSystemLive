using FIRManagementSystem.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FIRManagementSystem2.ViewModels
{
    public class FirCopyViewModel
    {
        public int Srno { get; set; }
        public string FirNo { get; set; }
        public DateTime? FirDate { get; set; }
        public string Status { get; set; }
        public string Description { get; set; }
        public string Complainant { get; set; }
        public string Accused { get; set; }
        public string InvestigatingOfficer { get; set; }
        public string Location { get; set; }
        public string PoliceStation { get; set; }
        public string Sections { get; set; }

        public List<RemarkViewModel> Remarks { get; set; } = new List<RemarkViewModel>();
        public List<FirDocument> Documents { get; set; } = new List<FirDocument>();
    }

    public class RemarkViewModel
    {
        public int Id { get; set; }
        public string AuthorName { get; set; }
        public string AuthorRole { get; set; }
        public string Body { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class AddRemarkRequest
    {
        public int Srno { get; set; }
        public string Body { get; set; }
    }
}