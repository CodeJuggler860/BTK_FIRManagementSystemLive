using FIRManagementSystem.DataAccess;
using FIRManagementSystem2.Models;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace FIRManagementSystem2.Repositories
{
    public class CaseRepository
    {
        public List<CASE_MST> GetAllCases()
        {
            var list = new List<CASE_MST>();
            try
            {
                using (var conn = OracleDbHelper.GetConnection())
                {
                    string sql = @"
                SELECT CASE_ID, CASE_DESCRIPTION, COMPLAINANT_NAME, ACCUSED_NAME,
                       CASE_STATUS, FIR_REGISTERED, LOCATION,
                       FIR_MST_SRNO,   -- ← ADD THIS
                       CREATED_BY, CREATED_AT, CREATED_IP,
                       MODIFIED_BY, MODIFIED_AT, MODIFIED_IP
                FROM CASE_MST WHERE IS_DELETED = 0 ORDER BY CASE_ID DESC";

                    using (var cmd = new OracleCommand(sql, conn))
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            list.Add(new CASE_MST
                            {
                                CASE_ID = Convert.ToInt32(reader["CASE_ID"]),
                                CASE_DESCRIPTION = reader["CASE_DESCRIPTION"]?.ToString(),
                                COMPLAINANT_NAME = reader["COMPLAINANT_NAME"]?.ToString(),
                                ACCUSED_NAME = reader["ACCUSED_NAME"]?.ToString(),
                                CASE_STATUS = reader["CASE_STATUS"]?.ToString(),
                                FIR_REGISTERED = reader["FIR_REGISTERED"]?.ToString(),
                                LOCATION = reader["LOCATION"]?.ToString(),
                                FIR_MST_SRNO = reader["FIR_MST_SRNO"] != DBNull.Value ? Convert.ToInt32(reader["FIR_MST_SRNO"]) : (int?)null,   // ← ADD THIS
                                CREATED_BY = reader["CREATED_BY"]?.ToString(),
                                CREATED_AT = reader["CREATED_AT"] != DBNull.Value ? Convert.ToDateTime(reader["CREATED_AT"]) : (DateTime?)null,
                                CREATED_IP = reader["CREATED_IP"]?.ToString(),
                                MODIFIED_BY = reader["MODIFIED_BY"]?.ToString(),
                                MODIFIED_AT = reader["MODIFIED_AT"] != DBNull.Value ? Convert.ToDateTime(reader["MODIFIED_AT"]) : (DateTime?)null,
                                MODIFIED_IP = reader["MODIFIED_IP"]?.ToString()
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                list.Add(new CASE_MST
                {
                    CASE_ID = 0,
                    CASE_DESCRIPTION = ex.ToString(),
                    CASE_STATUS = "ERROR"
                });
            }
            return list;
        }

        // Insert a new case
        public int InsertCase(CASE_MST model, string createdBy, string createdIp)
        {
            using (var conn = OracleDbHelper.GetConnection())
            {
                int caseId;
                string sql = @"
            INSERT INTO CASE_MST (CASE_DESCRIPTION, COMPLAINANT_NAME, ACCUSED_NAME,
                                  CASE_STATUS, FIR_REGISTERED, LOCATION,
                                  CREATED_BY, CREATED_IP)
            VALUES (:caseDesc, :compl, :acc, :caseStatus, 'N', :location,
                    :createdBy, :createdIp)
            RETURNING CASE_ID INTO :caseId";

                using (var cmd = new OracleCommand(sql, conn))
                {
                    cmd.Parameters.Add("caseDesc", OracleDbType.Varchar2).Value = model.CASE_DESCRIPTION;
                    cmd.Parameters.Add("compl", OracleDbType.Varchar2).Value = model.COMPLAINANT_NAME;
                    cmd.Parameters.Add("acc", OracleDbType.Varchar2).Value = model.ACCUSED_NAME ?? (object)DBNull.Value;
                    cmd.Parameters.Add("caseStatus", OracleDbType.Varchar2).Value = model.CASE_STATUS ?? "OPEN";
                    cmd.Parameters.Add("location", OracleDbType.Varchar2).Value = model.LOCATION ?? (object)DBNull.Value;
                    cmd.Parameters.Add("createdBy", OracleDbType.Varchar2).Value = createdBy;
                    cmd.Parameters.Add("createdIp", OracleDbType.Varchar2).Value = createdIp ?? (object)DBNull.Value;
                    cmd.Parameters.Add("caseId", OracleDbType.Int32).Direction = System.Data.ParameterDirection.Output;
                    cmd.ExecuteNonQuery();
                    caseId = ((Oracle.ManagedDataAccess.Types.OracleDecimal)cmd.Parameters["caseId"].Value).ToInt32();
                }
                return caseId;
            }
        }
        public CASE_MST GetCaseById(int caseId)
        {
            using (var conn = OracleDbHelper.GetConnection())
            using (var cmd = new OracleCommand(
                "SELECT CASE_ID, CASE_DESCRIPTION, COMPLAINANT_NAME, ACCUSED_NAME, " +
                "CASE_STATUS, FIR_REGISTERED, LOCATION, " +
                "CREATED_BY, CREATED_AT, CREATED_IP, " +
                "MODIFIED_BY, MODIFIED_AT, MODIFIED_IP " +
                "FROM CASE_MST WHERE CASE_ID = :caseId AND IS_DELETED = 0", conn))
            {
                cmd.Parameters.Add("caseId", OracleDbType.Int32).Value = caseId;
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new CASE_MST
                        {
                            CASE_ID = Convert.ToInt32(reader["CASE_ID"]),
                            CASE_DESCRIPTION = reader["CASE_DESCRIPTION"]?.ToString(),
                            COMPLAINANT_NAME = reader["COMPLAINANT_NAME"]?.ToString(),
                            ACCUSED_NAME = reader["ACCUSED_NAME"]?.ToString(),
                            CASE_STATUS = reader["CASE_STATUS"]?.ToString(),
                            FIR_REGISTERED = reader["FIR_REGISTERED"]?.ToString(),
                            LOCATION = reader["LOCATION"]?.ToString(),   // ← make sure this line exists
                            CREATED_BY = reader["CREATED_BY"]?.ToString(),
                            CREATED_AT = reader["CREATED_AT"] != DBNull.Value ? Convert.ToDateTime(reader["CREATED_AT"]) : (DateTime?)null,
                            CREATED_IP = reader["CREATED_IP"]?.ToString(),
                            MODIFIED_BY = reader["MODIFIED_BY"]?.ToString(),
                            MODIFIED_AT = reader["MODIFIED_AT"] != DBNull.Value ? Convert.ToDateTime(reader["MODIFIED_AT"]) : (DateTime?)null,
                            MODIFIED_IP = reader["MODIFIED_IP"]?.ToString()
                        };
                    }
                    return null;
                }
            }
        }

        public void UpdateCase(CASE_MST model, string modifiedBy, string modifiedIp)
        {
            using (var conn = OracleDbHelper.GetConnection())
            using (var cmd = new OracleCommand(
                "UPDATE CASE_MST SET CASE_DESCRIPTION = :caseDesc, COMPLAINANT_NAME = :compl, " +
                "ACCUSED_NAME = :acc, CASE_STATUS = :status, LOCATION = :location, " +
                "MODIFIED_BY = :modifiedBy, MODIFIED_AT = SYSDATE, MODIFIED_IP = :modifiedIp " +
                "WHERE CASE_ID = :caseId", conn))
            {
                cmd.Parameters.Add("caseDesc", OracleDbType.Varchar2).Value = model.CASE_DESCRIPTION;
                cmd.Parameters.Add("compl", OracleDbType.Varchar2).Value = model.COMPLAINANT_NAME;
                cmd.Parameters.Add("acc", OracleDbType.Varchar2).Value = model.ACCUSED_NAME ?? (object)DBNull.Value;
                cmd.Parameters.Add("status", OracleDbType.Varchar2).Value = model.CASE_STATUS;
                cmd.Parameters.Add("location", OracleDbType.Varchar2).Value = model.LOCATION ?? (object)DBNull.Value;
                cmd.Parameters.Add("modifiedBy", OracleDbType.Varchar2).Value = modifiedBy;
                cmd.Parameters.Add("modifiedIp", OracleDbType.Varchar2).Value = modifiedIp ?? (object)DBNull.Value;
                cmd.Parameters.Add("caseId", OracleDbType.Int32).Value = model.CASE_ID;
                cmd.ExecuteNonQuery();
            }
        }

        public void DeleteCase(int caseId)
        {
            using (var conn = OracleDbHelper.GetConnection())
            using (var cmd = new OracleCommand("UPDATE CASE_MST SET IS_DELETED = 1 WHERE CASE_ID = :caseId", conn))
            {
                cmd.Parameters.Add("caseId", OracleDbType.Int32).Value = caseId;
                cmd.ExecuteNonQuery();
            }
        }
    }
}