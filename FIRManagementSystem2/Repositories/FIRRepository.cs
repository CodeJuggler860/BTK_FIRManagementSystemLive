using FIRManagementSystem.DataAccess;
using FIRManagementSystem.Models;
using FIRManagementSystem.ViewModels;
using FIRManagementSystem2.ViewModels;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FIRManagementSystem.Repositories
{
    public class FIRRepository
    {
        public List<FIRListViewModel> GetAllFIRs()
        {
            var list = new List<FIRListViewModel>();
            try
            {
                using (var conn = OracleDbHelper.GetConnection())
                {
                    string sql = @"
                    SELECT m.SRNO, m.FIR_NO, m.FIR_DATE, m.FIR_DESCRP, m.FIR_STATUS,
                    m.FIR_COMPLAINT, m.FIR_ACCUSED,
                    m.FIR_LOCATION, m.POLICESTATION, m.SECTIONS,
                    d.INVESTIGATIONOFFICER AS IO_NAME
                    FROM FIR_MST m
                    LEFT JOIN (
                    SELECT FIRMSTSRNO, INVESTIGATIONOFFICER,
                    ROW_NUMBER() OVER (PARTITION BY FIRMSTSRNO ORDER BY HEARINGDATE DESC NULLS LAST) AS rn
                    FROM FIR_DTL
                                ) d ON m.SRNO = d.FIRMSTSRNO AND d.rn = 1
                    WHERE m.IS_DELETED = 0
                    ORDER BY m.SRNO DESC";

                    using (var cmd = new OracleCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new FIRListViewModel
                            {
                                Srno = Convert.ToInt32(reader["SRNO"]),
                                FirNo = reader["FIR_NO"]?.ToString(),
                                FirDate = reader["FIR_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["FIR_DATE"]) : (DateTime?)null,
                                Description = reader["FIR_DESCRP"]?.ToString(),
                                Status = reader["FIR_STATUS"]?.ToString(),
                                Complainant = reader["FIR_COMPLAINT"]?.ToString(),
                                Accused = reader["FIR_ACCUSED"]?.ToString(),
                                Location = reader["FIR_LOCATION"]?.ToString(),
                                PoliceStation = reader["POLICESTATION"]?.ToString(),
                                Sections = reader["SECTIONS"]?.ToString(),
                                IoName = reader["IO_NAME"]?.ToString() ?? ""
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                list.Add(new FIRListViewModel
                {
                    Srno = 0,
                    FirNo = "ERROR",
                    Description = ex.ToString(),
                    Status = "ERROR",
                    Complainant = "",
                    Accused = "",
                    IoName = ""
                });
            }
            return list;
        }
        public int InsertFirMst(FirMstDtlViewModel model, string createdBy, string createdIp, int? caseId = null)
        {
            using (var conn = OracleDbHelper.GetConnection())
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    // 1. Insert FIR_MST
                    string sqlMst = @"
                INSERT INTO FIR_MST (FIR_NO, FIR_DATE, FIR_COMPLAINT, FIR_ACCUSED, FIR_DESCRP,
                                     FIR_STATUS, FIR_LOCATION, POLICESTATION, SECTIONS,
                                     IS_DELETED, CREATED_BY, CREATED_AT, CREATED_IP)
                VALUES (:firNo, :firDate, :compl, :acc, :descrp,
                        :status, :location, :policeStation, :sections,
                        0, :createdBy, SYSDATE, :createdIp)
                RETURNING SRNO INTO :srno";

                    using (var cmd = new OracleCommand(sqlMst, conn))
                    {
                        cmd.Parameters.Add("firNo", OracleDbType.Varchar2).Value = model.FirNo;
                        cmd.Parameters.Add("firDate", OracleDbType.Date).Value = model.FirDate ?? (object)DBNull.Value;
                        cmd.Parameters.Add("compl", OracleDbType.Varchar2).Value = model.Complainant;
                        cmd.Parameters.Add("acc", OracleDbType.Varchar2).Value = model.Accused;
                        cmd.Parameters.Add("descrp", OracleDbType.Varchar2).Value = model.Description;
                        cmd.Parameters.Add("status", OracleDbType.Varchar2).Value = model.Status;
                        cmd.Parameters.Add("location", OracleDbType.Varchar2).Value = model.Location ?? (object)DBNull.Value;
                        cmd.Parameters.Add("policeStation", OracleDbType.Varchar2).Value = model.PoliceStation ?? (object)DBNull.Value;
                        cmd.Parameters.Add("sections", OracleDbType.Varchar2).Value = model.Sections ?? (object)DBNull.Value;
                        cmd.Parameters.Add("createdBy", OracleDbType.Varchar2).Value = createdBy;
                        cmd.Parameters.Add("createdIp", OracleDbType.Varchar2).Value = createdIp ?? (object)DBNull.Value;

                        OracleParameter srnoParam = new OracleParameter("srno", OracleDbType.Int32);
                        srnoParam.Direction = System.Data.ParameterDirection.Output;
                        cmd.Parameters.Add(srnoParam);

                        cmd.ExecuteNonQuery();

                        if (srnoParam.Value == DBNull.Value || srnoParam.Value == null)
                            throw new Exception("Failed to retrieve generated SRNO.");

                        int mstSrno = ((Oracle.ManagedDataAccess.Types.OracleDecimal)srnoParam.Value).ToInt32();

                        // 2. Insert FIR_DTL
                        string sqlDtl = @"
                    INSERT INTO FIR_DTL (FIRMSTSRNO, INVESTIGATIONOFFICER)
                    VALUES (:mstSrno, :io)";
                        using (var cmdDtl = new OracleCommand(sqlDtl, conn))
                        {
                            cmdDtl.Parameters.Add("mstSrno", OracleDbType.Int32).Value = mstSrno;
                            cmdDtl.Parameters.Add("io", OracleDbType.Varchar2).Value = model.InvestigationOfficer ?? (object)DBNull.Value;
                            cmdDtl.ExecuteNonQuery();
                        }

                        // 3. If linked to a case, update cross-references
                        if (caseId.HasValue)
                        {
                            string sqlFirUpdate = "UPDATE FIR_MST SET CASE_ID = :caseId WHERE SRNO = :srno";
                            using (var cmdFirUpd = new OracleCommand(sqlFirUpdate, conn))
                            {
                                cmdFirUpd.Parameters.Add("caseId", OracleDbType.Int32).Value = caseId.Value;
                                cmdFirUpd.Parameters.Add("srno", OracleDbType.Int32).Value = mstSrno;
                                cmdFirUpd.ExecuteNonQuery();
                            }

                            string sqlCase = @"UPDATE CASE_MST 
                                       SET FIR_REGISTERED = 'Y', FIR_MST_SRNO = :firSrno 
                                       WHERE CASE_ID = :caseId";
                            using (var cmdCase = new OracleCommand(sqlCase, conn))
                            {
                                cmdCase.Parameters.Add("firSrno", OracleDbType.Int32).Value = mstSrno;
                                cmdCase.Parameters.Add("caseId", OracleDbType.Int32).Value = caseId.Value;
                                cmdCase.ExecuteNonQuery();
                            }
                        }

                        tran.Commit();
                        return mstSrno;
                    }
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
        }
        public void UpdateFirMst(FirMstDtlViewModel model, string modifiedBy, string modifiedIp)
        {
            using (var conn = OracleDbHelper.GetConnection())
            using (var tran = conn.BeginTransaction())
            {
                try
                {
                    // Ensure Srno is a valid integer
                    int srno = Convert.ToInt32(model.Srno.Value);

                    // Update FIR_MST
                    string sqlMst = @"
                UPDATE FIR_MST
                SET FIR_NO = :firNo,
                    FIR_DATE = :firDate,
                    FIR_COMPLAINT = :compl,
                    FIR_ACCUSED = :acc,
                    FIR_DESCRP = :descrp,
                    FIR_STATUS = :status,
                    FIR_LOCATION = :location,
                    POLICESTATION = :policeStation,
                    SECTIONS = :sections,
                    MODIFIED_BY = :modifiedBy,
                    MODIFIED_AT = SYSDATE,
                    MODIFIED_IP = :modifiedIp
                WHERE SRNO = :srno";

                    using (var cmd = new OracleCommand(sqlMst, conn))
                    {
                        cmd.Parameters.Add("firNo", OracleDbType.Varchar2).Value = model.FirNo;
                        cmd.Parameters.Add("firDate", OracleDbType.Date).Value = model.FirDate ?? (object)DBNull.Value;
                        cmd.Parameters.Add("compl", OracleDbType.Varchar2).Value = model.Complainant;
                        cmd.Parameters.Add("acc", OracleDbType.Varchar2).Value = model.Accused;
                        cmd.Parameters.Add("descrp", OracleDbType.Varchar2).Value = model.Description;
                        cmd.Parameters.Add("status", OracleDbType.Varchar2).Value = model.Status;
                        cmd.Parameters.Add("location", OracleDbType.Varchar2).Value = model.Location ?? (object)DBNull.Value;
                        cmd.Parameters.Add("policeStation", OracleDbType.Varchar2).Value = model.PoliceStation ?? (object)DBNull.Value;
                        cmd.Parameters.Add("sections", OracleDbType.Varchar2).Value = model.Sections ?? (object)DBNull.Value;
                        cmd.Parameters.Add("modifiedBy", OracleDbType.Varchar2).Value = modifiedBy;
                        cmd.Parameters.Add("modifiedIp", OracleDbType.Varchar2).Value = modifiedIp ?? (object)DBNull.Value;
                        cmd.Parameters.Add("srno", OracleDbType.Int32).Value = srno;

                        cmd.ExecuteNonQuery();
                    }

                    // Update FIR_DTL – only the IO name for the first detail row
                    string sqlDtl = @"
                UPDATE FIR_DTL
                SET INVESTIGATIONOFFICER = :io
                WHERE SRNO = (
                    SELECT MIN(SRNO) FROM FIR_DTL WHERE FIRMSTSRNO = :mstSrno
                )";

                    using (var cmd = new OracleCommand(sqlDtl, conn))
                    {
                        cmd.Parameters.Add("io", OracleDbType.Varchar2).Value = model.InvestigationOfficer ?? (object)DBNull.Value;
                        cmd.Parameters.Add("mstSrno", OracleDbType.Int32).Value = srno;
                        cmd.ExecuteNonQuery();
                    }

                    tran.Commit();
                }
                catch (OracleException ex)
                {
                    tran.Rollback();
                    // Add extra info for debugging
                    throw new Exception($"Update failed for SRNO={model.Srno}. Oracle error: {ex.Message}", ex);
                }
                catch
                {
                    tran.Rollback();
                    throw;
                }
            }
        }
        public void DeleteFir(int mstSrno)
        {
            using (var conn = OracleDbHelper.GetConnection())
            using (var cmd = new OracleCommand("UPDATE FIR_MST SET IS_DELETED = 1 WHERE SRNO = :srno", conn))
            {
                cmd.Parameters.Add("srno", OracleDbType.Int32).Value = mstSrno;
                cmd.ExecuteNonQuery();
            }
        }
        // Insert a new document record
        public int InsertDocument(int mstSrno, string docType, string fileName, string fileExt,
            string filePath, string mimeType, int fileSizeKb, string description)
        {
            using (var conn = OracleDbHelper.GetConnection())
            {
                int docId;
                string sql = @"
            INSERT INTO FIR_DOCUMENT (FIR_ID, DOC_TYPE, FILE_NAME, FILE_EXT, DOC_PATH,
                                      MIME_TYPE, FILE_SIZE_KB, DESCRIPTION, UPLOADED_BY)
            VALUES (:firId, :docType, :fileName, :fileExt, :filePath,
                    :mimeType, :fileSizeKb, :description, 1)
            RETURNING DOC_ID INTO :docId";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add("firId", OracleDbType.Int32).Value = mstSrno;
                    cmd.Parameters.Add("docType", OracleDbType.Varchar2).Value = docType;
                    cmd.Parameters.Add("fileName", OracleDbType.Varchar2).Value = fileName;
                    cmd.Parameters.Add("fileExt", OracleDbType.Varchar2).Value = fileExt ?? (object)DBNull.Value;
                    cmd.Parameters.Add("filePath", OracleDbType.Varchar2).Value = filePath;
                    cmd.Parameters.Add("mimeType", OracleDbType.Varchar2).Value = mimeType ?? (object)DBNull.Value;
                    cmd.Parameters.Add("fileSizeKb", OracleDbType.Int32).Value = fileSizeKb;
                    cmd.Parameters.Add("description", OracleDbType.Varchar2).Value =
                        string.IsNullOrEmpty(description) ? (object)DBNull.Value : description;
                    cmd.Parameters.Add("docId", OracleDbType.Int32).Direction = System.Data.ParameterDirection.Output;
                    cmd.ExecuteNonQuery();
                    docId = ((Oracle.ManagedDataAccess.Types.OracleDecimal)cmd.Parameters["docId"].Value).ToInt32();
                }
                return docId;
            }
        }

        // Get all documents for a FIR
        public List<FirDocument> GetDocumentsByFirId(int mstSrno)
        {
            var list = new List<FirDocument>();
            using (var conn = OracleDbHelper.GetConnection())
            using (var cmd = new OracleCommand(
                "SELECT d.DOC_ID, d.FIR_ID, d.DOC_TYPE, d.FILE_NAME, d.FILE_EXT, d.DOC_PATH, " +
                "d.MIME_TYPE, d.FILE_SIZE_KB, d.DESCRIPTION, d.UPLOADED_AT, d.UPLOADED_BY " +
                "FROM FIR_DOCUMENT d INNER JOIN FIR_MST m ON d.FIR_ID = m.SRNO " +
                "WHERE d.FIR_ID = :firId AND m.IS_DELETED = 0 " +
                "ORDER BY d.UPLOADED_AT DESC", conn))
            {
                cmd.Parameters.Add("firId", OracleDbType.Int32).Value = mstSrno;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new FirDocument
                        {
                            DocId = Convert.ToInt32(reader["DOC_ID"]),
                            FirId = Convert.ToInt32(reader["FIR_ID"]),
                            DocType = reader["DOC_TYPE"].ToString(),
                            FileName = reader["FILE_NAME"].ToString(),
                            FileExt = reader["FILE_EXT"]?.ToString(),
                            DocPath = reader["DOC_PATH"].ToString(),
                            MimeType = reader["MIME_TYPE"]?.ToString(),
                            FileSizeKb = reader["FILE_SIZE_KB"] != DBNull.Value ? Convert.ToInt32(reader["FILE_SIZE_KB"]) : (int?)null,
                            Description = reader["DESCRIPTION"]?.ToString(),
                            UploadedAt = reader["UPLOADED_AT"] != DBNull.Value ? Convert.ToDateTime(reader["UPLOADED_AT"]) : (DateTime?)null,
                            UploadedBy = Convert.ToInt32(reader["UPLOADED_BY"])
                        });
                    }
                }
            }
            return list;
        }

        // Get a single document (for download)
        public FirDocument GetDocumentById(int docId)
        {
            using (var conn = OracleDbHelper.GetConnection())
            using (var cmd = new OracleCommand(
                "SELECT DOC_ID, FIR_ID, DOC_TYPE, FILE_NAME, FILE_EXT, DOC_PATH, MIME_TYPE, " +
                "FILE_SIZE_KB, DESCRIPTION, UPLOADED_AT, UPLOADED_BY " +
                "FROM FIR_DOCUMENT WHERE DOC_ID = :docId", conn))
            {
                cmd.Parameters.Add("docId", OracleDbType.Int32).Value = docId;
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new FirDocument
                        {
                            DocId = Convert.ToInt32(reader["DOC_ID"]),
                            FirId = Convert.ToInt32(reader["FIR_ID"]),
                            DocType = reader["DOC_TYPE"].ToString(),
                            FileName = reader["FILE_NAME"].ToString(),
                            FileExt = reader["FILE_EXT"]?.ToString(),
                            DocPath = reader["DOC_PATH"].ToString(),
                            MimeType = reader["MIME_TYPE"]?.ToString(),
                            FileSizeKb = reader["FILE_SIZE_KB"] != DBNull.Value ? Convert.ToInt32(reader["FILE_SIZE_KB"]) : (int?)null,
                            Description = reader["DESCRIPTION"]?.ToString(),
                            UploadedAt = reader["UPLOADED_AT"] != DBNull.Value ? Convert.ToDateTime(reader["UPLOADED_AT"]) : (DateTime?)null,
                            UploadedBy = Convert.ToInt32(reader["UPLOADED_BY"])
                        };
                    }
                    return null;
                }
            }
        }

        // Get single FIR by SRNO
        public FirCopyViewModel GetFirBySrno(int srno)
        {
            using (var conn = OracleDbHelper.GetConnection())
            {
                string sql = @"
            SELECT m.SRNO, m.FIR_NO, m.FIR_DATE, m.FIR_DESCRP, m.FIR_STATUS,
                   m.FIR_COMPLAINT, m.FIR_ACCUSED, m.FIR_LOCATION,
                   m.POLICESTATION, m.SECTIONS,
                   d.INVESTIGATIONOFFICER AS IO_NAME
            FROM FIR_MST m
            LEFT JOIN (
                SELECT FIRMSTSRNO, INVESTIGATIONOFFICER,
                       ROW_NUMBER() OVER (PARTITION BY FIRMSTSRNO ORDER BY HEARINGDATE DESC NULLS LAST) AS rn
                FROM FIR_DTL
            ) d ON m.SRNO = d.FIRMSTSRNO AND d.rn = 1
            WHERE m.SRNO = :srno AND m.IS_DELETED = 0";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add("srno", OracleDbType.Int32).Value = srno;
                    using (var reader = cmd.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return new FirCopyViewModel
                            {
                                Srno = srno,
                                FirNo = reader["FIR_NO"].ToString(),
                                FirDate = reader["FIR_DATE"] != DBNull.Value ? Convert.ToDateTime(reader["FIR_DATE"]) : (DateTime?)null,
                                Status = reader["FIR_STATUS"].ToString(),
                                Description = reader["FIR_DESCRP"]?.ToString(),
                                Complainant = reader["FIR_COMPLAINT"].ToString(),
                                Accused = reader["FIR_ACCUSED"].ToString(),
                                InvestigatingOfficer = reader["IO_NAME"]?.ToString() ?? "",
                                Location = reader["FIR_LOCATION"]?.ToString(),
                                PoliceStation = reader["POLICESTATION"]?.ToString(),
                                Sections = reader["SECTIONS"]?.ToString()
                            };
                        }
                        return null;
                    }
                }
            }
        }

        // Get remarks for a FIR (ordered ascending by creation time)
        public List<RemarkViewModel> GetRemarksByFirSrno(int srno)
        {
            var list = new List<RemarkViewModel>();
            using (var conn = OracleDbHelper.GetConnection())
            using (var cmd = new OracleCommand(
                "SELECT r.REMARK_ID, r.REMARKS, r.CREATED_AT, u.FULL_NAME, u.ROLE " +
                "FROM FIR_REMARKS r JOIN APP_USER u ON r.USER_ID = u.USER_ID " +
                "WHERE r.FIRMSTSRNO = :srno ORDER BY r.CREATED_AT ASC", conn))
            {
                cmd.Parameters.Add("srno", OracleDbType.Int32).Value = srno;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        list.Add(new RemarkViewModel
                        {
                            Id = Convert.ToInt32(reader["REMARK_ID"]),
                            Body = reader["REMARKS"].ToString(),
                            AuthorName = reader["FULL_NAME"].ToString(),
                            AuthorRole = reader["ROLE"].ToString(),
                            CreatedAt = Convert.ToDateTime(reader["CREATED_AT"])
                        });
                    }
                }
            }
            return list;
        }

        // Insert a new remark
        public int InsertRemark(int srno, int userId, string body)
        {
            using (var conn = OracleDbHelper.GetConnection())
            {
                int remarkId;
                string sql = @"
            INSERT INTO FIR_REMARKS (FIRMSTSRNO, USER_ID, REMARKS)
            VALUES (:srno, :userId, :body)
            RETURNING REMARK_ID INTO :id";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add("srno", OracleDbType.Int32).Value = srno;
                    cmd.Parameters.Add("userId", OracleDbType.Int32).Value = userId;
                    cmd.Parameters.Add("body", OracleDbType.Varchar2).Value = body;
                    cmd.Parameters.Add("id", OracleDbType.Int32).Direction = System.Data.ParameterDirection.Output;
                    cmd.ExecuteNonQuery();
                    remarkId = ((Oracle.ManagedDataAccess.Types.OracleDecimal)cmd.Parameters["id"].Value).ToInt32();
                }
                return remarkId;
            }
        }

        // Get USER_ID of the currently logged-in user
        public int GetUserIdByUsername(string username)
        {
            using (var conn = OracleDbHelper.GetConnection())
            using (var cmd = new OracleCommand("SELECT USER_ID FROM APP_USER WHERE USERNAME = :uname AND IS_ACTIVE = 'Y'", conn))
            {
                cmd.Parameters.Add("uname", OracleDbType.Varchar2).Value = username;
                object result = cmd.ExecuteScalar();
                if (result != null && result != DBNull.Value)
                    return Convert.ToInt32(result);
                throw new Exception("User not found or inactive.");
            }
        }
    }
    
}