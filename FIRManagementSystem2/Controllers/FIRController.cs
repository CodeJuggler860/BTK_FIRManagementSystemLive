using FIRManagementSystem.DataAccess;
using FIRManagementSystem.Repositories;
using FIRManagementSystem.ViewModels;
using FIRManagementSystem2.Helpers;
using FIRManagementSystem2.Models;
using FIRManagementSystem2.Repositories;
using FIRManagementSystem2.ViewModels;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace FIRManagementSystem.Controllers
{
    [Authorize]
    public class FIRController : Controller
    {
        private CaseRepository _caseRepo = new CaseRepository();
        private FIRRepository _firRepo = new FIRRepository();

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult TestLayout()
        {
            return View();
        }

        public ActionResult CheckConnection()
        {
            try
            {
                using (var conn = OracleDbHelper.GetConnection())
                {
                    string user = "", dbName = "";
                    using (var cmd = new OracleCommand("SELECT USER, SYS_CONTEXT('USERENV','DB_NAME') FROM DUAL", conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            user = reader.GetString(0);
                            dbName = reader.GetString(1);
                        }
                    }
                    int count = 0;
                    using (var cmd = new OracleCommand("SELECT COUNT(*) FROM FIR_MST", conn))
                    {
                        count = Convert.ToInt32(cmd.ExecuteScalar());
                    }
                    return Content($"Connected to DB: {dbName}, User: {user}, FIR_MST count: {count}");
                }
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.Message);
            }
        }

        // ── Dashboard list ──
        public JsonResult GetFIRList()
        {
            var firs = _firRepo.GetAllFIRs();
            var rows = firs.Select(f => new FIRDataTableRow
            {
                firId = f.Srno,
                firNo = f.FirNo,
                date = f.FirDate?.ToString("yyyy-MM-dd"),
                desc = f.Description,
                complainant = f.Complainant,
                accused = f.Accused,
                io = f.IoName,
                status = f.Status,
                location = f.Location,
                policeStation = f.PoliceStation,
                sections = f.Sections
            }).ToList();

            // --- Current and previous periods ---
            DateTime today = DateTime.Today;
            DateTime currentMonthStart = new DateTime(today.Year, today.Month, 1);
            DateTime lastMonthStart = currentMonthStart.AddMonths(-1);
            DateTime lastMonthEnd = currentMonthStart.AddDays(-1);

            int ThisMonthCount(DateTime? dt) => dt != null && dt.Value >= currentMonthStart ? 1 : 0;
            int LastMonthCount(DateTime? dt) => dt != null && dt.Value >= lastMonthStart && dt.Value <= lastMonthEnd ? 1 : 0;
            int TodayCount(DateTime? dt) => dt?.Date == today ? 1 : 0;
            int YesterdayCount(DateTime? dt) => dt?.Date == today.AddDays(-1) ? 1 : 0;

            var stats = new
            {
                total = rows.Count,
                resolved = rows.Count(r => r.status == "RESOLVED"),
                underTrial = rows.Count(r => r.status == "UNDER_TRIAL"),
                dormant = rows.Count(r => r.status == "DORMANT"),

                // delta comparisons (previous month / yesterday) – unchanged logic, just use new status codes
                totalThisMonth = rows.Sum(r => ThisMonthCount(DateTime.Parse(r.date))),
                totalLastMonth = rows.Sum(r => LastMonthCount(DateTime.Parse(r.date))),
                resolvedThisMonth = rows.Where(r => r.status == "RESOLVED").Sum(r => ThisMonthCount(DateTime.Parse(r.date))),
                resolvedLastMonth = rows.Where(r => r.status == "RESOLVED").Sum(r => LastMonthCount(DateTime.Parse(r.date))),
                underTrialToday = rows.Where(r => r.status == "UNDER_TRIAL").Sum(r => TodayCount(DateTime.Parse(r.date))),
                underTrialYesterday = rows.Where(r => r.status == "UNDER_TRIAL").Sum(r => YesterdayCount(DateTime.Parse(r.date))),
                dormantThisMonth = rows.Where(r => r.status == "DORMANT").Sum(r => ThisMonthCount(DateTime.Parse(r.date))),
                dormantLastMonth = rows.Where(r => r.status == "DORMANT").Sum(r => LastMonthCount(DateTime.Parse(r.date)))
            };

            return Json(new { data = rows, stats = stats }, JsonRequestBehavior.AllowGet);
        }

        private string GetClientIP()
        {
            // Check common forwarded headers
            string[] headers = { "HTTP_X_FORWARDED_FOR", "X-Forwarded-For", "X-Forwarded-For-Client" };
            foreach (var header in headers)
            {
                string forwarded = Request.ServerVariables[header] ?? Request.Headers[header];
                if (!string.IsNullOrEmpty(forwarded))
                {
                    return forwarded.Split(',')[0].Trim();
                }
            }
            return Request.UserHostAddress;
        }
        //Controller for create or edit
        [HttpPost]
        public ActionResult CreateOrEdit(FirMstDtlViewModel model, int? caseId = null, HttpPostedFileBase firAttachment = null)
        {
            try
            {
                string ip = GetClientIP();
                string currentUser = User.Identity.Name;

                int firSrno;
                if (model.Srno.HasValue)
                {
                    _firRepo.UpdateFirMst(model, currentUser, ip);
                    firSrno = model.Srno.Value;
                }
                else
                {
                    firSrno = _firRepo.InsertFirMst(model, currentUser, ip, caseId);
                }

                // Handle attachment if a file was uploaded
                if (firAttachment != null && firAttachment.ContentLength > 0)
                {
                    var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv" };
                    string extension = Path.GetExtension(firAttachment.FileName).ToLower();
                    if (!allowedExtensions.Contains(extension))
                    {
                        return Content("File type not allowed.");
                    }
                    else if (firAttachment.ContentLength <= 10 * 1024 * 1024)
                    {
                        string root = AttachmentPathHelper.GetAttachmentRoot(Server);
                        string folder = Path.Combine(root, "FIR", firSrno.ToString());
                        if (!Directory.Exists(folder))
                            Directory.CreateDirectory(folder);

                        string uniqueName = Guid.NewGuid().ToString() + extension;
                        string fullPath = Path.Combine(folder, uniqueName);
                        firAttachment.SaveAs(fullPath);

                        int sizeKb = (int)Math.Ceiling(firAttachment.ContentLength / 1024.0);
                        string relativePath = AttachmentPathHelper.BuildRelativePath(firSrno, uniqueName);
                        _firRepo.InsertDocument(firSrno, "OTHER", firAttachment.FileName,
                            extension.TrimStart('.'), relativePath, firAttachment.ContentType, sizeKb, "Attached during FIR creation");
                    }
                }

                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.ToString());
            }
        }

        // ── Delete ──
        [HttpPost]
        public ActionResult Delete(int id)
        {
            try
            {
                _firRepo.DeleteFir(id);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.Message);
            }
        }

        // ── Dropdown for IO (names only, no IDs) ──
        public JsonResult GetIOList()
        {
            var ioList = new List<object>();
            using (var conn = OracleDbHelper.GetConnection())
            using (var cmd = new OracleCommand("SELECT FULL_NAME FROM INVESTIGATING_OFFICER WHERE IS_ACTIVE = 'Y' ORDER BY FULL_NAME", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    ioList.Add(new { name = reader["FULL_NAME"].ToString() });
                }
            }
            return Json(ioList, JsonRequestBehavior.AllowGet);
        }

        // ── Status options (unchanged) ──
        public JsonResult GetStatusOptions()
        {
            var statuses = new List<object>();
            using (var conn = OracleDbHelper.GetConnection())
            using (var cmd = new OracleCommand("SELECT CODE_VALUE, CODE_LABEL FROM LOOKUP_CODE WHERE CODE_TYPE = 'FIR_STATUS' ORDER BY SORT_ORDER", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    statuses.Add(new { value = reader["CODE_VALUE"].ToString(), label = reader["CODE_LABEL"].ToString() });
                }
            }
            return Json(statuses, JsonRequestBehavior.AllowGet);
        }

        // ── Document Upload ── (parameter name changed to "mstSrno", but still received as "firId" from JS)
        [HttpPost]
        public JsonResult UploadDocument(int firId, string docType, string description)
        {
            try
            {
                if (Request.Files.Count == 0)
                    return Json(new { success = false, message = "No file selected." });

                var file = Request.Files[0];
                if (file == null || file.ContentLength == 0)
                    return Json(new { success = false, message = "Empty file." });

                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".csv" };
                string extension = Path.GetExtension(file.FileName).ToLower();
                if (!allowedExtensions.Contains(extension))
                    return Json(new { success = false, message = "File type not allowed." });

                if (file.ContentLength > 10 * 1024 * 1024)
                    return Json(new { success = false, message = "File too large (max 10 MB)." });

                // Local project folder (same as CreateOrEdit)
                string root = AttachmentPathHelper.GetAttachmentRoot(Server);
                string folder = Path.Combine(root, "FIR", firId.ToString());
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                string uniqueName = Guid.NewGuid().ToString() + extension;
                string fullPath = Path.Combine(folder, uniqueName);
                file.SaveAs(fullPath);

                int sizeKb = (int)Math.Ceiling(file.ContentLength / 1024.0);
                string relativePath = AttachmentPathHelper.BuildRelativePath(firId, uniqueName);
                int docId = _firRepo.InsertDocument(firId, docType, file.FileName,
                    extension.TrimStart('.'), relativePath, file.ContentType, sizeKb, description);

                return Json(new { success = true, docId = docId });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        public JsonResult GetDocuments(int firId)
        {
            try
            {
                var docs = _firRepo.GetDocumentsByFirId(firId);
                var result = docs.Select(d => new
                {
                    docId = d.DocId,
                    fileName = d.FileName,
                    docType = d.DocType,
                    description = d.Description,
                    fileSizeKb = d.FileSizeKb,
                    uploadedAt = d.UploadedAt,
                    downloadUrl = Url.Action("DownloadDocument", new { id = d.DocId })
                }).ToList();
                return Json(result, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public JsonResult GetDocTypes()

        {
            var list = new List<object>();
            using (var conn = OracleDbHelper.GetConnection())
            using (var cmd = new OracleCommand("SELECT CODE_VALUE, CODE_LABEL FROM LOOKUP_CODE WHERE CODE_TYPE = 'DOC_TYPE' ORDER BY SORT_ORDER", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                    list.Add(new { value = reader["CODE_VALUE"].ToString(), label = reader["CODE_LABEL"].ToString() });
            }
            return Json(list, JsonRequestBehavior.AllowGet);
        }
        // GET: /FIR/Details/5
        public ActionResult Details(int id)
        {
            var fir = _firRepo.GetFirBySrno(id);
            if (fir == null)
                return HttpNotFound();

            fir.Remarks = _firRepo.GetRemarksByFirSrno(id);
            fir.Documents = _firRepo.GetDocumentsByFirId(id);  // existing method

            return View("Copy", fir);   // looks for Views/FIR/Copy.cshtml
        }

        // POST: /FIR/AddRemark
        [HttpPost]
        public JsonResult AddRemark(AddRemarkRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Body))
                return Json(new { success = false, message = "Remark cannot be empty." });

            try
            {
                string currentUser = User.Identity.Name;  // "admin"
                int userId = _firRepo.GetUserIdByUsername(currentUser);

                int remarkId = _firRepo.InsertRemark(req.Srno, userId, req.Body);

                // Fetch the newly inserted remark's author details
                var remarks = _firRepo.GetRemarksByFirSrno(req.Srno);
                var newRemark = remarks.LastOrDefault(); // because we order ASC, the last one is the newest

                return Json(new
                {
                    success = true,
                    id = remarkId,
                    authorName = newRemark?.AuthorName ?? currentUser,
                    authorRole = newRemark?.AuthorRole ?? "Officer",
                    body = req.Body,
                    createdAt = DateTime.Now.ToString("o")  // ISO 8601 for JS
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
        public ActionResult DownloadDocument(int id, bool preview = false)
        {
            var doc = _firRepo.GetDocumentById(id);
            if (doc == null) return HttpNotFound();

            string filePath = AttachmentPathHelper.ResolveFullPath(Server, doc.DocPath);
            if (filePath == null)
                return HttpNotFound();

            byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
            string mimeType = doc.MimeType ?? "application/octet-stream";

            var cd = new System.Net.Mime.ContentDisposition
            {
                FileName = doc.FileName,
                Inline = preview
            };
            Response.AddHeader("Content-Disposition", cd.ToString());

            return File(fileBytes, mimeType);
        }
        public JsonResult GetCaseList()
        {
            var cases = _caseRepo.GetAllCases();
            var rows = cases.Select(c => new
            {
                caseId = c.CASE_ID,
                description = c.CASE_DESCRIPTION,
                complainant = c.COMPLAINANT_NAME,
                accused = c.ACCUSED_NAME,
                status = c.CASE_STATUS,
                firRegistered = c.FIR_REGISTERED == "Y" ? "Yes" : "No",
                firSrno = c.FIR_MST_SRNO,   
                createdAt = c.CREATED_AT?.ToString("yyyy-MM-dd")
            }).ToList();

            var stats = new
            {
                total = rows.Count,
                open = rows.Count(r => r.status == "OPEN"),
                forwarded = rows.Count(r => r.firRegistered == "Yes"),
                closed = rows.Count(r => r.status == "CLOSED"),
                underInvestigation = rows.Count(r => r.status == "Under Investigation")
            };

            return Json(new { data = rows, stats = stats }, JsonRequestBehavior.AllowGet);
        }

        // ── Create case (POST) ──
        [HttpPost]
        public ActionResult CreateCase(CASE_MST model)
        {
            try
            {
                string ip = GetClientIP();
                string currentUser = User.Identity.Name ?? "SYSTEM";
                _caseRepo.InsertCase(model, currentUser, ip);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.Message);
            }
        }
        // GET: /FIR/GetCaseById/5
        public JsonResult GetCaseById(int id)
        {
            var caseItem = _caseRepo.GetCaseById(id);
            if (caseItem == null)
                return Json(new { success = false, message = "Case not found." }, JsonRequestBehavior.AllowGet);
            return Json(new
            {
                success = true,
                caseId = caseItem.CASE_ID,
                description = caseItem.CASE_DESCRIPTION,
                complainant = caseItem.COMPLAINANT_NAME,
                accused = caseItem.ACCUSED_NAME,
                status = caseItem.CASE_STATUS,
                firRegistered = caseItem.FIR_REGISTERED,
                location = caseItem.LOCATION   
            }, JsonRequestBehavior.AllowGet);
        }

        // POST: /FIR/EditCase
        [HttpPost]
        public ActionResult EditCase(CASE_MST model)
        {
            try
            {
                string ip = GetClientIP();
                string currentUser = User.Identity.Name ?? "SYSTEM";
                _caseRepo.UpdateCase(model, currentUser, ip);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.Message);
            }
        }

        // POST: /FIR/DeleteCase
        [HttpPost]
        public ActionResult DeleteCase(int id)
        {
            try
            {
                _caseRepo.DeleteCase(id);
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.Message);
            }
        }
        public JsonResult GetCaseForFir(int caseId)
        {
            var caseItem = _caseRepo.GetCaseById(caseId);
            if (caseItem == null)
                return Json(new { success = false }, JsonRequestBehavior.AllowGet);
            return Json(new
            {
                success = true,
                complainant = caseItem.COMPLAINANT_NAME,
                accused = caseItem.ACCUSED_NAME,
                description = caseItem.CASE_DESCRIPTION,
                location = caseItem.LOCATION
            }, JsonRequestBehavior.AllowGet);
        }
    }
}